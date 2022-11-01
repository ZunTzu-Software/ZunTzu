// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Open.Nat;
using ZunTzu.VideoCompression;

namespace ZunTzu.Networking
{

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public sealed class RakServer : IServer
	{
		/// <summary>Constructor.</summary>
		public RakServer()
		{
			_server = Peer.Create();
		}

		/// <summary>Begin a new game as a host.</summary>
		/// <param name="port">IP port that will listen.</param>
		public void Start(int port)
		{
			try
			{
				// NAT traversal using NAT-PMP or UPnP
				IPAddress natIp = null;
				try
				{
					var discoverer = new NatDiscoverer();
					var device = discoverer.DiscoverDeviceAsync().Result;
					natIp = device.GetExternalIPAsync().Result;
					device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, 3600, "zuntzu")).Wait(3000);
				}
				catch { }

				// NAT traversal using an external server (will not work with 'symmetric' NATs, though)
				IPAddress publicIp = null;
				int? publicPort = null;
				string sessionId = null;
				try
				{
					using (UdpClient socket = new UdpClient(port))
					{
						socket.Client.ReceiveTimeout = 2000;
						socket.Connect("zuntzu.ovh", 1805);

						// Send a START_GAME_SESSION message
						byte[] datagram = new[] { (byte)'z', (byte)'t', (byte)0 };
						socket.Send(datagram, datagram.Length);

						// Wait for a response
						var endPoint = new IPEndPoint(IPAddress.Any, 0);
						byte[] response = socket.Receive(ref endPoint);

						if (response != null
							&& response[0] == 'z'
							&& response[1] == 't'
							&& response[2] == 0)
						{
							sessionId = Encoding.UTF8.GetString(response, 3, 5);
							publicIp = new IPAddress(new byte[] { response[8], response[9], response[10], response[11] });
							publicPort = response[12] | ((int)response[13] << 8);
						}
					}
				}
				catch { }

				if (publicIp == null)
				{
					// fallback: discover public IP through a public website
					try
					{
						HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://www.whatismyip-address.com/");
						request.Timeout = 10000;
						request.Method = "GET";
						request.UserAgent = "ZunTzu";

						using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
						{
							if (response.StatusCode == HttpStatusCode.OK)
							{
								using (Stream stream = response.GetResponseStream())
								{
									using (StreamReader reader = new StreamReader(stream))
									{
										string responseContent = reader.ReadToEnd();
										Regex ipAddressRegex = new Regex(@">(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})<", RegexOptions.Singleline);
										Match ipAddressMatch = ipAddressRegex.Match(responseContent);
										if (ipAddressMatch.Success)
										{
											IPAddress.TryParse(ipAddressMatch.Groups[1].Value, out publicIp);
										}
									}
								}
							}
						}
					}
					catch
					{
					}
				}

				StartupResult result = _server.StartupServer((ushort)port);

				// notify the parent process via the standard output
				switch (result) {
					case StartupResult.RAKNET_STARTED:
						_boundIpAddress = _server.BoundAddress;
						Console.Out.WriteLine("Server started {0}/{1}/{2}",
							publicIp?.ToString() ?? "?",
							publicPort ?? port,
							sessionId ?? "?");
						break;
					case StartupResult.RAKNET_ALREADY_STARTED:
						Console.Out.WriteLine("Cannot start server: RAKNET_ALREADY_STARTED");
						break;
					case StartupResult.SOCKET_PORT_ALREADY_IN_USE:
						Console.Out.WriteLine("Cannot start server: SOCKET_PORT_ALREADY_IN_USE");
						break;
					case StartupResult.SOCKET_FAILED_TO_BIND:
						Console.Out.WriteLine("Cannot start server: SOCKET_FAILED_TO_BIND");
						break;
					case StartupResult.SOCKET_FAILED_TEST_SEND:
						Console.Out.WriteLine("Cannot start server: SOCKET_FAILED_TEST_SEND");
						break;
					default:
						Console.Out.WriteLine("Cannot start server: error code {0}", result);
						break;
				}
			}
			catch (Exception e)
			{
				// notify the parent process via the standard output
				Console.Out.WriteLine(e.Message);
			}
			Console.Out.Flush();

