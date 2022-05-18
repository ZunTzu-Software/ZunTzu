// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackIntoOtherStackCommand command.</summary>
	public sealed class DragDropAttachedStackIntoOtherStackCommand : Command {

		public DragDropAttachedStackIntoOtherStackCommand(IModel model, IStack stack, IStack stackAfter, int insertionIndex)
		: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection && !stackAfter.AttachedToCounterSection && insertionIndex <= stackAfter.Pieces.Length);
			stackBefore = stack;
			piece = stack.Pieces[0];
			this.stackAfter = stackAfter;
			this.insertionIndex = insertionIndex;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			side = stackBefore.Pieces[0].Side;

			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackBefore, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stackAsArray = new IStack[] { stackBefore };
			model.AnimationManager.LaunchAnimationSequence(
				new SplitStackAnimation(stackAfter, new IPiece[] { piece }, stackBefore),
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new ReturnStacksAnimation(stackAsArray),
				new AttachStacksAnimation(stackAsArray));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stackAsArray = new IStack[] { stackBefore };
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stackAsArray, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackAsArray, stackAfter.Board),
				new UndoReturnStacksAnimation(stackAsArray, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
		}

		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private int insertionIndex;
		private Side side;
	}
}
