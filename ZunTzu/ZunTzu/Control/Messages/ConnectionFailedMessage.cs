// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Networking;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>ConnectionFailedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ConnectionFailedMessage : SystemMessage {
		internal ConnectionFailedMessage() { }

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.ConnectionFailed; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			Debug.Assert(!serializer.IsSerializing);
			serializer.Serialize(ref cause);
		}

		public sealed override void Handle(Controller controller) {
			string text;
			switch((ConnectionFailureCause) cause) {
				case ConnectionFailureCause.HostRejectedConnection: text = Resources.ConnectionFailureHostRejectedConnection; break;
				case ConnectionFailureCause.NoConnection: text = Resources.ConnectionFailureNoConnection; break;
				case ConnectionFailureCause.NotHost: text = Resources.ConnectionFailureNotHost; break;
				case ConnectionFailureCause.SessionFull: text = Resources.ConnectionFailureSessionFull; break;
				case ConnectionFailureCause.TimeOut: text = Resources.ConnectionFailureTimeOut; break;
				default: text = Resources.ConnectionFailureOther; break;
			}
			controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.ConnectionFailed + " " + text);
		}

		private byte cause;
	}
}
