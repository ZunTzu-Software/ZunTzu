// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ZunTzu.Control.Messages;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class PlayerDialog : Form {
		public PlayerDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			if(firstNameTextBox.Text != controller.Model.ThisPlayer.FirstName || lastNameTextBox.Text != controller.Model.ThisPlayer.LastName)
				controller.NetworkClient.Send(new PlayerNameChangedMessage(firstNameTextBox.Text, lastNameTextBox.Text));
			string cultureChosen = cultureChoices[languageComboBox.SelectedIndex];
			CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;
			if(currentCulture.ToString() != cultureChosen &&
				(currentCulture.Parent == null || currentCulture.Parent.ToString() != cultureChosen))
			{
				string specificCulture = specificCultureResults[languageComboBox.SelectedIndex];
				Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(specificCulture);
				Settings.Default.Language = specificCulture;
			}
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void PlayerDialog_Load(object sender, EventArgs e) {
			firstNameTextBox.Text = controller.Model.ThisPlayer.FirstName;
			lastNameTextBox.Text = controller.Model.ThisPlayer.LastName;

			CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;
			languageComboBox.SelectedIndex = 0;
			for(int i = 0; i < cultureChoices.Length; ++i) {
				if(currentCulture.ToString() == cultureChoices[i] ||
					currentCulture.Parent != null && currentCulture.Parent.ToString() == cultureChoices[i])
				{
					languageComboBox.SelectedIndex = i;
					break;
				}
			}
		}

		private readonly Controller controller;

		private static readonly string[] cultureChoices = { "de", "en", "es", "fr", "it", "pt" };
		private static readonly string[] specificCultureResults = { "de-DE", "en-US", "es-ES", "fr-FR", "it-IT", "pt-BR" };
	}
}