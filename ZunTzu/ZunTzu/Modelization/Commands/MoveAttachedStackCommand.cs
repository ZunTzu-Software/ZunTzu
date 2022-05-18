// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>MoveAttachedStackCommand command.</summary>
	public sealed class MoveAttachedStackCommand : Command {

		public MoveAttachedStackCommand(IModel model, IStack stack, PointF positionAfter)
		: base(model)
		{
			this.stack = stack;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			side = stack.Pieces[0].Side;
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stack, stack.Board),
				new MoveStackAnimation(stack, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);
			IStack[] stackAsArray = new IStack[] { stack };
			model.AnimationManager.LaunchAnimationSequence(
				new ReturnStacksAnimation(stackAsArray),
				new AttachStacksAnimation(stackAsArray));
		}

		public override void Redo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stack, stack.Board),
				new MoveStackAnimation(stack, positionAfter));
		}

		private IStack stack;
		private PointF positionAfter;
		private Side side;
	}
}
