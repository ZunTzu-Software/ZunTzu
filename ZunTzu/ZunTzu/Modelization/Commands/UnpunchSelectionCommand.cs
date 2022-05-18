// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>UnpunchSelectionCommand command.</summary>
	public sealed class UnpunchSelectionCommand : Command {

		public UnpunchSelectionCommand(IModel model, IStack stack)
		: base(model)
		{
			Debug.Assert(!stack.AttachedToCounterSection);
			stacks = new IStack[stack.Pieces.Length];
			stacks[0] = stack;
			for(int i = 1; i < stacks.Length; ++i)
				stacks[i] = new Stack();
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			foreach(IStack stack in stacks)
				preventConflict(stack);

			IStack stackBefore = stacks[0];
			arrangementBefore = stackBefore.Pieces;
			boardBefore = stackBefore.Board;
			positionBefore = stackBefore.Position;
			zOrderBefore = ((Board) boardBefore).GetZOrder(stackBefore);
			sidesBefore = new Side[arrangementBefore.Length];
			rotationAnglesBefore = new float[arrangementBefore.Length];
			for(int i = 0; i < rotationAnglesBefore.Length; ++i) {
				sidesBefore[i] = arrangementBefore[i].Side;
				rotationAnglesBefore[i] = arrangementBefore[i].RotationAngle;
			}

			List<Animation> animations = new List<Animation>();
			for(int i = 1; i < stacks.Length; ++i)
				animations.Add(new SplitStackAnimation(stacks[0], new IPiece[] { arrangementBefore[i] }, stacks[i]));
			animations.Add(new MoveToFrontOfBoardAnimation(stacks, boardBefore));
			animations.Add(new ReturnStacksAnimation(stacks));
			animations.Add(new AttachStacksAnimation(stacks));

			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			foreach(IStack stack in stacks)
				preventConflict(stack);

			IStack[] otherStacks = new IStack[stacks.Length - 1];
			Array.Copy(stacks, 1, otherStacks, 0, otherStacks.Length);

			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stacks, sidesBefore, rotationAnglesBefore),
				new MoveToFrontOfBoardAnimation(stacks, boardBefore),
				new UndoReturnStacksAnimation(stacks, positionBefore),
				new MergeStacksAnimation(stacks[0], otherStacks, 1),
				new SetZOrderAnimation(stacks[0], zOrderBefore));
		}

		public override void Redo() {
			foreach(IStack stack in stacks)
				preventConflict(stack);

			List<Animation> animations = new List<Animation>();
			for(int i = 1; i < stacks.Length; ++i)
				animations.Add(new SplitStackAnimation(stacks[0], new IPiece[] { arrangementBefore[i] }, stacks[i]));
			animations.Add(new MoveToFrontOfBoardAnimation(stacks, boardBefore));
			animations.Add(new ReturnStacksAnimation(stacks));
			animations.Add(new AttachStacksAnimation(stacks));

			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private IPiece[] arrangementBefore;
		private IBoard boardBefore;
		private PointF positionBefore;
		private int zOrderBefore;
		private IStack[] stacks;
		private Side[] sidesBefore;
		private float[] rotationAnglesBefore;
	}
}
