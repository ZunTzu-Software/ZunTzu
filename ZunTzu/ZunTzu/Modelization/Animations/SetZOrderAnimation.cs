// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class SetZOrderAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public SetZOrderAnimation(IStack stack, int zOrder) {
			this.stack = stack;
			this.zOrder = zOrder;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			((Board)stack.Board).MoveStackToZOrder(stack, zOrder);
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private IStack stack;
		private int zOrder;
	}
}
