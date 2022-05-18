// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using ZunTzu.Control.Dialogs;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>PlayerListMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GameLoadedMessage : StateChangeMessage {

		internal GameLoadedMessage() {}

		public GameLoadedMessage(int stateChangeSequenceNumber, byte[] gameData) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.gameData = gameData;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GameLoaded; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref gameData);
		}

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;

			if(!model.IsHosting) {
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
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouHaveBeenDisconnectedGameBoxNotFound, gameBoxName);
					model.AudioManager.PlayAudioFile("Disconnect.wma");
				} else {
					model.ClearTransientState();
					model.StateChangeSequenceNumber = stateChangeSequenceNumber;
					try {
						model.OpenGameBox(gameBoxReference);
						using(MemoryStream stream = new MemoryStream(gameData, false)) {
							using(InflaterInputStream compressionStream = new InflaterInputStream(stream)) {
								model.CurrentGameBox.OpenGame(compressionStream);
							}
						}
					} catch(Exception e) {
						model.OpenGameBox(model.GameLibrary.DefaultGameBox);
						model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);

						model.IsHosting = true;
						model.RemoveAllPlayers();
						controller.Model.NetworkClient.Disconnect();
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouHaveBeenDisconnectedGameBoxNotFound, gameBoxName);
						model.AudioManager.PlayAudioFile("Disconnect.wma");

						string message = string.Format(Resources.CommandError, e.GetType().Name, e.Message);
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, message);
						controller.DialogState.Dialog = new MessageDialog(SystemIcons.Error, message);
						controller.State = controller.DialogState;
						controller.View.ShowDialog(controller.DialogState.Dialog);
					} finally {
						controller.View.ResetGraphicsElements();
					}
				}
			}
		}

		private byte[] gameData;
	}
}
