// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackOnTopOfOtherStackFromOtherBoardCommand command.</summary>
	public sealed class DragDropAttachedStackOnTopOfOtherStackFromOtherBoardCommand : Command {

		public DragDropAttachedStackOnTopOfOtherStackFromOtherBoardCommand(IModel model, IStack stack, IStack stackAfter)
		: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection && !stackAfter.AttachedToCounterSection);
			stackBefore = stack;
			piece = stack.Pieces[0];
			this.stackAfter = stackAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			side = stackBefore.Pieces[0].Side;

			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackInstantlyAnimation(stackBefore, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, stackAfter.Pieces.Length));
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

			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stackBefore, stackAfter.Board),
				new MoveStackFromEdgeOfScreenAnimation(stackBefore, stackAfter.Position),
				new MergeStacksAnimation(stackAfter, stackBefore, stackAfter.Pieces.Length));
		}

		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private Side side;
	}
}
