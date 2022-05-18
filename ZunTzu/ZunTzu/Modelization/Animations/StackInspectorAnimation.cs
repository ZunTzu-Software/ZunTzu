// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class StackInspectorAnimation : Animation {

		private const long duration = 300000;

		public StackInspectorAnimation(IStack stack) {
			this.stack = stack;
		}

		/// <summary>Can be run in parallel with state changes.</summary>
		public override bool RunInParallelWithStateChanges { get { return true; } }

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + (endImmediately ? (long)0 : duration); } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack) {
				startPositions = (PointF[]) model.StackInspectorPositions.Clone();
				endPositions = new PointF[startPositions.Length];

				// arrange pieces in stack inspector
				float yPos = 0.0f;
				for(int i = endPositions.Length - 1; i >= 0; --i) {
					endPositions[i] = new PointF(0.0f, yPos);
					yPos += stack.Pieces[i].Diagonal;
				}
			} else {
				endImmediately = true;
			}
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			if(!endImmediately && model.CurrentSelection != null && model.CurrentSelection.Stack == stack &&
				model.StackInspectorPositions.Length == endPositions.Length)
			{
				float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
				for(int i = 0; i < startPositions.Length; ++i) {
					model.StackInspectorPositions[i] = new PointF(
						startPositions[i].X,
						startPositions[i].Y + (endPositions[i].Y - startPositions[i].Y) * progress);
				}
			} else {
				endImmediately = true;
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(!endImmediately && model.CurrentSelection != null && model.CurrentSelection.Stack == stack &&
				model.StackInspectorPositions.Length == endPositions.Length)
			{
				((Model)model).StackInspectorPositions = endPositions;
			}
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private IStack stack;
		private PointF[] startPositions = null;
		private PointF[] endPositions = null;
		private bool endImmediately = false;
	}
}
