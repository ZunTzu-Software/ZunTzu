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

			IPiece stackBottom = model.CurrentGameBox.CurrentGame.GetPieceById(stackId);
			IStack stack = stackBottom.Stack;
			ISelection selection = stack.Select();
			bool pieceEligible = false;
			foreach(IPiece piece in stack.Pieces) {
				if(piece == stackBottom)
					pieceEligible = true;
				if(!pieceEligible || piece.CounterSection.IsSingleSided)
					selection = selection.RemovePiece(piece);
			}
			model.CommandManager.ExecuteCommandSequence(new FlipSelectionCommand(model, selection));
		}

		private int stackId;
	}
}
