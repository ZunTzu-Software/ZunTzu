// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropStackIntoHandMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropStackIntoHandMessage : StateChangeRequestMessage {

		internal DragDropStackIntoHandMessage() { }

		public DragDropStackIntoHandMessage(int stateChangeSequenceNumber, int stackBeingDroppedId, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingDroppedId = stackBeingDroppedId;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropStackIntoHand; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingDroppedId);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty) {
				IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingDroppedId);
				IStack stackBeingDropped = pieceBeingDropped.Stack;
				CommandContext context = new CommandContext(stackBeingDropped.Board, stackBeingDropped.BoundingBox);
				if(stackBeingDropped.AttachedToCounterSection) {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new DragDropAttachedStackIntoHandCommand(model, sender.Guid, stackBeingDropped, insertionIndex));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						(pieceBeingDropped == stackBeingDropped.Pieces[0] ?
							(ICommand) new DragDropStackIntoHandCommand(model, sender.Guid, stackBeingDropped, insertionIndex) :
							(ICommand) new DragDropTopOfStackIntoHandCommand(model, sender.Guid, pieceBeingDropped, insertionIndex)));
				}
			}

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
