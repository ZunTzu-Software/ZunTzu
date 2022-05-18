// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
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

	public delegate void MessageHandler(byte messageType, int senderId, byte[] message);
	public delegate void PlayerRelatedNetworkEventHandler(int playerId);
	public delegate void NetworkEventHandler();

	/// <summary>A network Message.</summary>
	/// <remarks>
	/// structure of the message byte code:
	/// bits 0-1: transmission mode
	///    00: a reliable message from host to a single client
	///    01: a reliable message from client to host
	///    10: a reliable message from client to all
	///    11: an unreliable message from client to all others
	/// bits 2-7: unique application-defined message code (64 combinations for each category)
	///
	/// reserved message codes:
	/// 0x00 : connection established (no data)
	/// 0x01 : host disconnected (no data)
	/// 0x02 : player wants to join (no data)
	/// 0x03 : player has left (no data)
    /// 0x04 : voice recording started (no data)
    /// 0x05 : voice recording stopped (no data)
    /// 0x06 : voice playback started (no data)
    /// 0x07 : voice playback stopped (no data)
	/// 0x08 : video frame received
	/// 0xfc : video frame received, internal
	/// 0xfd : video frame ack
	/// 0xfe : video captured disabled (no data)
	/// 0xff : video playback enabled/disabled (one byte data)
	/// </remarks>
	public struct NetworkMessage {
		internal NetworkMessage(int senderId, byte type, byte[] data) {
			SenderId = senderId;
			Type = type;
			Data = data;
		}
		public readonly byte Type;
		public readonly int SenderId;
		public readonly byte[] Data;
	}

	public enum ReservedMessageType : byte {
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

		VideoFrame = 0xfc,
		VideoFrameAck,
		VideoCaptureDisabled,
		VideoPlaybackToggled
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
		int PlayerId { get; }
		/// <summary>Connect to a server.</summary>
		/// <param name="serverName">IP address or hostname of the server.</param>
		/// <param name="serverPort">IP port on which the server is listening.</param>
		/// <remarks>The first client to connect becomes the hosting player.</remarks>
		void Connect(string serverName, int serverPort);
		/// <summary>Disconnect from server.</summary>
		void Disconnect();
		/// <summary>Send a message.</summary>
		/// <param name="messageType">Byte code that indicates the type of message.</param>
		/// <param name="message">Message content.</param>
		void Send(byte messageType, byte[] message);
		/// <summary>Send a message to a single client.</summary>
		/// <param name="recipientId">Player that will receive the message.</param>
		/// <param name="messageType">Byte code that indicates the type of message.</param>
		/// <param name="message">Message content.</param>
		/// <remarks>Use this method only with messages from host (zero in bits 0-1).</remarks>
		void Send(int recipientId, byte messageType, byte[] message);
		/// <summary>Send a video frame to all other players.</summary>
		/// <param name="frameBuffer">A 64x64 discrete cosine transformed frame.</param>
		void SendVideoFrame(byte[] frameBuffer);
		/// <summary>Get all pending messages received.</summary>
		/// <returns>An array of NetworkMessage instances.</returns>
		NetworkMessage[] RetrieveNetworkMessages();
        /// <summary>Indicates that this player is transmitting a voice communication.</summary>
        bool IsRecording { get; }
		/// <summary>The IP address of this computer as seen from the Internet.</summary>
		//string PublicIpAddress { get; }
		/// <summary>Retrieves statistics for the connection between this client and the host.</summary>
		string[] Statistics { get; }
	}

	public enum InternetConnectivity { Unknown, None, NoEgress, NoIngress, Full }

	/// <summary>Component in charge of relaying network communication between clients.</summary>
	/// <remarks>The topology used is client/server.</remarks>
	public interface IServer : IDisposable {
		/// <summary>Begin a new game as a host.</summary>
		/// <param name="port">IP port that will listen.</param>
		void Start(int port);
	}
}
