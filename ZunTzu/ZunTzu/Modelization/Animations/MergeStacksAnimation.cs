// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class MergeStacksAnimation : InstantaneousAnimation {

		public MergeStacksAnimation(IStack containingStack, IStack containedStack, int insertionIndex) {
			this.containingStack = (Stack) containingStack;
			this.containedStacks = new IStack[] { containedStack };
			this.insertionIndex = insertionIndex;
		}

		public MergeStacksAnimation(IStack containingStack, IStack[] containedStack, int insertionIndex) {
			this.containingStack = (Stack) containingStack;
			this.containedStacks = containedStack;
			this.insertionIndex = insertionIndex;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			int totalPieceCount = containingStack.Pieces.Length;
			foreach(IStack stack in containedStacks)
				totalPieceCount += stack.Pieces.Length;

			if(model.CurrentSelection != null) {
				if(model.CurrentSelection.Stack == containingStack) {
					PointF[] newStackInspectorPositions = new PointF[totalPieceCount];

					// calculate ideal arrangement of pieces in stack inspector
					float yPos = 0.0f;
					for(int i = containingStack.Pieces.Length - 1; i >= insertionIndex; --i) {
						yPos += containingStack.Pieces[i].Diagonal;
					}
					int j = totalPieceCount - 1 - containingStack.Pieces.Length + insertionIndex;
					for(int stackIndex = containedStacks.Length - 1; stackIndex >= 0; --stackIndex) {
						IPiece[] stackPieces = containedStacks[stackIndex].Pieces;
						for(int i = stackPieces.Length - 1; i >= 0; --i) {
							newStackInspectorPositions[j--] = new PointF(1.0f, yPos);
							yPos += stackPieces[i].Diagonal;
						}
					}

					// use current arrangement for existing pieces
					Array.Copy(model.StackInspectorPositions, newStackInspectorPositions, insertionIndex);
					Array.Copy(model.StackInspectorPositions, insertionIndex, newStackInspectorPositions,
						totalPieceCount - containingStack.Pieces.Length + insertionIndex, containingStack.Pieces.Length - insertionIndex);

					((Model) model).StackInspectorPositions = newStackInspectorPositions;

					model.AnimationManager.LaunchAnimationSequence(new StackInspectorAnimation(containingStack));
				} else {
					foreach(IStack stack in containedStacks) {
						if(model.CurrentSelection.Stack == stack) {
							model.CurrentSelection = null;
							break;
						}
					}
				}
			}

			for(int stackIndex = containedStacks.Length - 1; stackIndex >= 0; --stackIndex) {
				IStack containedStack = containedStacks[stackIndex];
				containingStack.MergeToPosition(containedStack, containedStack.Pieces, insertionIndex);
				((Board)containedStack.Board).RemoveStack(containedStack);
			}
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			if(containingStack == stack)
				return true;
			foreach(IStack containedStack in containedStacks)
				if(containedStack == stack)
					return true;
			return false;
		}

		private Stack containingStack;
		private IStack[] containedStacks;
		private int insertionIndex;
	}
}
