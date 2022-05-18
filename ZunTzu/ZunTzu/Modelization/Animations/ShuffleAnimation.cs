// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.AudioVideo;
using ZunTzu.Randomness;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class ShuffleAnimation : InstantaneousAnimation {

		public ShuffleAnimation(IStack stack, Permutation permutation) {
			this.stack = (Stack) stack;
			newArrangement = permutation.Apply(stack.Pieces);
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack) {
				// use current positions, in a different order
				PointF[] currentStackInspectorPositions = model.StackInspectorPositions;
				int count = newArrangement.Length;
				Debug.Assert(count == currentStackInspectorPositions.Length);
				PointF[] newStackInspectorPositions = new PointF[count];
				unsafe {
					// use a second random permutation to mask the initial position of each piece
					bool* alreadyUsed = stackalloc bool[count];
					for(int i = 0; i < count; ++i)
						alreadyUsed[i] = false;
					for(int i = 0; i < count - 1; ++i) {
						if(i == count - 2 && !alreadyUsed[count - 1]) {
							newStackInspectorPositions[i] = currentStackInspectorPositions[count - 1]; 
							alreadyUsed[count - 1] = true;
						} else {
							int choiceCount = count - i - (alreadyUsed[i] ? 0 : 1);
							int randomNumber = model.RandomNumberGenerator.GenerateInt32(0, choiceCount - 1);
							int permutedIndex = 0;
							while(true) {
								while(alreadyUsed[permutedIndex] || permutedIndex == i)
									++permutedIndex;
								if(randomNumber == 0)
									break;
								--randomNumber;
								++permutedIndex;
							}
							newStackInspectorPositions[i] = currentStackInspectorPositions[permutedIndex];
							alreadyUsed[permutedIndex] = true;
						}
					}
					for(int i = 0; i < count; ++i) {
						if(!alreadyUsed[i]) {
							newStackInspectorPositions[count - 1] = currentStackInspectorPositions[i];
							break;
						}
					}
				}

				((Model) model).StackInspectorPositions = newStackInspectorPositions;
				model.AnimationManager.LaunchAnimationSequence(new StackInspectorAnimation(stack));

				model.AudioManager.PlayAudioTrack(AudioTrack.Shuffle);
			}

			stack.RearrangePieces(newArrangement);

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack)
				model.CurrentSelection = ((Selection) model.CurrentSelection).RearrangePieces();
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			return stack == this.stack;
		}

		private Stack stack;
		private IPiece[] newArrangement;
	}
}
