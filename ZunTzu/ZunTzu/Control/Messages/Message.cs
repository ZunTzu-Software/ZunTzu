// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Networking;

// Connection sequence:

//                                         Server                               Host
//                                           |                         (connect) |
//                                           |<----------------------------------+
//                                           | ConnectionEstablished             |
//     Client                                +---------------------------------->|
//       | (connect)                         |                                   |
//       +---------------------------------->|                                   |
//       |             ConnectionEstablished | PlayerWantsToJoin                 |
//       |<----------------------------------+---------------------------------->|
//       |                                   |                ConnectionAccepted |
//       |                ConnectionAccepted |<----------------------------------+
//       |<----------------------------------+                                   |
//       | PlayerHasJoined                   |                                   |
//       +---------------------------------->| PlayerHasJoined                   |
//       |                                   +---------------------------------->|
//       |                                   |                                   |

//       | (disconnect)                      |                                   |
//       +---------------------------------->| PlayerHasLeft                     |
//       X                                   +---------------------------------->|
//                                           |                                   |

//                                           |                      (disconnect) |
//                                           |<----------------------------------+
//                                           X (quit)                            X

// Game state change sequence:

//     Client                              Server                               Host
//       | <change>                          |                                   |
//       +---------------------------------->| <change>                          |
//       |                                   +---------------------------------->|
//       |                                   |          ChangeAccepted(<change>) |
//       |                                   |<----------------------------------+
//       |          ChangeAccepted(<change>) | ChangeAccepted(<change>)          |
//       |<----------------------------------+---------------------------------->|
//       |                                   |                                   |

namespace ZunTzu.Control {

	// structure of the message byte code:
	// bits 0-1: transmission mode
	//    00: a reliable message from host to a single client (recipient defined using bytes 1-4)
	//    01: a reliable message from client to host
	//    10: a reliable message from client to all
	//    11: an unreliable message from client to all others
	// bits 2-7: unique application-defined message code (64 combinations for each category)

	[ObfuscationAttribute(Exclude = true)]
	public enum NetworkMessageType : byte {

		// reserved system messages

		ConnectionEstablished = ReservedMessageType.ConnectionEstablished,
		ConnectionFailed = ReservedMessageType.ConnectionFailed,
		HostDisconnected = ReservedMessageType.HostDisconnected,
		PlayerWantsToJoin = ReservedMessageType.PlayerWantsToJoin,
		PlayerHasLeft = ReservedMessageType.PlayerHasLeft,
		VoiceConnectionFailed = ReservedMessageType.VoiceConnectionFailed,
		VoiceRecordingStarted = ReservedMessageType.VoiceRecordingStarted,
		VoiceRecordingStopped = ReservedMessageType.VoiceRecordingStopped,
		VoicePlaybackStarted = ReservedMessageType.VoicePlaybackStarted,
		VoicePlaybackStopped = ReservedMessageType.VoicePlaybackStopped,
		VideoFrameReceived = ReservedMessageType.VideoFrameReceived,

		// reliable messages from host to single client

		ConnectionAccepted,	// binary: 00 ......
		ChangeRejected,
		ContinueRotation,

		// reliable messages from client to host

		Undo = 0x40,	// binary: 01 000000
		Redo,
		VisibleBoardChanged,
		SelectionChanged,
		MoveSelection,
		UnpunchSelection,
		DragDropStack,
		DragDropStackIntoOtherStack,
		DragDropStackOnTopOfOtherStack,
		DragDropPiece,
		DragDropPieceIntoSameStack,
		DragDropPieceOnTopOfOtherStack,
		DiceCast,
		FlipPiece,
		FlipCounterSheet,
		FlipStack,
		UnpunchStack,
		UnpunchPiece,
		BeginRotation,
		RotatePiece,
		RotateStack,
		Shuffle,
		AddPlayerHand,
		RemovePlayerHand,
		RearrangePlayerHand,
		DragDropPieceIntoOtherStack,
		DragDropStackIntoHand,
		DragDropPieceIntoHand,
		ChangeMode,
		Invert,
		ChangeStacking,
		DragDropTerrain,
		DragDropTerrainIntoHand,
		RotateTerrain,
		FlipTerrain,
		RemoveTerrain,
		HideRevealBoard,
		GrabPiece,
		GrabStack,
		GrabTerrain,

