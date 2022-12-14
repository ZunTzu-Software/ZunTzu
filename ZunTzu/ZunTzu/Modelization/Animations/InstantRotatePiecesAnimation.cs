// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class InstantRotatePiecesAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public InstantRotatePiecesAnimation(Guid executorPlayerGuid, IPiece[] pieces, int rotationIncrements) {
			this.pieces = pieces;
			this.rotationIncrements = rotationIncrements;
			this.executorPlayerGuid = executorPlayerGuid;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			// each 120 increment is equivalent to a PI/12 angle
			for(int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece) pieces[i];
                if (!piece.IsBlock)
                {
                    int totalDetents = (int)(piece.RotationAngle * (12.0f / (float)Math.PI) + 0.5f) * 120;
					totalDetents += rotationIncrements;
					while (totalDetents >= (120 * 24))
						totalDetents -= (120 * 24);
					while (totalDetents < 0)
						totalDetents += (120 * 24);
					piece.RotationAngle = (float)(totalDetents / 120) * ((float)Math.PI / 12.0f);
                }
                else if (piece.Owner == Guid.Empty || piece.Owner == executorPlayerGuid)
				{
                    float[] rotationAngles = { -(float)Math.PI / 2.0f, 0.0f, (float)Math.PI / 2.0f, (float)Math.PI };
					int index = (int)(((piece.RotationAngle / (float)Math.PI) + 0.5f) * 2);
					int increment = rotationIncrements / 120;
					
					int indexFinal = (index + increment) % 4;
					while (indexFinal < 0)
						indexFinal += 4;
					
					piece.RotationAngle = rotationAngles[indexFinal];
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
		private int rotationIncrements;
		private Guid executorPlayerGuid;
	}
}
