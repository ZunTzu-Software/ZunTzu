// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>RotateTerrainMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RotateTerrainMessage : StateChangeRequestMessage {

		internal RotateTerrainMessage() { }

		public RotateTerrainMessage(int stateChangeSequenceNumber, int boardId, int zOrder, int rotationIncrements) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
			this.rotationIncrements = rotationIncrements;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RotateTerrain; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
			serializer.Serialize(ref rotationIncrements);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IGame game = model.CurrentGameBox.CurrentGame;
			// is the terrain in the player's hand?
			if(boardId != -1) {
				// no
				IBoard board = game.GetBoardById(boardId);
				if(board != null) {
					IStack stackBeingDropped = board.GetStackFromZOrder(zOrder);
					IPiece piece = stackBeingDropped.Pieces[0];
					if(senderId == model.ThisPlayer.Id) {
						controller.IdleState.AcceptRotation();
						model.CommandManager.ExecuteCommandSequence(new ConfirmedRotatePieceCommand(model, piece, rotationIncrements));
					} else {
						model.CommandManager.ExecuteCommandSequence(new RotatePieceCommand(model, piece, rotationIncrements));
					}
				}
			} else {
				// yes, in the hand
				if(senderId == model.ThisPlayer.Id) {
					controller.IdleState.AcceptRotation();
				} else {
					IPlayer sender = model.GetPlayer(senderId);
					if(sender != null && sender.Guid != Guid.Empty) {
						IPlayerHand playerHand = game.GetPlayerHand(sender.Guid);
						if(playerHand != null && playerHand.Count > zOrder) {
							IPiece piece = playerHand.Pieces[zOrder];
							if(model.AnimationManager.IsBeingAnimated(piece.Stack))
								model.AnimationManager.EndAllAnimations();
							model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(new IPiece[1] { piece }, rotationIncrements));
						}
					}
				}
			}
		}

		public sealed override void HandleReject(Controller controller) {
			controller.IdleState.RejectRotation(rotationIncrements);
		}

		private int boardId;
		private int zOrder;
		private int rotationIncrements;
	}
}
