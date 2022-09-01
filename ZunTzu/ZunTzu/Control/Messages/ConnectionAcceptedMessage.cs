// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ZunTzu.Control.Dialogs;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerListMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ConnectionAcceptedMessage : ReliableMessageFromHostToSingleClient {

		internal ConnectionAcceptedMessage() {}

		public struct PlayerInfo {
			public UInt64 Id;
			public string FirstName;
			public string LastName;
			public Guid Guid;
			public uint Color;
			public PointF CursorPosition;
			public bool IsCursorVisible;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ConnectionAccepted; } }

		public struct SelectionInfo {
			public bool IsEmpty { get { return StackId == -1; } }
			public int StackId;
			public int[] PieceIds;
		}
 
		public ConnectionAcceptedMessage(
			bool widescreen,
			int stateChangeSequenceNumber,
			PlayerInfo[] currentPlayers,
			SelectionInfo selection,
			byte[] gameData)
		{
			this.widescreen = widescreen;
			this.currentPlayers = currentPlayers;
			this.selection = selection;
			this.gameData = gameData;
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref widescreen);

			int playerCount = 0;
			if(serializer.IsSerializing) {
				playerCount = currentPlayers.Length;
			}
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref playerCount);

			if(!serializer.IsSerializing) {
				currentPlayers = new PlayerInfo[playerCount];
			}

			for(int i = 0; i < playerCount; ++i) {
				serializer.Serialize(ref currentPlayers[i].Id);
				serializer.Serialize(ref currentPlayers[i].FirstName);
				serializer.Serialize(ref currentPlayers[i].LastName);
				serializer.Serialize(ref currentPlayers[i].Guid);
				serializer.Serialize(ref currentPlayers[i].Color);
				serializer.Serialize(ref currentPlayers[i].CursorPosition);
				serializer.Serialize(ref currentPlayers[i].IsCursorVisible);
			}

			serializer.Serialize(ref selection.StackId);
			if(!selection.IsEmpty) {
				int selectionSize = 0;
				if(serializer.IsSerializing) {
					selectionSize = selection.PieceIds.Length;
				}
				serializer.Serialize(ref selectionSize);
				if(!serializer.IsSerializing) {
					selection.PieceIds = new int[selectionSize];
				}
				for(int i = 0; i < selectionSize; ++i)
					serializer.Serialize(ref selection.PieceIds[i]);
			}

			serializer.Serialize(ref gameData);
		}

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;

			string gameBoxName;
			byte[] gameBoxHash;
			using(MemoryStream stream = new MemoryStream(gameData, false)) {
				using(InflaterInputStream compressionStream = new InflaterInputStream(stream)) {
					model.RetrieveGameBoxInfoFromGameData(compressionStream, out gameBoxName, out gameBoxHash);
				}
			}

			IGameBoxReference gameBoxReference = model.GameLibrary.FindGameBox(gameBoxHash);
			if(gameBoxReference == null) {
				model.ClearTransientState();
				model.IsHosting = true;
				model.RemoveAllPlayers();
				controller.Model.NetworkClient.Disconnect();
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.UnableToConnectGameBoxNotFound, gameBoxName);
				model.AudioManager.PlayAudioFile("Disconnect.wma");
				return;
			} else {
				model.ClearTransientState();
				model.RemoveAllPlayers();
				model.StateChangeSequenceNumber = stateChangeSequenceNumber;
				try {
					model.OpenGameBox(gameBoxReference);
					using(MemoryStream stream = new MemoryStream(gameData, false)) {
						using(InflaterInputStream compressionStream = new InflaterInputStream(stream)) {
							model.CurrentGameBox.OpenGame(compressionStream);
						}
					}

					if(!selection.IsEmpty) {
						IGame game = model.CurrentGameBox.CurrentGame;
						IStack stack = game.GetStackById(selection.StackId);
						ISelection newSelection = stack.Select().RemoveAllPieces();
						foreach(int pieceId in selection.PieceIds) {
							IPiece piece = game.GetPieceById(pieceId);
							newSelection = newSelection.AddPiece(piece);
						}
						model.CurrentSelection = newSelection;
					}

					foreach(ConnectionAcceptedMessage.PlayerInfo player in currentPlayers) {
						model.AddPlayer(player.Id, player.FirstName, player.LastName, player.Guid, player.Color,
							Point.Truncate(controller.View.ConvertModelToScreenCoordinates(player.CursorPosition)),
							player.IsCursorVisible);
						// is this player already connected with another instance?
						if(player.Guid == model.ThisPlayer.Guid)
							model.ThisPlayer.Guid = Guid.Empty;	// this is to prevent the sharing of a hand
					}

					DisplayProperties properties = controller.View.DisplayProperties;
					if(widescreen != (properties.GameAspectRatio == AspectRatioType.SixteenToTen)) {
						properties.GameAspectRatio = (widescreen ? AspectRatioType.SixteenToTen : AspectRatioType.FourToThree);
						controller.View.DisplayProperties = properties;
					}

					controller.View.ResetGraphicsElements();

					controller.NetworkClient.Send(new PlayerHasJoinedMessage(
						model.ThisPlayer.FirstName, model.ThisPlayer.LastName, model.ThisPlayer.Guid, controller.View.ConvertScreenToModelCoordinates(controller.MainForm.PointToClient(Cursor.Position)), model.ThisPlayer.IsCursorVisible));
				} catch(Exception e) {
					model.OpenGameBox(model.GameLibrary.DefaultGameBox);
					model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);

					model.IsHosting = true;
					model.RemoveAllPlayers();
					controller.Model.NetworkClient.Disconnect();
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.UnableToConnectGameBoxNotFound, gameBoxName);
					model.AudioManager.PlayAudioFile("Disconnect.wma");

					string message = string.Format(Resources.CommandError, e.GetType().Name, e.Message);
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, message);
					controller.DialogState.Dialog = new MessageDialog(SystemIcons.Error, message);
					controller.State = controller.DialogState;
					controller.View.ShowDialog(controller.DialogState.Dialog);

					controller.View.ResetGraphicsElements();
				}
			}
		}

		private bool widescreen;
		private PlayerInfo[] currentPlayers;
		private SelectionInfo selection;
		private byte[] gameData;
		private int stateChangeSequenceNumber;
	}
}
