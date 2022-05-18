// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class FillPlayerHandAnimation : InstantaneousAnimation {

		/// <summary>Constructor.</summary>
		public FillPlayerHandAnimation(Guid playerGuid, IStack stack) {
			this.playerGuid = playerGuid;
			this.stack = stack;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(model.CurrentSelection != null && model.CurrentSelection.Stack == stack)
				model.CurrentSelection = null;

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			if(playerHand == null)
				playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.AddPlayerHand(playerGuid);
			if(stack.Board != null)
				((Board) stack.Board).RemoveStack(stack);
			playerHand.Stack = (Stack) stack;
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stackBeingAnimated) {
			return stackBeingAnimated == stack;
		}

		private Guid playerGuid;
		private IStack stack;
	}
}
