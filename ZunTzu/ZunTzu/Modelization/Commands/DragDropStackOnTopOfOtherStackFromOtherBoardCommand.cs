// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropStackOnTopOfOtherStackFromOtherBoardCommand command.</summary>
	public sealed class DragDropStackOnTopOfOtherStackFromOtherBoardCommand : Command {

		public DragDropStackOnTopOfOtherStackFromOtherBoardCommand(IModel model, IStack stack, IStack stackAfter)
		: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && !stackAfter.AttachedToCounterSection);
			stackBefore = stack;
			this.stackAfter = stackAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			stackBeforeArrangement = stackBefore.Pieces;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackBefore, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, stackAfter.Pieces.Length));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, stackBeforeArrangement, stackBefore),
				new MoveToFrontOfBoardAnimation(stackBefore, boardBefore),
				new MoveStackFromEdgeOfScreenAnimation(stackBefore, positionBefore),
				new SetZOrderAnimation(stackBefore, zOrderBefore));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			stackBeforeArrangement = stackBefore.Pieces;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackFromEdgeOfScreenAnimation(stackBefore, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, stackAfter.Pieces.Length));
		}

		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardBefore;
		private PointF positionBefore;
		private IPiece[] stackBeforeArrangement;
		private int zOrderBefore;
	}
}
