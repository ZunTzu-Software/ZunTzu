// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class InvertAnimation : InstantaneousAnimation {

		public InvertAnimation(IStack stack) {
			this.stack = (Stack) stack;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			int count = stack.Pieces.Length;

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack) {
				PointF[] currentStackInspectorPositions = model.StackInspectorPositions;
				Debug.Assert(count == currentStackInspectorPositions.Length);
				PointF[] newStackInspectorPositions = new PointF[count];
				for(int i = 0; i < count; ++i)
					newStackInspectorPositions[i] = currentStackInspectorPositions[(count - 1) - i];
				((Model) model).StackInspectorPositions = newStackInspectorPositions;
				model.AnimationManager.LaunchAnimationSequence(new StackInspectorAnimation(stack));
			}

			IPiece[] newArrangement = new IPiece[count];
			for(int i = 0; i < count; ++i)
				newArrangement[i] = stack.Pieces[(count - 1) - i];
			stack.RearrangePieces(newArrangement);

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack)
				model.CurrentSelection = ((Selection) model.CurrentSelection).RearrangePieces();
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private Stack stack;
	}
}
