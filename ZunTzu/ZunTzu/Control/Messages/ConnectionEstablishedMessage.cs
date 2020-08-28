// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ZunTzu.Control.Dialogs;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>ConnectionEstablishedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ConnectionEstablishedMessage : SystemMessage {
		internal ConnectionEstablishedMessage() {}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.ConnectionEstablished; } }

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;
			if(model.IsHosting) {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouAreHosting);
				controller.NetworkClient.Send(new PlayerHasJoinedMessage(
					model.ThisPlayer.FirstName, model.ThisPlayer.LastName, model.ThisPlayer.Guid, controller.View.ConvertScreenToModelCoordinates(controller.MainForm.PointToClient(Cursor.Position)), model.ThisPlayer.IsCursorVisible));

				if(System.Environment.OSVersion.Version.Major >= 6) {	// running Vista?
					controller.DialogState.Dialog = new MessageDialog(SystemIcons.Warning, Resources.VistaWarningNoVoice);
					controller.State = controller.DialogState;
					controller.View.ShowDialog(controller.DialogState.Dialog);
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouAreConnected);
			}
		}
	}
}
