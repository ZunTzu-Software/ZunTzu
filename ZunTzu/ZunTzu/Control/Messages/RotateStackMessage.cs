// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>RotateStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RotateStackMessage : StateChangeRequestMessage {

		internal RotateStackMessage() { }

		public RotateStackMessage(int stateChangeSequenceNumber, int stackBottomPieceId, int rotationIncrements) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBottomPieceId = stackBottomPieceId;
			this.rotationIncrements = rotationIncrements;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RotateStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBottomPieceId);
			serializer.Serialize(ref rotationIncrements);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece stackBottom = model.CurrentGameBox.CurrentGame.GetPieceById(stackBottomPieceId);
			if (senderId == model.ThisPlayer.Id) {
				controller.IdleState.AcceptRotation();
				model.CommandManager.ExecuteCommandSequence(new ConfirmedRotateTopOfStackCommand(model, stackBottom, rotationIncrements));
			} else {
				model.CommandManager.ExecuteCommandSequence(new RotateTopOfStackCommand(model, stackBottom, rotationIncrements));
			}
		}

		public sealed override void HandleReject(Controller controller) {
			controller.IdleState.RejectRotation(rotationIncrements);
		}

		private int stackBottomPieceId;
		private int rotationIncrements;
	}
}
