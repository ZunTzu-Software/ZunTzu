// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class MoveStackToEdgeOfScreenAnimation : Animation {

		private const long duration = 300000;

		/// <summary>Constructor</summary>
		public MoveStackToEdgeOfScreenAnimation(IStack stack) {
			this.stack = (Stack) stack;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			startPosition = stack.Position;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			PointF endPosition = new PointF(
				startPosition.X,
				stack.Board.VisibleArea.Top - stack.BoundingBox.Height * 0.5f);
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
			stack.Position = new PointF(
				startPosition.X + (endPosition.X - startPosition.X) * progress,
				startPosition.Y + (endPosition.Y - startPosition.Y) * progress);
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			stack.Position = new PointF(
				startPosition.X,
				stack.Board.VisibleArea.Top - stack.BoundingBox.Height * 0.5f);
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private Stack stack;
		private PointF startPosition;
	}
}
