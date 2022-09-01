// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//
//  +------------+     +---------------+     +---------+
//  | Networking |<----+ Modelization  |<----+         |
//  +------------+     +---------------+     |         |     
//                             ^             |         |
//                             |             | Control |
//                             |             |         |
//  +------------+     +-------+-------+     |         |     
//  |  Graphics  |<----+ Visualization |<----+         |
//  +------------+     +---------------+     +---------+
// 

namespace ZunTzu.Networking {

	public enum NetworkStatus { Disconnected, Connecting, Connected }

	/// <summary>A network Message.</summary>
	/// <remarks>
	/// structure of a typical message:
	///    byte   0: RakNet message code (indicates transmission mode)
	///           1: ZunTzu message code (only for app-specific messages)
	///         2-9: Sender ID (or recipient ID in case of a message from host)
	///         10+: serialized message body (can be empty)
	/// 
	/// RakNet message codes:
	///    134: a system message (e.g. "connection failed", ...)
	///    135: a reliable message from host to a single client
	///    136: a reliable message from client to host
	///    137: a reliable message from client to all
	///    138: an unreliable message from client to all others
	///    ...
	/// </remarks>
	public struct NetworkMessage {
		internal NetworkMessage(Packet packet)
		{
			Packet = packet;
			Data = null;
		}

		internal NetworkMessage(byte[] data) {
			Packet = null;
			Data = data;
		}

		public readonly Packet Packet;
		public readonly byte[] Data;
    }

	public sealed class Packet : IDisposable
	{
		internal SystemAddress SenderAddress
		{
			get
			{
				unsafe
				{
					PacketData* pkt = (PacketData*)_internal.ToPointer();
					return pkt->systemAddress;
				}
			}
		}

		internal RakNetGuid SenderGuid
		{
			get
			{
				unsafe
				{
					PacketData* pkt = (PacketData*)_internal.ToPointer();
					return pkt->guid;
				}
			}
		}

		public IntPtr Data
		{
			get
			{
				unsafe
				{
					PacketData* pkt = (PacketData*)_internal.ToPointer();
					return pkt->data;
				}
			}
		}

		public int Length
		{
			get
			{
				unsafe
				{
					PacketData* pkt = (PacketData*)_internal.ToPointer();
					return (int)pkt->length;
				}
			}
		}

		public void Dispose()
		{
			if (_internal != IntPtr.Zero)
			{
				ZunTzuLib.DeallocatePacket(_peer, _internal);
				_internal = IntPtr.Zero;
			}
		}

		internal IntPtr _peer;
		internal IntPtr _internal;

		[StructLayout(LayoutKind.Sequential, Pack = 0)]
		struct PacketData
		{
			public SystemAddress systemAddress;
			public RakNetGuid guid;
			public UInt32 length;
			UInt32 bitSize;
			public IntPtr data;
			bool deleteData;
			bool wasGeneratedLocally;
		}
	}

	public enum ReservedMessageType : byte
	{
		ConnectionEstablished,
		ConnectionFailed,
		HostDisconnected,
		PlayerWantsToJoin,
		PlayerHasLeft,
		VoiceConnectionFailed,
		VoiceRecordingStarted,
		VoiceRecordingStopped,
		VoicePlaybackStarted,
		VoicePlaybackStopped,
		VideoFrameReceived,
	}

	public enum ConnectionFailureCause : byte {
		HostRejectedConnection,
		NoConnection,
		NotHost,
		SessionFull,
		TimeOut,
		Other
	}

	/// <summary>Component in charge of network communication.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public interface IClient : IDisposable {
		/// <summary>Network connection status.</summary>
		NetworkStatus Status { get; }
		/// <summary>Network id of this player.</summary>
		UInt64 PlayerId { get; }
		/// <summary>Connect to a server.</summary>
		/// <param name="serverName">IP address or hostname of the server.</param>
		/// <param name="serverPort">IP port on which the server is listening.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		void Connect(string serverName, int serverPort);
		/// <summary>Connect to a server.</summary>
		/// <param name="sessionId">ID of an existing game session.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		void Connect(string sessionId);
		/// <summary>Disconnect from server.</summary>
		void Disconnect();
		/// <summary>Send a message.</summary>
		/// <param name="message">Message content.</param>
		void Send(byte[] message);
		/// <summary>Send a message to a single client.</summary>
		/// <param name="recipientId">Player that will receive the message.</param>
		/// <param name="message">Message content.</param>
		/// <remarks>Use this method only with messages from host.</remarks>
		void Send(UInt64 recipientId, byte[] message);
		/// <summary>Send a video frame to all other players.</summary>
		/// <param name="frameBuffer">A 64x64 discrete cosine transformed frame.</param>
		void SendVideoFrame(byte[] frameBuffer);
		/// <summary>Get all pending messages received.</summary>
		/// <returns>An array of NetworkMessage instances.</returns>
		List<NetworkMessage> RetrieveNetworkMessages();
        /// <summary>Indicates that this player is transmitting a voice communication.</summary>
        bool IsRecording { get; }
		/// <summary>Retrieves statistics for the connection between this client and the host.</summary>
		string[] Statistics { get; }
	}

	/// <summary>Component in charge of relaying network communication between clients.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public interface IServer : IDisposable {
		/// <summary>Begin a new game as a host.</summary>
		/// <param name="port">IP port that will listen.</param>
		void Start(int port);
	}
}
