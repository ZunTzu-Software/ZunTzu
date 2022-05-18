// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Control.Dialogs;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>GameBoxHasChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GameBoxHasChangedMessage : StateChangeMessage {

		internal GameBoxHasChangedMessage() {}

		public GameBoxHasChangedMessage(int stateChangeSequenceNumber, string gameBoxName, byte[] hash) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.gameBoxName = gameBoxName;
			this.hash = hash;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GameBoxHasChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref gameBoxName);
			serializer.Serialize(ref hash);
		}

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;

			if(!model.IsHosting)
				model.StateChangeSequenceNumber = stateChangeSequenceNumber;

			IGameBoxReference gameBoxReference = model.GameLibrary.FindGameBox(hash);
			if(gameBoxReference == null) {
				model.ClearTransientState();
				model.IsHosting = true;
				model.RemoveAllPlayers();
				controller.Model.NetworkClient.Disconnect();
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouHaveBeenDisconnectedGameBoxNotFound, gameBoxName);
				model.AudioManager.PlayAudioFile("Disconnect.wma");
			} else {
				model.ClearTransientState();
				try {
					model.OpenGameBox(gameBoxReference);
					model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.GameBoxOpened, model.CurrentGameBox.Reference.Name);
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

		private string gameBoxName;
		private byte[] hash;
	}
}
