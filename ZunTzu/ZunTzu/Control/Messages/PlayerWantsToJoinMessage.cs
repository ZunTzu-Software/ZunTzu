// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ZunTzu.Graphics;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerWantsToJoinMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class PlayerWantsToJoinMessage : SystemMessage {
		internal PlayerWantsToJoinMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.PlayerWantsToJoin; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer)
		{
			Debug.Assert(!serializer.IsSerializing);
			serializer.Serialize(ref _senderId);
		}

		public sealed override void Handle(Controller controller) {
			Debug.Assert(controller.Model.IsHosting);

			IModel model = controller.Model;

			ConnectionAcceptedMessage.PlayerInfo[] currentPlayers = new ConnectionAcceptedMessage.PlayerInfo[model.PlayerCount];
			{
				int i = 0;
				foreach(IPlayer player in model.Players) {
					currentPlayers[i].Id = player.Id;
					currentPlayers[i].FirstName = player.FirstName;
					currentPlayers[i].LastName = player.LastName;
					currentPlayers[i].Guid = player.Guid;
					currentPlayers[i].Color = player.Color;
					currentPlayers[i].CursorPosition = controller.View.ConvertScreenToModelCoordinates(player.CursorLocation.ScreenPosition);
					++i;
				}
			}

			ConnectionAcceptedMessage.SelectionInfo selectionInfo = new ConnectionAcceptedMessage.SelectionInfo();
			ISelection selection = model.CurrentSelection;
			if(selection == null) {
				selectionInfo.StackId = -1;
			} else {
				selectionInfo.StackId = selection.Stack.Id;
				selectionInfo.PieceIds = new int[selection.Pieces.Length];
				for(int i = 0; i < selection.Pieces.Length; ++i)
					selectionInfo.PieceIds[i] = selection.Pieces[i].Id;
			}

			byte[] gameData;
			using(MemoryStream stream = new MemoryStream()) {
				using(DeflaterOutputStream compressionStream = new DeflaterOutputStream(stream)) {
					model.CurrentGameBox.CurrentGame.Save(compressionStream, false);
					compressionStream.Flush();
				}
				stream.Flush();
				gameData = stream.ToArray();
			}

			controller.NetworkClient.Send(_senderId, new ConnectionAcceptedMessage((controller.View.DisplayProperties.GameAspectRatio == AspectRatioType.SixteenToTen), model.StateChangeSequenceNumber, currentPlayers, selectionInfo, gameData));
		}

		UInt64 _senderId = 0;
	}
}
