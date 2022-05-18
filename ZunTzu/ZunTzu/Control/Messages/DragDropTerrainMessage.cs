// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropTerrainMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropTerrainMessage : StateChangeRequestMessage {

		internal DragDropTerrainMessage() { }

		public DragDropTerrainMessage(int stateChangeSequenceNumber, int boardId, int zOrder, PointF newPosition) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
			this.newPosition = newPosition;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropTerrain; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
			serializer.Serialize(ref newPosition);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayer sender = model.GetPlayer(senderId);
			// is the terrain in the player's hand?
			if(boardId != -1) {
				// no
				IBoard board = game.GetBoardById(boardId);
				if(board != null) {
					IStack stackBeingDropped = board.GetStackFromZOrder(zOrder);
					IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
					if(board == visibleBoard) {
						model.CommandManager.ExecuteCommandSequence(
							new CommandContext(visibleBoard, stackBeingDropped.BoundingBox),
							new CommandContext(visibleBoard),
							new DragDropStackCommand(model, stackBeingDropped, newPosition));
					} else {
						model.CommandManager.ExecuteCommandSequence(
							new CommandContext(stackBeingDropped.Board, stackBeingDropped.BoundingBox),
							new CommandContext(visibleBoard),
							new DragDropStackFromOtherBoardCommand(model, stackBeingDropped, visibleBoard, newPosition));
					}
				}
				if(sender != null)
					sender.StackBeingDragged = null;
			} else {
				// yes, in the hand
				if(sender != null && sender.Guid != Guid.Empty) {
					IPlayerHand playerHand = game.GetPlayerHand(sender.Guid);
					if(playerHand != null && playerHand.Count > zOrder) {
						ITerrainClone piece = (ITerrainClone) playerHand.Pieces[zOrder];
						model.CommandManager.ExecuteCommandSequence(
							new CloneTerrainFromHandCommand(model, sender.Guid, piece, newPosition));
					}
					sender.PieceBeingDragged = null;
				}
			}
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int boardId;
		private int zOrder;
		private PointF newPosition;
	}
}
