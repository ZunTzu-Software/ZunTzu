// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropTopOfStackFromOtherBoardCommand command.</summary>
	public sealed class DragDropTopOfStackFromOtherBoardCommand : Command {

		public DragDropTopOfStackFromOtherBoardCommand(IModel model, IPiece stackBottom, IBoard boardAfter, PointF positionAfter)
			: base(model)
		{
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && stackBottom.Stack.Board != boardAfter && stackBottom != stackBottom.Stack.Pieces[0]);
			stackBefore = stackBottom.Stack;
			bottomIndex = stackBottom.IndexInStackFromBottomToTop;
			this.positionAfter = positionAfter;
			stackAfter = new Stack();
			this.boardAfter = boardAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			IPiece[] pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAfter, stackBefore.Board),
				new MoveStackFromEdgeOfScreenAnimation(stackAfter, stackBefore.Position),
				new MergeStacksAnimation(stackBefore, stackAfter, bottomIndex));
		}

		/// <summary>Execute this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			IPiece[] pieces = new IPiece[stackBefore.Pieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackBefore.Pieces[i + bottomIndex];

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackBefore, pieces, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackFromEdgeOfScreenAnimation(stackAfter, positionAfter));
		}

		private IStack stackBefore;
		private int bottomIndex;
		private IStack stackAfter;
		private IBoard boardAfter;
		private PointF positionAfter;
	}
}
