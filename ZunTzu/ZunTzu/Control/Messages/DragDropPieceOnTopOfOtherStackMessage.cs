// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropPieceOnTopOfOtherStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropPieceOnTopOfOtherStackMessage : StateChangeRequestMessage {

		internal DragDropPieceOnTopOfOtherStackMessage() { }

		public DragDropPieceOnTopOfOtherStackMessage(int stateChangeSequenceNumber, int pieceBeingDroppedId, int otherStackId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingDroppedId = pieceBeingDroppedId;
			this.otherStackId = otherStackId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropPieceOnTopOfOtherStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingDroppedId);
			serializer.Serialize(ref otherStackId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingDroppedId);
			IStack otherStack = model.CurrentGameBox.CurrentGame.GetStackById(otherStackId);
			IPlayer sender = model.GetPlayer(senderId);
			if(pieceBeingDropped.Stack.Board == null) {
				// piece is currently in the player's hand
				if(sender != null && sender.Guid != Guid.Empty)
					model.CommandManager.ExecuteCommandSequence(new DragDropHandPieceOnTopOfOtherStackCommand(model, sender.Guid, pieceBeingDropped, otherStack));
			} else {
				model.CommandManager.ExecuteCommandSequence(
					new CommandContext(pieceBeingDropped.Stack.Board, pieceBeingDropped.Stack.BoundingBox),
					new CommandContext(otherStack.Board),
					new DragDropPieceOnTopOfOtherStackCommand(model, pieceBeingDropped, otherStack));
			}

			if(sender != null)
				sender.PieceBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int pieceBeingDroppedId;
		private int otherStackId;
	}
}
