// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class DetachStacksAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public DetachStacksAnimation(IStack[] stacks, Side[] sides) : this(stacks, sides, null) {}

		/// <summary>Constructor</summary>
		public DetachStacksAnimation(IStack[] stacks, Side[] sides, float[] rotationAngles) {
			Debug.Assert(stacks != null && stacks.Length > 0 &&
				sides != null && sides.Length == stacks.Length &&
				(rotationAngles == null || rotationAngles.Length == stacks.Length));
			this.stacks = stacks;
			this.sides = sides;
			this.rotationAngles = rotationAngles;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < stacks.Length; ++i) {
				Stack stack = (Stack) stacks[i];
				PointF positionWhenAttached = stack.Position;
				stack.AttachedToCounterSection = false;
				stack.Position = positionWhenAttached;
				((Piece)stack.Pieces[0]).Side = sides[i];
				if(rotationAngles != null)
					((Piece)stack.Pieces[0]).RotationAngle = rotationAngles[i];
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
		private Side[] sides;
		private float[] rotationAngles;
	}
}
