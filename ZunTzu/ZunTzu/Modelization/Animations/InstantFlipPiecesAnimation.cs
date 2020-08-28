// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class InstantFlipPiecesAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public InstantFlipPiecesAnimation(IPiece[] pieces) {
			this.pieces = pieces;
			finalSides = new Side[pieces.Length];
			for(int i = 0; i < finalSides.Length; ++i)
				finalSides[i] = (Side) (1 - (int) pieces[i].Side);
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece) pieces[i];
				piece.Side = finalSides[i];
				piece.FlipAngleCosinus = 1.0f;
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
		private Side[] finalSides;
	}
}
