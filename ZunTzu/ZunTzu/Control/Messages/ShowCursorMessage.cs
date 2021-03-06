// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>ShowCursorMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ShowCursorMessage : Message {

		internal ShowCursorMessage() { }

		public override NetworkMessageType Type { get { return NetworkMessageType.ShowCursor; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) { }

		public sealed override void Handle(Controller controller) {
			if(senderId != controller.Model.ThisPlayer.Id) {
				IPlayer sender = controller.Model.GetPlayer(senderId);
				if(sender != null)
					sender.IsCursorVisible = true;
			}
		}
	}
}
