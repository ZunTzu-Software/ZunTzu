// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class RotatePiecesAnimation : Animation {

		private const long duration = 300000;

		/// <summary>Constructor</summary>
		public RotatePiecesAnimation(IPiece[] pieces, int rotationIncrements) {
			this.pieces = pieces;
			this.initialRotationAngle = new float[pieces.Length];
			this.rotationIncrements = rotationIncrements;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			for(int i = 0; i < pieces.Length; ++i)
				initialRotationAngle[i] = pieces[i].RotationAngle;
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float) (currentTimeInMicroseconds - beginTimeInMicroseconds) / (float) duration;

			for (int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece) pieces[i];
				float rotation;
				if (piece.IsBlock) {
					// each 120 increment is equivalent to a PI/2 angle
					rotation = (float)(rotationIncrements / 120) * ((float)Math.PI / 2.0f);
				} else {
					// each 120 increment is equivalent to a PI/12 angle
					rotation = (float)(rotationIncrements / 120) * ((float)Math.PI / 12.0f);
				}
				while (rotation > (float)Math.PI)
					rotation -= 2 * (float)Math.PI;
				while (rotation <= (float)-Math.PI)
					rotation += 2 * (float)Math.PI;
				piece.RotationAngle = initialRotationAngle[i] + rotation * progress;
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece)pieces[i];
				if (piece.IsBlock) {
					// each 120 increment is equivalent to a PI/2 angle
					int totalDetents = rotationIncrements + (int)(initialRotationAngle[i] * (2.0f / (float)Math.PI) + 0.5f) * 120;
					while (totalDetents >= (120 * 4))
						totalDetents -= (120 * 4);
					while (totalDetents < 0)
						totalDetents += (120 * 4);
					piece.RotationAngle = (float)(totalDetents / 120) * ((float)Math.PI / 2.0f);
				} else {
					// each 120 increment is equivalent to a PI/12 angle
					int totalDetents = rotationIncrements + (int)(initialRotationAngle[i] * (12.0f / (float)Math.PI) + 0.5f) * 120;
					while (totalDetents >= (120 * 24))
						totalDetents -= (120 * 24);
					while (totalDetents < 0)
						totalDetents += (120 * 24);
					piece.RotationAngle = (float)(totalDetents / 120) * ((float)Math.PI / 12.0f);
				}
			}
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			foreach(IPiece animatedPiece in pieces)
				if(animatedPiece.Stack == stack)
					return true;
			return false;
		}

		private IPiece[] pieces;
		private float[] initialRotationAngle;
		private int rotationIncrements;
	}
}
