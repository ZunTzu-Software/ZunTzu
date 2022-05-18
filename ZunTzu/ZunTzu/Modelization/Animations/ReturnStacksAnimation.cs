// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class ReturnStacksAnimation : Animation {

		private const long duration = 300000;

		/// <summary>Constructor</summary>
		public ReturnStacksAnimation(IStack[] stacks) {
			Debug.Assert(stacks != null && stacks.Length > 0 && !stacks[0].AttachedToCounterSection);
			this.stacks = stacks;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			startPosition = stacks[0].Position;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
			for(int i = 0; i < stacks.Length; ++i) {
				Stack stack = (Stack) stacks[i];
				PointF endPosition = (stack.Board == stack.Pieces[0].CounterSection.CounterSheet ?
					stack.Pieces[0].PositionWhenAttached :
					new PointF(startPosition.X, stack.Board.VisibleArea.Top - stack.BoundingBox.Height * 0.5f));
				stack.Position = new PointF(
					startPosition.X + (endPosition.X - startPosition.X) * progress,
					startPosition.Y + (endPosition.Y - startPosition.Y) * progress);
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < stacks.Length; ++i) {
				Stack stack = (Stack) stacks[i];
				stack.Position = (stack.Board == stack.Pieces[0].CounterSection.CounterSheet ?
					stack.Pieces[0].PositionWhenAttached :
					new PointF(startPosition.X, stack.Board.VisibleArea.Top - stack.BoundingBox.Height * 0.5f));
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
		private PointF startPosition;
	}
}
