// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>HostDisconnectedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class HostDisconnectedMessage : SystemMessage {
		internal HostDisconnectedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.HostDisconnected; } }

		public sealed override void Handle(Controller controller) {
			controller.Model.ClearTransientState();
			controller.Model.IsHosting = true;
			controller.Model.RemoveAllPlayers();
			controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.HostHasLeft);
		}
	}
}
