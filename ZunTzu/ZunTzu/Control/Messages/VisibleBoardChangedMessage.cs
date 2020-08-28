// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>VisibleBoardChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VisibleBoardChangedMessage : StateChangeRequestMessage {

		internal VisibleBoardChangedMessage() {}

		public VisibleBoardChangedMessage(int stateChangeSequenceNumber, int visibleBoardId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.visibleBoardId = visibleBoardId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.VisibleBoardChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref visibleBoardId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IGame game = controller.Model.CurrentGameBox.CurrentGame;
			IBoard visibleBoard = game.GetBoardById(visibleBoardId);
			if(visibleBoard != null) {
				game.VisibleBoard = visibleBoard;
			}
			if(senderId == controller.Model.ThisPlayer.Id) {
				controller.DraggingStackState.WaitingForBoardChange = false;
				controller.DraggingPieceState.WaitingForBoardChange = false;
				controller.DraggingHandPieceState.WaitingForBoardChange = false;
			}
		}

		public override void HandleReject(Controller controller) {
			controller.DraggingStackState.WaitingForBoardChange = false;
			controller.DraggingPieceState.WaitingForBoardChange = false;
			controller.DraggingHandPieceState.WaitingForBoardChange = false;
		}

		private int visibleBoardId;
	}
}
