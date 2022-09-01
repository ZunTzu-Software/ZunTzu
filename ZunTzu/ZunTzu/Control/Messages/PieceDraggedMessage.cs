// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>PieceDraggedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PieceDraggedMessage : ReliableMessageFromClientToAll
	{

		internal PieceDraggedMessage() {}

		public PieceDraggedMessage(int pieceId, PointF anchor) {
			this.pieceId = pieceId;
			this.anchor = anchor;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.PieceDragged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref pieceId);
			serializer.Serialize(ref anchor);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				IPiece piece = controller.Model.CurrentGameBox.CurrentGame.GetPieceById(pieceId);
				sender.PieceBeingDragged = piece;
				sender.DragAndDropAnchor = anchor;
			}
		}

		private int pieceId;
		private PointF anchor;
	}
}
