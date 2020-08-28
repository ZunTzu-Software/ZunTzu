// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>GrabStackCommand command.</summary>
	public sealed class GrabStackCommand : Command {

		public GrabStackCommand(IModel model, Guid playerGuid, IStack stack)
			: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && playerGuid != Guid.Empty);
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
			stackBeforeArrangement = stackBefore.Pieces;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);
			rotationAnglesBefore = new float[stackBeforeArrangement.Length];
			sidesBefore = new Side[stackBeforeArrangement.Length];
			for(int i = 0; i < stackBeforeArrangement.Length; ++i) {
				rotationAnglesBefore[i] = stackBeforeArrangement[i].RotationAngle;
				sidesBefore[i] = stackBeforeArrangement[i].Side;
			}

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));
			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
			animations.Add(new MoveStackToHandAnimation(stackBefore));
			if(stackBefore == stackAfter)
				animations.Add(new FillPlayerHandAnimation(playerGuid, stackBefore));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
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

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));
			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, boardBefore));
			animations.Add(new MoveStackToHandAnimation(stackBefore));
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
		private IPiece[] stackBeforeArrangement;
		private int zOrderBefore;
		private float[] rotationAnglesBefore;
		private Side[] sidesBefore;
	}
}
