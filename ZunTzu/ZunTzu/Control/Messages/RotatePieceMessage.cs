// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>RotatePieceMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RotatePieceMessage : StateChangeRequestMessage {

		internal RotatePieceMessage() { }

		public RotatePieceMessage(int stateChangeSequenceNumber, int pieceId, int rotationIncrements) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceId = pieceId;
			this.rotationIncrements = rotationIncrements;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RotatePiece; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceId);
			serializer.Serialize(ref rotationIncrements);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece piece = model.CurrentGameBox.CurrentGame.GetPieceById(pieceId);
			if (senderId == model.ThisPlayer.Id) {
				controller.IdleState.AcceptRotation();
				// piece is not in the player's hand?
				if(piece.Stack.Board != null)
					model.CommandManager.ExecuteCommandSequence(new ConfirmedRotatePieceCommand(model, piece, rotationIncrements));
			} else {
				if(piece.Stack.Board == null) {
					// piece is in a player's hand -> it can't be undone
					if(model.AnimationManager.IsBeingAnimated(piece.Stack))
						model.AnimationManager.EndAllAnimations();
					model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(new IPiece[1] { piece }, rotationIncrements));
				} else {
					model.CommandManager.ExecuteCommandSequence(new RotatePieceCommand(model, piece, rotationIncrements));
				}
			}
		}

		public sealed override void HandleReject(Controller controller) {
			controller.IdleState.RejectRotation(rotationIncrements);
		}

		private int pieceId;
		private int rotationIncrements;
	}
}
