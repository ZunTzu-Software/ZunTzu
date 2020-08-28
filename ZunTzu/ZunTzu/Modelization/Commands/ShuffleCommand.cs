// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;
using ZunTzu.Randomness;

namespace ZunTzu.Modelization.Commands {

	/// <summary>Summary description for ShuffleCommand.</summary>
	public sealed class ShuffleCommand : Command {

		/// <summary>Constructor.</summary>
		public ShuffleCommand(IModel model, Permutation permutation)
			: base(model)
		{
			Debug.Assert(model.CurrentSelection != null && model.CurrentSelection.Stack.Pieces.Length > 1);
			stack = model.CurrentSelection.Stack;
			this.permutation = permutation;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(new ShuffleAnimation(stack, permutation));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(new ShuffleAnimation(stack, permutation.Inverse));
		}

		private IStack stack;
		private Permutation permutation;
	}
}
