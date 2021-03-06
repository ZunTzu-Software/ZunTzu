// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>Summary description for FlipSelectionCommand.</summary>
	public sealed class FlipSelectionCommand : Command {

		/// <summary>Constructor.</summary>
		public FlipSelectionCommand(IModel model, ISelection selection)
		: base(model)
		{
			Debug.Assert(selection != null && !selection.Empty);

			this.selection = selection;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(selection.Stack);
			model.AnimationManager.LaunchAnimationSequence(new FlipPiecesAnimation(selection.Pieces));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			Do();
		}

		private ISelection selection;
	}
}
