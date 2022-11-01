// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Open.Nat;
using ZunTzu.VideoCompression;

namespace ZunTzu.Networking
{

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public sealed class RakClient : IClient
	{
		/// <summary>Network connection status.</summary>
		public NetworkStatus Status { get { return _status; } }

		public RakClient()
		{
			_client = Peer.Create();
		}

		/// <summary>Connect to a server.</summary>
		/// <param name="serverName">IP address or hostname of the server.</param>
		/// <param name="serverPort">IP port on which the server is listening.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		public void Connect(string serverName, int serverPort)
		{
			Debug.Assert(_status == NetworkStatus.Disconnected);
			Connect(serverName, serverPort, null);
		}

		/// <summary>Connect to a server.</summary>
		/// <param name="sessionId">ID of an existing game session.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		public void Connect(string sessionId)
		{
			Debug.Assert(_status == NetworkStatus.Disconnected);

			// NAT traversal using an external server (will not work with 'symmetric' NATs, though)
			IPAddress serverIp = null;
			int? serverPort = null;
			int? clientPort = null;
			try
			{
				using (UdpClient socket = new UdpClient())
				{
					socket.Client.ReceiveTimeout = 2000;
					socket.Connect("zuntzu.ovh", 1805);

					// Send a REQUEST_JOIN message
					byte[] sessionIdBytes = Encoding.UTF8.GetBytes(sessionId);
					byte[] datagram = new[] { (byte)'z', (byte)'t', (byte)1, sessionIdBytes[0], sessionIdBytes[1], sessionIdBytes[2], sessionIdBytes[3], sessionIdBytes[4] };
					socket.Send(datagram, datagram.Length);

					IPEndPoint localEndPoint = (IPEndPoint)socket.Client.LocalEndPoint;
					clientPort = localEndPoint.Port;

					// Wait for a response
					var endPoint = new IPEndPoint(IPAddress.Any, 0);
					byte[] response = socket.Receive(ref endPoint);

					if (response != null
						&& response[0] == 'z'
						&& response[1] == 't'
						&& response[2] == 1)
					{
						string responseSessionId = Encoding.UTF8.GetString(response, 3, 5);
						if (responseSessionId == sessionId)
						{
							serverIp = new IPAddress(new byte[] { response[8], response[9], response[10], response[11] });
							serverPort = response[12] | ((int)response[13] << 8);
						}
					}
				}
			}
			catch { }

			if (serverIp == null)
			{
				_status = NetworkStatus.Disconnected;

				var message = new NetworkMessage(new byte[] {
					(byte)MessageId.SystemMessage,
					(byte)ReservedMessageType.ConnectionFailed,
					(byte)ConnectionFailureCause.NotHost,
				});

				_networkMessages.Enqueue(message);
			}
			else
			{
				// NAT traversal using NAT-PMP or UPnP
				IPAddress natIp = null;
				try
				{
					var discoverer = new NatDiscoverer();
					var device = discoverer.DiscoverDeviceAsync().Result;
					natIp = device.GetExternalIPAsync().Result;
					device.CreatePortMapAsync(new Mapping(Protocol.Udp, clientPort.Value, clientPort.Value, 3600, "zuntzu")).Wait(3000);
				}
				catch { }

				Connect(serverIp.ToString(), serverPort.Value, clientPort.Value);
			}
		}

		/// <summary>Connect to a server.</summary>
		/// <param name="serverName">IP address or hostname of the server.</param>
		/// <param name="serverPort">IP port on which the server is listening.</param>
		/// <param name="clientPort">IP port that this client should use.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		void Connect(string serverName, int serverPort, int? clientPort)
		{
			Debug.Assert(_status == NetworkStatus.Disconnected);

			_serverIsOnSameComputer = (serverName == "localhost");
			_outboundVideoFrameHistory = new OutboundVideoFrameHistory();
			_inboundVideoFrameHistories = new Dictionary<UInt64, InboundVideoFrameHistory>();

			_status = NetworkStatus.Connecting;

			StartupResult startupResult = _client.StartupClient((ushort)(clientPort ?? 0));

			var connectionResult = ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED;
			if (startupResult == StartupResult.RAKNET_STARTED)
			{
				// try using the host's public address
				connectionResult = _client.Connect(serverName, (ushort)serverPort);
			}

			if (startupResult != StartupResult.RAKNET_STARTED
				|| connectionResult != ConnectionAttemptResult.CONNECTION_ATTEMPT_STARTED)
			{
				// failure
				_status = NetworkStatus.Disconnected;
				ConnectionFailureCause cause = (connectionResult == ConnectionAttemptResult.CANNOT_RESOLVE_DOMAIN_NAME
					? ConnectionFailureCause.NotHost
					: ConnectionFailureCause.Other);

				var message = new NetworkMessage(new byte[] {
					(byte)MessageId.SystemMessage,
					(byte)ReservedMessageType.ConnectionFailed,
					(byte)cause,
				});

				_networkMessages.Enqueue(message);
			}
		}

		/// <summary>Disconnect from server.</summary>
		public void Disconnect()
		{
			Debug.Assert(_status != NetworkStatus.Disconnected);

			_status = NetworkStatus.Disconnected;
			_client.Shutdown();
		}

		/// <summary>Network id of this player.</summary>
		public UInt64 PlayerId => _playerId;

		/// <summary>Leave game.</summary>
		public void Dispose()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		/// <summary>Send a message.</summary>
		/// <param name="messageData">Message content.</param>
		public void Send(byte[] messageData)
		{
			byte messageCode = messageData[0];
			Debug.Assert(messageCode == (byte)MessageId.ReliableMessageFromClientToAll
				|| messageCode == (byte)MessageId.ReliableMessageFromClientToHost
				|| messageCode == (byte)MessageId.UnreliableMessageFromClientToAllOthers);

			if (_status == NetworkStatus.Connected)
			{
				if (messageCode == (byte)MessageId.UnreliableMessageFromClientToAllOthers)
				{
					_client.Send(messageData,
						PacketPriority.LOW_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
						(int)OrderingChannel.Unreliable,
						_serverAddress, false);
				}
				else
				{
					_client.Send(messageData,
						PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
						(int)OrderingChannel.Reliable,
						_serverAddress, false);
				}
			}
			else
			{
				// Player is disconnected: loopback so that he receives the messages he sent
				if (messageCode != (byte)MessageId.UnreliableMessageFromClientToAllOthers)
				{   // is message reliable

					// overwrite zeroed sender ID with this player's ID
					Debug.Assert(messageData.Length >= 10);
					messageData[2] = (byte)((_playerId & 0x00000000000000ff) >> 0);
					messageData[3] = (byte)((_playerId & 0x000000000000ff00) >> 8);
					messageData[4] = (byte)((_playerId & 0x0000000000ff0000) >> 16);
					messageData[5] = (byte)((_playerId & 0x00000000ff000000) >> 24);
					messageData[6] = (byte)((_playerId & 0x000000ff00000000) >> 32);
					messageData[7] = (byte)((_playerId & 0x0000ff0000000000) >> 40);
					messageData[8] = (byte)((_playerId & 0x00ff000000000000) >> 48);
					messageData[9] = (byte)((_playerId & 0xff00000000000000) >> 56);

					var message = new NetworkMessage(messageData);
					_networkMessages.Enqueue(message);
				}
			}
		}

		/// <summary>Send a message to a single client.</summary>
		/// <param name="recipientId">Player that will receive the message.</param>
		/// <param name="messageData">Message content.</param>
		/// <remarks>Use this method only with messages from host (zero in bits 0-1).</remarks>
		public void Send(UInt64 recipientId, byte[] messageData)
		{
			Debug.Assert(messageData[0] == (byte)MessageId.ReliableMessageFromHostToSingleClient);

			// overwrite recipient ID in message
			Debug.Assert(messageData.Length >= 10);
			messageData[2] = (byte)((recipientId & 0x00000000000000ff) >> 0);
			messageData[3] = (byte)((recipientId & 0x000000000000ff00) >> 8);
			messageData[4] = (byte)((recipientId & 0x0000000000ff0000) >> 16);
			messageData[5] = (byte)((recipientId & 0x00000000ff000000) >> 24);
			messageData[6] = (byte)((recipientId & 0x000000ff00000000) >> 32);
			messageData[7] = (byte)((recipientId & 0x0000ff0000000000) >> 40);
			messageData[8] = (byte)((recipientId & 0x00ff000000000000) >> 48);
			messageData[9] = (byte)((recipientId & 0xff00000000000000) >> 56);

			if (recipientId == _playerId)
			{
				// loopback so that he receives the message
				var message = new NetworkMessage(messageData);
				_networkMessages.Enqueue(message);
			}
			else if (_status == NetworkStatus.Connected)
			{
				_client.Send(messageData,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					_serverAddress, false);
			}
		}

		/// <summary>Send a video frame to all other players.</summary>
		/// <param name="frameBuffer">A 64x64 R8G8B8 frame.</param>
		public void SendVideoFrame(byte[] frameBuffer)
		{
			if (_status != NetworkStatus.Connected) return;

			byte frameId = _outboundVideoFrameHistory.AddFrame(frameBuffer);
			byte? latestAckedFrameId = _outboundVideoFrameHistory.LatestAckedFrameId;

			byte[] data;
			if (_serverIsOnSameComputer)
			{
				// plenty of bandwidth -> no compression to save some CPU
				data = new byte[frameBuffer.Length + 3];
				data[0] = (byte)MessageId.VideoFrame;
				data[1] = frameId;
				data[2] = latestAckedFrameId ?? frameId;
				Array.Copy(frameBuffer, 0, data, 3, frameBuffer.Length);
			}
			else
			{
				// compression
				unsafe
				{
					fixed (byte* frameBufferPtr = frameBuffer)
					{
						byte* compressedBuffer = stackalloc byte[5000 * 3]; // the main thread in .NET has a fairly fixed size of 1 MB
						int byteCount;
						if (!latestAckedFrameId.HasValue)
						{
							// no reference frame
							_videoCodec.Encode((IntPtr)frameBufferPtr, (IntPtr)compressedBuffer, out byteCount);
						}
						else
						{
							// reference frame
							byte[] oldestFrameData = _outboundVideoFrameHistory.LatestAckedFrameData;
							fixed (byte* referenceFramePtr = oldestFrameData)
							{
								_videoCodec.Encode((IntPtr)referenceFramePtr, (IntPtr)frameBufferPtr, (IntPtr)compressedBuffer, out byteCount);
							}
						}

						data = new byte[byteCount + 3];
						data[0] = (byte)MessageId.VideoFrame;
						data[1] = frameId;
						data[2] = latestAckedFrameId ?? frameId;
						Marshal.Copy((IntPtr)compressedBuffer, data, 3, byteCount);
					}
				}
			}

			_client.Send(data,
				PacketPriority.LOW_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
				(int)OrderingChannel.Video,
				_serverAddress, false);
		}

		struct VideoFrame
		{
			public UInt64 SenderId;
			public Packet Packet;
		}

		/// <summary>Get all pending messages received.</summary>
		/// <returns>A list of NetworkMessage instances.</returns>
		public List<NetworkMessage> RetrieveNetworkMessages()
		{
			var messageList = new List<NetworkMessage>(_networkMessages); // shallow copy
			_networkMessages.Clear();

			var videoFrameList = new List<VideoFrame>();

			while (true)
			{
				Packet packet = _client.Receive();
				if (packet == null) break;

				unsafe
				{
					var message = (byte*)packet.Data.ToPointer();
					byte messageCode = message[0];

					switch (messageCode)
					{
						case (byte)MessageId.ID_CONNECTION_REQUEST_ACCEPTED:
							onConnectionRequestedAccepted(messageList, packet);
							break;

						case (byte)MessageId.ID_CONNECTION_ATTEMPT_FAILED:
						case (byte)MessageId.ID_ALREADY_CONNECTED:
						case (byte)MessageId.ID_CONNECTION_BANNED:
						case (byte)MessageId.ID_INVALID_PASSWORD:
						case (byte)MessageId.ID_INCOMPATIBLE_PROTOCOL_VERSION:
						case (byte)MessageId.ID_IP_RECENTLY_CONNECTED:
							onConnectionAttemptFailed(messageList, packet);
							break;

						case (byte)MessageId.ID_DISCONNECTION_NOTIFICATION:
						case (byte)MessageId.ID_CONNECTION_LOST:
							onDisconnectionNotification(messageList, packet);
							break;

						case (byte)MessageId.SystemMessage:
						case (byte)MessageId.ReliableMessageFromHostToSingleClient:
						case (byte)MessageId.ReliableMessageFromClientToHost:
						case (byte)MessageId.ReliableMessageFromClientToAll:
						case (byte)MessageId.UnreliableMessageFromClientToAllOthers:
							messageList.Add(new NetworkMessage(packet));
							break;

						case (byte)MessageId.VideoFrame:
							onVideoFrame(videoFrameList, packet);
							break;

						case (byte)MessageId.VideoFrameAck:
							onVideoFrameAck(messageList, packet);
							break;

						case (byte)MessageId.VideoCaptureDisabled:
							onVideoCaptureDisabled(messageList, packet);
							break;

						case (byte)MessageId.VideoPlaybackToggled:
							onVideoPlaybackToggled(messageList, packet);
							break;

						default:
							// Ignore this packet
							packet.Dispose();
							break;
					}
				}
			}

			processVideoFrames(messageList, videoFrameList);

			return messageList;
		}

		void onConnectionRequestedAccepted(List<NetworkMessage> messageList, Packet packet)
		{
			using (packet)
			{
				if (_status == NetworkStatus.Connecting)
				{
					_serverAddress = new AddressOrGuid
					{
						rakNetGuid = packet.SenderGuid,
						systemAddress = packet.SenderAddress,
					};
					_playerId = _client.Guid;
					_status = NetworkStatus.Connected;

					// add the new message to the message list
					var message = new NetworkMessage(new byte[] {
						(byte)MessageId.SystemMessage,
						(byte)ReservedMessageType.ConnectionEstablished,
					});

					messageList.Add(message);
				}
			}
		}

		void onConnectionAttemptFailed(List<NetworkMessage> messageList, Packet packet)
		{
			using (packet)
			{
				if (_status == NetworkStatus.Connecting)
				{
					ConnectionFailureCause cause;
					unsafe
					{
						byte* ptr = (byte*)packet.Data.ToPointer();
						switch (*ptr)
						{
							case (byte)MessageId.ID_ALREADY_CONNECTED:
							case (byte)MessageId.ID_CONNECTION_BANNED:
							case (byte)MessageId.ID_INVALID_PASSWORD:
							case (byte)MessageId.ID_INCOMPATIBLE_PROTOCOL_VERSION:
							case (byte)MessageId.ID_IP_RECENTLY_CONNECTED:
								cause = ConnectionFailureCause.HostRejectedConnection;
								break;

							case (byte)MessageId.ID_CONNECTION_ATTEMPT_FAILED:
							default:
								cause = ConnectionFailureCause.Other;
								break;
						}
					}

					var message = new NetworkMessage(new byte[] {
						(byte)MessageId.SystemMessage,
						(byte)ReservedMessageType.ConnectionFailed,
						(byte)cause,
					});

					messageList.Add(message);
				}
			}

			if (_status != NetworkStatus.Disconnected)
			{
				_status = NetworkStatus.Disconnected;
				_client.Shutdown();
			}
		}

		void onDisconnectionNotification(List<NetworkMessage> messageList, Packet packet)
		{
			using(packet)
			{
				if (_status != NetworkStatus.Disconnected)
				{
					// add the new message to the message list
					var message = new NetworkMessage(new byte[] {
						(byte)MessageId.SystemMessage,
						(byte)ReservedMessageType.HostDisconnected,
					});

					messageList.Add(message);
				}
			}

			if (_status != NetworkStatus.Disconnected)
			{
				_status = NetworkStatus.Disconnected;
				_client.Shutdown();
			}
		}

		void onVideoFrame(List<VideoFrame> videoFrameList, Packet packet)
		{
			unsafe
			{
				byte* ptr = (byte*)packet.Data.ToPointer();
				UInt64 senderId =
					((UInt64)(*(ptr + 1)) << 0) |
					((UInt64)(*(ptr + 2)) << 8) |
					((UInt64)(*(ptr + 3)) << 16) |
					((UInt64)(*(ptr + 4)) << 24) |
					((UInt64)(*(ptr + 5)) << 32) |
					((UInt64)(*(ptr + 6)) << 40) |
					((UInt64)(*(ptr + 7)) << 48) |
					((UInt64)(*(ptr + 8)) << 56);

				videoFrameList.Add(new VideoFrame { SenderId = senderId, Packet = packet });
			}
		}

		void processVideoFrames(List<NetworkMessage> messageList, List<VideoFrame> videoFrameList)
		{
			// process video frames from most recent to least recent
			while (videoFrameList.Count > 0)
			{
				VideoFrame videoFrame = videoFrameList[videoFrameList.Count - 1];
				videoFrameList.RemoveAt(videoFrameList.Count - 1);

				// ignore earlier frames from same sender
				UInt64 senderId = videoFrame.SenderId;
				for (int i = videoFrameList.Count - 1; i >= 0; --i)
				{
					if (videoFrameList[i].SenderId == senderId)
					{
						videoFrameList[i].Packet.Dispose();
						videoFrameList.RemoveAt(i);
					}
				}

				using (var packet = videoFrame.Packet)
				{
					// extract frame IDs
					byte frameId;
					byte referenceFrameId;
					unsafe
					{
						byte* ptr = (byte*)packet.Data.ToPointer();
						frameId = *(ptr + 9);
						referenceFrameId = *(ptr + 10);
					}

					// send video frame reception notification
					if (_status == NetworkStatus.Connected)
					{
						var data = new byte[] {
							(byte)MessageId.VideoFrameAck,
							(byte)((senderId & 0x00000000000000ff) >> 0),
							(byte)((senderId & 0x000000000000ff00) >> 8),
							(byte)((senderId & 0x0000000000ff0000) >> 16),
							(byte)((senderId & 0x00000000ff000000) >> 24),
							(byte)((senderId & 0x000000ff00000000) >> 32),
							(byte)((senderId & 0x0000ff0000000000) >> 40),
							(byte)((senderId & 0x00ff000000000000) >> 48),
							(byte)((senderId & 0xff00000000000000) >> 56),
							frameId,
						};

						_client.Send(data,
							PacketPriority.HIGH_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
							(int)OrderingChannel.Video,
							_serverAddress, false);
					}

					if (!_inboundVideoFrameHistories.TryGetValue(senderId, out var history))
					{
						history = new InboundVideoFrameHistory();
						_inboundVideoFrameHistories.Add(senderId, history);
					}
					byte[] frame = new byte[64 * 64 * 3 + 10];
					frame[0] = (byte)MessageId.SystemMessage;
					frame[1] = (byte)ReservedMessageType.VideoFrameReceived;
					frame[2] = (byte)((senderId & 0x00000000000000ff) >> 0);
					frame[3] = (byte)((senderId & 0x000000000000ff00) >> 8);
					frame[4] = (byte)((senderId & 0x0000000000ff0000) >> 16);
					frame[5] = (byte)((senderId & 0x00000000ff000000) >> 24);
					frame[6] = (byte)((senderId & 0x000000ff00000000) >> 32);
					frame[7] = (byte)((senderId & 0x0000ff0000000000) >> 40);
					frame[8] = (byte)((senderId & 0x00ff000000000000) >> 48);
					frame[9] = (byte)((senderId & 0xff00000000000000) >> 56);

					if (_serverIsOnSameComputer)
					{
						// no compression
						Debug.Assert(packet.Length == 64 * 64 * 3 + 11);
						unsafe
						{
							byte* ptr = (byte*)packet.Data.ToPointer() + 11;
							Marshal.Copy((IntPtr)ptr, frame, 10, frame.Length - 10);
						}
					}
					else
					{
						// uncompress video frame
						unsafe
						{
							byte* dataPtr = (byte*)packet.Data.ToPointer() + 11;

							fixed (byte* framePtr = frame)
							{
								if (frameId == referenceFrameId)
								{
									// no reference frame
									_videoCodec.Decode((IntPtr)dataPtr, (IntPtr)(framePtr + 10));
								}
								else
								{
									// reference frame
									fixed (byte* referenceFramePtr = history.GetFrameData(referenceFrameId))
									{
										_videoCodec.Decode((IntPtr)(referenceFramePtr + 10), (IntPtr)dataPtr, (IntPtr)(framePtr + 10));
									}
								}
							}
						}
					}
					if (referenceFrameId != frameId)
					{
						history.ClearHistoryUntilThisFrame(referenceFrameId);
					}
					history.AddFrame(frameId, frame);

					var message = new NetworkMessage(frame);
					messageList.Add(message);
				}
			}
		}

		void onVideoFrameAck(List<NetworkMessage> messageList, Packet packet)
		{
			byte frameId;
			unsafe
			{
				var ptr = (byte*)packet.Data.ToPointer();
				frameId = *(ptr + 1);
			}

			_outboundVideoFrameHistory.AckFrame(frameId);
		}

		void onVideoCaptureDisabled(List<NetworkMessage> messageList, Packet packet)
		{
		}

		void onVideoPlaybackToggled(List<NetworkMessage> messageList, Packet packet)
		{
		}

		/// <summary>Indicates that this player is transmitting a voice communication.</summary>
		public bool IsRecording { get { return _isRecording; } }

		/// <summary>Retrieves statistics for the connection between this client and the host.</summary>
		public string[] Statistics
		{
			get
			{
				if (_status == NetworkStatus.Connected)
				{
					return new string[] { "No statistics available (not implemented)." };
					//ConnectionInformation info = _client.GetConnectionInformation();
					//int highPrioritySendQueueMessageCount, normalPrioritySendQueueMessageCount, lowPrioritySendQueueMessageCount;
					//int highPrioritySendQueueByteCount, normalPrioritySendQueueByteCount, lowPrioritySendQueueByteCount;
					//_client.GetSendQueueInformation(out highPrioritySendQueueMessageCount, out highPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityHigh);
					//_client.GetSendQueueInformation(out normalPrioritySendQueueMessageCount, out normalPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityNormal);
					//_client.GetSendQueueInformation(out lowPrioritySendQueueMessageCount, out lowPrioritySendQueueByteCount, GetSendQueueInformationFlags.PriorityLow);
					//return new string[] {
					//	string.Format(" Round trip latency: {0} ms.", info.RoundTripLatencyMs),
					//	string.Format(" Throughput: {0} bits/s.", 8 * info.ThroughputBps),
					//	string.Format(" Peak throughput: {0} bits/s.", 8 * info.PeakThroughputBps),
					//	string.Format(" Packets lost: {0}%.", (100 * (info.PacketsRetried + info.PacketsDropped)) / (1 + info.PacketsSentGuaranteed + info.PacketsSentNonGuaranteed)),
					//	string.Format(" Game messages timed out: {0}%.", (100 * info.MessagesTimedOutHighPriority) / (1 + info.MessagesTimedOutHighPriority + info.MessagesTransmittedHighPriority)),
					//	string.Format(" Video messages timed out: {0}%.", (100 * info.MessagesTimedOutLowPriority) / (1 + info.MessagesTimedOutLowPriority + info.MessagesTransmittedLowPriority)),
					//	string.Format(" Game messages queued: {0} ({0} bytes).", highPrioritySendQueueMessageCount, highPrioritySendQueueByteCount),
					//	string.Format(" Video messages queued: {0} ({0} bytes).", lowPrioritySendQueueMessageCount, lowPrioritySendQueueByteCount),
					//};
				}
				else
				{
					return new string[] { "No statistics available (not connected)." };
				}
			}
		}

		void onTimeout(object state)
		{
			if (_status == NetworkStatus.Connecting)
			{
				_status = NetworkStatus.Disconnected;
				_client.Shutdown();

				var message = new NetworkMessage(new byte[] {
					(byte)MessageId.SystemMessage,
					(byte)ReservedMessageType.ConnectionFailed,
					(byte)ConnectionFailureCause.TimeOut,
				});

				_networkMessages.Enqueue(message);
			}
		}

		Peer _client = null;
		volatile bool _isRecording = false;
		volatile NetworkStatus _status = NetworkStatus.Disconnected;
		UInt64 _playerId = 0;
		AddressOrGuid _serverAddress = AddressOrGuid.UNASSIGNED;
		Queue<NetworkMessage> _networkMessages = new Queue<NetworkMessage>();
		IVideoCodec _videoCodec = new ZtcVideoCodec();
		OutboundVideoFrameHistory _outboundVideoFrameHistory = null;
		Dictionary<UInt64, InboundVideoFrameHistory> _inboundVideoFrameHistories = null;
		bool _serverIsOnSameComputer = false;
	}
}
