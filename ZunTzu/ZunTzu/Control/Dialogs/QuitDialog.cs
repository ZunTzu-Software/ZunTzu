// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZunTzu.Control.Menu;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class QuitDialog : Form {
		public QuitDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void yesButton_Click(object sender, EventArgs e) {
			IModel model = controller.Model;
			if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox && model.IsHosting) {
				if(model.CurrentGameBox.CurrentGame.FileName != null) {
					controller.ExecuteCommand("savequit");
				} else {
					SaveAsMenuItem saveAsMenuItem = new SaveAsMenuItem(true);
					saveAsMenuItem.Select(controller);
				}
			} else {
				controller.ExecuteCommand("quit");
			}
			Close();
		}

		private void noButton_Click(object sender, EventArgs e) {
			controller.ExecuteCommand("quit");
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private readonly Controller controller;

		private void QuitDialog_Load(object sender, EventArgs e) {
			IModel model = controller.Model;
			if(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || !model.IsHosting) {
				label1.Text = Resources.QuitNoSavingRequiredPrompt;
				yesButton.Text = Resources.QuitNoSavingRequiredButton;
				noButton.Visible = false;
				yesButton.Location = new Point(yesButton.Location.X + 32, yesButton.Location.Y);
				cancelButton.Location = new Point(cancelButton.Location.X - 32, cancelButton.Location.Y);
			}
		}
	}
}