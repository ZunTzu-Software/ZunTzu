// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class SmoothMouseAnimation : Animation {

		private const int interpolationSteps = 6;
		private const long duration = (interpolationSteps * 3000000) / 60;

		/// <summary>Constructor</summary>
		public SmoothMouseAnimation(IPlayer player, Point cursorScreenPosition) {
			this.player = player;
			endingCursorScreenPosition = cursorScreenPosition;
		}

		/// <summary>Can be run in parallel with state changes.</summary>
		public override bool RunInParallelWithStateChanges { get { return true; } }

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			startingCursorScreenPosition = player.CursorLocation.ScreenPosition;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			if(increment < interpolationSteps) {
				++increment;
				player.CursorLocation.ScreenPosition = Point.Truncate(new PointF(
					(increment * endingCursorScreenPosition.X + (interpolationSteps - increment) * startingCursorScreenPosition.X) / (float) interpolationSteps,
					(increment * endingCursorScreenPosition.Y + (interpolationSteps - increment) * startingCursorScreenPosition.Y) / (float) interpolationSteps));
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(increment < interpolationSteps) {
				player.CursorLocation.ScreenPosition = endingCursorScreenPosition;
			}
		}

		private int increment = 0;
		private IPlayer player;
		public Point startingCursorScreenPosition = Point.Empty;
		public Point endingCursorScreenPosition;
	}
}
