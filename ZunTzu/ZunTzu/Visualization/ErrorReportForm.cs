// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Visualization {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class ErrorReportForm : Form {
		public ErrorReportForm(string reportContent) {
			InitializeComponent();
			contentTextBox.Text = reportContent;
		}
	}
}