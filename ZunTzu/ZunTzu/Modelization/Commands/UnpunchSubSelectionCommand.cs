// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>UnpunchSubSelectionCommand command.</summary>
	public sealed class UnpunchSubSelectionCommand : Command {

		public UnpunchSubSelectionCommand(IModel model, ISelection selection)
		: base(model)
		{
			Debug.Assert(selection.Pieces.Length > 0 && selection.Pieces.Length < selection.Stack.Pieces.Length);
			this.selection = selection;
			stacksAfter = new IStack[selection.Pieces.Length];
			for(int i = 0; i < stacksAfter.Length; ++i)
				stacksAfter[i] = new Stack();
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(selection.Stack);
			foreach(IStack stack in stacksAfter)
				preventConflict(stack);

			arrangementBefore = selection.Stack.Pieces;
			sidesBefore = new Side[stacksAfter.Length];
			rotationAnglesBefore = new float[stacksAfter.Length];
			for(int i = 0; i < rotationAnglesBefore.Length; ++i) {
				sidesBefore[i] = selection.Pieces[i].Side;
				rotationAnglesBefore[i] = selection.Pieces[i].RotationAngle;
			}

			List<Animation> animations = new List<Animation>();
			for(int i = 0; i < stacksAfter.Length; ++i)
				animations.Add(new SplitStackAnimation(selection.Stack, new IPiece[] { selection.Pieces[i] }, stacksAfter[i]));
			animations.Add(new MoveToFrontOfBoardAnimation(stacksAfter, selection.Stack.Board));
			animations.Add(new ReturnStacksAnimation(stacksAfter));
			animations.Add(new AttachStacksAnimation(stacksAfter));

			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(selection.Stack);
			foreach(IStack stack in stacksAfter)
				preventConflict(stack);

			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stacksAfter, sidesBefore, rotationAnglesBefore),
				new MoveToFrontOfBoardAnimation(stacksAfter, selection.Stack.Board),
				new UndoReturnStacksAnimation(stacksAfter, selection.Stack.Position),
				new MergeStacksAnimation(selection.Stack, stacksAfter, 0),
				new RearrangeStackAnimation(selection.Stack, arrangementBefore));
		}

		public override void Redo() {
			preventConflict(selection.Stack);
			foreach(IStack stack in stacksAfter)
				preventConflict(stack);

			List<Animation> animations = new List<Animation>();
			for(int i = 0; i < stacksAfter.Length; ++i)
				animations.Add(new SplitStackAnimation(selection.Stack, new IPiece[] { selection.Pieces[i] }, stacksAfter[i]));
			animations.Add(new MoveToFrontOfBoardAnimation(stacksAfter, selection.Stack.Board));
			animations.Add(new ReturnStacksAnimation(stacksAfter));
			animations.Add(new AttachStacksAnimation(stacksAfter));

			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private ISelection selection;
		private IPiece[] arrangementBefore;
		private IStack[] stacksAfter;
		private Side[] sidesBefore;
		private float[] rotationAnglesBefore;
	}
}
