// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class HelpDialog : Form {
		public HelpDialog() {
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void webSiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			Process.Start("IExplore.exe", "http://www.zuntzu.com/index.htm");
		}
	}
}