// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Control.Messages {

	/// <summary>HideRevealBoardMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class HideRevealBoardMessage : StateChangeRequestMessage {

		internal HideRevealBoardMessage() { }

		public HideRevealBoardMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.HideRevealBoard; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty) {
				IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
				if(visibleBoard.Owner == Guid.Empty)
					visibleBoard.Owner = sender.Guid;
				else if(visibleBoard.Owner == sender.Guid)
					visibleBoard.Owner = Guid.Empty;
			}
		}
	}
}
