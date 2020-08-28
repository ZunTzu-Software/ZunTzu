// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropStackMessage : StateChangeRequestMessage {

		internal DragDropStackMessage() {}

		public DragDropStackMessage(int stateChangeSequenceNumber, int stackBeingDroppedId, PointF newPosition) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingDroppedId = stackBeingDroppedId;
			this.newPosition = newPosition;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingDroppedId);
			serializer.Serialize(ref newPosition);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingDroppedId);
			IStack stackBeingDropped = pieceBeingDropped.Stack;
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			if(stackBeingDropped.AttachedToCounterSection) {
				if(stackBeingDropped.Board == visibleBoard) {
					model.CommandManager.ExecuteCommandSequence(
						new DragDropAttachedStackCommand(model, stackBeingDropped, newPosition));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						new DragDropAttachedStackFromOtherBoardCommand(model, stackBeingDropped, visibleBoard, newPosition));
				}
			} else {
				if(stackBeingDropped.Board == visibleBoard) {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(visibleBoard, stackBeingDropped.BoundingBox),
						new CommandContext(visibleBoard),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackCommand(model, stackBeingDropped, newPosition) :
							(ICommand) new DragDropTopOfStackCommand(model, pieceBeingDropped, newPosition)));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(stackBeingDropped.Board, stackBeingDropped.BoundingBox),
						new CommandContext(visibleBoard),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackFromOtherBoardCommand(model, stackBeingDropped, visibleBoard, newPosition) :
							(ICommand) new DragDropTopOfStackFromOtherBoardCommand(model, pieceBeingDropped, visibleBoard, newPosition)));
				}
			}

			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null)
				sender.StackBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int stackBeingDroppedId;
		private PointF newPosition;
	}
}
