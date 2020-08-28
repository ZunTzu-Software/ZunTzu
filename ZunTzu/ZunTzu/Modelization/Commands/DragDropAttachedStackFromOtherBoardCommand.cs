// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackFromOtherBoardCommand command.</summary>
	public sealed class DragDropAttachedStackFromOtherBoardCommand : Command {

		public DragDropAttachedStackFromOtherBoardCommand(IModel model, IStack stack, IBoard boardAfter, PointF positionAfter)
		: base(model)
		{
			this.stack = stack;
			this.boardAfter = boardAfter;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			if(stack.Pieces[0] is ITerrainPrototype) {
				clone = new TerrainClone((TerrainPrototype) stack.Pieces[0]);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(clone.Stack, boardAfter),
					new MoveStackInstantlyAnimation(clone.Stack, positionAfter));
			} else {
				side = stack.Pieces[0].Side;
				model.AnimationManager.LaunchAnimationSequence(
					new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
					new MoveToFrontOfBoardAnimation(stack, boardAfter),
					new MoveStackInstantlyAnimation(stack, positionAfter));
			}
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			if(clone != null) {
				preventConflict(clone.Stack);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveStackToEdgeOfScreenAnimation(clone.Stack),
					new RemoveTerrainAnimation(clone.Stack));
			} else {
				preventConflict(stack);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveStackToEdgeOfScreenAnimation(stack),
					new AttachStacksAnimation(new IStack[] { stack }));
			}
		}

		/// <summary>Execute this command.</summary>
		public override void Redo() {
			if(clone != null) {
				preventConflict(clone.Stack);
				model.AnimationManager.LaunchAnimationSequence(
					new MoveToFrontOfBoardAnimation(clone.Stack, boardAfter),
					new MoveStackFromEdgeOfScreenAnimation(clone.Stack, positionAfter));
			} else {
				preventConflict(stack);
				model.AnimationManager.LaunchAnimationSequence(
					new DetachStacksAnimation(new IStack[] { stack }, new Side[] { side }),
					new MoveToFrontOfBoardAnimation(stack, boardAfter),
					new MoveStackFromEdgeOfScreenAnimation(stack, positionAfter));
			}
		}

		private IStack stack;
		private ITerrainClone clone = null;
		private IBoard boardAfter;
		private PointF positionAfter;
		private Side side;
	}
}
