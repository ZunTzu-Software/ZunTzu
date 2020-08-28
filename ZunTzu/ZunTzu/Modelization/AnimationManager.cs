// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;

namespace ZunTzu.Modelization {

	/// <summary>The component in charge of running animations.</summary>
	public sealed class AnimationManager : IAnimationManager {

		/// <summary>Constructor.</summary>
		internal AnimationManager(Model model) {
			this.model = model;
		}

		/// <summary>Starts an animation sequence.</summary>
		/// <param name="animationSequence">All the animations to chain, ordered as in the sequence.</param>
		public void LaunchAnimationSequence(params IAnimation[] animationSequence) {
			for(int i = 0; i < animationSequence.Length - 1; ++i) {
				((Animation)animationSequence[i]).NextChainedAnimation = (Animation) animationSequence[i + 1];
			}
			animations.Add((Animation) animationSequence[0]);
		}

		/// <summary>Updates the game state according to all animations still running.</summary>
		/// <param name="currentTimeInMicroseconds">The current time of this frame.</param>
		public void Animate(long currentTimeInMicroseconds) {
			int i = 0;
			bool stateChangePending = false;
			while(i < animations.Count) {
				Animation animation = animations[i];

				if(!stateChangePending || animation.RunInParallelWithStateChanges) {
					if(!animation.BeginTimeSet)
						animation.SetBeginTimeInMicroseconds(currentTimeInMicroseconds);
					animation.Animate(model, currentTimeInMicroseconds);

					if(animation.EndTimeInMicroseconds <= currentTimeInMicroseconds) {
						if(animation.NextChainedAnimation != null) {
							animation.NextChainedAnimation.SetBeginTimeInMicroseconds(animation.EndTimeInMicroseconds);
							animations[i] = animation.NextChainedAnimation;
						} else {
							animations.RemoveAt(i);
						}
					} else {
						if(!stateChangePending)
							stateChangePending = !animation.RunInParallelWithStateChanges;
						++i;
					}
				} else {
					++i;
				}
			}
		}

		/// <summary>Stops all animations immediately.</summary>
		/// <remarks>This is used for instance when a game is loaded.</remarks>
		public void EndAllAnimations() {
			int i = 0;
			while(i < animations.Count) {
				Animation animation = animations[i];

				if(!animation.BeginTimeSet)
					animation.SetBeginTimeInMicroseconds(0L);
				animation.Animate(model, animation.EndTimeInMicroseconds);

				if(animation.NextChainedAnimation != null) {
					animation.NextChainedAnimation.SetBeginTimeInMicroseconds(animation.EndTimeInMicroseconds);
					animations[i] = animation.NextChainedAnimation;
				} else {
					++i;
				}
			}
			animations.Clear();
		}

		/// <summary>Determines if a stack is currently involved in an animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		public bool IsBeingAnimated(IStack stack) {
			foreach(Animation animation in animations)
				for(Animation chainedAnimation = animation; chainedAnimation != null; chainedAnimation = chainedAnimation.NextChainedAnimation)
					if(chainedAnimation.IsBeingAnimated(stack))
						return true;
			return false;
		}

		private readonly Model model;
		private List<Animation> animations = new List<Animation>();
	}
}
