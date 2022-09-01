// Copyright (c) 2022 ZunTzu Software and contributors

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

		ConnectionAccepted,
		ChangeRejected,
		ContinueRotation,

		// reliable messages from client to host

		Undo,
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

		ChangeAccepted,
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

		MouseMoved,
		VisibleAreaChanged
	}

	/// <summary>Abstract class for all messages.</summary>
	public abstract class Message {

		/// <summary>Creates a message instance from a message type.</summary>
		/// <param name="type">Type of the message to create.</param>
		/// <returns>A new Message instance.</returns>
		public static Message CreateInstance(NetworkMessage networkMessage) {
			unsafe
            {
				if (networkMessage.Packet != null)
				{
					using (var packet = networkMessage.Packet)
					{
						byte* ptr = (byte*)packet.Data.ToPointer();
						return CreateInstance(ptr, packet.Length);
					}
				}
				else if(networkMessage.Data.Length > 2 && 
					networkMessage.Data[1] == (byte)ReservedMessageType.VideoFrameReceived)
                {
					return new VideoFrameReceivedMessage(networkMessage.Data);
                }
				else
				{
					fixed(byte* ptr = networkMessage.Data)
                    {
						return CreateInstance(ptr, networkMessage.Data.Length);
					}
				}
			}
		}

		static unsafe Message CreateInstance(byte* data, int length)
		{
			byte messageType = *(data + 1);
			Message message = Message.CreateInstance(messageType);
			message.Deserialize((IntPtr)(data + 2), length - 2);

			return message;
		}

		/// <summary>RakNet Type of this message.</summary>
		internal abstract MessageId MessageId { get; }

		/// <summary>Type of this message.</summary>
		public abstract NetworkMessageType Type { get; }

		/// <summary>Serializes this message as a byte array.</summary>
		/// <returns>Serialized data.</returns>
		public byte[] Serialize() {
			using(Serializer serializer = new Serializer(serializationEncoding)) {
				byte messageId = (byte)MessageId;
				serializer.Serialize(ref messageId);
				byte messageType = (byte)Type;
				serializer.Serialize(ref messageType);
				SerializeDeserializeSenderId(serializer);
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
		/// <param name="length">The number of bytes in the serialized data.</param>
		internal virtual void Deserialize(IntPtr serializedMessage, int length) {
			if(serializedMessage != IntPtr.Zero && length > 0) {
				using(Deserializer deserializer = new Deserializer(serializationEncoding, serializedMessage, length)) {
					SerializeDeserializeSenderId(deserializer);
					SerializeDeserialize(deserializer);
				}
			}
		}

		/// <summary>Encoding used to serialize and deserialize all messages.</summary>
		protected static readonly Encoding serializationEncoding = Encoding.UTF8;

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
			void Serialize(ref UInt64 data);
		}

		/// <summary>Implements the serialization or deserialization of the sender ID of this message.</summary>
		protected abstract void SerializeDeserializeSenderId(ISerializer deserializer);

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
			public void Serialize(ref UInt64 data) { writer.Write(data); }

			private BinaryWriter writer;
		}

		private sealed class Deserializer : ISerializer, IDisposable {
			public Deserializer(Encoding encoding, IntPtr serializedData, int length) {
				unsafe
				{
					reader = new BinaryReader(new UnmanagedMemoryStream((byte*)serializedData.ToPointer(), length), encoding);
				}
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
			public void Serialize(ref UInt64 data) { data = reader.ReadUInt64(); }

			private BinaryReader reader;
		}
	}

	/// <summary>Abstract class for all system messages.</summary>
	public abstract class SystemMessage : Message {
		internal override MessageId MessageId => Networking.MessageId.SystemMessage;
		protected override void SerializeDeserializeSenderId(ISerializer serializer) {}
		protected override void SerializeDeserialize(ISerializer serializer) {}
	}

	/// <summary>Abstract class for all reliable messages from host to single client.</summary>
	public abstract class ReliableMessageFromHostToSingleClient : Message
	{
		internal override MessageId MessageId => Networking.MessageId.ReliableMessageFromHostToSingleClient;
		
		protected override void SerializeDeserializeSenderId(ISerializer serializer)
		{
			UInt64 placeholderForRecipientId = 0;
			serializer.Serialize(ref placeholderForRecipientId);
		}
	}

	/// <summary>Abstract class for all messages from client (instead of host).</summary>
	public abstract class MessageFromClient : Message
	{
		/// <summary>Id of the player who sent this message.</summary>
		public UInt64 SenderId => senderId;

		protected override void SerializeDeserializeSenderId(ISerializer serializer)
		{
			serializer.Serialize(ref senderId);
		}

		/// <summary>Id of the player who sent this message.</summary>
		protected UInt64 senderId = 0;
	}

	/// <summary>Abstract class for all reliable messages from client to host.</summary>
	public abstract class ReliableMessageFromClientToHost : MessageFromClient
	{
		internal override MessageId MessageId => Networking.MessageId.ReliableMessageFromClientToHost;
	}

	/// <summary>Abstract class for all reliable messages from client to all.</summary>
	public abstract class ReliableMessageFromClientToAll : MessageFromClient
	{
		internal override MessageId MessageId => Networking.MessageId.ReliableMessageFromClientToAll;
	}

	/// <summary>Abstract class for all unreliable message from client to all others.</summary>
	public abstract class UnreliableMessageFromClientToAllOthers : MessageFromClient
	{
		internal override MessageId MessageId => Networking.MessageId.UnreliableMessageFromClientToAllOthers;
	}

	/// <summary>Abstract class for all messages sent only to the host, asking for a change of the game state.</summary>
	public abstract class StateChangeRequestMessage : ReliableMessageFromClientToHost {
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
	public abstract class StateChangeMessage : ReliableMessageFromClientToAll {
		/// <summary>Indicates the new game state after this message is applied.</summary>
		/// <notes>This is used to prevent collisions of two conflicting state changes.</notes>
		protected int stateChangeSequenceNumber;
	}
}
