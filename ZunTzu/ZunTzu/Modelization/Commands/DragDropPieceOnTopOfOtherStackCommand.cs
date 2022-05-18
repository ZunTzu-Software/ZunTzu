// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropPieceOnTopOfOtherStackCommand command.</summary>
	public sealed class DragDropPieceOnTopOfOtherStackCommand : Command {

		public DragDropPieceOnTopOfOtherStackCommand(IModel model, IPiece piece, IStack stackAfter)
		: base(model)
		{
			Debug.Assert(piece.Stack.Pieces.Length > 1 && !stackAfter.AttachedToCounterSection);
			this.piece = piece;
			this.stackAfter = stackAfter;
			stackBefore = piece.Stack;
			transitionStack = new Stack();
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, new IPiece[] { piece }, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				new MoveStackInstantlyAnimation(transitionStack, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, new IPiece[] { piece }, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board),
				(stackBefore.Board == stackAfter.Board ?
					(Animation) new MoveStackAnimation(transitionStack, stackBefore.Position) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(transitionStack, stackBefore.Position)),
				new MergeStacksAnimation(stackBefore, transitionStack, indexInStackBefore));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, new IPiece[] { piece }, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				(stackBefore.Board == stackAfter.Board ?
					(Animation) new MoveStackAnimation(transitionStack, stackAfter.Position) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(transitionStack, stackAfter.Position)),
				new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
		}

		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private int indexInStackBefore;
	}
}
