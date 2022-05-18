// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

// Host                         zuntzu.com
// ====                         ==========
//  | OPEN_NOTIFICATION_CHANNEL     |
//  +------------------------------>|
//  | ACK_OPEN_NOTIFICATION_CHANNEL |
//  |<------------------------------+
//  | Start DPlay Session           |
//  +--\                            |
//  |<-/                            |
//  | HOST (spoofed port)           |
//  +------------------------------>|
//  |                      ACK_HOST |
//  |<------------------------------+
// ...                             ...
//  | KEEP_ALIVE                    |
//  +------------------------------>|
//  |                               |                            Client
// ...                             ...                           ======
//  |                               |                       LOOKUP |
//  |                       CONNECT |<-----------------------------+
//  |<------------------------------+                              |
//  | Punch hole (spoofed port)     |               blocked by NAT |
//  +-------------------------------------------------------->X    |
//  | Retry with private IP         |               blocked by NAT |
//  +-------------------------------------------------------->X    |
//  | ACK_CONNECT                   |                              |
//  +------------------------------>| ACK_LOOKUP                   |
//  |                               +----------------------------->|
//  |                               |     Connect to DPlay Session |
//  |<-------------------------------------------------------------+
//  |                               |        Retry with private IP |
//  |<-------------------------------------------------------------+
//  | PLAYER_HAS_JOINED             |                              |
//  +------------------------------>|                              |
//  |                               |                              |
//                                 ...                            ...
//                                  |                         TEST |
//                                  |<-----------------------------+
//                                  | ACK_TEST                     |
//                                  +----------------------------->|
//                                  |                              |

namespace ZunTzu.Networking {

	/// <summary>A NAT traversal session allowing traversal of routers/firewalls to this host.</summary>
	internal interface INatTraversalSession : IDisposable {
		/// <summary>The unique id of this session.</summary>
		Guid SessionId { get; }
		/// <summary>True if NAT traversal is allowed.</summary>
		bool Enabled { get; }
		/// <summary>The public IP address of this host.</summary>
		/// <remarks>Irrelevant if Enabled is false.</remarks>
		string PublicIpAddress { get; }
		/// <summary>The public port of this host.</summary>
		/// <remarks>Irrelevant if Enabled is false. 0 if port is unknown.</remarks>
		int PublicPort { get; }
		/// <summary>Stops allowing traversal of routers/firewalls to this host.</summary>
		/// <remarks>This will stop this NAT traversal session.</remarks>
		void Disable();
	}

	internal class EnabledAddresses {
		/// <summary>The public IP address of the host.</summary>
		public string HostPublicAddress;
		/// <summary>The public port of the host.</summary>
		public int HostPublicPort;
		/// <summary>The private IP address of the host.</summary>
		public string HostPrivateAddress;
		/// <summary>The private port of the host.</summary>
		public int HostPrivatePort;
		/// <summary>The private IP address of the client.</summary>
		public string ClientPrivateAddress;
		/// <summary>The private port of the client.</summary>
		public int ClientPrivatePort;
	}

	/// <summary>Router/firewall traversal service</summary>
	internal static class NatResolver {

		private static string natTraversalServiceUrl = "www.zuntzu.com";
		private static int natTraversalServicePort = 1805;

		/// <summary>Allows traversal of routers/firewalls to this host.</summary>
		/// <param name="privatePort">The port the host is listening from.</param>
		/// <returns>If successful a INatTraversalSession is returned, otherwise null.</returns>
		/// <remarks>This method should be called by a host after having started a DirectPlay session.</remarks>
		public static INatTraversalSession EnableNatTraversal(int privatePort) {
			NatTraversalSession session = new NatTraversalSession(privatePort);
			using(session.NatTraversalEnabled = new ManualResetEvent(false)) {
				new Thread(new ParameterizedThreadStart(notificationLoop)).Start(session);
				session.NatTraversalEnabled.WaitOne(2200, false);
				session.NatTraversalEnabled = null;
			}
			return session;
		}

