// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>UnpunchStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class UnpunchStackMessage : StateChangeRequestMessage {

		internal UnpunchStackMessage() { }

		public UnpunchStackMessage(int stateChangeSequenceNumber, int stackBeingUnpunchedId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingUnpunchedId = stackBeingUnpunchedId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.UnpunchStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingUnpunchedId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingUnpunched = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingUnpunchedId);
			IStack stack = pieceBeingUnpunched.Stack;
			ISelection selection = stack.Select();

			IPlayer sender = model.GetPlayer(senderId);
			Guid senderGuid = Guid.Empty;
			if (sender != null && sender.Guid != Guid.Empty)
				senderGuid = sender.Guid;

			bool fromUnpunched = false;
			foreach (IPiece piece in stack.Pieces) {
				if (piece == pieceBeingUnpunched) {
					fromUnpunched = true;
					if (piece.IsBlock && piece.Owner != Guid.Empty && piece.Owner != senderGuid)
						selection = selection.RemovePiece(piece);
				} else {
					if (!fromUnpunched)
						selection = selection.RemovePiece(piece);
					else {
						if (piece.IsBlock && piece.Owner != Guid.Empty && piece.Owner != senderGuid)
							selection = selection.RemovePiece(piece);
					}
				}
			}

			CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
			if(stack.Pieces.Length == selection.Pieces.Length) {
				if (stack.Pieces != null && stack.Pieces.Length > 0) {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new UnpunchSelectionCommand(model, stack));
				}
			} else {
				if (selection.Pieces != null && selection.Pieces.Length > 0) {
					model.CommandManager.ExecuteCommandSequence(
					context, context,
					new UnpunchSubSelectionCommand(model, selection));
				}
			}

			if(sender != null)
				sender.StackBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int stackBeingUnpunchedId;
	}
}
