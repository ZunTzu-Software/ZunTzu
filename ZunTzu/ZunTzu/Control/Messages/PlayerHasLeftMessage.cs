// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.AudioVideo;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerHasLeftMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PlayerHasLeftMessage : SystemMessage {
		internal PlayerHasLeftMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.PlayerHasLeft; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer)
		{
			Debug.Assert(!serializer.IsSerializing);
			serializer.Serialize(ref _senderId);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer leavingPlayer = controller.Model.GetPlayer(_senderId);
			if(leavingPlayer != null) {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.PlayerHasLeft, leavingPlayer.FirstName + " " + leavingPlayer.LastName);
				controller.Model.RemovePlayer(_senderId);

				controller.Model.AudioManager.PlayAudioFile("Disconnect.wma");
			}
		}

		UInt64 _senderId = 0;
	}
}
