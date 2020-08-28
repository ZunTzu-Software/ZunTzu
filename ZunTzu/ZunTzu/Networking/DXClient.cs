// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.DirectX.DirectPlay;
using Microsoft.DirectX.DirectPlay.Voice;
using Microsoft.DirectX.DirectSound;
using ZunTzu.AudioVideo;
using ZunTzu.VideoCompression;

namespace ZunTzu.Networking {

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public sealed class DXClient : IClient {

		/// <summary>Constructor.</summary>
		/// <param name="voiceFocusWindow">Window that will be used to determine focus for sound playback.</param>
		public DXClient(Form voiceFocusWindow, AudioManager audioManager) {
			this.voiceFocusWindow = voiceFocusWindow;
			this.audioManager = audioManager;
			audioManager.AudioPropertiesChanged += new AudioPropertiesChangedHandler(onAudioPropertiesChanged);

			// test audio setup
			if(System.Environment.OSVersion.Version.Major < 6) {	// not Vista?
				try {
					using(Test test = new Test()) {
						try {
							test.CheckAudioSetup(DSoundHelper.DefaultPlaybackDevice, DSoundHelper.DefaultCaptureDevice, TestFlags.QueryOnly);
						} catch(Microsoft.DirectX.DirectXException) {
							test.CheckAudioSetup(DSoundHelper.DefaultPlaybackDevice, DSoundHelper.DefaultCaptureDevice, 0 /*TestFlags.AllowBack*/);
						}
					}
					voiceSetupOk = true;
				} catch(Exception) { }
			}
		}

		/// <summary>Voice connection status.</summary>
		public VoiceStatus VoiceStatus { get { return voiceStatus; } }

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
			soundBuffers = new Dictionary<int, Buffer3D>();

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
			voiceStatus = VoiceStatus.Disconnected;
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
				if(voiceClient != null) {
					if(!voiceClient.Disposed)
						voiceClient.Dispose();
					voiceClient = null;
				}
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

		/// <summary>Settings for voice recording and playback.</summary>
		private void onAudioPropertiesChanged() {
			if(voiceClient != null) {
				updateClientConfig();
				voiceClient.ClientConfig = clientConfig;
			}
		}

		/// <summary>Indicates that this player is transmitting a voice communication.</summary>
		public bool IsRecording { get { return isRecording; } }

		/// <summary>Peak level across the current recording frame.</summary>
		/// <remarks>
		/// A frame corresponds to approximately 1/10 second of audio stream. The current frame typically
		/// lags 50-200 ms behind real-time. This value can range from 0 through 99, with 0 being completely
		/// silent and 99 being the highest possible input level.
		/// </remarks>
		public int VoiceInputLevel { get { return voiceInputLevel; } }

		/// <summary>Current voice recording volume.</summary>
		/// <remarks>The value can range from -10,000 to 0. This member is available even when automatic gain control is active.</remarks>
		public int VoiceGainLevel { get { return voiceGainLevel; } }

		/// <summary>Voice activation level that caused the transmission to begin.</summary>
		public int VoiceActivationThresholdLevel { get { return audioManager.AudioProperties.ActivationThreshold; } }

		/// <summary>Indicates if the mandatory audio setup has been performed on this computer.</summary>
		public bool VoiceSetupOk { get { return voiceSetupOk; } }

		/// <summary>The IP address of this computer as seen from the Internet.</summary>
		//public string PublicIpAddress { get { return publicIpAddress; } }

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
						string.Format(" Voice messages timed out: {0}%.", (100 * info.MessagesTimedOutNormalPriority) / (1 + info.MessagesTimedOutNormalPriority + info.MessagesTransmittedNormalPriority)),
						string.Format(" Video messages timed out: {0}%.", (100 * info.MessagesTimedOutLowPriority) / (1 + info.MessagesTimedOutLowPriority + info.MessagesTransmittedLowPriority)),
						string.Format(" Game messages queued: {0} ({0} bytes).", highPrioritySendQueueMessageCount, highPrioritySendQueueByteCount),
						string.Format(" Voice messages queued: {0} ({0} bytes).", normalPrioritySendQueueMessageCount, normalPrioritySendQueueByteCount),
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

					if(voiceSetupOk) {
						// start voice client
						voiceClient = new Microsoft.DirectX.DirectPlay.Voice.Client(client,
							new int[] { // message mask
							0x0006, // RecordStarted
							0x0007, // RecordStopped
							0x0008,	// ConnectResult
							0x000A // InputLevel
						});

						// Add event handlers
						voiceClient.ConnectResult += new ConnectResultEventHandler(onVoiceConnectResult);
						voiceClient.PlayerStarted += new PlayerStartedEventHandler(onPlayerStarted);	// never raised, but necessary to allow for the calling of onRecordStarted
						voiceClient.PlayerStopped += new PlayerStoppedEventHandler(onPlayerStopped);	// never raised, but necessary to allow for the calling of onRecordStopped
						voiceClient.RecordStarted += new RecordStartedEventHandler(onRecordStarted);
						voiceClient.RecordStopped += new RecordStoppedEventHandler(onRecordStopped);
						voiceClient.InputLevel += new InputLevelEventHandler(onInputLevel);

						SoundDeviceConfig soundConfig = new SoundDeviceConfig();
						soundConfig.Flags = (audioManager.AudioProperties.DisableAutoconfiguration ? 0 : SoundConfigFlags.AutoSelect); /*SoundConfigFlags.HalfDuplex*/
						if(audioManager.DirectSoundDevice != null)
							soundConfig.PlaybackDevice = audioManager.DirectSoundDevice;
						soundConfig.GuidPlaybackDevice = DSoundHelper.DefaultPlaybackDevice;
						soundConfig.GuidCaptureDevice = DSoundHelper.DefaultCaptureDevice;
						soundConfig.Window = voiceFocusWindow;
						soundConfig.MainBufferPriority = 0;

						updateClientConfig();

						voiceStatus = VoiceStatus.Connecting;

						// Connect to the voice session
						try {
							voiceClient.Connect(soundConfig, clientConfig, 0 /*VoiceFlags.Sync*/);
						} catch(TransportNoPlayerException) {
						} catch(Exception exception) {
							voiceStatus = VoiceStatus.Disconnected;
							VoiceFailureCause cause =
								(exception is CompressionNotSupportedException ? VoiceFailureCause.CompressionNotSupported :
								(exception is IncompatibleVersionException ? VoiceFailureCause.IncompatibleVersion :
								(exception is NoVoiceSessionException ? VoiceFailureCause.NoVoiceSession :
								(exception is RunSetupException ? VoiceFailureCause.RunSetup :
								(exception is SoundInitializeFailureException ? VoiceFailureCause.SoundInitFailure :
								(exception is Microsoft.DirectX.DirectPlay.Voice.TimedOutException ? VoiceFailureCause.TimeOut :
								VoiceFailureCause.Other))))));
							NetworkMessage errorMessage = new NetworkMessage(0, (byte) ReservedMessageType.VoiceConnectionFailed, new byte[1] { (byte) cause });
							lock(networkMessages) {
								networkMessages.Enqueue(errorMessage);
							}
							return;
						}
					} else {
						NetworkMessage errorMessage = new NetworkMessage(0, (byte) ReservedMessageType.VoiceConnectionFailed, new byte[1] { (byte) VoiceFailureCause.RunSetup });
						lock(networkMessages) {
							networkMessages.Enqueue(errorMessage);
						}
					}

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

		private void onVoiceConnectResult(object sender, ConnectResultEventArgs e) {
			if(e.Message.Result == 0) {
				voiceStatus = VoiceStatus.Connected;
				// Set DirectPlay to send voice messages to all players
				voiceClient.TransmitTargets = new int[] { (int) Microsoft.DirectX.DirectPlay.PlayerID.AllPlayers };
			} else {
				voiceStatus = VoiceStatus.Disconnected;

				VoiceFailureCause cause;
				switch(e.Message.Result) {
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.CompressionNotSupported: cause = VoiceFailureCause.CompressionNotSupported; break;
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.IncompatibleVersion: cause = VoiceFailureCause.IncompatibleVersion; break;
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.NoVoiceSession: cause = VoiceFailureCause.NoVoiceSession; break;
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.RunSetup: cause = VoiceFailureCause.RunSetup; break;
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.SoundInitFailure: cause = VoiceFailureCause.SoundInitFailure; break;
					case Microsoft.DirectX.DirectPlay.Voice.ResultCode.TimeOut: cause = VoiceFailureCause.TimeOut; break;
					default: cause = VoiceFailureCause.Other; break;
				}

				NetworkMessage message = new NetworkMessage(0, (byte) ReservedMessageType.VoiceConnectionFailed, new byte[1] { (byte) cause });
				lock(networkMessages) {
					networkMessages.Enqueue(message);
				}
			}
		}

		private void updateClientConfig() {
			AudioProperties voiceProperties = audioManager.AudioProperties;
			clientConfig.Flags =
				(voiceProperties.DisableAutomaticGainControl ? 0 : ClientConfigFlags.AutoRecordVolume) |
				(voiceProperties.ActivateEchoSuppression ? ClientConfigFlags.EchoSuppression : 0) |
				(voiceProperties.MuteAll ? ClientConfigFlags.MuteGlobal : 0) |
				(voiceProperties.MuteRecording ? ClientConfigFlags.RecordMute : 0) |
				(voiceProperties.MutePlayback ? ClientConfigFlags.PlaybackMute : 0) |
				(voiceProperties.UseVoiceActivation ? (voiceProperties.AdjustActivationThresholdAutomatically ? ClientConfigFlags.AutoVoiceActivated : ClientConfigFlags.ManualVoiceActivated) : 0);
			clientConfig.RecordVolume = (voiceProperties.DisableAutomaticGainControl ? voiceProperties.MicrophoneInputLevel : (int) RecordVolume.Last);
			clientConfig.PlaybackVolume = (int) PlaybackVolume.Default;
			clientConfig.Threshold = (voiceProperties.UseVoiceActivation && !voiceProperties.AdjustActivationThresholdAutomatically ?
				(Threshold)voiceProperties.ActivationThreshold :
				Threshold.Unused);
			//clientConfig.BufferQuality = BufferQuality.Min;
			//clientConfig.BufferAggressiveness = BufferAggressiveness.Max;
			clientConfig.BufferQuality = (voiceProperties.UseAutomaticJitterControl ?
				BufferQuality.Default :
				(BufferQuality)voiceProperties.JitterControl);
			clientConfig.BufferAggressiveness = BufferAggressiveness.Default;
			clientConfig.NotifyPeriod = Math.Max(100, (int)NotifyPeriod.Minimum);
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
				} else if(messageType == (byte) ReservedMessageType.VoicePlaybackStarted) {
					if(voiceStatus == VoiceStatus.Connected && !soundBuffers.ContainsKey(senderId)) {
						// create a 3D sound buffer?
						lock(soundBuffers) {
							if(!soundBuffers.ContainsKey(senderId)) {
								// create a 3D sound buffer
								Buffer3D buffer = voiceClient.Create3DSoundBuffer(senderId);
								soundBuffers.Add(senderId, buffer);
								buffer.MinDistance = 2.0f;

								// adjust the origin of each voice
								adjustSoundOrigin();
							}
						}
					}
				} else if(messageType == (byte) ReservedMessageType.PlayerHasLeft) {
					if(voiceStatus == VoiceStatus.Connected && soundBuffers.ContainsKey(senderId)) {
						// deallocate a 3D sound buffer?
						lock(soundBuffers) {
							Buffer3D buffer;
							if(soundBuffers.TryGetValue(senderId, out buffer)) {
								soundBuffers.Remove(senderId);
								voiceClient.Delete3DSoundBuffer(senderId, buffer);

								// adjust the origin of each voice
								adjustSoundOrigin();
							}
						}
					}
				}
				e.Message.ReceiveData.Dispose();
				// add the message to the message list
				NetworkMessage message = new NetworkMessage(senderId, messageType, dataCopy);
				lock(networkMessages) {
					networkMessages.Enqueue(message);
				}
			}
		}

		/// <summary>Adjust the origin in 3D of each voice.</summary>
		/// <remarks>Acquire a lock on soundBuffers before calling this method.</remarks>
		private void adjustSoundOrigin() {
			int i = 0;
			foreach(Buffer3D buffer in soundBuffers.Values) {
				float originAngle = (float) Math.PI * (i * 2 + 1) / (soundBuffers.Count * 2);
				buffer.Position = new Microsoft.DirectX.Vector3((float) -Math.Cos(originAngle) * 1.0f, 0.0f, (float) Math.Sin(originAngle) * 2.0f);
				++i;
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
			//lock(inboundVideoFrameHistories) {
			//	if(inboundVideoFrameHistories.ContainsKey(senderId))
			//		inboundVideoFrameHistories.Remove(senderId);
			//}
		}

		private void handleVideoPlaybackToggledMessage(int senderId, byte[] messageData) {
		}

		private void onSessionTerminated(object sender, SessionTerminatedEventArgs e) {
			//Disconnect();
			status = NetworkStatus.Disconnected;
			client = null;
			voiceClient = null;

			// add the new message to the message list
			NetworkMessage message = new NetworkMessage(0, (byte)ReservedMessageType.HostDisconnected, null);
			lock(networkMessages) {
				networkMessages.Enqueue(message);
			}
		}

		private void onRecordStarted(object sender, RecordStartedEventArgs e) {
			isRecording = true;
			voiceActivationThresholdLevel = e.Message.PeakLevel;
			using(NetworkPacket packet = new NetworkPacket(1)) {
				packet.Write((byte)ReservedMessageType.VoicePlaybackStarted);
				client.Send(packet, 1500, SendFlags.NoComplete | SendFlags.Coalesce);
			}
			// add the new message to the message list
			NetworkMessage message = new NetworkMessage(0, (byte)ReservedMessageType.VoiceRecordingStarted, null);
			lock(networkMessages) {
				networkMessages.Enqueue(message);
			}
		}

		private void onRecordStopped(object sender, RecordStoppedEventArgs e) {
			isRecording = false;
			using(NetworkPacket packet = new NetworkPacket(1)) {
				packet.Write((byte)ReservedMessageType.VoicePlaybackStopped);
				client.Send(packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce);
			}
			// add the new message to the message list
			NetworkMessage message = new NetworkMessage(0, (byte)ReservedMessageType.VoiceRecordingStopped, null);
			lock(networkMessages) {
				networkMessages.Enqueue(message);
			}
		}

		private void onPlayerStarted(object sender, PlayerStartedEventArgs e) {
			// never called, but necessary to allow for the calling of onRecordStarted
		}

		private void onPlayerStopped(object sender, PlayerStoppedEventArgs e) {
			// never called, but necessary to allow for the calling of onRecordStopped
		}

		private void onInputLevel(object sender, InputLevelEventArgs e) {
			voiceInputLevel = e.Message.PeakLevel;
			voiceGainLevel = e.Message.RecordVolume;
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

		/*
		private void onTestRequestSuccessful(IAsyncResult asyncResult) {
			try {
				HttpWebRequest request = (HttpWebRequest) asyncResult.AsyncState;
				using(HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(asyncResult)) {
					if(response.StatusCode == HttpStatusCode.OK) {
						using(Stream stream = response.GetResponseStream()) {
							using(StreamReader reader = new StreamReader(stream)) {
								string responseContent = reader.ReadToEnd();
								Regex ipAddressRegex = new Regex(@"^(?<1>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Singleline);
								Match ipAddressMatch = ipAddressRegex.Match(responseContent);
								if(ipAddressMatch.Success)
									publicIpAddress = ipAddressMatch.Groups[1].Value;
							}
						}
					}
				}
			} catch(Exception) {}
		}

		private void onTestRequestCompleted(object state, bool timedOut) {
			if(timedOut) {
				HttpWebRequest request = state as HttpWebRequest;
				if(request != null) {
					request.Abort();
				}
			}
		}
		 */

		private readonly Form voiceFocusWindow;
		private AudioManager audioManager;
		private ClientConfig clientConfig = new ClientConfig();
		private Microsoft.DirectX.DirectPlay.Client client = null;
		private Microsoft.DirectX.DirectPlay.Voice.Client voiceClient = null;
		private volatile bool isRecording = false;
		private volatile int voiceInputLevel = 0;
		private volatile int voiceGainLevel = 0;
		private volatile int voiceActivationThresholdLevel = 0;
		private volatile NetworkStatus status = NetworkStatus.Disconnected;
		private volatile VoiceStatus voiceStatus = VoiceStatus.Disconnected;
		private volatile int playerId = 0;
		private Queue<NetworkMessage> networkMessages = new Queue<NetworkMessage>();	// watch out for thread safety
		private System.Threading.Timer timeoutTimer = null;
		private bool voiceSetupOk = false;
		//private string publicIpAddress = null;
        private IVideoCodec videoCodec = new ZtcVideoCodec();
		private OutboundVideoFrameHistory outboundVideoFrameHistory = null;
		private Dictionary<int, InboundVideoFrameHistory> inboundVideoFrameHistories = null;
		private Dictionary<int, Buffer3D> soundBuffers = null;
		private bool serverIsOnSameComputer = false;
	}
}
