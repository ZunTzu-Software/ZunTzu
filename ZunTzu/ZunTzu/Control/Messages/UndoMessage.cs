// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>UndoMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class UndoMessage : StateChangeRequestMessage {

		internal UndoMessage() {}

		public UndoMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.Undo; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			controller.Model.CommandManager.Undo();
		}
	}
}
