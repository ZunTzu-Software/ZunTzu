// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.DirectX.DirectPlay;
using Microsoft.DirectX.DirectSound;
using ZunTzu.AudioVideo;
using ZunTzu.VideoCompression;

namespace ZunTzu.Networking {

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public sealed class DXClient : IClient {

		/// <summary>Network connection status.</summary>
		public NetworkStatus Status { get { return status; } }

		/// <summary>Connect to a server.</summary>
		/// <param name="serverName">IP address or hostname of the server.</param>
		/// <param name="serverPort">IP port on which the server is listening.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		public void Connect(string serverName, int serverPort) {
			Debug.Assert(status == NetworkStatus.Disconnected);

			serverIsOnSameComputer = (serverName == "localhost");
			outboundVideoFrameHistory = new OutboundVideoFrameHistory();
			inboundVideoFrameHistories = new Dictionary<int,InboundVideoFrameHistory>();

			client = new Microsoft.DirectX.DirectPlay.Client(InitializeFlags.DisableParameterValidation);
			client.ConnectComplete += new ConnectCompleteEventHandler(onConnectComplete);
			client.Receive += new ReceiveEventHandler(onReceive);
			client.SessionTerminated += new SessionTerminatedEventHandler(onSessionTerminated);

			status = NetworkStatus.Connecting;

			// trigger NAT traversal
			EnabledAddresses enabledAddresses = NatResolver.TestNatTraversal(serverName, serverPort);

			ApplicationDescription description = new ApplicationDescription();
			description.GuidApplication = new Guid("{920BAF09-A06C-47d8-BCE0-21B30D0C3586}");
			// try first using the host's public address
			using(Address hostAddress = (enabledAddresses == null ? new Address(serverName, serverPort) : new Address(enabledAddresses.HostPublicAddress, enabledAddresses.HostPublicPort))) {
				hostAddress.ServiceProvider = Address.ServiceProviderTcpIp;
				using(Address device = new Address()) {
					device.ServiceProvider = Address.ServiceProviderTcpIp;
					device.AddComponent(Address.KeyTraversalMode, Address.TraversalModeNone);
					if(enabledAddresses != null)
						device.AddComponent(Address.KeyPort, enabledAddresses.ClientPrivatePort);
					using(NetworkPacket packet = new NetworkPacket()) {
						try {
							client.Connect(description, hostAddress, device, packet, 0);
						} catch(Exception e) {
							status = NetworkStatus.Disconnected;
							ConnectionFailureCause cause =
								(e is NoConnectionException ? ConnectionFailureCause.NoConnection :
								(e is NotHostException ? ConnectionFailureCause.NotHost :
								(e is SessionFullException ? ConnectionFailureCause.SessionFull :
								ConnectionFailureCause.Other)));

							// try again using the host's private address
							if(enabledAddresses != null) {
								using(Address hostPrivateAddress = new Address(enabledAddresses.HostPrivateAddress, enabledAddresses.HostPrivatePort)) {
									try {
										client.Connect(description, hostAddress, device, packet, 0);
									} catch {
										NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.ConnectionFailed, new byte[1] { (byte) cause });
										lock(networkMessages) {
											networkMessages.Enqueue(message);
										}
										return;
									}
								}
							} else {
								NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.ConnectionFailed, new byte[1] { (byte) cause });
								lock(networkMessages) {
									networkMessages.Enqueue(message);
								}
								return;
							}
						}
					}
				}
			}

			// launch a timer to monitor timeout
			timeoutTimer = new System.Threading.Timer(onTimeout, client, 4000, 0);
		}

		/// <summary>Disconnect from server.</summary>
		public void Disconnect() {
			Debug.Assert(status != NetworkStatus.Disconnected);
			status = NetworkStatus.Disconnected;
			Dispose();
		}

		/// <summary>Network id of this player.</summary>
		public int PlayerId { get { return playerId; } }

		/// <summary>Leave game.</summary>
		public void Dispose() {
			if(client != null) {
				client.ConnectComplete -= new ConnectCompleteEventHandler(onConnectComplete);
				client.Receive -= new ReceiveEventHandler(onReceive);
				client.SessionTerminated -= new SessionTerminatedEventHandler(onSessionTerminated);
				if(!client.Disposed)
					client.Dispose();
				client = null;
			}
		}

		/// <summary>Send a message.</summary>
		/// <param name="messageType">Byte code that indicates the type of message.</param>
		/// <param name="messageData">Message content.</param>
		public void Send(byte messageType, byte[] messageData) {
			Debug.Assert((messageType & 0xC0) != 0x00);
			if(status == NetworkStatus.Connected) {
				using(NetworkPacket packet = new NetworkPacket(messageData.Length + 1)) {
					packet.Write(messageType);
					if(messageData.Length > 0)
						packet.Write(messageData);
					client.Send(packet, ((messageType & 0xC0) == 0xC0 ? 1000 : 0),
						((messageType & 0xC0) == 0xC0 ?
						SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityHigh :
						SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh));
				}
			} else {
				// Player is disconnected: loopback so that he receives the messages he sent
				if(messageType < (byte)0xC0) {	// is message reliable
					NetworkMessage message = new NetworkMessage(playerId, messageType, messageData);
					lock(networkMessages) {
						networkMessages.Enqueue(message);
					}
				}
			}
		}

		/// <summary>Send a message to a single client.</summary>
		/// <param name="recipientId">Player that will receive the message.</param>
		/// <param name="messageType">Byte code that indicates the type of message.</param>
		/// <param name="messageData">Message content.</param>
		/// <remarks>Use this method only with messages from host (zero in bits 0-1).</remarks>
		public void Send(int recipientId, byte messageType, byte[] messageData) {
			Debug.Assert((messageType & 0xC0) == 0x00);
			if(recipientId == playerId) {
				// loopback so that he receives the message
				NetworkMessage message = new NetworkMessage(playerId, messageType, messageData);
				lock(networkMessages) {
					networkMessages.Enqueue(message);
				}
			} else if(status == NetworkStatus.Connected) {
				using(NetworkPacket packet = new NetworkPacket(messageData.Length + 5)) {
					packet.Write(messageType);
					packet.Write(recipientId);
					if(messageData.Length > 0)
						packet.Write(messageData);
					client.Send(packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh);
				}
			}
		}

		/// <summary>Send a video frame to all other players.</summary>
		/// <param name="frameBuffer">A 64x64 R8G8B8 frame.</param>
		public void SendVideoFrame(byte[] frameBuffer) {
			if(status == NetworkStatus.Connected) {
				byte oldestFrameId;
				byte[] oldestFrameData;
				lock(outboundVideoFrameHistory) {
					oldestFrameData = outboundVideoFrameHistory.OldestFrameData;
					oldestFrameId = (oldestFrameData != null ? outboundVideoFrameHistory.OldestFrameId : (byte) 0);
				}
				byte[] encodedData;
				if(serverIsOnSameComputer) {
					// plenty of bandwidth -> no compression to save some CPU
					encodedData = frameBuffer;
				} else {
					// compression
					unsafe {
						fixed(byte* frameBufferPtr = frameBuffer) {
							byte* compressedBuffer = stackalloc byte[4096 * 3];
							int byteCount;
							if(oldestFrameData == null) {
								// no reference frame
								videoCodec.Encode((IntPtr) frameBufferPtr, (IntPtr) compressedBuffer, out byteCount);
							} else {
								// reference frame
								fixed(byte* referenceFramePtr = oldestFrameData) {
									videoCodec.Encode((IntPtr) referenceFramePtr, (IntPtr) frameBufferPtr, (IntPtr) compressedBuffer, out byteCount);
								}
							}
							encodedData = new byte[byteCount];
							for(int i = 0; i < encodedData.Length; ++i)
								encodedData[i] = compressedBuffer[i];
						}
					}
				}
				byte frameId;
				lock(outboundVideoFrameHistory) {
					frameId = outboundVideoFrameHistory.AddFrame(frameBuffer);
				}
				using(NetworkPacket packet = new NetworkPacket(encodedData.Length + 3)) {
					packet.Write((byte) ReservedMessageType.VideoFrame);
					packet.Write((byte) frameId);
					packet.Write((byte) (oldestFrameData != null ? oldestFrameId : frameId));
					packet.Write(encodedData);
					// use a timeout equal to the capture period (15 times per second)
					client.Send(packet, 1000 / 15, SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityLow);
				}
			}
		}

		/// <summary>Get all pending messages received.</summary>
		/// <returns>An array of NetworkMessage instances.</returns>
		public NetworkMessage[] RetrieveNetworkMessages() {
			NetworkMessage[] shallowCopy;
			lock(networkMessages) {
				shallowCopy = networkMessages.ToArray();
				networkMessages.Clear();
			}
			for(int i = shallowCopy.Length - 1; i >= 0; --i) {
				NetworkMessage message = shallowCopy[i];
				if(message.Type == (byte) ReservedMessageType.VideoFrameReceived && message.Data != null) {
					byte frameId = message.Data[5];

					// send video frame reception notification
					if(status == NetworkStatus.Connected) {
						using(NetworkPacket packet = new NetworkPacket(6)) {
							packet.Write((byte) ReservedMessageType.VideoFrameAck);
							packet.Write((int) message.SenderId);
							packet.Write((byte) frameId);
							// use a timeout equal to 10 times the capture period (15 times per second)
							client.Send(packet, 10 * 1000 / 15, SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityHigh);
						}
					}

					// ignore earlier frames from same sender
					for(int j = 0; j < i; ++j) {
						NetworkMessage earlierMessage = shallowCopy[j];
						if(earlierMessage.Type == (byte) ReservedMessageType.VideoFrameReceived && earlierMessage.SenderId == message.SenderId)
							shallowCopy[j] = new NetworkMessage(earlierMessage.SenderId, earlierMessage.Type, null);
					}
				}
			}
			for(int i = 0; i < shallowCopy.Length; ++i) {
				NetworkMessage message = shallowCopy[i];
				if(message.Type == (byte) ReservedMessageType.VideoFrameReceived && message.Data != null) {
					byte frameId = message.Data[5];
					byte referenceFrameId = message.Data[6];

					InboundVideoFrameHistory history;
					if(!inboundVideoFrameHistories.TryGetValue(message.SenderId, out history)) {
						history = new InboundVideoFrameHistory();
						inboundVideoFrameHistories.Add(message.SenderId, history);
					}
					byte[] frame = new byte[64 * 64 * 3];
					if(serverIsOnSameComputer) {
						// no compression
						Array.Copy(message.Data, 7, frame, 0, frame.Length);
					} else {
						// uncompress video frame
						unsafe {
							fixed(byte* framePtr = frame) {
								fixed(byte* dataPtr = message.Data) {
									if(frameId == referenceFrameId) {
										// no reference frame
										videoCodec.Decode((IntPtr) (dataPtr + 7), (IntPtr) framePtr);
									} else {
										// reference frame
										fixed(byte* referenceFramePtr = history.GetFrameData(referenceFrameId)) {
											videoCodec.Decode((IntPtr) referenceFramePtr, (IntPtr) (dataPtr + 7), (IntPtr) framePtr);
										}
									}
								}
							}
						}
					}
					if(referenceFrameId != frameId)
						history.ClearHistoryUntilThisFrame(referenceFrameId);
					history.AddFrame(frameId, frame);
					shallowCopy[i] = new NetworkMessage(message.SenderId, message.Type, frame);
				}
			}
			return shallowCopy;
		}

        /// <summary>Indicates that this player is transmitting a voice communication.</summary>
        public bool IsRecording { get { return isRecording; } }

		/// <summary>Retrieves statistics for the connection between this client and the host.</summary>
		public string[] Statistics {
			get {
				if(status == NetworkStatus.Connected) {
					ConnectionInformation info = client.GetConnectionInformation();
					int highPrioritySendQueueMessageCount, normalPrioritySendQueueMessageCount, lowPrioritySendQueueMessageCount;
					int highPrioritySendQueueByteCount, normalPrioritySendQueueByteCount, lowPrioritySendQueueByteCount;
					client.GetSendQueueInformation(out highPrioritySendQueueMessageCount, out highPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityHigh);
					client.GetSendQueueInformation(out normalPrioritySendQueueMessageCount, out normalPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityNormal);
					client.GetSendQueueInformation(out lowPrioritySendQueueMessageCount, out lowPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityLow);
					return new string[] {
						string.Format(" Round trip latency: {0} ms.", info.RoundTripLatencyMs),
						string.Format(" Throughput: {0} bits/s.", 8 * info.ThroughputBps),
						string.Format(" Peak throughput: {0} bits/s.", 8 * info.PeakThroughputBps),
						string.Format(" Packets lost: {0}%.", (100 * (info.PacketsRetried + info.PacketsDropped)) / (1 + info.PacketsSentGuaranteed + info.PacketsSentNonGuaranteed)),
						string.Format(" Game messages timed out: {0}%.", (100 * info.MessagesTimedOutHighPriority) / (1 + info.MessagesTimedOutHighPriority + info.MessagesTransmittedHighPriority)),
						string.Format(" Video messages timed out: {0}%.", (100 * info.MessagesTimedOutLowPriority) / (1 + info.MessagesTimedOutLowPriority + info.MessagesTransmittedLowPriority)),
						string.Format(" Game messages queued: {0} ({0} bytes).", highPrioritySendQueueMessageCount, highPrioritySendQueueByteCount),
						string.Format(" Video messages queued: {0} ({0} bytes).", lowPrioritySendQueueMessageCount, lowPrioritySendQueueByteCount),
					};
				} else {
					return new string[] { "No statistics available (not connected)." };
				}
			}
		}

		private void onConnectComplete(object sender, ConnectCompleteEventArgs e) {
			timeoutTimer.Dispose();
			if(status == NetworkStatus.Connecting) {
				if(e.Message.ResultCode == Microsoft.DirectX.DirectPlay.ResultCode.Success) {
					playerId = e.Message.LocalPlayerId;
					status = NetworkStatus.Connected;

					// add the new message to the message list
					NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.ConnectionEstablished, null);
					lock(networkMessages) {
						networkMessages.Enqueue(message);
					}
				} else {
					status = NetworkStatus.Disconnected;

					ConnectionFailureCause cause;
					switch(e.Message.ResultCode) {
						case Microsoft.DirectX.DirectPlay.ResultCode.HostRejectedConnection: cause = ConnectionFailureCause.HostRejectedConnection; break;
						case Microsoft.DirectX.DirectPlay.ResultCode.NoConnection: cause = ConnectionFailureCause.NoConnection; break;
						case Microsoft.DirectX.DirectPlay.ResultCode.NotHost: cause = ConnectionFailureCause.NotHost; break;
						case Microsoft.DirectX.DirectPlay.ResultCode.SessionFull: cause = ConnectionFailureCause.SessionFull; break;
						default: cause = ConnectionFailureCause.Other; break;
					}

					NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.ConnectionFailed, new byte[1] { (byte) cause });
					lock(networkMessages) {
						networkMessages.Enqueue(message);
					}
				}
			}
		}

		private void onReceive(object sender, ReceiveEventArgs e) {
			byte[] messageData = e.Message.ReceiveData.GetData();
			byte messageType = messageData[0];
			int senderId = BitConverter.ToInt32(messageData, 1);
			if(messageType >= (byte) ReservedMessageType.VideoFrame) {
				// videoconferencing message
				switch(messageType) {
					case (byte) ReservedMessageType.VideoFrame:
						handleVideoFrameMessage(senderId, messageData);
						break;

					case (byte) ReservedMessageType.VideoFrameAck:
						handleVideoFrameAckMessage(senderId, messageData);
						break;

					case (byte) ReservedMessageType.VideoCaptureDisabled:
						handleVideoCaptureDisabledMessage(senderId, messageData);
						break;

					case (byte) ReservedMessageType.VideoPlaybackToggled:
						handleVideoPlaybackToggledMessage(senderId, messageData);
						break;
				}
				e.Message.ReceiveData.Dispose();
			} else {
				byte[] dataCopy = null;
				if(messageType > (byte) ReservedMessageType.VoicePlaybackStopped) {
					// application-defined message
					dataCopy = new byte[messageData.Length - 5];
					if(dataCopy.Length > 0)
						Array.Copy(messageData, 5, dataCopy, 0, dataCopy.Length);
				}
				e.Message.ReceiveData.Dispose();
				// add the message to the message list
				NetworkMessage message = new NetworkMessage(senderId, messageType, dataCopy);
				lock(networkMessages) {
					networkMessages.Enqueue(message);
				}
			}
		}

		private void handleVideoFrameMessage(int senderId, byte[] messageData) {
			// add the message to the message list
			NetworkMessage message = new NetworkMessage(senderId, (byte) ReservedMessageType.VideoFrameReceived, messageData);
			lock(networkMessages) {
				networkMessages.Enqueue(message);
			}
		}

		private void handleVideoFrameAckMessage(int senderId, byte[] messageData) {
			byte frameId = messageData[5];
			lock(outboundVideoFrameHistory) {
				outboundVideoFrameHistory.ClearHistoryUntilThisFrame(frameId);
			}
		}

		private void handleVideoCaptureDisabledMessage(int senderId, byte[] messageData) {
		}

		private void handleVideoPlaybackToggledMessage(int senderId, byte[] messageData) {
		}

		private void onSessionTerminated(object sender, SessionTerminatedEventArgs e) {
			//Disconnect();
			status = NetworkStatus.Disconnected;
			client = null;

			// add the new message to the message list
			NetworkMessage message = new NetworkMessage(0, (byte)ReservedMessageType.HostDisconnected, null);
			lock(networkMessages) {
				networkMessages.Enqueue(message);
			}
		}

		private void onTimeout(object state) {
			timeoutTimer.Dispose();
			if(status == NetworkStatus.Connecting && object.ReferenceEquals(state, client)) {
				status = NetworkStatus.Disconnected;
				Dispose();

				NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.ConnectionFailed, new byte[1] { (byte) ConnectionFailureCause.TimeOut });
				lock(networkMessages) {
					networkMessages.Enqueue(message);
				}
			}
		}

		private Microsoft.DirectX.DirectPlay.Client client = null;
		private volatile bool isRecording = false;
		private volatile NetworkStatus status = NetworkStatus.Disconnected;
		private volatile int playerId = 0;
		private Queue<NetworkMessage> networkMessages = new Queue<NetworkMessage>();	// watch out for thread safety
		private System.Threading.Timer timeoutTimer = null;
        private IVideoCodec videoCodec = new ZtcVideoCodec();
		private OutboundVideoFrameHistory outboundVideoFrameHistory = null;
		private Dictionary<int, InboundVideoFrameHistory> inboundVideoFrameHistories = null;
		private bool serverIsOnSameComputer = false;
	}
}
