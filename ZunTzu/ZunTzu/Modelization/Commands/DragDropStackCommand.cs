// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropStackCommand command.</summary>
	public sealed class DragDropStackCommand : Command {

		public DragDropStackCommand(IModel model, IStack stack, PointF positionAfter)
		: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection);
			this.stack = stack;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);

			positionBefore = stack.Position;
			zOrderBefore = ((Board) stack.Board).GetZOrder(stack);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stack, stack.Board),
				new MoveStackInstantlyAnimation(stack, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(
				new MoveStackAnimation(stack, positionBefore),
				new SetZOrderAnimation(stack, zOrderBefore));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stack);

			positionBefore = stack.Position;
			zOrderBefore = ((Board) stack.Board).GetZOrder(stack);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stack, stack.Board),
				new MoveStackAnimation(stack, positionAfter));
		}

		private IStack stack;
		private PointF positionBefore;
		private PointF positionAfter;
		private int zOrderBefore;
	}
}
