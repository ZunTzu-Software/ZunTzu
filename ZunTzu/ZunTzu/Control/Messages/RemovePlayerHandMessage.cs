// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Control.Messages {

	/// <summary>RemovePlayerHandMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RemovePlayerHandMessage : StateChangeRequestMessage {

		internal RemovePlayerHandMessage() { }

		public RemovePlayerHandMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RemovePlayerHand; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty)
				model.AnimationManager.LaunchAnimationSequence(new RemovePlayerHandAnimation(sender.Guid));
		}
	}
}
