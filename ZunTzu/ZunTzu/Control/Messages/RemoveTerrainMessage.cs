// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>RemoveTerrainMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RemoveTerrainMessage : StateChangeRequestMessage {

		internal RemoveTerrainMessage() { }

		public RemoveTerrainMessage(int stateChangeSequenceNumber, int boardId, int zOrder) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RemoveTerrain; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
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
					IStack stack = board.GetStackFromZOrder(zOrder);
					CommandContext context = new CommandContext(board, stack.BoundingBox);
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new RemoveTerrainCommand(model, stack));
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
							new RemoveTerrainFromHandCommand(model, sender.Guid, piece));
					}
				}
				if(sender != null)
					sender.PieceBeingDragged = null;
			}
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int boardId;
		private int zOrder;
	}
}
