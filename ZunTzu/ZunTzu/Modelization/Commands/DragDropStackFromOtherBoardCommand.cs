// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropStackFromOtherBoardCommand command.</summary>
	public sealed class DragDropStackFromOtherBoardCommand : Command {

		public DragDropStackFromOtherBoardCommand(IModel model, IStack stack, IBoard boardAfter, PointF positionAfter)
		: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection && stack.Board != boardAfter);
			this.stack = stack;
			this.boardAfter = boardAfter;
			this.positionAfter = positionAfter;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);

			boardBefore = stack.Board;
			positionBefore = stack.Position;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stack);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stack, boardAfter),
				new MoveStackInstantlyAnimation(stack, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stack, boardBefore),
				new MoveStackFromEdgeOfScreenAnimation(stack, positionBefore),
				new SetZOrderAnimation(stack, zOrderBefore));
		}

		/// <summary>Execute this command.</summary>
		public override void Redo() {
			preventConflict(stack);

			boardBefore = stack.Board;
			positionBefore = stack.Position;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stack);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stack, boardAfter),
				new MoveStackFromEdgeOfScreenAnimation(stack, positionAfter));
		}

		private IStack stack;
		private IBoard boardBefore;
		private IBoard boardAfter;
		private PointF positionBefore;
		private PointF positionAfter;
		private int zOrderBefore;
	}
}