			runEventLoop();
		}

		public void Dispose()
		{
			if (_server != null)
			{
				for(int i = 0; i < _uncompressJobs.Count; ++i)
				{
					_uncompressJobs[i].Packet.Dispose();
				}

				_server.Dispose();
				_server = null;
			}
		}

		void runEventLoop()
		{
			while (true)
			{
				Packet packet = _server.Receive();
				if (packet != null)
				{
					processPacket(packet);
				}
				else if(_uncompressJobs.Count + _compressJobs.Count > 0)
				{
					processVideoFrame();
				}
				else
				{
					// yield and wait for next job
					Thread.Sleep(1); // according to Eric Lippert "Thread.Sleep(1) cedes control to any ready thread of the operating system’s choice"
				}
			}
		}

		void processPacket(Packet packet)
		{
			Debug.Assert(packet.Length > 0);

			byte messageCode;
			unsafe
			{
				var message = (byte*)packet.Data.ToPointer();
				messageCode = message[0];
			}

			if (messageCode == (byte)MessageId.VideoFrame)
			{
				// do NOT dispose this packet right now! (it will be disposed when the video frame is processed)
				onVideoFrame(packet);
				return;
			}

			using(packet)
			{
				switch (messageCode)
				{
					case (byte)MessageId.ID_NEW_INCOMING_CONNECTION:
						onPlayerConnected(packet);
						break;

					case (byte)MessageId.ID_DISCONNECTION_NOTIFICATION:
					case (byte)MessageId.ID_CONNECTION_LOST:
						onPlayerDisconnected(packet);
						break;

					case (byte)MessageId.ReliableMessageFromHostToSingleClient:
						onReliableMessageFromHostToSingleClient(packet);
						break;

					case (byte)MessageId.ReliableMessageFromClientToHost:
						onReliableMessageFromClientToHost(packet);
						break;

					case (byte)MessageId.ReliableMessageFromClientToAll:
						onReliableMessageFromClientToAll(packet);
						break;

					case (byte)MessageId.UnreliableMessageFromClientToAllOthers:
						onUnreliableMessageFromClientToAllOthers(packet);
						break;

					case (byte)MessageId.VideoFrameAck:
						onVideoFrameAck(packet);
						break;

					case (byte)MessageId.VideoCaptureDisabled:
						onVideoCaptureDisabled(packet);
						break;

					case (byte)MessageId.VideoPlaybackToggled:
						onVideoPlaybackToggled(packet);
						break;

					default:
						// Ignore this packet
						break;
				}
			}
		}

		void onPlayerConnected(Packet packet)
		{
			var address = extractSenderAddress(packet);
			UInt64 playerId = address.rakNetGuid.g;
			var playerState = new PlayerState {
				Address = address,
				ServerIsOnSameMachine = (address.systemAddress.sin_addr == _boundIpAddress),
			};
			_playersById.Add(playerId, playerState);

			bool playerIsHosting = (_playersById.Count == 1);

			if (playerIsHosting)
			{
				_hostingPlayer = playerState;
			}
			else
			{
				// notify host
				var data = new byte[]
				{
					(byte)MessageId.SystemMessage,
					(byte)ReservedMessageType.PlayerWantsToJoin,
					(byte)((playerId & 0x00000000000000ff) >> 0),
					(byte)((playerId & 0x000000000000ff00) >> 8),
					(byte)((playerId & 0x0000000000ff0000) >> 16),
					(byte)((playerId & 0x00000000ff000000) >> 24),
					(byte)((playerId & 0x000000ff00000000) >> 32),
					(byte)((playerId & 0x0000ff0000000000) >> 40),
					(byte)((playerId & 0x00ff000000000000) >> 48),
					(byte)((playerId & 0xff00000000000000) >> 56),
				};

				_server.Send(data,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					_hostingPlayer.Address, false);
			}
		}

		void onPlayerDisconnected(Packet packet)
		{
			AddressOrGuid playerAddress = extractSenderAddress(packet);
			UInt64 playerId = playerAddress.rakNetGuid.g;

			bool playerIsHosting = (playerId == _hostingPlayer.Address.rakNetGuid.g);

			if (playerIsHosting)
			{
				// hosting player has left -> stop server
				_server.Shutdown();
				Dispose();
				Environment.Exit(0);
			}
			else
			{
				// remove player
				if (!_playersById.Remove(playerId)) return;

				// remove video history
				foreach(var otherPlayer in _playersById.Values)
				{
					otherPlayer.OutboundVideoFrameHistoryByRecipientId.Remove(playerId);
				}

				// notify all other players
				var data = new byte[]
				{
					(byte)MessageId.SystemMessage,
					(byte)ReservedMessageType.PlayerHasLeft,
					(byte)((playerId & 0x00000000000000ff) >> 0),
					(byte)((playerId & 0x000000000000ff00) >> 8),
					(byte)((playerId & 0x0000000000ff0000) >> 16),
					(byte)((playerId & 0x00000000ff000000) >> 24),
					(byte)((playerId & 0x000000ff00000000) >> 32),
					(byte)((playerId & 0x0000ff0000000000) >> 40),
					(byte)((playerId & 0x00ff000000000000) >> 48),
					(byte)((playerId & 0xff00000000000000) >> 56),
				};

				_server.Send(data,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					AddressOrGuid.UNASSIGNED, true);
			}
		}

		void onReliableMessageFromHostToSingleClient(Packet packet)
		{
			unsafe
			{
				// The recipient ID is stored on bytes 2 to 9 of the packet
				if (packet.Length < 10) return; // sanity check
				byte* ptr = (byte*)packet.Data.ToPointer();
				UInt64 recipientId =
					((UInt64)(*(ptr + 2)) << 0) |
					((UInt64)(*(ptr + 3)) << 8) |
					((UInt64)(*(ptr + 4)) << 16) |
					((UInt64)(*(ptr + 5)) << 24) |
					((UInt64)(*(ptr + 6)) << 32) |
					((UInt64)(*(ptr + 7)) << 40) |
					((UInt64)(*(ptr + 8)) << 48) |
					((UInt64)(*(ptr + 9)) << 56);

				if (!_playersById.TryGetValue(recipientId, out var recipient)) return;

				_server.Send(ptr, packet.Length,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					recipient.Address, false);
			}
		}

		void onReliableMessageFromClientToHost(Packet packet)
		{
			if (packet.Length < 10) return; // sanity check

			AddressOrGuid senderAddress = extractSenderAddress(packet);
			UInt64 senderId = senderAddress.rakNetGuid.g;
			if (!_playersById.ContainsKey(senderId)) return;

			replaceSenderId(packet, senderId);

			unsafe
			{
				_server.Send((byte*)packet.Data.ToPointer(), packet.Length,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					_hostingPlayer.Address, false);
			}
		}

		void onReliableMessageFromClientToAll(Packet packet) 
		{
			if (packet.Length < 10) return; // sanity check

			AddressOrGuid senderAddress = extractSenderAddress(packet);
			UInt64 senderId = senderAddress.rakNetGuid.g;
			if (!_playersById.ContainsKey(senderId)) return;

			replaceSenderId(packet, senderId);

			unsafe
			{
				_server.Send((byte*)packet.Data.ToPointer(), packet.Length,
					PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED,
					(int)OrderingChannel.Reliable,
					AddressOrGuid.UNASSIGNED, true);
			}
		}

		void onUnreliableMessageFromClientToAllOthers(Packet packet)
		{
			if (packet.Length < 10) return; // sanity check

			AddressOrGuid senderAddress = extractSenderAddress(packet);
			UInt64 senderId = senderAddress.rakNetGuid.g;
			if (!_playersById.ContainsKey(senderId)) return;

			replaceSenderId(packet, senderId);

			unsafe
			{
				_server.Send((byte*)packet.Data.ToPointer(), packet.Length,
					PacketPriority.HIGH_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
					(int)OrderingChannel.Unreliable,
					senderAddress, true);
			}
		}

		void onVideoFrame(Packet packet)
		{
			// a video frame is a low-priority unreliable message from client to all others
			// TODO: contextual sending to each player with playback enabled (except sender)

			AddressOrGuid senderAddress = extractSenderAddress(packet);
			UInt64 senderId = senderAddress.rakNetGuid.g;
			if (!_playersById.ContainsKey(senderId)) return;

			_uncompressJobs.Add(new UncompressJob(senderId, packet));
		}

		void onVideoFrameAck(Packet packet)
		{
			// watch out: sender and recipient semantics are inverted!
			// - VideoFrame:      sender    -> recipient
			// - VideoFrameAck:   recipient -> sender

			AddressOrGuid recipientAddress = extractSenderAddress(packet);
			UInt64 recipientId = recipientAddress.rakNetGuid.g;
			if (!_playersById.ContainsKey(recipientId)) return;

			UInt64 senderId;
			byte frameId;
			unsafe
			{
				// The sender ID is stored on bytes 1 to 8 of the packet
				if (packet.Length < 10) return; // sanity check
				byte* ptr = (byte*)packet.Data.ToPointer();
				senderId =
					((UInt64)(*(ptr + 1)) << 0) |
					((UInt64)(*(ptr + 2)) << 8) |
					((UInt64)(*(ptr + 3)) << 16) |
					((UInt64)(*(ptr + 4)) << 24) |
					((UInt64)(*(ptr + 5)) << 32) |
					((UInt64)(*(ptr + 6)) << 40) |
					((UInt64)(*(ptr + 7)) << 48) |
					((UInt64)(*(ptr + 8)) << 56);

				// The frame ID is stored on byte 9
				frameId = *(ptr + 9);
			}

			if (!_playersById.TryGetValue(senderId, out var sender)) return;
			if (!sender.OutboundVideoFrameHistoryByRecipientId.TryGetValue(recipientId, out var history)) return;
			history.AckFrame(frameId);
		}

		void onVideoCaptureDisabled(Packet packet)
		{
		}

		void onVideoPlaybackToggled(Packet packet)
		{
			// TODO: avoid sending video to players who have disabled playback
		}

		void processVideoFrame()
		{
			if (_uncompressJobs.Count > 0)
			{
				// retrieve most recent uncompress job
				UncompressJob job = _uncompressJobs[_uncompressJobs.Count - 1];

				// remove all jobs from same sender (they are all less recent)
				for(int i = _uncompressJobs.Count - 1; i >= 0; --i)
				{
					if (_uncompressJobs[i].SenderId == job.SenderId)
					{
						_uncompressJobs.RemoveAt(i);
					}
				}

				uncompressFrame(job);
			}
			else if (_compressJobs.Count > 0)
			{
				// retrieve most recent compress job
				CompressJob job = _compressJobs[_compressJobs.Count - 1];

				// remove all jobs with same sender/recipient pair (they are all less recent)
				for(int i = _compressJobs.Count - 1; i >= 0; --i)
				{
					if (_compressJobs[i].SenderId == job.SenderId &&
						_compressJobs[i].RecipientId == job.RecipientId)
					{
						_compressJobs.RemoveAt(i);
					}
				}

				compressAndSendFrame(job);
			}
		}

		void uncompressFrame(UncompressJob job)
		{
			// Packet data:
			//   Byte 0:   message code (value is always MessageId.VideoFrame)
			//   Byte 1:   frame ID
			//   Byte 2:   reference frame ID
			//   Byte 3-N: compressed image (uncompressed if sent from hosting player)

			using (Packet packet = job.Packet)
			{
				UInt64 senderId = job.SenderId;
				if (!_playersById.TryGetValue(senderId, out var sender)) return;

				byte frameId;
				byte referenceFrameId;
				unsafe
				{
					if (packet.Length < 3) return; // sanity check
					byte* ptr = (byte*)packet.Data.ToPointer();
					frameId = *(ptr + 1);
					referenceFrameId = *(ptr + 2);
				}

				// send video frame reception notification
				var data = new byte[]
				{
					(byte)MessageId.VideoFrameAck,
					(byte)frameId,
				};

				_server.Send(data,
					PacketPriority.HIGH_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
					(int)OrderingChannel.Video,
					sender.Address, false);

				// uncompress video frame
				InboundVideoFrameHistory history = sender.InboundVideoFrameHistory;
				byte[] frame = new byte[64 * 64 * 3];
				if (sender.ServerIsOnSameMachine)
				{
					// hosting player -> no compression needed (it is on the same computer)
					unsafe
					{
						if (packet.Length < 64 * 64 * 3 + 3) return; // sanity check
						byte* ptr = (byte*)packet.Data.ToPointer() + 3;
						Marshal.Copy((IntPtr)ptr, frame, 0, frame.Length);
					}
				}
				else
				{
					unsafe
					{
						byte* dataPtr = (byte*)packet.Data.ToPointer() + 3;

						fixed (byte* framePtr = frame)
						{
							if (frameId == referenceFrameId)
							{
								// no reference frame
								_videoCodec.Decode((IntPtr)dataPtr, (IntPtr)framePtr);
							}
							else
							{
								// reference frame
								fixed (byte* referenceFramePtr = history.GetFrameData(referenceFrameId))
								{
									_videoCodec.Decode((IntPtr)referenceFramePtr, (IntPtr)dataPtr, (IntPtr)framePtr);
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

				// make one compress job for each other player
				foreach(var playerId in _playersById.Keys)
				{
					if (playerId == senderId) continue;
					byte[] frameCopy = (byte[]) frame.Clone(); // this is necessary because the frame is modified by the encoding algorithm
					_compressJobs.Add(new CompressJob(senderId, playerId, frameCopy));
				}
			}
		}

		void compressAndSendFrame(CompressJob job)
		{
			UInt64 recipientId = job.RecipientId;
			if (!_playersById.TryGetValue(recipientId, out var recipient)) return;

			UInt64 senderId = job.SenderId;
			if (!_playersById.TryGetValue(senderId, out var sender)) return;

			byte[] frame = job.Frame;

			if (!sender.OutboundVideoFrameHistoryByRecipientId.TryGetValue(recipientId, out var history))
			{
				history = new OutboundVideoFrameHistory();
				sender.OutboundVideoFrameHistoryByRecipientId.Add(recipientId, history);
			}
			byte frameId = history.AddFrame(frame);
			byte? latestAckedFrameId = history.LatestAckedFrameId;

			byte[] data;
			if (recipient.ServerIsOnSameMachine)
			{
				// no compression needed (it is on the same computer)
				data = new byte[frame.Length + 11];
				data[0] = (byte)MessageId.VideoFrame;
				data[1] = (byte)((senderId & 0x00000000000000ff) >> 0);
				data[2] = (byte)((senderId & 0x000000000000ff00) >> 8);
				data[3] = (byte)((senderId & 0x0000000000ff0000) >> 16);
				data[4] = (byte)((senderId & 0x00000000ff000000) >> 24);
				data[5] = (byte)((senderId & 0x000000ff00000000) >> 32);
				data[6] = (byte)((senderId & 0x0000ff0000000000) >> 40);
				data[7] = (byte)((senderId & 0x00ff000000000000) >> 48);
				data[8] = (byte)((senderId & 0xff00000000000000) >> 56);
				data[9] = frameId;
				data[10] = latestAckedFrameId ?? frameId;
				Array.Copy(frame, 0, data, 11, frame.Length);
			}
			else
			{
				unsafe
				{
					fixed (byte* frameBufferPtr = frame)
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
							byte[] latestAckedFrameData = history.LatestAckedFrameData;
							fixed (byte* referenceFramePtr = latestAckedFrameData)
							{
								_videoCodec.Encode((IntPtr)referenceFramePtr, (IntPtr)frameBufferPtr, (IntPtr)compressedBuffer, out byteCount);
							}
						}

						data = new byte[byteCount + 11];
						data[0] = (byte)MessageId.VideoFrame;
						data[1] = (byte)((senderId & 0x00000000000000ff) >> 0);
						data[2] = (byte)((senderId & 0x000000000000ff00) >> 8);
						data[3] = (byte)((senderId & 0x0000000000ff0000) >> 16);
						data[4] = (byte)((senderId & 0x00000000ff000000) >> 24);
						data[5] = (byte)((senderId & 0x000000ff00000000) >> 32);
						data[6] = (byte)((senderId & 0x0000ff0000000000) >> 40);
						data[7] = (byte)((senderId & 0x00ff000000000000) >> 48);
						data[8] = (byte)((senderId & 0xff00000000000000) >> 56);
						data[9] = frameId;
						data[10] = latestAckedFrameId ?? frameId;
						Marshal.Copy((IntPtr)compressedBuffer, data, 11, byteCount);
					}
				}
			}

			_server.Send(data,
				PacketPriority.LOW_PRIORITY, PacketReliability.UNRELIABLE_SEQUENCED,
				(int)OrderingChannel.Video,
				recipient.Address, false);
		}

		AddressOrGuid extractSenderAddress(Packet packet)
		{
			return new AddressOrGuid
			{
				rakNetGuid = packet.SenderGuid,
				systemAddress = packet.SenderAddress,
			};
		}

		void replaceSenderId(Packet packet, UInt64 senderId)
		{
			unsafe
			{
				Debug.Assert(packet.Length >= 10);
				var ptr = (byte*)packet.Data.ToPointer();

				// overwrite sender id
				*(ptr + 2) = (byte)((senderId & 0x00000000000000ff) >> 0);
				*(ptr + 3) = (byte)((senderId & 0x000000000000ff00) >> 8);
				*(ptr + 4) = (byte)((senderId & 0x0000000000ff0000) >> 16);
				*(ptr + 5) = (byte)((senderId & 0x00000000ff000000) >> 24);
				*(ptr + 6) = (byte)((senderId & 0x000000ff00000000) >> 32);
				*(ptr + 7) = (byte)((senderId & 0x0000ff0000000000) >> 40);
				*(ptr + 8) = (byte)((senderId & 0x00ff000000000000) >> 48);
				*(ptr + 9) = (byte)((senderId & 0xff00000000000000) >> 56);
			}
		}

		struct UncompressJob
		{
			public UncompressJob(UInt64 senderId, Packet packet)
			{
				SenderId = senderId;
				Packet = packet;
			}
			public UInt64 SenderId;
			public Packet Packet;
		}

		struct CompressJob
		{
			public CompressJob(UInt64 senderId, UInt64 recipientId, byte[] frame)
			{
				SenderId = senderId;
				RecipientId = recipientId;
				Frame = frame;
			}
			public UInt64 SenderId;
			public UInt64 RecipientId;
			public byte[] Frame;
		}

		sealed class PlayerState
		{
			public AddressOrGuid Address = AddressOrGuid.UNASSIGNED;
			public bool ServerIsOnSameMachine;
			public InboundVideoFrameHistory InboundVideoFrameHistory = new InboundVideoFrameHistory();
			public Dictionary<UInt64, OutboundVideoFrameHistory> OutboundVideoFrameHistoryByRecipientId = new Dictionary<UInt64, OutboundVideoFrameHistory>();
		}

		Peer _server = null;
		UInt32 _boundIpAddress = 0;
		Dictionary<UInt64, PlayerState> _playersById = new Dictionary<UInt64, PlayerState>();
		PlayerState _hostingPlayer = null; // the hosting player is always the first one
		List<UncompressJob> _uncompressJobs = new List<UncompressJob>();
		List<CompressJob> _compressJobs = new List<CompressJob>();
		IVideoCodec _videoCodec = new ZtcVideoCodec();
	}
}