		/// <summary>Triggers NAT traversal for a given host.</summary>
		/// <param name="serverName">The public IP address of the host.</param>
		/// <param name="serverPort">The public port of the host.</param>
		/// <returns>If successful an instance of EnabledAddresses is returned, otherwise null.</returns>
		/// <remarks>This method should be called by a client before attempting to connect to the DirectPlay session.</remarks>
		public unsafe static EnabledAddresses TestNatTraversal(string serverName, int serverPort) {
			Regex ipAddressRegex = new Regex(@"^(?<1>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", RegexOptions.Singleline);
			Match ipAddressMatch = ipAddressRegex.Match(serverName);
			if(ipAddressMatch.Success) {
				IPAddress publicServerAddress = IPAddress.Parse(ipAddressMatch.Groups[1].Value);
				try {
					using(UdpClient socket = new UdpClient()) {
						socket.Client.ReceiveTimeout = 2000;
						socket.Connect(natTraversalServiceUrl, natTraversalServicePort);

						IPEndPoint localEndPoint = (IPEndPoint) socket.Client.LocalEndPoint;

						// send "lookup" message
						{
							MsgLookup msg = new MsgLookup(publicServerAddress, (ushort) serverPort, localEndPoint.Address, (ushort) localEndPoint.Port);
							byte[] datagram = new byte[sizeof(MsgLookup)];
							for(int i = 0; i < datagram.Length; ++i)
								datagram[i] = ((byte*) &msg)[i];
							socket.Send(datagram, datagram.Length);
						}

						// block until a message is received or 2 seconds have elapsed
						IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
						byte[] bytesReceived = socket.Receive(ref remoteEndPoint);

						// handle the message
						if(bytesReceived.Length >= 3 && bytesReceived[0] == 'z' && bytesReceived[1] == 't') {
							switch(bytesReceived[2]) {
								case (byte) MessageType.AckLookup:
									if(bytesReceived.Length == sizeof(MsgAckLookup)) {
										MsgAckLookup msg = new MsgAckLookup();
										for(int i = 0; i < sizeof(MsgAckLookup); ++i)
											((byte*) &msg)[i] = bytesReceived[i];

										// handle MsgAckLookup
										// report host's private address
										EnabledAddresses result = new EnabledAddresses();
										result.HostPublicAddress = new IPAddress((uint) IPAddress.HostToNetworkOrder((int) msg.HostPublicIp)).ToString();
										result.HostPublicPort = msg.HostPublicPort;
										result.HostPrivateAddress = new IPAddress((uint) IPAddress.HostToNetworkOrder((int) msg.HostPrivateIp)).ToString();
										result.HostPrivatePort = msg.HostPrivatePort;
										result.ClientPrivateAddress = ((IPEndPoint) socket.Client.LocalEndPoint).Address.ToString();
										result.ClientPrivatePort = ((IPEndPoint) socket.Client.LocalEndPoint).Port;
										return result;
									}
									break;

								case (byte) MessageType.NotFound:
									if(bytesReceived.Length == sizeof(MsgNotFound)) {
										MsgNotFound msg = new MsgNotFound();
										for(int i = 0; i < sizeof(MsgNotFound); ++i)
											((byte*) &msg)[i] = bytesReceived[i];

										// handle MsgNotFound
										// just exit and return null
									}
									break;
							}
						}
					}
				} catch { }
			}
			return null;
		}

		/// <summary>Notifies NAT traversal service that a player has joined (for statistical purpose).</summary>
		/// <param name="serverPrivatePort">The private port of the host.</param>
		/// <param name="clientIp">The IP address of the client.</param>
		/// <param name="clientPort">The port of the client.</param>
		public unsafe static void NotifyPlayerHasJoined(INatTraversalSession session, IPAddress clientIp, int clientPort) {
			try {
				// send "player_has_joined" message, from the same port as DirectPlay (spoofed)
				// connect to service to determine end points
				using(Socket dummySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
					dummySocket.Connect(natTraversalServiceUrl, natTraversalServicePort);
					IPEndPoint serviceEndPoint = (IPEndPoint) dummySocket.RemoteEndPoint;
					IPEndPoint localEndPoint = (IPEndPoint) dummySocket.LocalEndPoint;
					IPEndPoint spoofedEndPoint = new IPEndPoint(localEndPoint.Address, ((NatTraversalSession) session).PrivatePort);
					MsgPlayerHasJoined msgPlayerHasJoined = new MsgPlayerHasJoined(session.SessionId, clientIp, (ushort) clientPort);
					byte[] datagram = new byte[sizeof(MsgPlayerHasJoined)];
					for(int i = 0; i < datagram.Length; ++i)
						datagram[i] = ((byte*) &msgPlayerHasJoined)[i];
					PortSpoofing.Send(spoofedEndPoint, serviceEndPoint, datagram);
				}
			} catch { }
		}

