// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class HostDialog : Form {
		public HostDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			string command = "host " + portTextBox.Text + (copyToClipboardCheckBox.Checked ? " copy" : "");
			Close();
			controller.ExecuteCommand(command);
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private readonly Controller controller;

		private void HostDialog_Load(object sender, EventArgs e) {
			portTextBox.Text = Settings.Default.HostPort.ToString();
		}
	}
}