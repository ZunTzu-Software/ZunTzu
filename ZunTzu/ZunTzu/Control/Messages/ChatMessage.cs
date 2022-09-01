// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>ChatMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChatMessage : ReliableMessageFromClientToAll {

		internal ChatMessage() {}

		public ChatMessage(string text) {
			this.text = text;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.Chat; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref text);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null)
				controller.View.Prompter.AddTextToHistory(sender.Color, text);
		}

		private string text;
	}
}
