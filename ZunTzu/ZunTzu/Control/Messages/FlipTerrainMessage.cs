// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>FlipTerrainMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class FlipTerrainMessage : StateChangeRequestMessage {

		internal FlipTerrainMessage() { }

		public FlipTerrainMessage(int stateChangeSequenceNumber, int boardId, int zOrder) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.FlipTerrain; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IGame game = model.CurrentGameBox.CurrentGame;
			// is the terrain in the player's hand?
			if(boardId != -1) {
				// no
				IBoard board = game.GetBoardById(boardId);
				if(board != null) {
					IStack stack = board.GetStackFromZOrder(zOrder);
					IPiece piece = stack.Pieces[0];
					if(piece.CounterSection.Type == CounterSectionType.TwoSided) {
						ISelection selection = piece.Select();
						model.CommandManager.ExecuteCommandSequence(new FlipSelectionCommand(model, selection));
					}
				}
			} else {
				// yes, in the hand
				IPlayer sender = model.GetPlayer(senderId);
				if(sender != null && sender.Guid != Guid.Empty) {
					IPlayerHand playerHand = game.GetPlayerHand(sender.Guid);
					if(playerHand != null && playerHand.Count > zOrder) {
						IPiece piece = playerHand.Pieces[zOrder];
						// piece is in a player's hand -> it can't be undone
						if(model.AnimationManager.IsBeingAnimated(piece.Stack))
							model.AnimationManager.EndAllAnimations();
						if(senderId == model.ThisPlayer.Id)
							model.AnimationManager.LaunchAnimationSequence(new FlipPiecesAnimation(new IPiece[] { piece }));
						else
							model.AnimationManager.LaunchAnimationSequence(new InstantFlipPiecesAnimation(new IPiece[] { piece }));
					}
				}
			}
		}

		private int boardId;
		private int zOrder;
	}
}
