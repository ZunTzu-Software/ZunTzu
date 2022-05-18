// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;

namespace ZunTzu.Modelization {

	/// <summary>Abstract class for all animations.</summary>
	public abstract class Animation : IAnimation {

		/// <summary>Sate changes are run in sequence so the end result is predictable.</summary>
		public virtual bool RunInParallelWithStateChanges { get { return false; } }

		/// <summary>Returns false if this start time of the animation is still unknown.</summary>
		internal bool BeginTimeSet { get { return beginTimeSet; } }
		private bool beginTimeSet = false;
		/// <summary>Called on the first frame of the animation.</summary>
		/// <param name="beginTimeInMicroseconds">The time at which this animation was launched.</param>
		internal void SetBeginTimeInMicroseconds(long beginTimeInMicroseconds) {
			Debug.Assert(!beginTimeSet && !initialStateSet);

			this.beginTimeInMicroseconds = beginTimeInMicroseconds;
			beginTimeSet = true;
		}
		protected long beginTimeInMicroseconds;

		/// <summary>Called on every frame of the animation.</summary>
		/// <param name="model">Current state of the program.</param>
		/// <param name="currentTimeInMicroseconds">The current time of this frame.</param>
		internal void Animate(IModel model, long currentTimeInMicroseconds) {
			Debug.Assert(beginTimeSet);

			if(!initialStateSet) {
				SetInitialState(model);
				initialStateSet = true;
			}
			if(EndTimeInMicroseconds <= currentTimeInMicroseconds)
				SetFinalState(model);
			else if(currentTimeInMicroseconds >= beginTimeInMicroseconds)
				SetIntermediateState(model, currentTimeInMicroseconds);
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal virtual bool IsBeingAnimated(IStack stack) { return false; }

		private bool initialStateSet = false;
		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public abstract long EndTimeInMicroseconds { get; }
		/// <summary>The animation to launch automatically as soon as this one ends.</summary>
		internal Animation NextChainedAnimation { get { return nextChainedAnimation; } set { nextChainedAnimation = value; } }
		private Animation nextChainedAnimation = null;

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		/// <param name="model">Current state of the program.</param>
		protected abstract void SetInitialState(IModel model);
		/// <summary>Called every frame.</summary>
		/// <param name="model">Current state of the program.</param>
		protected abstract void SetIntermediateState(IModel model, long currentTimeInMicroseconds);
		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		/// <param name="model">Current state of the program.</param>
		protected abstract void SetFinalState(IModel model);
	}

	/// <summary>Abstract class for all animations of zero duration.</summary>
	/// <remarks>These animations are typically used in the middle of a sequence of animations.</remarks>
	public abstract class InstantaneousAnimation : Animation {
		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds; } }
		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {}
		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) { Debug.Assert(false); }
	}
}
