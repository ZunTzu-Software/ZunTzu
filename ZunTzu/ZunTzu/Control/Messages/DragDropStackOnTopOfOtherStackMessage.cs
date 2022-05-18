// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropStackOnTopOfOtherStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropStackOnTopOfOtherStackMessage : StateChangeRequestMessage {

		internal DragDropStackOnTopOfOtherStackMessage() {}

		public DragDropStackOnTopOfOtherStackMessage(int stateChangeSequenceNumber, int stackBeingDroppedId, int otherStackId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingDroppedId = stackBeingDroppedId;
			this.otherStackId = otherStackId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropStackOnTopOfOtherStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingDroppedId);
			serializer.Serialize(ref otherStackId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingDroppedId);
			IStack stackBeingDropped = pieceBeingDropped.Stack;
			IStack otherStack = model.CurrentGameBox.CurrentGame.GetStackById(otherStackId);
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			if(stackBeingDropped.AttachedToCounterSection) {
				if(stackBeingDropped.Board == visibleBoard) {
					model.CommandManager.ExecuteCommandSequence(
						new DragDropAttachedStackOnTopOfOtherStackCommand(model, stackBeingDropped, otherStack));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						new DragDropAttachedStackOnTopOfOtherStackFromOtherBoardCommand(model, stackBeingDropped, otherStack));
				}
			} else {
				if(stackBeingDropped.Board == visibleBoard) {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(visibleBoard, stackBeingDropped.BoundingBox),
						new CommandContext(visibleBoard),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackOnTopOfOtherStackCommand(model, stackBeingDropped, otherStack) :
							(ICommand) new DragDropTopOfStackOnTopOfOtherStackCommand(model, pieceBeingDropped, otherStack)));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(stackBeingDropped.Board, stackBeingDropped.BoundingBox),
						new CommandContext(visibleBoard),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackOnTopOfOtherStackFromOtherBoardCommand(model, stackBeingDropped, otherStack) :
							(ICommand) new DragDropTopOfStackOnTopOfOtherStackCommand(model, pieceBeingDropped, otherStack)));
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
		private int otherStackId;
	}
}
