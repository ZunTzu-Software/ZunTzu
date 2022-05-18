// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropStackIntoHandCommand command.</summary>
	public sealed class DragDropStackIntoHandCommand : Command {

		public DragDropStackIntoHandCommand(IModel model, Guid playerGuid, IStack stack, int insertionIndex)
			: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && playerGuid != Guid.Empty);
			this.playerGuid = playerGuid;
			stackBefore = stack;
			this.insertionIndex = insertionIndex;
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			Debug.Assert(playerHand != null && insertionIndex <= playerHand.Count);
			stackAfter = (playerHand.Count == 0 ?
				stackBefore :	// the hand is currently empty
				playerHand.Stack);
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			stackBeforeArrangement = stackBefore.Pieces;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);
			rotationAnglesBefore = new float[stackBeforeArrangement.Length];
			sidesBefore = new Side[stackBeforeArrangement.Length];
			for(int i = 0; i < stackBeforeArrangement.Length; ++i) {
				rotationAnglesBefore[i] = stackBeforeArrangement[i].RotationAngle;
				sidesBefore[i] = stackBeforeArrangement[i].Side;
			}

			if(playerGuid == model.ThisPlayer.Guid) {
				model.AnimationManager.LaunchAnimationSequence(
					(stackBefore == stackAfter ?
						(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
						(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
			} else {
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(stackBefore, boardBefore),
					new MoveStackToHandAnimation(stackBefore),
					(stackBefore == stackAfter ?
						(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
						(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
			}
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(stackBefore == stackAfter) {
				animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackAfter));
				animations.Add(new RearrangeStackAnimation(stackBefore, stackBeforeArrangement));
			} else {
				animations.Add(new SplitStackAnimation(stackAfter, stackBeforeArrangement, stackBefore));
			}
			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
			for(int i = 0; i < stackBeforeArrangement.Length; ++i) {
				IPiece piece = stackBeforeArrangement[i];
				if(piece.RotationAngle != rotationAnglesBefore[i]) {
					int totalDetentsBefore = (int) (piece.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
					int totalDetentsAfter = (int) (rotationAnglesBefore[i] * (12.0f / (float) Math.PI) + 0.5f) * 120;
					int rotationIncrements = totalDetentsAfter - totalDetentsBefore;
					animations.Add(new InstantRotatePiecesAnimation(new IPiece[] { piece }, rotationIncrements));
				}
				if(piece.Side != sidesBefore[i]) {
					animations.Add(new InstantFlipPiecesAnimation(new IPiece[] { piece }));
				}
			}
			animations.Add(new MoveStackFromHandAnimation(stackBefore, positionBefore));
			animations.Add(new SetZOrderAnimation(stackBefore, zOrderBefore));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackBefore, boardBefore),
				new MoveStackToHandAnimation(stackBefore),
				(stackBefore == stackAfter ?
					(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
		}

		private Guid playerGuid;
		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardBefore;
		private PointF positionBefore;
		private int insertionIndex;
		private IPiece[] stackBeforeArrangement;
		private int zOrderBefore;
		private float[] rotationAnglesBefore;
		private Side[] sidesBefore;
	}
}
