// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>GrabPieceMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GrabPieceMessage : StateChangeRequestMessage {

		internal GrabPieceMessage() { }

		public GrabPieceMessage(int stateChangeSequenceNumber, int pieceBeingGrabbedId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingGrabbedId = pieceBeingGrabbedId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GrabPiece; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingGrabbedId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty) {
				IPiece pieceBeingGrabbed = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingGrabbedId);
				IStack stack = pieceBeingGrabbed.Stack;
				Debug.Assert(stack.Pieces.Length > 1);
				CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
				model.CommandManager.ExecuteCommandSequence(
					context, context,
					new GrabPieceCommand(model, sender.Guid, pieceBeingGrabbed));
			}
		}

		private int pieceBeingGrabbedId;
	}
}
