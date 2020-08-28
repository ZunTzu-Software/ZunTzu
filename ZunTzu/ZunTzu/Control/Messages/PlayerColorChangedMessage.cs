// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerColorChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PlayerColorChangedMessage : Message {

		internal PlayerColorChangedMessage() {}

		public PlayerColorChangedMessage(int playerId, uint color) {
			this.playerId = playerId;
			this.color = color;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.PlayerColorChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref playerId);
			serializer.Serialize(ref color);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer player = controller.Model.GetPlayer(playerId);
			if(player != null)
				player.Color = color;
		}

		private int playerId;
		private uint color;
	}
}
