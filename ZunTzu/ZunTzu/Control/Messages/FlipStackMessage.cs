// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>MoveSelectionMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class FlipStackMessage : StateChangeRequestMessage {

		internal FlipStackMessage() {}

		public FlipStackMessage(int stateChangeSequenceNumber, int stackId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackId = stackId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.FlipStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;

			IPlayer sender = model.GetPlayer(senderId);
			Guid senderGuid = Guid.Empty;
			if (sender != null && sender.Guid != Guid.Empty)
				senderGuid = sender.Guid;

			IPiece stackBottom = model.CurrentGameBox.CurrentGame.GetPieceById(stackId);
			IStack stack = stackBottom.Stack;
			ISelection selection = stack.Select();
			bool pieceEligible = false;
			foreach(IPiece piece in stack.Pieces) {
				if(piece == stackBottom)
					pieceEligible = true;
				if(!pieceEligible || (piece.CounterSection.IsSingleSided && !piece.IsBlock))
					selection = selection.RemovePiece(piece);
			}
			model.CommandManager.ExecuteCommandSequence(new FlipSelectionCommand(senderGuid, model, selection));
		}

		private int stackId;
	}
}
