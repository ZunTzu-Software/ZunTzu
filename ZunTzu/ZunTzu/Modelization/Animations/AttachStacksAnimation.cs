// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class AttachStacksAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public AttachStacksAnimation(IStack[] stacks) {
			this.stacks = stacks;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			foreach(Stack stack in stacks) {
				IPiece piece = stack.Pieces[0];
				ICounterSection counterSection = piece.CounterSection;
				if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack) {
					if(piece is ICounter && ((int) counterSection.Type & (1 + (int) counterSection.CounterSheet.Side)) == 0 ||
						piece is ICard && (counterSection.CounterSheet.Side == Side.Front) != counterSection.HasCardFaceOnFront)
						model.CurrentSelection = null;
				}
				((CounterSheet)counterSection.CounterSheet).MoveStackToBack(stack);
				stack.AttachedToCounterSection = true;
			}
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			foreach(IStack animatedStack in stacks)
				if(animatedStack == stack)
					return true;
			return false;
		}

		private IStack[] stacks;
	}
}
