// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class GameBoxErrorDialog : Form {
		public GameBoxErrorDialog(Controller controller, string[] gameBoxFileNames) {
			InitializeComponent();
			this.controller = controller;
			this.gameBoxFileNames = gameBoxFileNames;
			this.currentIndex = 0;

			buildList();
		}

		private void buildList() {
			listView.Items.Clear();
			string fileName = gameBoxFileNames[currentIndex];
			gameBoxPathLabel.Text = fileName;
			foreach(string error in controller.Model.VerifyGameBox(fileName)) {
				listView.Items.Add(error.Substring(1), (error.StartsWith("E") ? 0 : 1));
			}
		}

		private void addButton_Click(object sender, EventArgs e) {
			string fileName = gameBoxFileNames[currentIndex];
			controller.Model.GameLibrary.AddReference(fileName);
			abortButton_Click(sender, e);
		}

		private void abortButton_Click(object sender, EventArgs e) {
			++currentIndex;
			if(currentIndex < gameBoxFileNames.Length) {
				buildList();
			} else {
				LibraryDialog libraryDialog = new LibraryDialog(controller);
				Close();
				controller.DialogState.Dialog = libraryDialog;
				controller.View.ShowDialog(libraryDialog);
			}
		}

		private readonly Controller controller;
		private readonly string[] gameBoxFileNames;
		private int currentIndex;
	}
}