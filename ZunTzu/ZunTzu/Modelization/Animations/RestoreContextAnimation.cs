// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class RestoreContextAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public RestoreContextAnimation(CommandContext context) {
			this.context = context;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			context.VisibleBoard.VisibleArea = context.VisibleArea;
			model.CurrentGameBox.CurrentGame.VisibleBoard = context.VisibleBoard;
		}

		private CommandContext context;
	}
}
