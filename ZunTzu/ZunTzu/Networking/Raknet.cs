// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ZunTzu.Networking
{
	sealed class Peer : IDisposable
	{
		public static Peer Create()
		{
			IntPtr peer = ZunTzuLib.CreatePeer();
			return new Peer { _internal = peer };
		}

		public StartupResult StartupClient(UInt16 port)
		{
			return (StartupResult)ZunTzuLib.StartupClient(_internal, port);
		}

		public StartupResult StartupServer(UInt16 port)
		{
			return (StartupResult)ZunTzuLib.StartupServer(_internal, port);
		}

		public void Shutdown()
		{
			ZunTzuLib.Shutdown(_internal);
		}

		public ConnectionAttemptResult Connect(string host, UInt16 remotePort)
		{
			return (ConnectionAttemptResult)ZunTzuLib.Connect(_internal, host, remotePort);
		}

		public int Send(
			byte[] data,
			PacketPriority priority, 
			PacketReliability reliability, 
			int orderingChannel, 
			AddressOrGuid recipient, 
			bool broadcast)
		{
			unsafe
			{
				fixed (byte* ptr = data)
				{
					return (int)ZunTzuLib.Send(
						_internal, 
						ptr, 
						data.Length, 
						(int)priority, 
						(int)reliability, 
						orderingChannel, 
						new IntPtr(&recipient), 
						broadcast);
				}
			}
		}

		public unsafe int Send(
			byte* data,
			int length,
			PacketPriority priority,
			PacketReliability reliability,
			int orderingChannel,
			AddressOrGuid recipient,
			bool broadcast)
		{
			return (int)ZunTzuLib.Send(
				_internal,
				data,
				length,
				(int)priority,
				(int)reliability,
				orderingChannel,
				new IntPtr(&recipient),
				broadcast);
		}

		public Packet Receive()
		{
			IntPtr pkt = ZunTzuLib.Receive(_internal);
			return (pkt == IntPtr.Zero ? null : new Packet { _peer = _internal, _internal = pkt });
		}

		public UInt64 Guid => ZunTzuLib.GetGuid(_internal);

		public UInt32 BoundAddress => ZunTzuLib.GetBoundAddress(_internal);

		public void Dispose()
		{
			if (_internal != IntPtr.Zero)
			{
				ZunTzuLib.FreePeer(_internal);
				_internal = IntPtr.Zero;
			}
		}

		IntPtr _internal;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	struct SystemAddress : IEquatable<SystemAddress>
	{
		UInt16 sin_family;
		UInt16 sin_port;
		public UInt32 sin_addr;
		UInt64 sin_zero;
		UInt16 debugPort;
		UInt16 systemIndex;

		public static SystemAddress UNASSIGNED_SYSTEM_ADDRESS = new SystemAddress
		{
			sin_family = 2, // AF_INET
			sin_port = 0,
			sin_addr = 0,
			sin_zero = 0,
			debugPort = 0,
			systemIndex = UInt16.MaxValue,
		};

		public static bool operator ==(SystemAddress x, SystemAddress y)
		{
			return (x.sin_addr == y.sin_addr &&
				x.sin_port == y.sin_port &&
				x.sin_family == y.sin_family);
		}

		public static bool operator !=(SystemAddress x, SystemAddress y)
		{
			return !(x == y);
		}

		public bool Equals(SystemAddress other)
		{
			return (this == other);
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(SystemAddress)) return false;
			return Equals((SystemAddress)obj);
		}

		public override int GetHashCode()
		{
			unchecked // from Effective Java by Josh Bloch
			{
				int hash = 17;
				hash = hash * 23 + sin_addr.GetHashCode();
				hash = hash * 23 + sin_port.GetHashCode();
				hash = hash * 23 + sin_family.GetHashCode();
				return hash;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	struct RakNetGuid : IEquatable<RakNetGuid>
	{
		public UInt64 g;
		UInt16 systemIndex;

		public static RakNetGuid UNASSIGNED_RAKNET_GUID = new RakNetGuid
		{ 
			g = UInt64.MaxValue, 
			systemIndex = UInt16.MaxValue
		};

		public static bool operator ==(RakNetGuid x, RakNetGuid y)
		{
			return x.g == y.g;
		}

		public static bool operator !=(RakNetGuid x, RakNetGuid y)
		{
			return !(x == y);
		}

		public bool Equals(RakNetGuid other)
		{
			return (this == other);
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(RakNetGuid)) return false;
			return Equals((RakNetGuid)obj);
		}

		public override int GetHashCode()
		{
			return g.GetHashCode();
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	struct AddressOrGuid : IEquatable<AddressOrGuid>
	{
		public RakNetGuid rakNetGuid;
		public SystemAddress systemAddress;

		public static AddressOrGuid UNASSIGNED = new AddressOrGuid
		{
			rakNetGuid = RakNetGuid.UNASSIGNED_RAKNET_GUID,
			systemAddress = SystemAddress.UNASSIGNED_SYSTEM_ADDRESS,
		};

		public static bool operator ==(AddressOrGuid x, AddressOrGuid y)
		{
			return (x.rakNetGuid != RakNetGuid.UNASSIGNED_RAKNET_GUID && x.rakNetGuid == y.rakNetGuid) || 
				(x.systemAddress != SystemAddress.UNASSIGNED_SYSTEM_ADDRESS && x.systemAddress == y.systemAddress);
		}

		public static bool operator !=(AddressOrGuid x, AddressOrGuid y)
		{
			return !(x == y);
		}

		public bool Equals(AddressOrGuid other)
		{
			return (this == other);
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(AddressOrGuid)) return false;
			return Equals((AddressOrGuid)obj);
		}

		public override int GetHashCode()
		{
			if (rakNetGuid != RakNetGuid.UNASSIGNED_RAKNET_GUID) return rakNetGuid.GetHashCode();
			return systemAddress.GetHashCode();
		}
	}

	enum StartupResult
	{
		RAKNET_STARTED,
		RAKNET_ALREADY_STARTED,
		INVALID_SOCKET_DESCRIPTORS,
		INVALID_MAX_CONNECTIONS,
		SOCKET_FAMILY_NOT_SUPPORTED,
		SOCKET_PORT_ALREADY_IN_USE,
		SOCKET_FAILED_TO_BIND,
		SOCKET_FAILED_TEST_SEND,
		PORT_CANNOT_BE_ZERO,
		FAILED_TO_CREATE_NETWORK_THREAD,
		COULD_NOT_GENERATE_GUID,
		STARTUP_OTHER_FAILURE
	}

	enum ConnectionAttemptResult
	{
		CONNECTION_ATTEMPT_STARTED,
		INVALID_PARAMETER,
		CANNOT_RESOLVE_DOMAIN_NAME,
		ALREADY_CONNECTED_TO_ENDPOINT,
		CONNECTION_ATTEMPT_ALREADY_IN_PROGRESS,
		SECURITY_INITIALIZATION_FAILED,
	};

	enum PacketPriority
	{
		IMMEDIATE_PRIORITY,
		HIGH_PRIORITY,
		MEDIUM_PRIORITY,
		LOW_PRIORITY,
	}

	enum PacketReliability
	{
		UNRELIABLE,
		UNRELIABLE_SEQUENCED,
		RELIABLE,
		RELIABLE_ORDERED,
		RELIABLE_SEQUENCED,
		UNRELIABLE_WITH_ACK_RECEIPT,
		RELIABLE_WITH_ACK_RECEIPT,
		RELIABLE_ORDERED_WITH_ACK_RECEIPT,
	}

	enum OrderingChannel
	{
		Reliable,
		Unreliable,
		Video,
		Voice,
	}

	enum MessageId
	{
		ID_CONNECTED_PING,
		ID_UNCONNECTED_PING,
		ID_UNCONNECTED_PING_OPEN_CONNECTIONS,
		ID_CONNECTED_PONG,
		ID_DETECT_LOST_CONNECTIONS,
		ID_OPEN_CONNECTION_REQUEST_1,
		ID_OPEN_CONNECTION_REPLY_1,
		ID_OPEN_CONNECTION_REQUEST_2,
		ID_OPEN_CONNECTION_REPLY_2,
		ID_CONNECTION_REQUEST,
		ID_REMOTE_SYSTEM_REQUIRES_PUBLIC_KEY,
		ID_OUR_SYSTEM_REQUIRES_SECURITY,
		ID_PUBLIC_KEY_MISMATCH,
		ID_OUT_OF_BAND_INTERNAL,
		ID_SND_RECEIPT_ACKED,
		ID_SND_RECEIPT_LOSS,

		ID_CONNECTION_REQUEST_ACCEPTED,
		ID_CONNECTION_ATTEMPT_FAILED,
		ID_ALREADY_CONNECTED,
		ID_NEW_INCOMING_CONNECTION,
		ID_NO_FREE_INCOMING_CONNECTIONS,
		ID_DISCONNECTION_NOTIFICATION,
		ID_CONNECTION_LOST,
		ID_CONNECTION_BANNED,
		ID_INVALID_PASSWORD,
		ID_INCOMPATIBLE_PROTOCOL_VERSION,
		ID_IP_RECENTLY_CONNECTED,
		ID_TIMESTAMP,
		ID_UNCONNECTED_PONG,
		ID_ADVERTISE_SYSTEM,
		ID_DOWNLOAD_PROGRESS,

		ID_REMOTE_DISCONNECTION_NOTIFICATION,
		ID_REMOTE_CONNECTION_LOST,
		ID_REMOTE_NEW_INCOMING_CONNECTION,

		ID_FILE_LIST_TRANSFER_HEADER,
		ID_FILE_LIST_TRANSFER_FILE,
		ID_FILE_LIST_REFERENCE_PUSH_ACK,

		ID_DDT_DOWNLOAD_REQUEST,

		ID_TRANSPORT_STRING,

		ID_REPLICA_MANAGER_CONSTRUCTION,
		ID_REPLICA_MANAGER_SCOPE_CHANGE,
		ID_REPLICA_MANAGER_SERIALIZE,
		ID_REPLICA_MANAGER_DOWNLOAD_STARTED,
		ID_REPLICA_MANAGER_DOWNLOAD_COMPLETE,

		ID_RAKVOICE_OPEN_CHANNEL_REQUEST,
		ID_RAKVOICE_OPEN_CHANNEL_REPLY,
		ID_RAKVOICE_CLOSE_CHANNEL,
		ID_RAKVOICE_DATA,

		ID_AUTOPATCHER_GET_CHANGELIST_SINCE_DATE,
		ID_AUTOPATCHER_CREATION_LIST,
		ID_AUTOPATCHER_DELETION_LIST,
		ID_AUTOPATCHER_GET_PATCH,
		ID_AUTOPATCHER_PATCH_LIST,
		ID_AUTOPATCHER_REPOSITORY_FATAL_ERROR,
		ID_AUTOPATCHER_CANNOT_DOWNLOAD_ORIGINAL_UNMODIFIED_FILES,
		ID_AUTOPATCHER_FINISHED_INTERNAL,
		ID_AUTOPATCHER_FINISHED,
		ID_AUTOPATCHER_RESTART_APPLICATION,

		ID_NAT_PUNCHTHROUGH_REQUEST,
		ID_NAT_CONNECT_AT_TIME,
		ID_NAT_GET_MOST_RECENT_PORT,
		ID_NAT_CLIENT_READY,

		ID_NAT_TARGET_NOT_CONNECTED,
		ID_NAT_TARGET_UNRESPONSIVE,
		ID_NAT_CONNECTION_TO_TARGET_LOST,
		ID_NAT_ALREADY_IN_PROGRESS,
		ID_NAT_PUNCHTHROUGH_FAILED,
		ID_NAT_PUNCHTHROUGH_SUCCEEDED,

		ID_READY_EVENT_SET,
		ID_READY_EVENT_UNSET,
		ID_READY_EVENT_ALL_SET,
		ID_READY_EVENT_QUERY,

		ID_LOBBY_GENERAL,

		ID_RPC_REMOTE_ERROR,
		ID_RPC_PLUGIN,

		ID_FILE_LIST_REFERENCE_PUSH,
		ID_READY_EVENT_FORCE_ALL_SET,

		ID_ROOMS_EXECUTE_FUNC,
		ID_ROOMS_LOGON_STATUS,
		ID_ROOMS_HANDLE_CHANGE,

		ID_LOBBY2_SEND_MESSAGE,
		ID_LOBBY2_SERVER_ERROR,

		ID_FCM2_NEW_HOST,
		ID_FCM2_REQUEST_FCMGUID,
		ID_FCM2_RESPOND_CONNECTION_COUNT,
		ID_FCM2_INFORM_FCMGUID,
		ID_FCM2_UPDATE_MIN_TOTAL_CONNECTION_COUNT,
		ID_FCM2_VERIFIED_JOIN_START,
		ID_FCM2_VERIFIED_JOIN_CAPABLE,
		ID_FCM2_VERIFIED_JOIN_FAILED,
		ID_FCM2_VERIFIED_JOIN_ACCEPTED,
		ID_FCM2_VERIFIED_JOIN_REJECTED,

		ID_UDP_PROXY_GENERAL,

		ID_SQLite3_EXEC,
		ID_SQLite3_UNKNOWN_DB,
		ID_SQLLITE_LOGGER,

		ID_NAT_TYPE_DETECTION_REQUEST,
		ID_NAT_TYPE_DETECTION_RESULT,

		ID_ROUTER_2_INTERNAL,
		ID_ROUTER_2_FORWARDING_NO_PATH,
		ID_ROUTER_2_FORWARDING_ESTABLISHED,
		ID_ROUTER_2_REROUTED,

		ID_TEAM_BALANCER_INTERNAL,
		ID_TEAM_BALANCER_REQUESTED_TEAM_FULL,
		ID_TEAM_BALANCER_REQUESTED_TEAM_LOCKED,
		ID_TEAM_BALANCER_TEAM_REQUESTED_CANCELLED,
		ID_TEAM_BALANCER_TEAM_ASSIGNED,

		ID_LIGHTSPEED_INTEGRATION,

		ID_XBOX_LOBBY,

		ID_TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_SUCCESS,
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_SUCCESS,
		ID_TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_FAILURE,
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_FAILURE,
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_TIMEOUT,
		ID_TWO_WAY_AUTHENTICATION_NEGOTIATION,

		ID_CLOUD_POST_REQUEST,
		ID_CLOUD_RELEASE_REQUEST,
		ID_CLOUD_GET_REQUEST,
		ID_CLOUD_GET_RESPONSE,
		ID_CLOUD_UNSUBSCRIBE_REQUEST,
		ID_CLOUD_SERVER_TO_SERVER_COMMAND,
		ID_CLOUD_SUBSCRIPTION_NOTIFICATION,

		ID_LIB_VOICE,

		ID_RELAY_PLUGIN,
		ID_NAT_REQUEST_BOUND_ADDRESSES,
		ID_NAT_RESPOND_BOUND_ADDRESSES,
		ID_FCM2_UPDATE_USER_CONTEXT,
		ID_RESERVED_3,
		ID_RESERVED_4,
		ID_RESERVED_5,
		ID_RESERVED_6,
		ID_RESERVED_7,
		ID_RESERVED_8,
		ID_RESERVED_9,

		ID_USER_PACKET_ENUM,

		// user-defined messages

		SystemMessage = ID_USER_PACKET_ENUM,
		ReliableMessageFromHostToSingleClient,
		ReliableMessageFromClientToHost,
		ReliableMessageFromClientToAll,
		UnreliableMessageFromClientToAllOthers,
		VideoFrame,
		VideoFrameAck,
		VideoCaptureDisabled,
		VideoPlaybackToggled,
	}
}