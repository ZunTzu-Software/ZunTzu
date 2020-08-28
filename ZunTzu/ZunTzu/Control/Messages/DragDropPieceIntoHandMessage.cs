// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropPieceIntoHandMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropPieceIntoHandMessage : StateChangeRequestMessage {

		internal DragDropPieceIntoHandMessage() { }

		public DragDropPieceIntoHandMessage(int stateChangeSequenceNumber, int pieceBeingDroppedId, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingDroppedId = pieceBeingDroppedId;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropPieceIntoHand; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingDroppedId);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty) {
				IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingDroppedId);
				IStack stack = pieceBeingDropped.Stack;
				Debug.Assert(stack.Pieces.Length > 1);
				CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
				model.CommandManager.ExecuteCommandSequence(
					context, context,
					new DragDropPieceIntoHandCommand(model, sender.Guid, pieceBeingDropped, insertionIndex));
			}

			if(sender != null)
				sender.PieceBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int pieceBeingDroppedId;
		private int insertionIndex;
	}
}
