// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;
using ZunTzu.Randomness;

namespace ZunTzu.Modelization.Commands {

	/// <summary>Summary description for InvertCommand.</summary>
	public sealed class InvertCommand : Command {

		/// <summary>Constructor.</summary>
		public InvertCommand(IModel model)
			: base(model)
		{
			Debug.Assert(model.CurrentSelection != null && model.CurrentSelection.Stack.Pieces.Length > 1);
			stack = model.CurrentSelection.Stack;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stack);
			model.AnimationManager.LaunchAnimationSequence(new InvertAnimation(stack));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			Do();
		}

		private IStack stack;
	}
}
