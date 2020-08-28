// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Deployment.Application;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class AboutDialog : Form {
		public AboutDialog() {
			InitializeComponent();
			pictureBox.Image = ZunTzu.Properties.Resources.AboutZunTzu.ToBitmap();
			if(ApplicationDeployment.IsNetworkDeployed)
				buildLabel.Text = "Build " + ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
			else
				buildLabel.Text = "";
		}

		private void okButton_Click(object sender, EventArgs e) {
			Close();
		}
	}
}