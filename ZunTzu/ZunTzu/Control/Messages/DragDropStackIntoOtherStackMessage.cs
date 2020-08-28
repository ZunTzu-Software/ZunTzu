// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropStackIntoOtherStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropStackIntoOtherStackMessage : StateChangeRequestMessage {

		internal DragDropStackIntoOtherStackMessage() {}

		public DragDropStackIntoOtherStackMessage(int stateChangeSequenceNumber, int stackBeingDroppedId, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingDroppedId = stackBeingDroppedId;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropStackIntoOtherStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingDroppedId);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingDroppedId);
			IStack stackBeingDropped = pieceBeingDropped.Stack;
			IStack otherStack = model.CurrentSelection.Stack;
			if(stackBeingDropped.AttachedToCounterSection) {
				CommandContext context = new CommandContext(otherStack.Board, otherStack.BoundingBox);
				if(otherStack.AttachedToCounterSection) {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new DragDropAttachedStackIntoOtherAttachedStackCommand(model, stackBeingDropped, otherStack, insertionIndex));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new DragDropAttachedStackIntoOtherStackCommand(model, stackBeingDropped, otherStack, insertionIndex));
				}
			} else {
				if(otherStack.AttachedToCounterSection) {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(stackBeingDropped.Board),
						new CommandContext(otherStack.Board, otherStack.BoundingBox),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackIntoOtherAttachedStackCommand(model, stackBeingDropped, otherStack, insertionIndex) :
							(ICommand) new DragDropTopOfStackIntoOtherAttachedStackCommand(model, pieceBeingDropped, otherStack, insertionIndex)));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						new CommandContext(stackBeingDropped.Board),
						new CommandContext(otherStack.Board, otherStack.BoundingBox),
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackIntoOtherStackCommand(model, stackBeingDropped, otherStack, insertionIndex) :
							(ICommand) new DragDropTopOfStackIntoOtherStackCommand(model, pieceBeingDropped, otherStack, insertionIndex)));
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
		private int insertionIndex;
	}
}
