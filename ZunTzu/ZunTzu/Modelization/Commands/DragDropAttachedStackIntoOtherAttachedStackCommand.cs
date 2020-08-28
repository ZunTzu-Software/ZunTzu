// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackIntoOtherAttachedStackCommand command.</summary>
	public sealed class DragDropAttachedStackIntoOtherAttachedStackCommand : Command {

		public DragDropAttachedStackIntoOtherAttachedStackCommand(IModel model, IStack stack, IStack stackAfter, int insertionIndex)
		: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection && stackAfter.AttachedToCounterSection && insertionIndex <= stackAfter.Pieces.Length);
			stackBefore = stack;
			piece = stack.Pieces[0];
			this.stackAfter = stackAfter;
			this.insertionIndex = insertionIndex;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			positionAfter = stackAfter.Position;
			sides = new Side[] { stackBefore.Pieces[0].Side, stackAfter.Pieces[0].Side };

			IStack[] stacks = new IStack[] { stackBefore, stackAfter };
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stacks, sides),
				new MoveToFrontOfBoardAnimation(stacks, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackBefore, positionAfter),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stacks = new IStack[] { stackBefore, stackAfter };
			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, new IPiece[] { piece }, stackBefore),
				new MoveToFrontOfBoardAnimation(stacks, stackAfter.Board),
				new ReturnStacksAnimation(stacks),
				new AttachStacksAnimation(stacks));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stacks = new IStack[] { stackBefore, stackAfter };
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stacks, sides),
				new MoveToFrontOfBoardAnimation(stacks, stackAfter.Board),
				new UndoReturnStacksAnimation(stacks, positionAfter),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private int insertionIndex;
		private Side[] sides = null;
		private PointF positionAfter;
	}
}
