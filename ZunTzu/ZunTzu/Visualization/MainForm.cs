// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Visualization {
	public partial class MainForm : Form {
		public MainForm(Size size, FormWindowState windowState) {
			InitializeComponent();
			Size = size;
			WindowState = windowState;
		}

		/*
		protected override void WndProc(ref Message m) {
			base.WndProc(ref m);
			if(msg.Msg == 0x85) {	// WM_NCPAINT
				...
			}
		}
		 */
	}
}