// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropTopOfStackIntoHandCommand command.</summary>
	public sealed class DragDropTopOfStackIntoHandCommand : Command {

		public DragDropTopOfStackIntoHandCommand(IModel model, Guid playerGuid, IPiece stackBottom, int insertionIndex)
			: base(model)
		{
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && playerGuid != Guid.Empty && stackBottom != stackBottom.Stack.Pieces[0]);
			this.playerGuid = playerGuid;
			stackBefore = stackBottom.Stack;
			this.insertionIndex = insertionIndex;
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			Debug.Assert(playerHand != null && insertionIndex <= playerHand.Count);
			transitionStack = new Stack();
			stackAfter = (playerHand.Count == 0 ?
				transitionStack :	// the hand is currently empty
				playerHand.Stack);
			bottomIndex = stackBottom.IndexInStackFromBottomToTop;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			rotationAnglesBefore = new float[pieces.Length];
			sidesBefore = new Side[pieces.Length];
			for(int i = 0; i < pieces.Length; ++i) {
				rotationAnglesBefore[i] = pieces[i].RotationAngle;
				sidesBefore[i] = pieces[i].Side;
			}

			if(playerGuid == model.ThisPlayer.Guid) {
				model.AnimationManager.LaunchAnimationSequence(
					new SplitStackAnimation(stackBefore, pieces, transitionStack),
					new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board),
					new MoveStackInstantlyAnimation(transitionStack, stackBefore.Position),
					(transitionStack == stackAfter ?
						(IAnimation) new FillPlayerHandAnimation(playerGuid, transitionStack) :
						(IAnimation) new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex)));
			} else {
				model.AnimationManager.LaunchAnimationSequence(
					new SplitStackAnimation(stackBefore, pieces, transitionStack),
					new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board),
					new MoveStackInstantlyAnimation(transitionStack, stackBefore.Position),
					new MoveStackToHandAnimation(transitionStack),
					(transitionStack == stackAfter ?
						(IAnimation) new FillPlayerHandAnimation(playerGuid, transitionStack) :
						(IAnimation) new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex)));
			}
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			List<IAnimation> animations = new List<IAnimation>(5);
			if(transitionStack == stackAfter) {
				animations.Add(new EmptyPlayerHandAnimation(playerGuid, transitionStack));
				animations.Add(new RearrangeStackAnimation(transitionStack, pieces));
			} else {
				animations.Add(new SplitStackAnimation(stackAfter, pieces, transitionStack));
			}
			animations.Add(new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board));
			for(int i = 0; i < pieces.Length; ++i) {
				IPiece piece = pieces[i];
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
			animations.Add(new MoveStackFromHandAnimation(transitionStack, stackBefore.Position));
			animations.Add(new MergeStacksAnimation(stackBefore, transitionStack, bottomIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board),
				new MoveStackInstantlyAnimation(transitionStack, stackBefore.Position),
				new MoveStackToHandAnimation(transitionStack),
				(transitionStack == stackAfter ?
					(IAnimation) new FillPlayerHandAnimation(playerGuid, transitionStack) :
					(IAnimation) new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex)));
		}

		private Guid playerGuid;
		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private int insertionIndex;
		private IPiece[] pieces;
		private float[] rotationAnglesBefore;
		private Side[] sidesBefore;
		private int bottomIndex;
	}
}
