// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>VoiceRecordingStoppedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VoiceRecordingStoppedMessage : SystemMessage {
		internal VoiceRecordingStoppedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VoiceRecordingStopped; } }

		public sealed override void Handle(Controller controller) {
		}
	}
}
