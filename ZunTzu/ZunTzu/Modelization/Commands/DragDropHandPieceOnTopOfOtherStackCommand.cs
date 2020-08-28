// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropHandPieceOnTopOfOtherStackCommand command.</summary>
	public sealed class DragDropHandPieceOnTopOfOtherStackCommand : Command {

		public DragDropHandPieceOnTopOfOtherStackCommand(IModel model, Guid playerGuid, IPiece piece, IStack stackAfter)
			: base(model)
		{
			Debug.Assert(piece.Stack.Board == null && playerGuid != Guid.Empty && !stackAfter.AttachedToCounterSection);
			this.playerGuid = playerGuid;
			this.piece = piece;
			this.stackAfter = stackAfter;
			stackBefore = piece.Stack;
			transitionStack = (stackBefore.Pieces.Length == 1 ?
				stackBefore :	// this is the last piece in the hand
				new Stack());
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			// memorize current state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;
			rotationAngleAfter = piece.RotationAngle;
			sideAfter = piece.Side;

			model.AnimationManager.LaunchAnimationSequence(
				(stackBefore == transitionStack ?
					(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new SplitStackAnimation(stackBefore, new IPiece[] { piece }, transitionStack)),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				new MoveStackInstantlyAnimation(transitionStack, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, new IPiece[] { piece }, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				new MoveStackToHandAnimation(transitionStack),
				(stackBefore == transitionStack ?
					(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new MergeStacksAnimation(stackBefore, transitionStack, indexInStackBefore)));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			// update state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			int rotationIncrements = 0;
			if(piece.RotationAngle != rotationAngleAfter) {
				int totalDetentsBefore = (int) (piece.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
				int totalDetentsAfter = (int) (rotationAngleAfter * (12.0f / (float) Math.PI) + 0.5f) * 120;
				rotationIncrements = totalDetentsAfter - totalDetentsBefore;
			}

			IPiece[] pieceAsArray = new IPiece[] { piece };
			List<IAnimation> animations = new List<IAnimation>(6);
			animations.Add(stackBefore == transitionStack ?
				(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
				(IAnimation) new SplitStackAnimation(stackBefore, pieceAsArray, transitionStack));
			animations.Add(new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board));
			if(rotationIncrements != 0)
				animations.Add(new InstantRotatePiecesAnimation(pieceAsArray, rotationIncrements));
			if(sideAfter != piece.Side)
				animations.Add(new InstantFlipPiecesAnimation(pieceAsArray));
			animations.Add(new MoveStackFromHandAnimation(transitionStack, stackAfter.Position));
			animations.Add(new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private int indexInStackBefore;
		private float rotationAngleAfter;
		private Side sideAfter;
	}
}
