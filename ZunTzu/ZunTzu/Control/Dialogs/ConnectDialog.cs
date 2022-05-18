// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class ConnectDialog : Form {
		public ConnectDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			controller.ExecuteCommand("connect " + hostTextBox.Text + " " + portTextBox.Text);
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private readonly Controller controller;

		private void ConnectDialog_Load(object sender, EventArgs e) {
			hostTextBox.Text = Settings.Default.ConnectHostNameOrAddress;
			portTextBox.Text = Settings.Default.ConnectPort.ToString();
		}
	}
}