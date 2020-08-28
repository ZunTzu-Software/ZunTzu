// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropTopOfStackCommand command.</summary>
	public sealed class DragDropTopOfStackCommand : Command {

		public DragDropTopOfStackCommand(IModel model, IPiece stackBottom, PointF positionAfter)
			: base(model)
		{
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && stackBottom != stackBottom.Stack.Pieces[0]);
			stackBefore = stackBottom.Stack;
			bottomIndex = stackBottom.IndexInStackFromBottomToTop;
			this.positionAfter = positionAfter;
			stackAfter = new Stack();
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			IPiece[] pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, stackBefore.Board),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveStackAnimation(stackAfter, stackBefore.Position),
				new MergeStacksAnimation(stackBefore, stackAfter, bottomIndex));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			IPiece[] pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, stackBefore.Board),
				new MoveStackAnimation(stackAfter, positionAfter));
		}

		private IStack stackBefore;
		private int bottomIndex;
		private IStack stackAfter;
		private PointF positionAfter;
	}
}
