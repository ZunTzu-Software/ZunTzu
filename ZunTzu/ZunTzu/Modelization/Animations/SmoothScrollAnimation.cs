// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class SmoothScrollAnimation : Animation {

		private const int interpolationSteps = 6;
		private const long duration = (interpolationSteps * 3000000) / 60;

		/// <summary>Constructor</summary>
		public SmoothScrollAnimation(IBoard visibleBoard, RectangleF visibleArea, IPlayer scrollingPlayer, Point cursorScreenPosition) {
			this.visibleBoard = visibleBoard;
			endingVisibleArea = visibleArea;
			this.scrollingPlayer = scrollingPlayer;
			endingCursorScreenPosition = cursorScreenPosition;
		}

		/// <summary>Can be run in parallel with state changes.</summary>
		public override bool RunInParallelWithStateChanges { get { return true; } }

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			startingVisibleArea = visibleBoard.VisibleArea;
			startingCursorScreenPosition = scrollingPlayer.CursorLocation.ScreenPosition;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			if(increment < interpolationSteps) {
				++increment;
				scrollingPlayer.CursorLocation.ScreenPosition = Point.Truncate(new PointF(
					(increment * endingCursorScreenPosition.X + (interpolationSteps - increment) * startingCursorScreenPosition.X) / (float) interpolationSteps,
					(increment * endingCursorScreenPosition.Y + (interpolationSteps - increment) * startingCursorScreenPosition.Y) / (float) interpolationSteps));
				visibleBoard.VisibleArea = RectangleF.FromLTRB(
					(increment * endingVisibleArea.Left + (interpolationSteps - increment) * startingVisibleArea.Left) / (float) interpolationSteps,
					(increment * endingVisibleArea.Top + (interpolationSteps - increment) * startingVisibleArea.Top) / (float) interpolationSteps,
					(increment * endingVisibleArea.Right + (interpolationSteps - increment) * startingVisibleArea.Right) / (float) interpolationSteps,
					(increment * endingVisibleArea.Bottom + (interpolationSteps - increment) * startingVisibleArea.Bottom) / (float) interpolationSteps);
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(increment < interpolationSteps) {
				scrollingPlayer.CursorLocation.ScreenPosition = endingCursorScreenPosition;
				visibleBoard.VisibleArea = endingVisibleArea;
			}
		}

		private int increment = 0;
		private IBoard visibleBoard;
		public RectangleF startingVisibleArea = RectangleF.Empty;
		public RectangleF endingVisibleArea;
		private IPlayer scrollingPlayer;
		public Point startingCursorScreenPosition = Point.Empty;
		public Point endingCursorScreenPosition;
	}
}
