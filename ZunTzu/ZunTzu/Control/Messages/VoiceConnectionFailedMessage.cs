// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Networking;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>VoiceConnectionFailedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VoiceConnectionFailedMessage : SystemMessage {
		internal VoiceConnectionFailedMessage() { }

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VoiceConnectionFailed; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			Debug.Assert(!serializer.IsSerializing);
			serializer.Serialize(ref cause);
		}

		public sealed override void Handle(Controller controller) {
			string text;
			switch((VoiceFailureCause) cause) {
				case VoiceFailureCause.CompressionNotSupported: text = Resources.VoiceFailureCompressionNotSupported; break;
				case VoiceFailureCause.IncompatibleVersion: text = Resources.VoiceFailureIncompatibleVersion; break;
				case VoiceFailureCause.NoVoiceSession: text = Resources.VoiceFailureNoVoiceSession; break;
				case VoiceFailureCause.RunSetup: text = Resources.VoiceFailureRunSetup; break;
				case VoiceFailureCause.SoundInitFailure: text = Resources.VoiceFailureSoundInitFailure; break;
				case VoiceFailureCause.TimeOut: text = Resources.VoiceFailureTimeOut; break;
				default: text = Resources.VoiceFailureOther; break;
			}
			controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.VoiceConnectionFailed + " " + text);
		}

		private byte cause;
	}
}
