// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Windows.Forms;
using ZunTzu.AudioVideo;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class VideoDialog : Form {
		public VideoDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			VideoConferencingClient videoConferencingClient = controller.VideoConferencingClient;
			if(enableVideoCaptureCheckBox.Checked != videoConferencingClient.CaptureEnabled) {
				if(enableVideoCaptureCheckBox.Checked) {
					IVideoCaptureDevice[] devices = videoConferencingClient.AvailableDevices;
					if(devices.Length > 0) {
						videoConferencingClient.Device = devices[0];
						videoConferencingClient.CaptureEnabled = true;
					}
				} else {
					videoConferencingClient.CaptureEnabled = false;
				}
			}
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void VideoDialog_Load(object sender, EventArgs e) {
			enableVideoCaptureCheckBox.Enabled = (controller.VideoConferencingClient.AvailableDevices.Length > 0);
			enableVideoCaptureCheckBox.Checked = controller.VideoConferencingClient.CaptureEnabled;
		}

		private readonly Controller controller;
	}
}