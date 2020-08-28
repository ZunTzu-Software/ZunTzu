// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>MoveAttachedStackFromOtherBoardCommand command.</summary>
	public sealed class MoveAttachedStackFromOtherBoardCommand : Command {

		public MoveAttachedStackFromOtherBoardCommand(IModel model, IStack stack, IBoard boardAfter, PointF positionAfter)
		: base(model)
		{
			this.stack = stack;
			this.boardAfter = boardAfter;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			side = stack.Pieces[0].Side;
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stack, boardAfter),
				new MoveStackFromEdgeOfScreenAnimation(stack, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(
				new MoveStackToEdgeOfScreenAnimation(stack),
				new AttachStacksAnimation(new IStack[] { stack }));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
				new MoveToFrontOfBoardAnimation(stack, boardAfter),
				new MoveStackFromEdgeOfScreenAnimation(stack, positionAfter));
		}

		private IStack stack;
		private IBoard boardAfter;
		private PointF positionAfter;
		private Side side;
	}
}
