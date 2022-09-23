// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Numerics;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class ResetDieAnimation : Animation {

		private const long duration = 2000000;

		/// <summary>Constructor</summary>
		public ResetDieAnimation(
			int diceHandIndex,
			int dieIndex,
			PointF initialPosition,
			PointF finalPosition,
			Quaternion initialOrientation,
			Quaternion finalOrientation,
			float initialSize,
			float finalSize)
		{
			this.diceHandIndex = diceHandIndex;
			this.dieIndex = dieIndex;
			this.initialPosition = initialPosition;
			this.finalPosition = finalPosition;
			this.initialOrientation = initialOrientation;
			this.finalOrientation = finalOrientation;
			this.initialSize = initialSize;
			this.finalSize = finalSize;
		}

		/// <summary>Can be run in parallel with state changes.</summary>
		public override bool RunInParallelWithStateChanges { get { return true; } }

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;

			Die[] dice = model.CurrentGameBox.CurrentGame.DiceHands[diceHandIndex].Dice;
			dice[dieIndex].Position = new PointF(
				initialPosition.X + (finalPosition.X - initialPosition.X) * progress,
				initialPosition.Y + (finalPosition.Y - initialPosition.Y) * progress);
			dice[dieIndex].Orientation =
				initialOrientation.InterpolateWith(finalOrientation, progress);
			dice[dieIndex].Size = initialSize + (finalSize - initialSize) * progress;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			diceHands[diceHandIndex].BeingCast = false;

			Die[] dice = diceHands[diceHandIndex].Dice;
			dice[dieIndex].Position = finalPosition;
			dice[dieIndex].Orientation = finalOrientation;
			dice[dieIndex].Size = finalSize;
		}

		private int diceHandIndex;
		private int dieIndex;
		private PointF initialPosition;
		private PointF finalPosition;
		private Quaternion initialOrientation;
		private Quaternion finalOrientation;
		private float initialSize;
		private float finalSize;
	}
}