		private class NatTraversalSession : INatTraversalSession {
			/// <summary>The unique id of this session.</summary>
			Guid INatTraversalSession.SessionId { get { return SessionId; } }
			/// <summary>True if NAT traversal is allowed.</summary>
			bool INatTraversalSession.Enabled { get { return Enabled; } }
			/// <summary>The public IP address of this host.</summary>
			/// <remarks>Irrelevant if Enabled is false.</remarks>
			string INatTraversalSession.PublicIpAddress { get { return PublicIpAddress; } }
			/// <summary>The public port of this host.</summary>
			/// <remarks>Irrelevant if Enabled is false. 0 if port is unknown.</remarks>
			int INatTraversalSession.PublicPort { get { return PublicPort; } }
			/// <summary>Stops allowing traversal of routers/firewalls to this host.</summary>
			/// <remarks>This will stop this NAT traversal session.</remarks>
			public void Disable() {
				if(!disabled) {
					// stop notification thread by sending a message from this local endpoint to itself
					byte[] emptyDatagram = new byte[0];
					PortSpoofing.Send(LocalEndPoint, LocalEndPoint, emptyDatagram);
					disabled = true;
				}
			}
			public NatTraversalSession(int privatePort) { PrivatePort = privatePort; }
			public void Dispose() { Disable(); }

			public ManualResetEvent NatTraversalEnabled = null;
			public Guid SessionId = Guid.NewGuid();
			public bool Enabled = false;
			public int PrivatePort;
			public string PublicIpAddress;
			public int PublicPort;
			public IPEndPoint LocalEndPoint;

			private bool disabled = false;
		}