		// reliable messages from client to all

		ChangeAccepted = 0x80,	// binary: 10 000000
		PlayerColorChanged,
		PlayerHasJoined,
		PlayerNameChanged,
		Chat,
		DiceResults,
		StackDragged,
		PieceDragged,
		DragDropAborted,
		GameBoxHasChanged,
		GameLoaded,
		GameAspectRatioChanged,
		ShuffleResult,
		ShowCursor,
		HideCursor,
		EnableDeckAutoInspect,
		DisableDeckAutoInspect,
		TerrainDragged,

		// unreliable messages from client to all others

		MouseMoved = 0xC0,	// binary: 11 000000
		VisibleAreaChanged
	}

	/// <summary>Abstract class for all messages.</summary>
	public abstract class Message {

		/// <summary>Creates a message instance from a message type.</summary>
		/// <param name="type">Type of the message to create.</param>
		/// <returns>A new Message instance.</returns>
		public static Message CreateInstance(NetworkMessage networkMessage) {
			Message message = Message.CreateInstance(networkMessage.Type);
			message.Deserialize(networkMessage.Data);
			message.SenderId = networkMessage.SenderId;
			return message;
		}

		/// <summary>Type of this message.</summary>
		public abstract NetworkMessageType Type { get; }

		/// <summary>Id of the player who sent this message.</summary>
		public int SenderId { get { return senderId; } set { senderId = value; } }

		/// <summary>Serializes this message as a byte array.</summary>
		/// <returns>Serialized data.</returns>
		public byte[] Serialize() {
			using(Serializer serializer = new Serializer(serializationEncoding)) {
				SerializeDeserialize(serializer);
				return serializer.Bytes;
			}
		}

		/// <summary>Handles a network message.</summary>
		public abstract void Handle(Controller controller);

		protected static Message CreateInstance(byte messageType) {
			Type networkMessageClass = System.Type.GetType(
				"ZunTzu.Control.Messages." + Enum.GetName(typeof(NetworkMessageType), (NetworkMessageType)messageType) + "Message");
			ConstructorInfo constructorInfo = networkMessageClass.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null);
			return (Message) constructorInfo.Invoke(new object[0]);
		}

		/// <summary>Deserializes the data for this message.</summary>
		/// <param name="serializedMessage">The serialized data of the message.</param>
		internal virtual void Deserialize(byte[] serializedMessage) {
			if(serializedMessage != null) {
				using(Deserializer deserializer = new Deserializer(serializationEncoding, serializedMessage)) {
					SerializeDeserialize(deserializer);
				}
			}
		}

		/// <summary>Encoding used to serialize and deserialize all messages.</summary>
		protected static readonly Encoding serializationEncoding = Encoding.GetEncoding(1252);

		/// <summary>Interface shared by the serializer and deserializer classes.</summary>
		protected interface ISerializer {
			bool IsSerializing { get; }
			void Serialize(ref byte data);
			void Serialize(ref bool data);
			void Serialize(ref int data);
			void Serialize(ref uint data);
			void Serialize(ref float data);
			void Serialize(ref string data);
			void Serialize(ref PointF data);
			void Serialize(ref RectangleF data);
			void Serialize(ref byte[] data);
			void Serialize(ref Guid data);
		}

		/// <summary>Implements the serialization or deserialization of the content of this message.</summary>
		protected abstract void SerializeDeserialize(ISerializer serializer);

