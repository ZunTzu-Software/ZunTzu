// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Control.Dialogs {
	public partial class MessageDialog : Form {
		public MessageDialog(Icon icon, string message) {
			this.icon = icon;
			InitializeComponent();
			messageLabel.Text = message;
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			e.Graphics.DrawIconUnstretched(icon, pictureBox.Bounds);
		}

		private void okButton_Click(object sender, EventArgs e) {
			Close();
		}

		private Icon icon;
	}
}