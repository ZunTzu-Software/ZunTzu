// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>ChangeRejectedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChangeRejectedMessage : Message {

		internal ChangeRejectedMessage() {}

		public ChangeRejectedMessage(StateChangeRequestMessage requestMessage) {
			this.requestMessage = requestMessage;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ChangeRejected; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			byte requestType = 0;
			int requestSenderId = 0;
			byte[] serializedRequestMessage = null;

			if(serializer.IsSerializing) {
				requestType = (byte)requestMessage.Type;
				requestSenderId = requestMessage.SenderId;
				serializedRequestMessage = requestMessage.Serialize();
			}

			serializer.Serialize(ref requestType);
			serializer.Serialize(ref requestSenderId);
			serializer.Serialize(ref serializedRequestMessage);

			if(!serializer.IsSerializing) {
				requestMessage = (StateChangeRequestMessage) Message.CreateInstance(requestType);
				requestMessage.Deserialize(serializedRequestMessage);
				requestMessage.SenderId = requestSenderId;
			}
		}

		public sealed override void Handle(Controller controller) {
			// TODO : "error" tone when change rejected
			requestMessage.HandleReject(controller);
		}

		private StateChangeRequestMessage requestMessage;
	}
}
