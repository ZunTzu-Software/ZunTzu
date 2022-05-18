// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Visualization {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class ErrorForm : Form {
		public ErrorForm(string reportContent) {
			InitializeComponent();
			this.reportContent = reportContent;

			pictureBox.Image = ZunTzu.Properties.Resources.AboutZunTzu.ToBitmap();
		}

		private void showReportContent_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
			ErrorReportForm errortReportForm = new ErrorReportForm(reportContent);
			errortReportForm.ShowDialog();
		}

		private string reportContent = "";
	}
}