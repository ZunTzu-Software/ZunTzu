// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackCommand command.</summary>
	public sealed class DragDropAttachedStackCommand : Command {

		public DragDropAttachedStackCommand(IModel model, IStack stack, PointF positionAfter)
		: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection);
			this.stack = stack;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			if(stack.Pieces[0] is ITerrainPrototype) {
				clone = new TerrainClone((TerrainPrototype) stack.Pieces[0]);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(clone.Stack, stack.Board),
					new MoveStackInstantlyAnimation(clone.Stack, positionAfter));
			} else {
				side = stack.Pieces[0].Side;
				model.AnimationManager.LaunchAnimationSequence(
					new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
					new MoveToFrontOfBoardAnimation(stack, stack.Board),
					new MoveStackInstantlyAnimation(stack, positionAfter));
			}
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			if(clone != null) {
				preventConflict(clone.Stack);
				model.AnimationManager.LaunchAnimationSequence(
					new ReturnStacksAnimation(new IStack[] { clone.Stack }),
					new RemoveTerrainAnimation(clone.Stack));
			} else {
				preventConflict(stack);
				IStack[] stackAsArray = new IStack[] { stack };
				model.AnimationManager.LaunchAnimationSequence(
					new ReturnStacksAnimation(stackAsArray),
					new AttachStacksAnimation(stackAsArray));
			}
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			if(clone != null) {
				preventConflict(clone.Stack);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(clone.Stack, stack.Board),
					new MoveStackAnimation(clone.Stack, positionAfter));
			} else {
				preventConflict(stack);
				model.AnimationManager.LaunchAnimationSequence(
					new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
					new MoveToFrontOfBoardAnimation(stack, stack.Board),
					new MoveStackAnimation(stack, positionAfter));
			}
		}

		private IStack stack;
		private ITerrainClone clone = null;
		private PointF positionAfter;
		private Side side;
	}
}
