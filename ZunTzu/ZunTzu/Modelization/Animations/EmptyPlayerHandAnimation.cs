// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class EmptyPlayerHandAnimation : InstantaneousAnimation {

		/// <summary>Constructor.</summary>
		public EmptyPlayerHandAnimation(Guid playerGuid, IStack stack) {
			this.playerGuid = playerGuid;
			this.stack = stack;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			if(playerHand != null) {
				playerHand.Stack = null;
			}
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
