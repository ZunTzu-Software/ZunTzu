// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>VoicePlaybackStartedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VoicePlaybackStartedMessage : SystemMessage {
		internal VoicePlaybackStartedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VoicePlaybackStarted; } }

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null)
				sender.VoicePlaybackInProgress = true;
		}
	}
}
