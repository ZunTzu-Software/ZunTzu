// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropHandPieceCommand command.</summary>
	public sealed class DragDropHandPieceCommand : Command {

		public DragDropHandPieceCommand(IModel model, Guid playerGuid, IPiece piece, PointF positionAfter)
			: base(model)
		{
			Debug.Assert(piece.Stack.Board == null && playerGuid != Guid.Empty);
			this.playerGuid = playerGuid;
			this.piece = piece;
			this.positionAfter = positionAfter;
			stackBefore = piece.Stack;
			stackAfter = (stackBefore.Pieces.Length == 1 ?
				stackBefore :	// this is the last piece in the hand
				new Stack());
			boardAfter = model.CurrentGameBox.CurrentGame.VisibleBoard;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			// memorize current state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;
			rotationAngleAfter = piece.RotationAngle;
			sideAfter = piece.Side;

			model.AnimationManager.LaunchAnimationSequence(
				(stackBefore == stackAfter ?
					(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter)),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stackAfter)
				model.CurrentSelection = null;

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackToHandAnimation(stackAfter),
				(stackBefore == stackAfter ?
					(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new MergeStacksAnimation(stackBefore, stackAfter, indexInStackBefore)));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			// update state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			int rotationIncrements = 0;
			if(piece.RotationAngle != rotationAngleAfter) {
				int totalDetentsBefore = (int) (piece.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
				int totalDetentsAfter = (int) (rotationAngleAfter * (12.0f / (float) Math.PI) + 0.5f) * 120;
				rotationIncrements = totalDetentsAfter - totalDetentsBefore;
			}

			IPiece[] pieceAsArray = new IPiece[] { piece };
			List<IAnimation> animations = new List<IAnimation>(5);
			animations.Add(stackBefore == stackAfter ?
				(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
				(IAnimation) new SplitStackAnimation(stackBefore, pieceAsArray, stackAfter));
			animations.Add(new MoveToFrontOfBoardAnimation(stackAfter, boardAfter));
			if(rotationIncrements != 0)
				animations.Add(new InstantRotatePiecesAnimation(playerGuid, pieceAsArray, rotationIncrements));
			if(sideAfter != piece.Side)
				animations.Add(new InstantFlipPiecesAnimation(playerGuid, pieceAsArray));
			animations.Add(new MoveStackFromHandAnimation(stackAfter, positionAfter));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardAfter;
		private PointF positionAfter;
		private int indexInStackBefore;
		private float rotationAngleAfter;
		private Side sideAfter;
	}
}
