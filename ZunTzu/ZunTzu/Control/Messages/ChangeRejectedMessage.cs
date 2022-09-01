// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>ChangeRejectedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChangeRejectedMessage : ReliableMessageFromHostToSingleClient
	{
		internal ChangeRejectedMessage() {}

		public ChangeRejectedMessage(StateChangeRequestMessage requestMessage) {
			this.requestMessage = requestMessage;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ChangeRejected; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			byte[] serializedRequestMessage = null;

			if(serializer.IsSerializing) {
				serializedRequestMessage = requestMessage.Serialize();
			}

			serializer.Serialize(ref serializedRequestMessage);

			if(!serializer.IsSerializing) {
				requestMessage = (StateChangeRequestMessage) Message.CreateInstance(new Networking.NetworkMessage(serializedRequestMessage));
			}
		}

		public sealed override void Handle(Controller controller) {
			// TODO : "error" tone when change rejected
			requestMessage.HandleReject(controller);
		}

		private StateChangeRequestMessage requestMessage;
	}
}
