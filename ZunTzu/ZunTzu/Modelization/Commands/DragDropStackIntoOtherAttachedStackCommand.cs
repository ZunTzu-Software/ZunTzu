// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropStackIntoOtherAttachedStackCommand command.</summary>
	public sealed class DragDropStackIntoOtherAttachedStackCommand : Command {

		public DragDropStackIntoOtherAttachedStackCommand(IModel model, IStack stack, IStack stackAfter, int insertionIndex)
		: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && stackAfter.AttachedToCounterSection && insertionIndex < 2);
			stackBefore = stack;
			this.stackAfter = stackAfter;
			this.insertionIndex = insertionIndex;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);
			stackBeforeArrangement = stackBefore.Pieces;
			side = stackAfter.Pieces[0].Side;
			positionAfter = stackAfter.Position;

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackBefore, positionAfter),
				new DetachStacksAnimation(new IStack[] { stackAfter }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackAfter, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, stackBeforeArrangement, stackBefore),
				new AttachStacksAnimation(new IStack[] { stackAfter }),
				new MoveToFrontOfBoardAnimation(stackBefore, boardBefore),
				(boardBefore == stackAfter.Board ?
					(Animation) new MoveStackAnimation(stackBefore, positionBefore) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(stackBefore, positionBefore)),
				new SetZOrderAnimation(stackBefore, zOrderBefore));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				(boardBefore == stackAfter.Board ?
					(Animation) new MoveStackAnimation(stackBefore, positionAfter) :
					(Animation) new MoveStackFromEdgeOfScreenAnimation(stackBefore, positionAfter)),
				new DetachStacksAnimation(new IStack[] { stackAfter }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackAfter, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackAfter, positionAfter),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		private IStack stackBefore;
		private IStack stackAfter;
		private IBoard boardBefore;
		private PointF positionBefore;
		private PointF positionAfter;
		private int insertionIndex;
		private IPiece[] stackBeforeArrangement;
		private int zOrderBefore;
		private Side side;
	}
}
