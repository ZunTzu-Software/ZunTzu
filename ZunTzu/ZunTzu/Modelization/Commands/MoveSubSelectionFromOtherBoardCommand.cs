// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>MoveSubSelectionFromOtherBoardCommand command.</summary>
	public sealed class MoveSubSelectionFromOtherBoardCommand : Command {

		public MoveSubSelectionFromOtherBoardCommand(IModel model, ISelection selection, IBoard boardAfter, PointF positionAfter)
		: base(model)
		{
			Debug.Assert(selection.Pieces.Length > 0 && selection.Pieces.Length < selection.Stack.Pieces.Length &&
				selection.Stack.Board != boardAfter);
			this.selection = selection;
			this.boardAfter = boardAfter;
			this.positionAfter = positionAfter;
			stackAfter = new Stack();
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(selection.Stack, stackAfter);

			arrangementBefore = selection.Stack.Pieces;

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(selection.Stack, selection.Pieces, stackAfter),
				new MoveToFrontOfBoardAnimation(stackAfter, boardAfter),
				new MoveStackFromEdgeOfScreenAnimation(stackAfter, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(selection.Stack, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAfter, selection.Stack.Board),
				new MoveStackFromEdgeOfScreenAnimation(stackAfter, selection.Stack.Position),
				new MergeStacksAnimation(selection.Stack, stackAfter, 0),
				new RearrangeStackAnimation(selection.Stack, arrangementBefore));
		}

		private ISelection selection;
		private IStack stackAfter;
		private IBoard boardAfter;
		private PointF positionAfter;
		private IPiece[] arrangementBefore;
	}
}
