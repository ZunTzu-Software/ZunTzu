// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class UndoReturnStacksAnimation : Animation {

		private const long duration = 300000;

		/// <summary>Constructor</summary>
		public UndoReturnStacksAnimation(IStack[] stacks, PointF endPosition) {
			Debug.Assert(stacks != null && stacks.Length > 0);
			this.stacks = stacks;
			this.endPosition = endPosition;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
			for(int i = 0; i < stacks.Length; ++i) {
				Stack stack = (Stack) stacks[i];
				PointF startPosition = (stack.Board == stack.Pieces[0].CounterSection.CounterSheet ?
					stack.Pieces[0].PositionWhenAttached :
					new PointF(endPosition.X, stack.Board.VisibleArea.Top - stack.BoundingBox.Height * 0.5f));
				stack.Position = new PointF(
					startPosition.X + (endPosition.X - startPosition.X) * progress,
					startPosition.Y + (endPosition.Y - startPosition.Y) * progress);
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < stacks.Length; ++i)
				((Stack)stacks[i]).Position = endPosition;
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
		private PointF endPosition;
	}
}
