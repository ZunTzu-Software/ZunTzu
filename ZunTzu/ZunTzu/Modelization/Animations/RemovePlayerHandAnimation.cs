// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class RemovePlayerHandAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public RemovePlayerHandAnimation(Guid playerGuid) {
			this.playerGuid = playerGuid;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			model.CurrentGameBox.CurrentGame.RemovePlayerHand(playerGuid);
		}

		private Guid playerGuid;
	}
}
