// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>DragDropPieceIntoSameStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DragDropPieceIntoSameStackMessage : StateChangeRequestMessage {

		internal DragDropPieceIntoSameStackMessage() { }

		public DragDropPieceIntoSameStackMessage(int stateChangeSequenceNumber, int pieceBeingDroppedId, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingDroppedId = pieceBeingDroppedId;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DragDropPieceIntoSameStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingDroppedId);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingDropped = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingDroppedId);
			CommandContext context = new CommandContext(pieceBeingDropped.Stack.Board, pieceBeingDropped.Stack.BoundingBox);
			model.CommandManager.ExecuteCommandSequence(
				context, context,
				new DragDropPieceIntoSameStackCommand(model, pieceBeingDropped, insertionIndex));

			IPlayer sender = model.GetPlayer(senderId);
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
