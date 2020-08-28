// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>VoicePlaybackStoppedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VoicePlaybackStoppedMessage : SystemMessage {
		internal VoicePlaybackStoppedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VoicePlaybackStopped; } }

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null)
				sender.VoicePlaybackInProgress = false;
		}
	}
}
