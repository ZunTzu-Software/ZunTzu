// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropPieceCommand command.</summary>
	public sealed class DragDropPieceCommand : Command {

		public DragDropPieceCommand(IModel model, IPiece piece, PointF positionAfter)
		: base(model)
		{
			Debug.Assert(piece.Stack.Pieces.Length > 1);
			this.piece = piece;
			this.positionAfter = positionAfter;
			stackBefore = piece.Stack;
			stackAfter = new Stack();
			boardAfter = model.CurrentGameBox.CurrentGame.VisibleBoard;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAfter, stackBefore.Board),
				(stackBefore.Board == boardAfter ?
					(Animation) new MoveStackAnimation(stackAfter, stackBefore.Position) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(stackAfter, stackBefore.Position)),
				new MergeStacksAnimation(stackBefore, stackAfter, indexInStackBefore));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				(stackBefore.Board == boardAfter ?
					(Animation) new MoveStackAnimation(stackAfter, positionAfter) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(stackAfter, positionAfter)));
		}

		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardAfter;
		private PointF positionAfter;
		private int indexInStackBefore;
	}
}
