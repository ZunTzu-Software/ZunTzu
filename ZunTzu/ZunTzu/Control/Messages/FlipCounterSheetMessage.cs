// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>MoveSelectionMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class FlipCounterSheetMessage : StateChangeRequestMessage {

		internal FlipCounterSheetMessage() {}

		public FlipCounterSheetMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.FlipCounterSheet; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			ICounterSheet visibleBoard = (ICounterSheet) controller.Model.CurrentGameBox.CurrentGame.VisibleBoard;
			if(visibleBoard != null && visibleBoard.Properties.BackImageFileName != null) {
				visibleBoard.Side = (Side) (1 - (int) visibleBoard.Side);

				// deselect any one-sided attached piece
				IModel model = controller.Model;
				if(model.CurrentSelection != null && model.CurrentSelection.Stack.AttachedToCounterSection) {
					IPiece piece = model.CurrentSelection.Stack.Pieces[0];
					if(piece.CounterSection.CounterSheet == visibleBoard && (piece is ICard || piece is ICounter && piece.CounterSection.Type != CounterSectionType.TwoSided))
						model.CurrentSelection = null;
				}
			}
		}
	}
}
