// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerNameChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PlayerNameChangedMessage : Message {

		internal PlayerNameChangedMessage() {}

		public PlayerNameChangedMessage(string newFirstName, string newLastName) {
			this.newFirstName = newFirstName;
			this.newLastName = newLastName;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.PlayerNameChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref newFirstName);
			serializer.Serialize(ref newLastName);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.PlayerRenamed, sender.FirstName + " " + sender.LastName, newFirstName + " " + newLastName);
				sender.FirstName = newFirstName;
				sender.LastName = newLastName;
			}
		}

		private string newFirstName;
		private string newLastName;
	}
}
