// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class RearrangeStackAnimation : InstantaneousAnimation {

		public RearrangeStackAnimation(IStack stack, IPiece[] newArrangement) {
			this.stack = (Stack) stack;
			this.newArrangement = newArrangement;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack) {
				PointF[] newStackInspectorPositions = new PointF[newArrangement.Length];
				// use current positions, in a different order
				for(int i = 0; i < stack.Pieces.Length; ++i) {
					IPiece piece = stack.Pieces[i];
					for(int j = 0; j < newArrangement.Length; ++j) {
						if(newArrangement[j] == piece) {
							newStackInspectorPositions[j] = model.StackInspectorPositions[i];
							break;
						}
					}
				}
				((Model)model).StackInspectorPositions = newStackInspectorPositions;
				model.AnimationManager.LaunchAnimationSequence(new StackInspectorAnimation(stack));
			}

			stack.RearrangePieces(newArrangement);

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack)
				model.CurrentSelection = ((Selection)model.CurrentSelection).RearrangePieces();
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private Stack stack;
		private IPiece[] newArrangement;
	}
}