		private sealed class Serializer : ISerializer, IDisposable {
			public Serializer(Encoding encoding) {
				writer = new BinaryWriter(new MemoryStream(), encoding);
			}
			public void Dispose() { writer.Close(); }
			public byte[] Bytes {
				get {
					writer.Flush();
					return ((MemoryStream)writer.BaseStream).ToArray();
				}
			}
			public bool IsSerializing { get { return true; } }
			public void Serialize(ref byte data) { writer.Write(data); }
			public void Serialize(ref bool data) { writer.Write(data ? (byte)1 : (byte)0); }
			public void Serialize(ref int data) { writer.Write(data); }
			public void Serialize(ref uint data) { writer.Write(data); }
			public void Serialize(ref float data) { writer.Write(data); }
			public void Serialize(ref string data) { writer.Write(data); }
			public void Serialize(ref PointF data) { writer.Write(data.X); writer.Write(data.Y); }
			public void Serialize(ref RectangleF data) { writer.Write(data.X); writer.Write(data.Y); writer.Write(data.Width); writer.Write(data.Height); }
			public void Serialize(ref byte[] data) { writer.Write(data.Length); writer.Write(data); }
			public void Serialize(ref Guid data) { writer.Write(data.ToByteArray()); }

			private BinaryWriter writer;
		}

		private sealed class Deserializer : ISerializer, IDisposable {
			public Deserializer(Encoding encoding, byte[] serializedData) {
				reader = new BinaryReader(new MemoryStream(serializedData), encoding);
			}
			public void Dispose() { reader.Close(); }
			public bool IsSerializing { get { return false; } }
			public void Serialize(ref byte data) { data = reader.ReadByte(); }
			public void Serialize(ref bool data) { data = (reader.ReadByte() == 1); }
			public void Serialize(ref int data) { data = reader.ReadInt32(); }
			public void Serialize(ref uint data) { data = reader.ReadUInt32(); }
			public void Serialize(ref float data) { data = reader.ReadSingle(); }
			public void Serialize(ref string data) { data = reader.ReadString(); }
			public void Serialize(ref PointF data) { data.X = reader.ReadSingle(); data.Y = reader.ReadSingle(); }
			public void Serialize(ref RectangleF data) { data.X = reader.ReadSingle(); data.Y = reader.ReadSingle(); data.Width = reader.ReadSingle(); data.Height = reader.ReadSingle(); }
			public void Serialize(ref byte[] data) { int byteCount = reader.ReadInt32(); data = reader.ReadBytes(byteCount); }
			public void Serialize(ref Guid data) { data = new Guid(reader.ReadBytes(16)); }

			private BinaryReader reader;
		}

		/// <summary>Id of the player who sent this message.</summary>
		protected int senderId;
	}

	/// <summary>Abstract class for all system messages.</summary>
	public abstract class SystemMessage : Message {
		protected override void SerializeDeserialize(ISerializer serializer) {}
	}

	/// <summary>Abstract class for all messages sent only to the host, asking for a change of the game state.</summary>
	public abstract class StateChangeRequestMessage : Message {
		/// <summary>Handles a network message.</summary>
		public sealed override void Handle(Controller controller) {
			// Handle state change request
			Debug.Assert(controller.Model.IsHosting);
			IModel model = controller.Model;
			if(stateChangeSequenceNumber < model.StateChangeSequenceNumber) {
				controller.NetworkClient.Send(senderId, new ChangeRejectedMessage(this));
			} else {
				++model.StateChangeSequenceNumber;
				controller.NetworkClient.Send(new ChangeAcceptedMessage(model.StateChangeSequenceNumber, this));
			}
		}

		/// <summary>Handles the acceptation of this state change.</summary>
		public abstract void HandleAccept(Controller controller);

		/// <summary>Handles the rejection of this state change.</summary>
		public virtual void HandleReject(Controller controller) {}

		/// <summary>Indicates the game state at the time this message was sent.</summary>
		/// <notes>This is used to prevent collisions of two conflicting state changes.</notes>
		protected int stateChangeSequenceNumber;
	}

	/// <summary>Abstract class for all messages that indicate a change of the game state.</summary>
	public abstract class StateChangeMessage : Message {
		/// <summary>Indicates the new game state after this message is applied.</summary>
		/// <notes>This is used to prevent collisions of two conflicting state changes.</notes>
		protected int stateChangeSequenceNumber;
	}
}
