// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZunTzu.AudioVideo;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class VoiceDialog : Form {
		public VoiceDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private readonly Controller controller;

		private void okButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void muteSoundEffectsCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MuteSoundEffects = muteSoundEffectsCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void VoiceDialog_Load(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;

			muteSoundEffectsCheckBox.Checked = properties.MuteSoundEffects;
		}
	}
}