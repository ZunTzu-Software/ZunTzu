// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>StackDraggedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class StackDraggedMessage : Message {

		internal StackDraggedMessage() {}

		public StackDraggedMessage(int pieceId, PointF anchor) {
			this.pieceId = pieceId;
			this.anchor = anchor;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.StackDragged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref pieceId);
			serializer.Serialize(ref anchor);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				IPiece piece = controller.Model.CurrentGameBox.CurrentGame.GetPieceById(pieceId);
				sender.StackBeingDragged = piece;
				sender.DragAndDropAnchor = anchor;
			}
		}

		private int pieceId;
		private PointF anchor;
	}
}
