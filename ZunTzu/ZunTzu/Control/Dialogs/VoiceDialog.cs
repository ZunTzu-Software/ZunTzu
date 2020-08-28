// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZunTzu.AudioVideo;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class VoiceDialog : Form {
		public VoiceDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private readonly Controller controller;

		private void okButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void muteAllCheckBox_CheckedChanged(object sender, EventArgs e) {
			muteSoundEffectsCheckBox.Enabled = !muteAllCheckBox.Checked;
			muteRecordingCheckBox.Enabled = !muteAllCheckBox.Checked;

			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MuteAll = muteAllCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void muteSoundEffectsCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MuteSoundEffects = muteSoundEffectsCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void muteRecordingCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MuteRecording = muteRecordingCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void mutePlaybackCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MuteSoundEffects = muteSoundEffectsCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void voiceActivationThresholdTrackBar_Scroll(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.ActivationThreshold = ((voiceActivationThresholdTrackBar.Value - voiceActivationThresholdTrackBar.Minimum) * 99) / (voiceActivationThresholdTrackBar.Maximum - voiceActivationThresholdTrackBar.Minimum);
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void activateEchoSuppressionCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.ActivateEchoSuppression = activateEchoSuppressionCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void automaticJitterControlCheckBox_CheckedChanged(object sender, EventArgs e) {
			jitterControlTrackBar.Enabled = !automaticJitterControlCheckBox.Checked;

			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.UseAutomaticJitterControl = automaticJitterControlCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void jitterControlTrackBar_Scroll(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.JitterControl = ((jitterControlTrackBar.Value - jitterControlTrackBar.Minimum) * 99) / (jitterControlTrackBar.Maximum - jitterControlTrackBar.Minimum);
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void disableAutoconfigurationCheckBox_CheckedChanged(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.DisableAutoconfiguration = disableAutoconfigurationCheckBox.Checked;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void VoiceDialog_Load(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;

			// mute
			muteAllCheckBox.Checked = properties.MuteAll;
			muteSoundEffectsCheckBox.Checked = properties.MuteSoundEffects;
			muteRecordingCheckBox.Checked = properties.MuteRecording;
			muteSoundEffectsCheckBox.Enabled = !muteAllCheckBox.Checked;
			muteRecordingCheckBox.Enabled = !muteAllCheckBox.Checked;

			// recording control
			microphoneInputLevelTrackBar.Value = microphoneInputLevelTrackBar.Minimum + ((properties.MicrophoneInputLevel + 3000) * (microphoneInputLevelTrackBar.Maximum - microphoneInputLevelTrackBar.Minimum)) / 3000;
			voiceActivationThresholdTrackBar.Value = voiceActivationThresholdTrackBar.Minimum + (properties.ActivationThreshold * (voiceActivationThresholdTrackBar.Maximum - voiceActivationThresholdTrackBar.Minimum)) / 99;
			activateEchoSuppressionCheckBox.Checked = properties.ActivateEchoSuppression;

			// jitter control
			automaticJitterControlCheckBox.Checked = properties.UseAutomaticJitterControl;
			jitterControlTrackBar.Value = jitterControlTrackBar.Minimum + (properties.JitterControl * (jitterControlTrackBar.Maximum - jitterControlTrackBar.Minimum)) / 99;
			jitterControlTrackBar.Enabled = !automaticJitterControlCheckBox.Checked;

			// audio mixer configuration
			disableAutoconfigurationCheckBox.Checked = properties.DisableAutoconfiguration;

			// server mode
			serverModeTrackBar.Value = Settings.Default.VoiceServerMode;
			serverModeTrackBar_Scroll(null, null);
		}

		private void microphoneInputLevelTrackBar_Scroll(object sender, EventArgs e) {
			AudioProperties properties = controller.Model.AudioManager.AudioProperties;
			properties.MicrophoneInputLevel = ((microphoneInputLevelTrackBar.Value - microphoneInputLevelTrackBar.Minimum) * 3000) / (microphoneInputLevelTrackBar.Maximum - microphoneInputLevelTrackBar.Minimum) - 3000;
			controller.Model.AudioManager.AudioProperties = properties;
		}

		private void serverModeTrackBar_Scroll(object sender, EventArgs e) {
			serverModeTextBox.Text = serverModeDescriptions[serverModeTrackBar.Value];
			Settings.Default.VoiceServerMode = serverModeTrackBar.Value;
		}

		private readonly string[] serverModeDescriptions = {
			Resources.ServerModeMixingGsm, Resources.ServerModeForwardingGsm, Resources.ServerModeMixingAdpcm, Resources.ServerModeForwardingAdpcm
		};
	}
}