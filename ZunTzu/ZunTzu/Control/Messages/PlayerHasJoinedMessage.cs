// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerHasJoined.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PlayerHasJoinedMessage : ReliableMessageFromClientToAll
	{

		internal PlayerHasJoinedMessage() {}

		public PlayerHasJoinedMessage(string playerFirstName, string playerLastName, Guid playerGuid, PointF playerCursorPosition, bool playerCursorIsVisible) {
			this.playerFirstName = playerFirstName;
			this.playerLastName = playerLastName;
			this.playerGuid = playerGuid;
			this.playerCursorPosition = playerCursorPosition;
			this.playerCursorIsVisible = playerCursorIsVisible;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.PlayerHasJoined; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref playerFirstName);
			serializer.Serialize(ref playerLastName);
			serializer.Serialize(ref playerGuid);
			serializer.Serialize(ref playerCursorPosition);
			serializer.Serialize(ref playerCursorIsVisible);
		}

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;
			uint playerColor = 0xffffffff;
			if(model.IsHosting) {
				// selected the first available color
				if(senderId == model.NetworkClient.PlayerId) {
					playerColor = model.ThisPlayer.Color;
				} else {
					int colorIndex = 0;
					bool colorAlreadyUsed;
					do {
						colorAlreadyUsed = false;
						playerColor = playerColors[colorIndex % playerColors.Length];
						foreach(IPlayer player in model.Players) {
							if(player.Color == playerColor) {
								colorAlreadyUsed = true;
								++colorIndex;
								break;
							}
						}
					} while(colorAlreadyUsed);
					controller.NetworkClient.Send(new PlayerColorChangedMessage(senderId, playerColor));
				}
			}
			model.AddPlayer(senderId, playerFirstName, playerLastName, playerGuid, playerColor,
				Point.Truncate(controller.View.ConvertModelToScreenCoordinates(playerCursorPosition)),
				playerCursorIsVisible);
			model.CommandManager.ClearUndoStack();
			controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.PlayerHasJoined, playerFirstName + " " + playerLastName);

			if(senderId != model.NetworkClient.PlayerId) {
				model.AudioManager.PlayAudioFile("Connect.wma");
				controller.FlashWindow();
			}
		}

		private string playerFirstName;
		private string playerLastName;
		private Guid playerGuid;
		private PointF playerCursorPosition;
		private bool playerCursorIsVisible;

		private static readonly uint[] playerColors = {
				0xFFFFFF00,	// yellow (hosting player)
				0xFF0000FF,	// blue (first player to connect)
				0xFF00FF00,	// green (second player to connect)
				0xFF00FFFF,	// turquoise (third...)
				0xFFEFEFEF,	// grey
				0xFFEF00EF,	// purple
				0xFFEFFFEF,	// light green
				0xFFEFEF00,	// gold
				0xFFEFEFFF,	// light blue
				0xFFFFEFEF,	// pink
				0xFF00EFEF,	// dark turquoise
				0xFFEF0000,	// dark red
				0xFFEFFFFF,	// light turquoise
				0xFFFFEF00,	// orange
				0xFF00EFFF, 0xFF00EF00, 0xFFFFEFFF, 0xFF0000EF, 0xFFFFFFEF,
				0xFFFF00EF, 0xFF00FFEF, 0xFFEF00FF, 0xFFEFFF00
		};
	}
}
