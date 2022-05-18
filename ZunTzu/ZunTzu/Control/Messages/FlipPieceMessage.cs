// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>MoveSelectionMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class FlipPieceMessage : StateChangeRequestMessage {

		internal FlipPieceMessage() {}

		public FlipPieceMessage(int stateChangeSequenceNumber, int pieceId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceId = pieceId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.FlipPiece; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece piece = model.CurrentGameBox.CurrentGame.GetPieceById(pieceId);
			if(piece.Stack.Board == null) {
				// piece is in a player's hand -> it can't be undone
				if(model.AnimationManager.IsBeingAnimated(piece.Stack))
					model.AnimationManager.EndAllAnimations();
				if(senderId == model.ThisPlayer.Id)
					model.AnimationManager.LaunchAnimationSequence(new FlipPiecesAnimation(new IPiece[] { piece }));
				else
					model.AnimationManager.LaunchAnimationSequence(new InstantFlipPiecesAnimation(new IPiece[] { piece }));
			} else {
				model.CommandManager.ExecuteCommandSequence(new FlipSelectionCommand(model, piece.Select()));
			}
		}

		private int pieceId;
	}
}
