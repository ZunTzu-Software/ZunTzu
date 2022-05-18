// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropTopOfStackIntoOtherStackCommand command.</summary>
	public sealed class DragDropTopOfStackIntoOtherStackCommand : Command {

		public DragDropTopOfStackIntoOtherStackCommand(IModel model, IPiece stackBottom, IStack stackAfter, int insertionIndex)
			: base(model)
		{
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && !stackAfter.AttachedToCounterSection && insertionIndex <= stackAfter.Pieces.Length && stackBottom != stackBottom.Stack.Pieces[0]);
			stackBefore = stackBottom.Stack;
			this.stackAfter = stackAfter;
			transitionStack = new Stack();
			this.insertionIndex = insertionIndex;
			bottomIndex = stackBottom.IndexInStackFromBottomToTop;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, transitionStack),
				new MoveToFrontOfBoardAnimation(transitionStack, stackAfter.Board),
				new MoveStackInstantlyAnimation(transitionStack, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex));
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
				new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex));
		}

		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private IPiece[] pieces;
		private int bottomIndex;
		private int insertionIndex;
	}
}
