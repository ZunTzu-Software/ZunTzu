// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>TerrainDraggedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class TerrainDraggedMessage : ReliableMessageFromClientToAll
	{

		internal TerrainDraggedMessage() { }

		/// <summary>Constructor.</summary>
		/// <param name="boardId">Id of the board containing the terrain, or -1 if in player's hand.</param>
		/// <param name="zOrder">Z-order of the terrain on the board, or left-to-right order if in the player's hand.</param>
		/// <param name="anchor">Position of the cursor anchor.</param>
		public TerrainDraggedMessage(int boardId, int zOrder, PointF anchor) {
			this.boardId = boardId;
			this.zOrder = zOrder;
			this.anchor = anchor;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.TerrainDragged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
			serializer.Serialize(ref anchor);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				IGame game = controller.Model.CurrentGameBox.CurrentGame;
				// is the terrain in the player's hand?
				if(boardId != -1) {
					// no
					IBoard board = game.GetBoardById(boardId);
					if(board != null) {
						IStack stack = board.GetStackFromZOrder(zOrder);
						sender.StackBeingDragged = stack.Pieces[0];
						sender.DragAndDropAnchor = anchor;
					}
				} else {
					// yes, in the hand
					if(sender.Guid != Guid.Empty) {
						IPlayerHand playerHand = game.GetPlayerHand(sender.Guid);
						if(playerHand != null && playerHand.Count > zOrder) {
							IPiece piece = playerHand.Pieces[zOrder];
							sender.PieceBeingDragged = piece;
							sender.DragAndDropAnchor = anchor;
						}
					}
				}
			}
		}

		private int boardId;
		private int zOrder;
		private PointF anchor;
	}
}
