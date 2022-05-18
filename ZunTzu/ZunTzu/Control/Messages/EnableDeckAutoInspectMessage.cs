// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>EnableDeckAutoInspectMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class EnableDeckAutoInspectMessage : Message {

		internal EnableDeckAutoInspectMessage() { }

		public override NetworkMessageType Type { get { return NetworkMessageType.EnableDeckAutoInspect; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) { }

		public sealed override void Handle(Controller controller) {
			if(senderId != controller.Model.ThisPlayer.Id) {
				IPlayer sender = controller.Model.GetPlayer(senderId);
				if(sender != null)
					sender.DeckAutoInspect = true;
			}
		}
	}
}