		private static unsafe void notificationLoop(object natTraversalSession) {
			NatTraversalSession session = (NatTraversalSession) natTraversalSession;
			Timer keepAliveTimer = null;

			try {
				IPEndPoint serviceEndPoint = null;

				// connect to service to open a port on the NAT devices
				using(Socket notificationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
					notificationSocket.Connect(natTraversalServiceUrl, natTraversalServicePort);
					serviceEndPoint = (IPEndPoint) notificationSocket.RemoteEndPoint;
					session.LocalEndPoint = (IPEndPoint) notificationSocket.LocalEndPoint;
				}

				// create a new socket in listening mode using the port just opened
				using(Socket notificationSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
					notificationSocket.ReceiveTimeout = 1000;
					notificationSocket.Bind(session.LocalEndPoint);

					// send "open notification channel" message
					{
						MsgOpenNotificationChannel msg = new MsgOpenNotificationChannel(session.SessionId);
						byte[] datagram = new byte[sizeof(MsgOpenNotificationChannel)];
						for(int i = 0; i < datagram.Length; ++i)
							datagram[i] = ((byte*) &msg)[i];
						notificationSocket.SendTo(datagram, serviceEndPoint);
					}

					while(true) {
						// block until a message is received or 1 second has elapsed
						EndPoint any = new IPEndPoint(IPAddress.Any, 0);
						byte[] bytesReceived = new byte[1024];
						int bytesReceivedLength = notificationSocket.ReceiveFrom(bytesReceived, ref any);
						IPEndPoint remoteEndPoint = (IPEndPoint) any;

						// test for stop condition: a message send from this local endpoint to itself
						if(remoteEndPoint.Equals(session.LocalEndPoint)) {
							break;
						} else if(remoteEndPoint.Equals(serviceEndPoint)) {
							// handle the message
							if(bytesReceivedLength >= 3 && bytesReceived[0] == 'z' && bytesReceived[1] == 't') {
								switch(bytesReceived[2]) {
									case (byte) MessageType.AckOpenNotificationChannel:
										if(bytesReceivedLength == sizeof(MsgAckOpenNotificationChannel)) {
											MsgAckOpenNotificationChannel msg = new MsgAckOpenNotificationChannel();
											for(int i = 0; i < sizeof(MsgAckOpenNotificationChannel); ++i)
												((byte*) &msg)[i] = bytesReceived[i];

											// handle MsgAckOpenNotificationChannel
											// send "host" message, from the same port as DirectPlay (spoofed)
											IPEndPoint spoofedEndPoint = new IPEndPoint(session.LocalEndPoint.Address, session.PrivatePort);
											MsgHost msgHost = new MsgHost(session.SessionId, session.LocalEndPoint.Address, (ushort) session.PrivatePort);
											byte[] datagram = new byte[sizeof(MsgHost)];
											for(int i = 0; i < datagram.Length; ++i)
												datagram[i] = ((byte*) &msgHost)[i];
											PortSpoofing.Send(spoofedEndPoint, serviceEndPoint, datagram);
										}
										break;

									case (byte) MessageType.AckHost:
										if(bytesReceivedLength == sizeof(MsgAckHost)) {
											if(keepAliveTimer == null) {	// ignore if already handled
												MsgAckHost msg = new MsgAckHost();
												for(int i = 0; i < sizeof(MsgAckHost); ++i)
													((byte*) &msg)[i] = bytesReceived[i];

												// handle MsgAckHost
												// record public address/port
												session.PublicIpAddress = new IPAddress((uint) IPAddress.HostToNetworkOrder((int) msg.HostPublicIp)).ToString();
												session.PublicPort = msg.HostPublicPort;

												// notify main thread that NAT traversal is enabled
												session.Enabled = true;
												if(session.NatTraversalEnabled != null)
													session.NatTraversalEnabled.Set();

												// set reception so it can't time-out
												notificationSocket.ReceiveTimeout = 0;

												// send a keep alive message every 19 seconds
												KeepAliveSettings settings = new KeepAliveSettings();
												settings.SpoofedLocalEndPoint = session.LocalEndPoint;
												settings.ServiceEndPoint = serviceEndPoint;
												MsgKeepAlive msgKeepAlive = new MsgKeepAlive(session.SessionId);
												settings.Datagram = new byte[sizeof(MsgKeepAlive)];
												for(int i = 0; i < settings.Datagram.Length; ++i)
													settings.Datagram[i] = ((byte*) &msgKeepAlive)[i];

												keepAliveTimer = new Timer(new TimerCallback(keepAlive), settings, 19000, 19000);
											}
										}
										break;

									case (byte) MessageType.Connect:
										if(bytesReceivedLength == sizeof(MsgConnect)) {
											MsgConnect msg = new MsgConnect();
											for(int i = 0; i < sizeof(MsgConnect); ++i)
												((byte*) &msg)[i] = bytesReceived[i];

											// handle MsgConnect
											// punch hole targeting client's public and private addresses
											IPAddress publicClientAddress = new IPAddress((uint) IPAddress.HostToNetworkOrder((int) msg.ClientPublicIp));
											IPAddress privateClientAddress = new IPAddress((uint) IPAddress.HostToNetworkOrder((int) msg.ClientPrivateIp));

											IPEndPoint spoofedEndPoint = new IPEndPoint(session.LocalEndPoint.Address, session.PrivatePort);
											byte[] emptyDatagram = new byte[0];

											PortSpoofing.Send(spoofedEndPoint, new IPEndPoint(publicClientAddress, msg.ClientPublicPort), emptyDatagram);
											PortSpoofing.Send(spoofedEndPoint, new IPEndPoint(privateClientAddress, msg.ClientPrivatePort), emptyDatagram);

											// send acknowledgement through notification channel
											MsgAckConnect msgAckConnect = new MsgAckConnect(session.SessionId, publicClientAddress, msg.ClientPublicPort);
											byte[] datagram = new byte[sizeof(MsgAckConnect)];
											for(int i = 0; i < datagram.Length; ++i)
												datagram[i] = ((byte*) &msgAckConnect)[i];
											notificationSocket.SendTo(datagram, serviceEndPoint);
										}
										break;
								}
							}
						}
					}
				}
			} catch {
				if(!session.Enabled && session.NatTraversalEnabled != null)
					session.NatTraversalEnabled.Set();
			} finally {
				if(keepAliveTimer != null) {
					keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
					keepAliveTimer.Dispose();
				}
			}
		}

