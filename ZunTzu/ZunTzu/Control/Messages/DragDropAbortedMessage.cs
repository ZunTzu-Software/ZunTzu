// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropAbortedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropAbortedMessage : Message {

		internal DragDropAbortedMessage() {}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropAborted; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				sender.PieceBeingDragged = null;
				sender.StackBeingDragged = null;
			}
		}
	}
}
