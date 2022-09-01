// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>ChangeAcceptedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChangeAcceptedMessage : StateChangeMessage {

		internal ChangeAcceptedMessage() {}

		public ChangeAcceptedMessage(int stateChangeSequenceNumber, StateChangeRequestMessage requestMessage) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.requestMessage = requestMessage;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ChangeAccepted; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			byte[] serializedRequestMessage = null;

			if(serializer.IsSerializing) {
				serializedRequestMessage = requestMessage.Serialize();
			}

			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref serializedRequestMessage);

			if(!serializer.IsSerializing) {
				requestMessage = (StateChangeRequestMessage) Message.CreateInstance(new Networking.NetworkMessage(serializedRequestMessage));
			}
		}

		public sealed override void Handle(Controller controller) {
			if(!controller.Model.IsHosting)
				controller.Model.StateChangeSequenceNumber = stateChangeSequenceNumber;
			requestMessage.HandleAccept(controller);
		}

		private StateChangeRequestMessage requestMessage;
	}
}
