// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class SplitStackAnimation : InstantaneousAnimation {

		/// <summary>Constructor.</summary>
		public SplitStackAnimation(IStack stackToSplit, IPiece[] piecesToExtract, IStack newStack) {
			this.stackToSplit = (Stack) stackToSplit;
			this.piecesToExtract = piecesToExtract;
			this.newStack = (Stack) newStack;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stackToSplit) {
				PointF[] newStackInspectorPositions = new PointF[stackToSplit.Pieces.Length - piecesToExtract.Length];

				// calculate new arrangement of pieces in stack inspector
				int j = 0;
				for(int i = 0; i < stackToSplit.Pieces.Length; ++i) {
					IPiece piece = stackToSplit.Pieces[i];
					bool isToBeExtracted = false;
					foreach(IPiece pieceToBeExtracted in piecesToExtract) {
						if(piece == pieceToBeExtracted) {
							isToBeExtracted = true;
							break;
						}
					}
					if(isToBeExtracted) {
						if(model.CurrentSelection.Contains(piece))
							model.CurrentSelection = model.CurrentSelection.RemovePiece(piece);
					} else {
						// use current arrangement for existing pieces
						newStackInspectorPositions[j] = model.StackInspectorPositions[i];
						++j;
					}
				}

				((Model)model).StackInspectorPositions = newStackInspectorPositions;

				model.AnimationManager.LaunchAnimationSequence(new StackInspectorAnimation(stackToSplit));
			}

			stackToSplit.Split(piecesToExtract, newStack);
			newStack.Position = stackToSplit.Position;
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return (stackToSplit == stack || newStack == stack);
		}

		private Stack stackToSplit;
		private IPiece[] piecesToExtract;
		private Stack newStack;
	}
}
