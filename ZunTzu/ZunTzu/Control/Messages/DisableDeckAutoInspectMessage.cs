// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>DisableDeckAutoInspectMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DisableDeckAutoInspectMessage : Message {

		internal DisableDeckAutoInspectMessage() { }

		public override NetworkMessageType Type { get { return NetworkMessageType.DisableDeckAutoInspect; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) { }

		public sealed override void Handle(Controller controller) {
			if(senderId != controller.Model.ThisPlayer.Id) {
				IPlayer sender = controller.Model.GetPlayer(senderId);
				if(sender != null)
					sender.DeckAutoInspect = false;
			}
		}
	}
}
