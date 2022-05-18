// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public enum Connectivity {
		NoInternet,
		PortInUse,
		NoEgress,
		NoIngress,
		Limited
	}

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class FirewallDialog : Form {

		public FirewallDialog(Controller controller, Connectivity status) {
			this.controller = controller;
			this.status = status;
			
			InitializeComponent();

			string statusAsString = status.ToString();
			using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ZunTzu.ResourceFiles.ConnectivityIcon" + statusAsString + ".png")) {
				pictureBox.Image = new Bitmap(stream);
			}
			ResourceManager resourceManager = new ResourceManager("ZunTzu.Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
			captionLabel.Text = resourceManager.GetString("ConnectivityCaption" + statusAsString);
			messageLabel.Text = resourceManager.GetString("ConnectivityText" + statusAsString);
			okButton.Text = resourceManager.GetString("ConnectivityOk" + statusAsString);
			cancelButton.Text = resourceManager.GetString("ConnectivityCancel" + statusAsString);
		}

		private void okButton_Click(object sender, EventArgs e) {
			Close();
			switch(status) {
				case Connectivity.PortInUse:
					HostDialog hostDialog = new HostDialog(controller);
					controller.DialogState.Dialog = hostDialog;
					controller.State = controller.DialogState;
					controller.View.ShowDialog(hostDialog);
					break;

				case Connectivity.Limited:
				case Connectivity.NoInternet:
				case Connectivity.NoEgress:
				case Connectivity.NoIngress:
					break;
			}
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
			switch(status) {
				case Connectivity.PortInUse:
					break;

				case Connectivity.Limited:
				case Connectivity.NoInternet:
				case Connectivity.NoEgress:
				case Connectivity.NoIngress:
					controller.ExecuteCommand("disconnect");
					break;
			}
		}
		
		private readonly Controller controller;
		private readonly Connectivity status;
	}
}