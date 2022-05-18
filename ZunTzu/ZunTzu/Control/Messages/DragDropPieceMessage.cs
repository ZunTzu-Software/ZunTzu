// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropPieceMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropPieceMessage : StateChangeRequestMessage {

		internal DragDropPieceMessage() { }

		public DragDropPieceMessage(int stateChangeSequenceNumber, int pieceBeingDroppedId, PointF newPosition) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingDroppedId = pieceBeingDroppedId;
			this.newPosition = newPosition;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropPiece; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingDroppedId);
			serializer.Serialize(ref newPosition);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingDroppedId);
			IPlayer sender = model.GetPlayer(senderId);
			if(pieceBeingDropped.Stack.Board == null) {
				// piece is currently in the player's hand
				if(sender != null && sender.Guid != Guid.Empty)
					model.CommandManager.ExecuteCommandSequence(new DragDropHandPieceCommand(model, sender.Guid, pieceBeingDropped, newPosition));
			} else {
				model.CommandManager.ExecuteCommandSequence(
					new CommandContext(pieceBeingDropped.Stack.Board, pieceBeingDropped.Stack.BoundingBox),
					new CommandContext(model.CurrentGameBox.CurrentGame.VisibleBoard),
					new DragDropPieceCommand(model, pieceBeingDropped, newPosition));
			}

			if(sender != null)
				sender.PieceBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int pieceBeingDroppedId;
		private PointF newPosition;
	}
}
