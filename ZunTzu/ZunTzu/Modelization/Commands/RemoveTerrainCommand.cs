// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>RemoveTerrainCommand command.</summary>
	public sealed class RemoveTerrainCommand : Command {

		public RemoveTerrainCommand(IModel model, IStack stack)
			: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && stack.Pieces.Length == 1 && stack.Pieces[0] is ITerrainClone);
			this.stack = stack;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);

			boardBefore = stack.Board;
			positionBefore = stack.Position;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stack);

			IStack[] stackAsArray = new IStack[] { stack };
			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAsArray, boardBefore),
				new ReturnStacksAnimation(stackAsArray),
				new RemoveTerrainAnimation(stack));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);

			IStack[] stackAsArray = new IStack[] { stack };
			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAsArray, boardBefore),
				new UndoReturnStacksAnimation(stackAsArray, positionBefore),
				new SetZOrderAnimation(stack, zOrderBefore));
		}

		public override void Redo() {
			preventConflict(stack);

			IStack[] stackAsArray = new IStack[] { stack };
			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAsArray, boardBefore),
				new ReturnStacksAnimation(stackAsArray),
				new RemoveTerrainAnimation(stack));
		}

		private IBoard boardBefore;
		private PointF positionBefore;
		private int zOrderBefore;
		private IStack stack;
	}
}
