// Copyright (c) 2020 ZunTzu Software and contributors

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
			foreach(IPiece piece in stack.Pieces)
				if(piece == pieceBeingUnpunched)
					break;
				else
					selection = selection.RemovePiece(piece);
			CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
			if(stack.Pieces.Length == selection.Pieces.Length) {
				model.CommandManager.ExecuteCommandSequence(
					context, context,
					new UnpunchSelectionCommand(model, stack));
			} else {
				model.CommandManager.ExecuteCommandSequence(
					context, context,
					new UnpunchSubSelectionCommand(model, selection));
			}

			IPlayer sender = model.GetPlayer(senderId);
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
