// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class FlipPiecesAnimation : Animation {

		// State transition for counters
		// =============================
		//
		// +---------------------+ Initial state
		// | side: Side.Front    |
		// | flipAngleCosinus: 1 | notYetFlipped = true
		// +------+--------------+
		//        | progress >= 0.5
		//        v
		// +---------------------+
		// | side: Side.Back     |
		// | flipAngleCosinus: 0 | notYetFlipped = false
		// +------+--------------+
		//        | progress == 1
		//        v
		// +---------------------+ Final state
		// | side: Side.Back     |
		// | flipAngleCosinus: 1 |
		// +---------------------+

		// State transition for blocks
		// ===========================
		//
		//   Initially NOT owned              Initially owned
		//   -------------------              ---------------
		//
		// +----------------------+       +----------------------+     Initial state
		// | owner: Guid.Empty    |	      | owner: owner         |     -------------
		// | ownershipProgress: 0 |	      | ownershipProgress: 1 |
		// +------+---------------+	      +------+---------------+
		//        | progress == P > 0            | progress == P > 0
		//        v					             v
		// +----------------------+	      +------------------------+   Intermediate state
		// | owner: owner         |	      | owner: owner           |   ------------------
		// | ownershipProgress: P |	      | ownershipProgress: 1-P |
		// +------+---------------+	      +------+-----------------+
		//        | progress == 1	             | progress == 1
		//        v					             v
		// +----------------------+	      +----------------------+     Final state
		// | owner: owner         |	      | owner: Guid.Empty    |     -----------
		// | ownershipProgress: 1 |	      | ownershipProgress: 0 |
		// +----------------------+	      +----------------------+

		private const long duration = 300000;

		/// <summary>Constructor</summary>
		public FlipPiecesAnimation(Guid ownerGuid, IPiece[] pieces) {
			this.owner = ownerGuid;
			this.pieces = pieces;
			finalSides = new Side[pieces.Length];
			initiallyOwnedBlocks = new bool[pieces.Length];
			for (int i = 0; i < pieces.Length; ++i) {
				IPiece piece = pieces[i];
				if (piece.IsBlock) {
					initiallyOwnedBlocks[i] = (piece.Owner != Guid.Empty);
				} else {
					finalSides[i] = (Side)(1 - (int)piece.Side);
				}
			}
			notYetFlipped = true;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {
			for (int i = 0; i < pieces.Length; ++i)
			{
				IPiece piece = pieces[i];
				if (piece.IsBlock) {
					piece.Owner = owner;
				}
			}
		}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
			float flipAngleCosinus = 1.0f - (float) Math.Sin(progress * Math.PI);
			for (int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece)pieces[i];
				if (piece.IsBlock) {
					piece.BlockOwnershipTransitionProgress = (initiallyOwnedBlocks[i] ? 1.0f - progress : progress);
				} else {
					piece.FlipAngleCosinus = flipAngleCosinus;
				}
			}
			if(notYetFlipped && progress > 0.5f) {
				notYetFlipped = false;
				for (int i = 0; i < pieces.Length; ++i) {
					Piece piece = (Piece)pieces[i];
					if (!piece.IsBlock) {
						piece.Side = finalSides[i];
					}
				}
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece) pieces[i];
				if (piece.IsBlock) {
					if (initiallyOwnedBlocks[i]) {
						piece.Owner = Guid.Empty;
						piece.BlockOwnershipTransitionProgress = 0.0f;
					} else {
						piece.Owner = owner;
						piece.BlockOwnershipTransitionProgress = 1.0f;
					}
				} else {
					if (notYetFlipped) piece.Side = finalSides[i];
					piece.FlipAngleCosinus = 1.0f;
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
		private Side[] finalSides; // for non-blocks
		private bool notYetFlipped; // for non-blocks
		private bool[] initiallyOwnedBlocks; // for blocks
		private Guid owner; // for blocks
	}
}
