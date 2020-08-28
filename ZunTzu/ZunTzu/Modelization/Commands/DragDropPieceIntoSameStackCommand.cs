// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropPieceIntoSameStackCommand command.</summary>
	public sealed class DragDropPieceIntoSameStackCommand : Command {

		public DragDropPieceIntoSameStackCommand(IModel model, IPiece piece, int insertionIndex)
		: base(model)
		{
			Debug.Assert(piece.Stack.Pieces.Length > 1 && insertionIndex <= piece.Stack.Pieces.Length);
			this.piece = piece;
			indexInStackAfter = (piece.IndexInStackFromBottomToTop < insertionIndex ? insertionIndex - 1 : insertionIndex);
			stack = piece.Stack;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;
			IPiece[] arrangementBefore = stack.Pieces;
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

			model.AnimationManager.LaunchAnimationSequence(
				new RearrangeStackAnimation(stack, arrangementAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);

			IPiece[] arrangementAfter = stack.Pieces;
			IPiece[] arrangementBefore = new IPiece[arrangementAfter.Length];
			if(indexInStackAfter < indexInStackBefore) {
				Array.Copy(arrangementAfter, arrangementBefore, indexInStackAfter);
				Array.Copy(arrangementAfter, indexInStackAfter + 1, arrangementBefore, indexInStackAfter, indexInStackBefore - indexInStackAfter);
				arrangementBefore[indexInStackBefore] = arrangementAfter[indexInStackAfter];
				Array.Copy(arrangementAfter, indexInStackBefore + 1, arrangementBefore, indexInStackBefore + 1, arrangementAfter.Length - (indexInStackBefore + 1));
			} else {
				Array.Copy(arrangementAfter, arrangementBefore, indexInStackBefore);
				arrangementBefore[indexInStackBefore] = arrangementAfter[indexInStackAfter];
				Array.Copy(arrangementAfter, indexInStackBefore, arrangementBefore, indexInStackBefore + 1, indexInStackAfter - indexInStackBefore);
				Array.Copy(arrangementAfter, indexInStackAfter + 1, arrangementBefore, indexInStackAfter + 1, arrangementAfter.Length - (indexInStackAfter + 1));
			}

			model.AnimationManager.LaunchAnimationSequence(
				new RearrangeStackAnimation(stack, arrangementBefore));
		}

		private IStack stack;
		private IPiece piece;
		private int indexInStackBefore;
		private int indexInStackAfter;
	}
}
