// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.DirectX.DirectPlay;
using ZunTzu.VideoCompression;

namespace ZunTzu.Networking {

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public sealed class DXServer : IServer {

		/// <summary>Constructor.</summary>
		public DXServer() {
			server = new Microsoft.DirectX.DirectPlay.Server(InitializeFlags.DisableParameterValidation);

			server.PlayerCreated += new PlayerCreatedEventHandler(onPlayerCreated);
			server.PlayerDestroyed += new PlayerDestroyedEventHandler(onPlayerDestroyed);
			server.Receive += new ReceiveEventHandler(onReceive);
			server.GroupCreated += new GroupCreatedEventHandler(onGroupCreated);
		}

		/// <summary>Begin a new game as a host.</summary>
		/// <param name="port">IP port that will listen.</param>
		public void Start(int port) {
			uncompressJobsPending = new ManualResetEvent(false);

			try {
				ApplicationDescription description = new ApplicationDescription();
				description.GuidApplication = new Guid("{920BAF09-A06C-47d8-BCE0-21B30D0C3586}");
				description.MaxPlayers = 0;	// unlimited
				description.SessionName = "ZunTzu";
				description.Flags =
					Microsoft.DirectX.DirectPlay.SessionFlags.ClientServer |
					Microsoft.DirectX.DirectPlay.SessionFlags.FastSigned |
					Microsoft.DirectX.DirectPlay.SessionFlags.NoDpnServer |
					Microsoft.DirectX.DirectPlay.SessionFlags.NoEnumerations;

				using(Address address = new Address()) {
					address.ServiceProvider = Address.ServiceProviderTcpIp;
					address.AddComponent(Address.KeyPort, port);

					server.Host(description, address);
				}

				// allow NAT traversal (3 trials)
				InternetConnectivity connectivity = InternetConnectivity.Unknown;
				for(int trial = 0; (natTraversalSession == null || !natTraversalSession.Enabled) && trial < 3; ++trial)
					natTraversalSession = NatResolver.EnableNatTraversal(port);
				string fallbackPublicIpAddress = null;
				if(natTraversalSession.Enabled) {
					// notify the parent process via the standard output
					connectivity = InternetConnectivity.Full;
				} else {
					// fallback: discover public IP through HTTP
					try {
						// Start a synchronous request.
						HttpWebRequest request = (HttpWebRequest) WebRequest.Create(@"http://www.zuntzu.com/hostfallback.php");
						request.UserAgent = "ZunTzu";
						request.Timeout = 10000;
						request.Method = "POST";
						request.ContentType = "application/x-www-form-urlencoded";

						byte[] bytes = System.Text.ASCIIEncoding.ASCII.GetBytes("id=" + natTraversalSession.SessionId.ToString("N"));
						request.ContentLength = bytes.Length;
						using(Stream requestStream = request.GetRequestStream()) {
							requestStream.Write(bytes, 0, bytes.Length);
						}

						using(HttpWebResponse response = (HttpWebResponse) request.GetResponse()) {
							if(response.StatusCode == HttpStatusCode.OK) {
								using(Stream stream = response.GetResponseStream()) {
									using(StreamReader reader = new StreamReader(stream)) {
										string responseContent = reader.ReadToEnd();
										Regex ipAddressRegex = new Regex(@"^(?<1>[012])(?<2>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Singleline);
										Match ipAddressMatch = ipAddressRegex.Match(responseContent);
										if(ipAddressMatch.Success) {
											switch(ipAddressMatch.Groups[1].Value) {
												case "0":
													connectivity = InternetConnectivity.Unknown;
													break;
												case "1":
													connectivity = InternetConnectivity.NoEgress;
													break;
												case "2":
													connectivity = InternetConnectivity.NoIngress;
													break;
											}
											fallbackPublicIpAddress = ipAddressMatch.Groups[2].Value;
										}
									}
								}
							} else {
								throw new WebException();
							}
						}
					} catch(Exception) {
						// fallback: query a public web site to check Internet connectivity
						try {
							// Start a synchronous request.
							HttpWebRequest request = (HttpWebRequest) WebRequest.Create(@"http://www.google.com/");
							request.Timeout = 10000;

							using(HttpWebResponse response = (HttpWebResponse) request.GetResponse()) {
								if(response.StatusCode != HttpStatusCode.OK)
									throw new WebException();
							}
						} catch(Exception) {
							connectivity = InternetConnectivity.None;
						}
					}
				}

				// notify the parent process via the standard output
				switch(connectivity) {
					case InternetConnectivity.Unknown:
					case InternetConnectivity.None:
						Console.Out.WriteLine("Server started {0}/?/?", (int) connectivity);
						break;
					case InternetConnectivity.NoEgress:
					case InternetConnectivity.NoIngress:
						Console.Out.WriteLine("Server started {0}/{1}/?", (int) connectivity, fallbackPublicIpAddress);
						break;
					case InternetConnectivity.Full:
						Console.Out.WriteLine("Server started {0}/{1}/{2}", (int) connectivity, natTraversalSession.PublicIpAddress, natTraversalSession.PublicPort);
						break;
				}
			} catch(InvalidDeviceAddressException) {
				// notify the parent process via the standard output
				Console.Out.WriteLine("Invalid Device Address");
			} catch(Exception e) {
				// notify the parent process via the standard output
				Console.Out.WriteLine(e.Message);
			}
			Console.Out.Flush();

			processVideoFrames();
		}

		public void Dispose() {
			if(server != null && !server.Disposed) {
				server.PlayerCreated -= new PlayerCreatedEventHandler(onPlayerCreated);
				server.PlayerDestroyed -= new PlayerDestroyedEventHandler(onPlayerDestroyed);
				server.Receive -= new ReceiveEventHandler(onReceive);
				server.GroupCreated -= new GroupCreatedEventHandler(onGroupCreated);
				server.Dispose();
				server = null;
			}
		}

		private sealed class PlayerContext {
			public int AllOtherPlayersGroupId;
		}

		private void onPlayerCreated(object sender, PlayerCreatedEventArgs e) {
			if(serverId == 0) {
				// this is not a real player, just the server starting
				serverId = e.Message.PlayerID;
				return;
			} else if(server.Groups.Count == 0) {
				// this is the first player to connect, therefore it becomes the hosting player
				hostingPlayerId = e.Message.PlayerID;
			} else {
				// add this player to all existing groups of "other players"
				foreach(int groupId in server.Groups) {
					server.AddPlayerToGroup(groupId, e.Message.PlayerID, 0);
				}
				// send a notification to the NAT traversal server
				if(natTraversalSession.Enabled) {
					using(Address playerAddress = server.GetClientAddress(e.Message.PlayerID)) {
						string playerIpAsString = playerAddress.GetComponentString(Address.KeyHostname);
						Regex ipAddressRegex = new Regex(@"^(?<1>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Singleline);
						Match ipAddressMatch = ipAddressRegex.Match(playerIpAsString);
						if(ipAddressMatch.Success) {
							IPAddress playerIp = IPAddress.Parse(ipAddressMatch.Groups[1].Value);
							int playerPort = playerAddress.GetComponentInteger(Address.KeyPort);
							NatResolver.NotifyPlayerHasJoined(natTraversalSession, playerIp, playerPort);
						}
					}
				}
			}

			// link the newly created player's context to the new "other players" group
			PlayerContext playerAndGroupContext = new PlayerContext();
			playerAndGroupContext.AllOtherPlayersGroupId = e.Message.PlayerID;	// to be passed to onGroupCreated
			e.Message.PlayerContext = playerAndGroupContext;

			// create a new "other players" group for this player
			server.CreateGroup(new GroupInformation(), SyncFlags.CreateGroup, playerAndGroupContext, null);

			// add all players except the owner to this "other players" group
			foreach(int playerId in server.Players) {
				if(playerId != e.Message.PlayerID && playerId != serverId)
					server.AddPlayerToGroup(playerAndGroupContext.AllOtherPlayersGroupId, playerId, 0);
			}

			// notify host
			if(e.Message.PlayerID != hostingPlayerId) {
				using(NetworkPacket packet = new NetworkPacket(5)) {
					packet.Write((byte)ReservedMessageType.PlayerWantsToJoin);
					packet.Write(e.Message.PlayerID);
					server.SendTo(hostingPlayerId, packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh);
				}
			}
		}

		private void onPlayerDestroyed(object sender, PlayerDestroyedEventArgs e) {
			if(e.Message.PlayerID == hostingPlayerId) {
				// stop server
				lock(uncompressJobs) {
					uncompressJobs.AddFirst(UncompressJob.StopServer);
					uncompressJobsPending.Set();
				}
			} else {
				int otherPlayersGroupId = ((PlayerContext)e.Message.PlayerContext).AllOtherPlayersGroupId;

				// notify all other players
				using(NetworkPacket packet = new NetworkPacket(5)) {
					packet.Write((byte)ReservedMessageType.PlayerHasLeft);
					packet.Write(e.Message.PlayerID);
					server.SendTo(otherPlayersGroupId, packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh);
				}

				// remove the "other players" group owned by this player
				server.DestroyGroup(otherPlayersGroupId, 0);
			}
		}

		private void onReceive(object sender, ReceiveEventArgs e) {
			byte[] message = e.Message.ReceiveData.GetData();
			byte messageType = message[0];
			if(messageType >= (byte) ReservedMessageType.VideoFrame) {
				// videoconferencing message
				switch(messageType) {
					case (byte) ReservedMessageType.VideoFrame:
						handleVideoFrameMessage(e.Message.SenderID, message);
						break;

					case (byte) ReservedMessageType.VideoFrameAck:
						handleVideoFrameAckMessage(e.Message.SenderID, message);
						break;

					case (byte) ReservedMessageType.VideoCaptureDisabled:
						handleVideoCaptureDisabledMessage(e.Message.SenderID, (PlayerContext) e.Message.PlayerContext, message);
						break;

					case (byte) ReservedMessageType.VideoPlaybackToggled:
						handleVideoPlaybackToggledMessage(e.Message.SenderID, (PlayerContext) e.Message.PlayerContext, message);
						break;
				}
			} else {
				switch(messageType & 0xC0) {
					case 0xC0:
						// an unreliable message from client to all others
						int otherPlayersGroupId = ((PlayerContext)e.Message.PlayerContext).AllOtherPlayersGroupId;
						using(NetworkPacket packet = new NetworkPacket(message.Length + 4)) {
							// insert sender id in the message
							message[0] = (byte)((uint)e.Message.SenderID >> 24);
							packet.Write(((uint)e.Message.SenderID << 8) | (uint)messageType);
							packet.Write(message);
							server.SendTo(otherPlayersGroupId, packet, 1000, SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityHigh);
						}
						break;
					case 0x80:
						// a reliable message from client to all
						using(NetworkPacket packet = new NetworkPacket(message.Length + 4)) {
							// insert sender id in the message
							message[0] = (byte)((uint)e.Message.SenderID >> 24);
							packet.Write(((uint)e.Message.SenderID << 8) | (uint)messageType);
							packet.Write(message);
							server.SendTo((int)PlayerID.AllPlayers, packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh | SendFlags.NoLoopback);
						}
						break;
					case 0x40:
						// a reliable message from client to host
						using(NetworkPacket packet = new NetworkPacket(message.Length + 4)) {
							// insert sender id in the message
							message[0] = (byte)((uint)e.Message.SenderID >> 24);
							packet.Write(((uint)e.Message.SenderID << 8) | (uint)messageType);
							packet.Write(message);
							server.SendTo(hostingPlayerId, packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh);
						}
						break;
					default:	// 0x00
						// a reliable message from host to a single client (recipient defined using bytes 1-4)
						int recipientId = BitConverter.ToInt32(message, 1);
						using(NetworkPacket packet = new NetworkPacket(message.Length)) {
							// replace recipient id by sender id in the message
							uint senderId = (uint)e.Message.SenderID;
							message[1] = (byte)(senderId & 0x000000FF);
							message[2] = (byte)((senderId & 0x0000FF00) >> 8);
							message[3] = (byte)((senderId & 0x00FF0000) >> 16);
							message[4] = (byte)((senderId & 0xFF000000) >> 24);
							packet.Write(message);
							try {
								server.SendTo(recipientId, packet, 0, SendFlags.Guaranteed | SendFlags.Coalesce | SendFlags.PriorityHigh);
							} catch(Microsoft.DirectX.DirectPlay.InvalidPlayerException) { }
						}
						break;
				}
			}
			e.Message.ReceiveData.Dispose();
		}

		private void onGroupCreated(object sender, GroupCreatedEventArgs e) {
			PlayerContext playerContext = (PlayerContext) e.Message.GroupContext;
			int ownerId = playerContext.AllOtherPlayersGroupId;
			playerContext.AllOtherPlayersGroupId = e.Message.GroupID;
		}

		private void handleVideoFrameMessage(int senderId, byte[] message) {
			// a video frame is a low-priority unreliable message from client to all others
			// TODO: contextual sending to each player with playback enabled (except sender)

			lock(uncompressJobs) {
				uncompressJobs.AddFirst(new UncompressJob(senderId, message));
				uncompressJobsPending.Set();
			}
		}

		private void handleVideoFrameAckMessage(int recipientId, byte[] message) {
			// watch out: sender and recipient semantics are inverted!
			int senderId = BitConverter.ToInt32(message, 1);
			byte frameId = message[5];
			lock(outboundVideoFrameHistories) {
				Dictionary<int, OutboundVideoFrameHistory> senderHistories;
				if(!outboundVideoFrameHistories.TryGetValue(senderId, out senderHistories)) {
					senderHistories = new Dictionary<int, OutboundVideoFrameHistory>();
					outboundVideoFrameHistories.Add(senderId, senderHistories);
				}
				OutboundVideoFrameHistory history;
				if(!senderHistories.TryGetValue(recipientId, out history)) {
					history = new OutboundVideoFrameHistory();
					senderHistories.Add(recipientId, history);
				}
				history.ClearHistoryUntilThisFrame(frameId);
			}
		}

		private void handleVideoCaptureDisabledMessage(int senderId, PlayerContext playerContext, byte[] message) {
		}

		private void handleVideoPlaybackToggledMessage(int senderId, PlayerContext playerContext, byte[] message) {
		}

		private void processVideoFrames() {
			// process video frames until the hosting player leaves
			while(true) {
				// retrieve most recent uncompress job
				UncompressJob mostRecentUncompressJob = new UncompressJob(0, null);
				lock(uncompressJobs) {
					if(uncompressJobs.Count > 0) {
						mostRecentUncompressJob = uncompressJobs.First.Value;
						if(mostRecentUncompressJob.Message == UncompressJob.StopServer.Message) {
							// hosting player has left -> stop server
							if(natTraversalSession != null)
								natTraversalSession.Disable();
							return;
						}
						uncompressJobs.RemoveFirst();

						// remove less recent jobs from same sender
						LinkedListNode<UncompressJob> nextNode;
						for(LinkedListNode<UncompressJob> node = uncompressJobs.First; node != null; node = nextNode) {
							nextNode = node.Next;
							if(node.Value.SenderId == mostRecentUncompressJob.SenderId)
								uncompressJobs.Remove(node);
						}
					} else {
						uncompressJobsPending.Reset();
					}
				}

				if(mostRecentUncompressJob.Message != null) {
					uncompressFrame(mostRecentUncompressJob.SenderId, mostRecentUncompressJob.Message);
				} else {
					if(compressJobs.Count == 0) {
						// wait for next job
						uncompressJobsPending.WaitOne();
					} else {
						// compress
						CompressJob mostRecentCompressJob = compressJobs.First.Value;
						compressJobs.RemoveFirst();

						// remove less recent jobs with same sender/recipient pair
						LinkedListNode<CompressJob> nextNode;
						for(LinkedListNode<CompressJob> node = compressJobs.First; node != null; node = nextNode) {
							nextNode = node.Next;
							if(node.Value.SenderId == mostRecentCompressJob.SenderId &&
								node.Value.RecipientId == mostRecentCompressJob.RecipientId)
								compressJobs.Remove(node);
						}

						compressAndSendFrame(mostRecentCompressJob.SenderId, mostRecentCompressJob.RecipientId, mostRecentCompressJob.Frame);
					}
				}
			}
		}

		private void uncompressFrame(int senderId, byte[] messageData) {
			byte frameId = messageData[1];
			byte referenceFrameId = messageData[2];

			// send video frame reception notification
			using(NetworkPacket packet = new NetworkPacket(2)) {
				packet.Write((byte) ReservedMessageType.VideoFrameAck);
				packet.Write((int) senderId);
				packet.Write((byte) frameId);
				// use a timeout equal to 10 times the capture period (15 times per second)
				try {
					server.SendTo(senderId, packet, 10 * 1000 / 15, SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityHigh);
				} catch(Microsoft.DirectX.DirectPlay.InvalidPlayerException) { }
			}

			// uncompress video frame
			InboundVideoFrameHistory history;
			if(!inboundVideoFrameHistories.TryGetValue(senderId, out history)) {
				history = new InboundVideoFrameHistory();
				inboundVideoFrameHistories.Add(senderId, history);
			}
			byte[] frame = new byte[64 * 64 * 3];
			if(senderId == hostingPlayerId) {
				// no compression needed
				Array.Copy(messageData, 3, frame, 0, frame.Length);
			} else {
				unsafe {
					fixed(byte* framePtr = frame) {
						fixed(byte* dataPtr = messageData) {
							if(frameId == referenceFrameId) {
								// no reference frame
								videoCodec.Decode((IntPtr) (dataPtr + 3), (IntPtr) framePtr);
							} else {
								// reference frame
								fixed(byte* referenceFramePtr = history.GetFrameData(referenceFrameId)) {
									videoCodec.Decode((IntPtr) referenceFramePtr, (IntPtr) (dataPtr + 3), (IntPtr) framePtr);
								}
							}
						}
					}
				}
			}
			if(referenceFrameId != frameId)
				history.ClearHistoryUntilThisFrame(referenceFrameId);
			history.AddFrame(frameId, frame);

			// make one compress job for each other player
			foreach(int playerId in server.Players) {
				if(playerId != senderId && playerId != serverId)
					compressJobs.AddFirst(new CompressJob(senderId, playerId, frame));
			}
		}

		private void compressAndSendFrame(int senderId, int recipientId, byte[] frame) {
			byte oldestFrameId;
			byte[] oldestFrameData;
			lock(outboundVideoFrameHistories) {
				Dictionary<int, OutboundVideoFrameHistory> senderHistories;
				if(!outboundVideoFrameHistories.TryGetValue(senderId, out senderHistories)) {
					senderHistories = new Dictionary<int, OutboundVideoFrameHistory>();
					outboundVideoFrameHistories.Add(senderId, senderHistories);
				}
				OutboundVideoFrameHistory history;
				if(!senderHistories.TryGetValue(recipientId, out history)) {
					history = new OutboundVideoFrameHistory();
					senderHistories.Add(recipientId, history);
				}
				oldestFrameData = history.OldestFrameData;
				oldestFrameId = (oldestFrameData != null ? history.OldestFrameId : (byte) 0);
			}
			byte[] encodedData;
			byte frameId;
			if(recipientId == hostingPlayerId) {
				// no compression needed
				encodedData = frame;
				lock(outboundVideoFrameHistories) {
					frameId = outboundVideoFrameHistories[senderId][recipientId].AddFrame(frame);
				}
			} else {
				byte[] frameBuffer = (byte[]) frame.Clone();
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
				lock(outboundVideoFrameHistories) {
					frameId = outboundVideoFrameHistories[senderId][recipientId].AddFrame(frameBuffer);
				}
			}
			using(NetworkPacket packet = new NetworkPacket(encodedData.Length + 7)) {
				packet.Write((byte) ReservedMessageType.VideoFrame);
				packet.Write((int) senderId);
				packet.Write((byte) frameId);
				packet.Write((byte) (oldestFrameData != null ? oldestFrameId : frameId));
				packet.Write(encodedData);
				// use a timeout equal to 10 times the capture period (15 times per second)
				// TODO : use measured average time between sends
				try {
					server.SendTo(recipientId, packet, 10 * 1000 / 15, SendFlags.NoComplete | SendFlags.Coalesce | SendFlags.PriorityLow);
				} catch(Microsoft.DirectX.DirectPlay.InvalidPlayerException) { }
			}
		}

		private Microsoft.DirectX.DirectPlay.Server server = null;
		private volatile int serverId = 0;
		private volatile int hostingPlayerId = 0;

		private struct UncompressJob {
			public UncompressJob(int senderId, byte[] message) {
				SenderId = senderId;
				Message = message;
			}
			public int SenderId;
			public byte[] Message;

			public static UncompressJob StopServer = new UncompressJob(0, null);
		}
		private struct CompressJob {
			public CompressJob(int senderId, int recipientId, byte[] frame) {
				SenderId = senderId;
				RecipientId = recipientId;
				Frame = frame;
			}
			public int SenderId;
			public int RecipientId;
			public byte[] Frame;
		}
		private LinkedList<UncompressJob> uncompressJobs = new LinkedList<UncompressJob>();
		private LinkedList<CompressJob> compressJobs = new LinkedList<CompressJob>();
		private ManualResetEvent uncompressJobsPending = null;
		private IVideoCodec videoCodec = new ZtcVideoCodec();
		private Dictionary<int, InboundVideoFrameHistory> inboundVideoFrameHistories = new Dictionary<int, InboundVideoFrameHistory>();
		private Dictionary<int, Dictionary<int, OutboundVideoFrameHistory>> outboundVideoFrameHistories = new Dictionary<int, Dictionary<int, OutboundVideoFrameHistory>>();
		private INatTraversalSession natTraversalSession = null;
	}
}
