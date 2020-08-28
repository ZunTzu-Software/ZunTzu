// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>ChangeStackingMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChangeStackingMessage : StateChangeRequestMessage {

		internal ChangeStackingMessage() { }

		public ChangeStackingMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ChangeStacking; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IGame game = controller.Model.CurrentGameBox.CurrentGame;
			game.StackingEnabled = !game.StackingEnabled;
		}
	}
}
