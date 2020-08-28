// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>VoiceRecordingStartedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VoiceRecordingStartedMessage : SystemMessage {
		internal VoiceRecordingStartedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VoiceRecordingStarted; } }

		public sealed override void Handle(Controller controller) {
		}
	}
}
