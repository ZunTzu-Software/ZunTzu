// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class AddPlayerHandAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public AddPlayerHandAnimation(Guid playerGuid) {
			this.playerGuid = playerGuid;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			model.CurrentGameBox.CurrentGame.AddPlayerHand(playerGuid);
		}

		private Guid playerGuid;
	}
}