		private class KeepAliveSettings {
			public IPEndPoint SpoofedLocalEndPoint;
			public IPEndPoint ServiceEndPoint;
			public byte[] Datagram;
		}

		private static void keepAlive(object state) {
			KeepAliveSettings settings = (KeepAliveSettings) state;
			PortSpoofing.Send(settings.SpoofedLocalEndPoint, settings.ServiceEndPoint, settings.Datagram);
		}

		private enum MessageType : byte {
			OpenNotificationChannel,
			AckOpenNotificationChannel,
			Host,
			AckHost,
			KeepAlive,
			Lookup,
			NotFound,
			Connect,
			AckConnect,
			AckLookup,
			PlayerHasJoined
		};

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgOpenNotificationChannel {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;

			public MsgOpenNotificationChannel(Guid id) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.OpenNotificationChannel;
				Id = id;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgAckOpenNotificationChannel {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgHost {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
			public uint HostPrivateIp;	// network byte order
			public ushort HostPrivatePort;

			public MsgHost(Guid id, IPAddress hostPrivateIp, ushort hostPrivatePort) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.Host;
				Id = id;
				HostPrivateIp = (uint) IPAddress.NetworkToHostOrder((int) hostPrivateIp.Address);
				HostPrivatePort = hostPrivatePort;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgAckHost {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
			public uint HostPublicIp;
			public ushort HostPublicPort;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgKeepAlive {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;

			public MsgKeepAlive(Guid id) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.KeepAlive;
				Id = id;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgLookup {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public uint HostPublicIp;
			public uint ClientPrivateIp;
			public ushort HostPublicPort;
			public ushort ClientPrivatePort;

			public MsgLookup(IPAddress hostPublicIp, ushort hostPublicPort, IPAddress clientPrivateIp, ushort clientPrivatePort) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.Lookup;
				HostPublicIp = (uint) IPAddress.NetworkToHostOrder((int) hostPublicIp.Address);
				HostPublicPort = hostPublicPort;
				ClientPrivateIp = (uint) IPAddress.NetworkToHostOrder((int) clientPrivateIp.Address);
				ClientPrivatePort = clientPrivatePort;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgConnect {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
			public uint ClientPublicIp;
			public uint ClientPrivateIp;
			public ushort ClientPublicPort;
			public ushort ClientPrivatePort;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgAckConnect {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
			public uint ClientPublicIp;
			public ushort ClientPublicPort;

			public MsgAckConnect(Guid id, IPAddress clientPublicIp, ushort clientPublicPort) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.AckConnect;
				Id = id;
				ClientPublicIp = (uint) IPAddress.NetworkToHostOrder((int) clientPublicIp.Address);
				ClientPublicPort = clientPublicPort;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgAckLookup {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public uint HostPublicIp;
			public uint HostPrivateIp;
			public ushort HostPublicPort;
			public ushort HostPrivatePort;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgNotFound {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public uint HostPublicIp;
			public ushort HostPublicPort;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct MsgPlayerHasJoined {
			public byte MagicNumber0;
			public byte MagicNumber1;
			public MessageType MsgType;
			public Guid Id;
			public uint ClientPublicIp;
			public ushort ClientPublicPort;

			public MsgPlayerHasJoined(Guid id, IPAddress clientPublicIp, ushort clientPublicPort) {
				MagicNumber0 = (byte) 'z';
				MagicNumber1 = (byte) 't';
				MsgType = MessageType.PlayerHasJoined;
				Id = id;
				ClientPublicIp = (uint) IPAddress.NetworkToHostOrder((int) clientPublicIp.Address);
				ClientPublicPort = clientPublicPort;
			}
		}
	}
}
