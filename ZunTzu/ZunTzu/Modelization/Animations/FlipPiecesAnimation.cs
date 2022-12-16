// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class FlipPiecesAnimation : Animation {

		private const long duration = 300000;
		private Guid executorPlayerGuid;

		/// <summary>Constructor</summary>
		public FlipPiecesAnimation(Guid executorPlayerGuid, IPiece[] pieces) {
			this.executorPlayerGuid = executorPlayerGuid;
			this.pieces = pieces;
			finalSides = new Side[pieces.Length];
			for(int i = 0; i < finalSides.Length; ++i)				
				finalSides[i] = (Side)(1 - (int)pieces[i].Side);
			notYetFlipped = true;
		}

		/// <summary>The time in microseconds at which this animation has ended.</summary>
		public override sealed long EndTimeInMicroseconds { get { return beginTimeInMicroseconds + duration; } }

		/// <summary>Called once when time is beginTimeInMicroseconds.</summary>
		protected override sealed void SetInitialState(IModel model) {}

		/// <summary>Called every frame.</summary>
		protected override sealed void SetIntermediateState(IModel model, long currentTimeInMicroseconds) {
			float progress = (float)(currentTimeInMicroseconds - beginTimeInMicroseconds) / (float)duration;
			float flipAngleCosinus = 1.0f - (float) Math.Sin(progress * Math.PI);
			for (int i = 0; i < pieces.Length; ++i) {
				if (!((Piece)pieces[i]).IsBlock) 
					((Piece)pieces[i]).FlipAngleCosinus = flipAngleCosinus;
			}
			if(notYetFlipped && progress > 0.5f) {
				notYetFlipped = false;
				for (int i = 0; i < pieces.Length; ++i) {
					if (!((Piece)pieces[i]).IsBlock) 
						((Piece)pieces[i]).Side = finalSides[i];
					if (((Piece)pieces[i]).IsBlock && (((Piece)pieces[i]).Owner == Guid.Empty || ((Piece)pieces[i]).Owner == executorPlayerGuid))
						((Piece)pieces[i]).Owner = ((Piece)pieces[i]).Owner == Guid.Empty ? executorPlayerGuid : Guid.Empty;
				}
			}
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < pieces.Length; ++i) {
				Piece piece = (Piece) pieces[i];
				if (notYetFlipped) {
					if (!piece.IsBlock) 
						piece.Side = finalSides[i];
					if (piece.IsBlock && (piece.Owner == Guid.Empty || piece.Owner == executorPlayerGuid))
						piece.Owner = piece.Owner == Guid.Empty ? executorPlayerGuid : Guid.Empty;
				}
				if (!piece.IsBlock)
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
		private bool notYetFlipped;
	}
}
