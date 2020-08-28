// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropPieceIntoOtherStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropPieceIntoOtherStackMessage : StateChangeRequestMessage {

		internal DragDropPieceIntoOtherStackMessage() { }

		public DragDropPieceIntoOtherStackMessage(int stateChangeSequenceNumber, int pieceBeingDroppedId, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingDroppedId = pieceBeingDroppedId;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropPieceIntoOtherStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingDroppedId);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			// piece is currently in the player's hand
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingDroppedId);
			IStack otherStack = model.CurrentSelection.Stack;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null) {
				if(sender.Guid != Guid.Empty) {
					CommandContext context = new CommandContext(otherStack.Board, otherStack.BoundingBox);
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						(otherStack.AttachedToCounterSection ?
							(ICommand) new DragDropHandPieceIntoOtherAttachedStackCommand(model, sender.Guid, pieceBeingDropped, otherStack, insertionIndex) :
							(ICommand) new DragDropHandPieceIntoOtherStackCommand(model, sender.Guid, pieceBeingDropped, otherStack, insertionIndex)));
				}

				sender.PieceBeingDragged = null;
			}
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int pieceBeingDroppedId;
		private int insertionIndex;
	}
}
