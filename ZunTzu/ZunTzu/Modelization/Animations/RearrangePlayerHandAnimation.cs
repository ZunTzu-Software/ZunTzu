// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class RearrangePlayerHandAnimation : InstantaneousAnimation {

		public RearrangePlayerHandAnimation(IPlayerHand playerHand, int currentIndex, int insertionIndex) {
			this.playerHand = (PlayerHand) playerHand;
			indexInStackBefore = currentIndex;
			indexInStackAfter = (insertionIndex < currentIndex ? insertionIndex : insertionIndex - 1);
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			Stack stack = playerHand.Stack;
			if(stack != null) {
				IPiece[] arrangementBefore = stack.Pieces;
				if(arrangementBefore.Length > Math.Min(0, Math.Min(indexInStackBefore, indexInStackAfter) - 1)) {
					IPiece[] arrangementAfter = new IPiece[arrangementBefore.Length];
					if(indexInStackBefore < indexInStackAfter) {
						Array.Copy(arrangementBefore, arrangementAfter, indexInStackBefore);
						Array.Copy(arrangementBefore, indexInStackBefore + 1, arrangementAfter, indexInStackBefore, indexInStackAfter - indexInStackBefore);
						arrangementAfter[indexInStackAfter] = arrangementBefore[indexInStackBefore];
						Array.Copy(arrangementBefore, indexInStackAfter + 1, arrangementAfter, indexInStackAfter + 1, arrangementBefore.Length - (indexInStackAfter + 1));
					} else {
						Array.Copy(arrangementBefore, arrangementAfter, indexInStackAfter);
						arrangementAfter[indexInStackAfter] = arrangementBefore[indexInStackBefore];
						Array.Copy(arrangementBefore, indexInStackAfter, arrangementAfter, indexInStackAfter + 1, indexInStackBefore - indexInStackAfter);
						Array.Copy(arrangementBefore, indexInStackBefore + 1, arrangementAfter, indexInStackBefore + 1, arrangementBefore.Length - (indexInStackBefore + 1));
					}
					stack.RearrangePieces(arrangementAfter);
				}
			}
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == playerHand.Stack;
		}

		private PlayerHand playerHand;
		private int indexInStackBefore;
		private int indexInStackAfter;
	}
}
