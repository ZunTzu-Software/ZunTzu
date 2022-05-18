// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropTopOfStackOnTopOfOtherStackCommand command.</summary>
	public sealed class DragDropTopOfStackOnTopOfOtherStackCommand : Command {

		public DragDropTopOfStackOnTopOfOtherStackCommand(IModel model, IPiece stackBottom, IStack stackAfter)
			: base(model)
		{
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && !stackAfter.AttachedToCounterSection && stackBottom != stackBottom.Stack.Pieces[0]);
			stackBefore = stackBottom.Stack;
			this.stackAfter = stackAfter;
			transitionStack = new Stack();
			this.stackBottom = stackBottom;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			bottomIndex = stackBottom.IndexInStackFromBottomToTop;

			pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				new MoveStackInstantlyAnimation(transitionStack, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, pieces, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board),
				(stackBefore.Board == stackAfter.Board ?
					(Animation) new MoveStackAnimation(transitionStack, stackBefore.Position) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(transitionStack, stackBefore.Position)),
				new MergeStacksAnimation(stackBefore, transitionStack, bottomIndex));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				(stackBefore.Board == stackAfter.Board ?
					(Animation) new MoveStackAnimation(transitionStack, stackAfter.Position) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(transitionStack, stackAfter.Position)),
				new MergeStacksAnimation(stackAfter, transitionStack, stackAfter.Pieces.Length));
		}

		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private IPiece stackBottom;
		private IPiece[] pieces;
		private int bottomIndex;
	}
}
