// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZunTzu.Control.Messages;
using ZunTzu.Graphics;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class DisplayDialog : Form {
		public DisplayDialog(Controller controller) {
			this.controller = controller;
			InitializeComponent();
		}

		private void okButton_Click(object sender, EventArgs e) {
			Hide();
			apply();
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void applyButton_Click(object sender, EventArgs e) {
			Hide();
			apply();
			Show();
		}

		private void apply() {
			if(widescreenCheckBox.Checked != (controller.View.DisplayProperties.GameAspectRatio == AspectRatioType.SixteenToTen))
				controller.NetworkClient.Send(new GameAspectRatioChangedMessage(widescreenCheckBox.Checked));

			DisplayProperties properties;
			properties.GameAspectRatio = (widescreenCheckBox.Checked ? AspectRatioType.SixteenToTen : AspectRatioType.FourToThree);
			properties.PreferredFullscreenMode = fullscreenModeComboBox.SelectedIndex;
			properties.TextureQuality = eligibleTextureFormats[texturesFormatTrackBar.Value];
			properties.MapsAndCountersDetailLevel = (DetailLevelType) (2 - mapsAndCounterDetailTrackBar.Value);
			properties.DiceModelsDetailLevel = (ModelDetailType) (1 - diceModelsTrackBar.Value);
			properties.WaitForVerticalBlank = waitForVerticalBlankCheckBox.Checked;
			controller.View.DisplayProperties = properties;
		}

		private readonly Controller controller;
		private List<TextureQualityType> eligibleTextureFormats = new List<TextureQualityType>();

		private readonly string[] textureFormatDescriptions = {
			Resources.TextureFormat32Bits,
			Resources.TextureFormat16Bits,
			Resources.TextureFormat8BitsCompressedQuality,
			null,
			Resources.TextureFormat8BitsCompressedFast
		};
		private readonly string[] mapsAndCounterDetailDescriptions = {
			Resources.MapDetail600Dpi, Resources.MapDetail300Dpi, Resources.MapDetail150Dpi
		};
		private readonly string[] diceModelsDescriptions = {
			Resources.DiceModelsComplex, Resources.DiceModelsSimple
		};

		private void DisplayDialog_Load(object sender, EventArgs e) {
			IView view = controller.View;
			DisplayProperties properties = view.DisplayProperties;

			// widescreen
			widescreenCheckBox.Checked = (properties.GameAspectRatio == AspectRatioType.SixteenToTen);
			widescreenCheckBox.Enabled = controller.Model.IsHosting;

			// preferred fullscreen mode
			fullscreenModeComboBox.BeginUpdate();
			fullscreenModeComboBox.Items.Clear();
			fullscreenModeComboBox.Items.Add(Resources.SameAsDesktop);
			fullscreenModeComboBox.Items.AddRange(view.EligibleFullscreenModes);
			fullscreenModeComboBox.EndUpdate();
			fullscreenModeComboBox.SelectedIndex = (properties.PreferredFullscreenMode >= 0 && properties.PreferredFullscreenMode < fullscreenModeComboBox.Items.Count ? properties.PreferredFullscreenMode : 0);

			// textures format
			foreach(TextureQualityType textureQuality in new TextureQualityType[] { TextureQualityType.CompressedEightBitsFast, TextureQualityType.CompressedEightBitsQuality, TextureQualityType.SixteenBits, TextureQualityType.ThirtyTwoBits }) {
				if(textureQuality == TextureQualityType.SixteenBits || view.SupportsTextureQuality(textureQuality)) {
					eligibleTextureFormats.Add(textureQuality);
					if(properties.TextureQuality == textureQuality)
						texturesFormatTrackBar.Value = eligibleTextureFormats.Count - 1;
				}
			}
			texturesFormatTrackBar.Maximum = eligibleTextureFormats.Count - 1;

			// maps and counters detail
			mapsAndCounterDetailTrackBar.Value = 2 - (int) properties.MapsAndCountersDetailLevel;

			// dice models
			diceModelsTrackBar.Value = 1 - (int) properties.DiceModelsDetailLevel;

			// wait for vertical blank
			waitForVerticalBlankCheckBox.Checked = properties.WaitForVerticalBlank;

			anyTrackBar_Scroll(null, null);
		}

		private void anyTrackBar_Scroll(object sender, EventArgs e) {
			texturesFormatTextBox.Text = textureFormatDescriptions[(int) eligibleTextureFormats[texturesFormatTrackBar.Value]];
			mapsAndCounterDetailTextBox.Text = mapsAndCounterDetailDescriptions[2 - mapsAndCounterDetailTrackBar.Value];
			diceModelsTextBox.Text = diceModelsDescriptions[1 - diceModelsTrackBar.Value];
		}
	}
}