// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>GrabTerrainCommand command.</summary>
	public sealed class GrabTerrainCommand : Command {

		public GrabTerrainCommand(IModel model, Guid playerGuid, IStack stack)
			: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && playerGuid != Guid.Empty && stack.Pieces.Length == 1 && stack.Pieces[0] is ITerrainClone);
			this.playerGuid = playerGuid;
			stackBefore = stack;
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			stackAfter = (playerHand == null || playerHand.Count == 0 ?
				stackBefore :	// the hand is currently empty
				playerHand.Stack);
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			insertionIndex = (playerHand != null ? playerHand.Count : 0);

			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			piece = (ITerrainClone) stackBefore.Pieces[0];
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);
			rotationAngleBefore = piece.RotationAngle;
			sideBefore = piece.Side;

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));

			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
			animations.Add(new MoveStackToHandAnimation(stackBefore));

			if(playerHand != null) {
				// does the hand already contain an identical clone?
				for(int i = 0; i < playerHand.Count; ++i) {
					ITerrainClone handPiece = stackAfter.Pieces[i] as ITerrainClone;
					if(handPiece != null && handPiece.Prototype == piece.Prototype) {
						// yes -> simply move the piece to the new insertion index
						animations.Add(new RemoveTerrainAnimation(stackBefore));
						animations.Add(new RearrangePlayerHandAnimation(playerHand, i, insertionIndex));
						model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
						return;
					}
				}
			}
			// add the piece
			if(stackBefore == stackAfter)
				animations.Add(new FillPlayerHandAnimation(playerGuid, stackBefore));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			if(piece.Stack != stackAfter) {
				// piece has not been added to hand (don't undo ordering of the hand, it's not transactional)
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(stackBefore, boardBefore),
					new MoveStackFromHandAnimation(stackBefore, positionBefore),
					new SetZOrderAnimation(stackBefore, zOrderBefore));
			} else {
				List<IAnimation> animations = new List<IAnimation>(4);
				if(stackBefore == stackAfter) {
					animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackAfter));
				} else {
					animations.Add(new SplitStackAnimation(stackAfter, new IPiece[] { piece }, stackBefore));
				}
				animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
				if(piece.RotationAngle != rotationAngleBefore) {
					int totalDetentsBefore = (int) (piece.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
					int totalDetentsAfter = (int) (rotationAngleBefore * (12.0f / (float) Math.PI) + 0.5f) * 120;
					int rotationIncrements = totalDetentsAfter - totalDetentsBefore;
					animations.Add(new InstantRotatePiecesAnimation(new IPiece[] { piece }, rotationIncrements));
				}
				if(piece.Side != sideBefore) {
					animations.Add(new InstantFlipPiecesAnimation(new IPiece[] { piece }));
				}
				animations.Add(new MoveStackFromHandAnimation(stackBefore, positionBefore));
				animations.Add(new SetZOrderAnimation(stackBefore, zOrderBefore));
				model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
			}
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));

			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
			animations.Add(new MoveStackToHandAnimation(stackBefore));

			if(playerHand != null) {
				// does the hand already contain an identical clone?
				for(int i = 0; i < playerHand.Count; ++i) {
					ITerrainClone handPiece = stackAfter.Pieces[i] as ITerrainClone;
					if(handPiece != null && handPiece.Prototype == piece.Prototype) {
						// yes -> don't redo ordering of the hand, it's not transactional
						animations.Add(new RemoveTerrainAnimation(stackBefore));
						model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
						return;
					}
				}
			}
			// add the piece
			if(stackBefore == stackAfter)
				animations.Add(new FillPlayerHandAnimation(playerGuid, stackBefore));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardBefore;
		private PointF positionBefore;
		private int insertionIndex;
		private ITerrainClone piece;
		private int zOrderBefore;
		private float rotationAngleBefore;
		private Side sideBefore;
	}
}
