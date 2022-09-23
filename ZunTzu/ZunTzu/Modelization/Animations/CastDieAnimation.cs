// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.AudioVideo;
using ZunTzu.Numerics;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class CastDieAnimation : Animation {

		private const long duration = 700000;

		/// <summary>Constructor</summary>
		public CastDieAnimation(
			int diceHandIndex,
			int dieIndex,
			PointF initialPosition,
			PointF finalPosition,
			Quaternion initialOrientation,
			Quaternion finalOrientation,
			float initialSize,
			float finalSize,
			AudioTrack audioTrack)
		{
			this.diceHandIndex = diceHandIndex;
			this.dieIndex = dieIndex;
			this.initialPosition = initialPosition;
			this.finalPosition = finalPosition;
			this.initialOrientation = initialOrientation;
			this.finalOrientation = finalOrientation;
			this.initialSize = initialSize;
			this.finalSize = finalSize;
			this.audioTrack = audioTrack;
		}

		/// <summary>Can be run in parallel with state changes.</summary>
		public override bool RunInParallelWithStateChanges { get { return true; } }

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			diceHands[diceHandIndex].BeingCast = true;
			soundPlaying = false;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;

			Die[] dice = model.CurrentGameBox.CurrentGame.DiceHands[diceHandIndex].Dice;
			dice[dieIndex].Position = new PointF(
				initialPosition.X + (finalPosition.X - initialPosition.X) * progress,
				initialPosition.Y + (finalPosition.Y - initialPosition.Y) * progress);
			int revolutions = 3;
			float rollingAngle =  -progress * 2.0f * (float)Math.PI * revolutions;
			Quaternion rolling = Quaternion.FromAxisAndAngle(
				finalPosition.Y - initialPosition.Y,
				finalPosition.X - initialPosition.X,
				0.0f,
				rollingAngle);
			dice[dieIndex].Orientation = initialOrientation.InterpolateWith(finalOrientation, progress).ComposeWith(rolling);
			dice[dieIndex].Size = (progress > 0.5f ? finalSize : initialSize + (finalSize - initialSize) * progress * 2.0f);
			if(progress > 0.5f && audioTrack != AudioTrack.None) {
				IAudioManager audioManager = model.AudioManager;
				if(!soundPlaying) {
					audioManager.PlayAudioTrack(audioTrack);
					soundPlaying = true;
				}
				audioManager.SetAudioTrackOrigin(audioTrack, dice[dieIndex].Position);
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			Die[] dice = model.CurrentGameBox.CurrentGame.DiceHands[diceHandIndex].Dice;
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
		private bool soundPlaying;
		private AudioTrack audioTrack;
	}
}
