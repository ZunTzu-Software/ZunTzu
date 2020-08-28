// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropTerrainIntoHandMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropTerrainIntoHandMessage : StateChangeRequestMessage {

		internal DragDropTerrainIntoHandMessage() { }

		public DragDropTerrainIntoHandMessage(int stateChangeSequenceNumber, int boardId, int zOrder, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropTerrainIntoHand; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty && boardId != -1) {
				IBoard board = game.GetBoardById(boardId);
				if(board != null) {
					IStack stackBeingDropped = board.GetStackFromZOrder(zOrder);
					ITerrainClone pieceBeingDropped = (ITerrainClone) stackBeingDropped.Pieces[0];
					CommandContext context = new CommandContext(stackBeingDropped.Board, stackBeingDropped.BoundingBox);
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new DragDropTerrainIntoHandCommand(model, sender.Guid, stackBeingDropped, insertionIndex));
				}
			}

			if(sender != null)
				sender.StackBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int boardId;
		private int zOrder;
		private int insertionIndex;
	}
}
