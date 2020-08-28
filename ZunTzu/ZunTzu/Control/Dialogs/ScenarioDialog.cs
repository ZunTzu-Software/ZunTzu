// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class ScenarioDialog : Form {
		public ScenarioDialog(Controller controller) {
			InitializeComponent();
			this.controller = controller;
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
			openButton.Enabled = (listView.SelectedItems.Count == 1);
		}

		private void openButton_Click(object sender, EventArgs e) {
			if(listView.SelectedItems.Count == 1) {
				IScenarioReference reference = (IScenarioReference) listView.SelectedItems[0].Tag;
				Close();
				controller.ExecuteCommand("openscenario " + reference.FileName);
			}
		}

		private void closeButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void ScenarioDialog_Load(object sender, EventArgs e) {
			smallImageList.Images.Add(ZunTzu.Properties.Resources.ZunTzuGame);
			largeImageList.Images.Add(ZunTzu.Properties.Resources.ZunTzuGame);

			foreach(IScenarioReference reference in controller.Model.CurrentGameBox.BuiltInScenarios) {
				ListViewItem item = new ListViewItem();
				item.Font = nameFont;
				item.ForeColor = Color.Blue;
				item.Tag = reference;
				item.Name = "name";
				item.Text = reference.Name;

				if(reference.Copyright != null && reference.Copyright != "") {
					ListViewItem.ListViewSubItem copyrightSubItem = new ListViewItem.ListViewSubItem();
					copyrightSubItem.Font = copyrightFont;
					copyrightSubItem.ForeColor = Color.DarkGray;
					copyrightSubItem.Name = "copyright";
					copyrightSubItem.Text = "Copyright© " + reference.Copyright;
					item.SubItems.Add(copyrightSubItem);
				}

				if(reference.Description != null && reference.Description != "") {
					ListViewItem.ListViewSubItem descriptionSubItem = new ListViewItem.ListViewSubItem();
					descriptionSubItem.Font = descriptionFont;
					descriptionSubItem.Name = "description";
					descriptionSubItem.Text = reference.Description;
					item.SubItems.Add(descriptionSubItem);
				}

				item.ImageIndex = 0;

				listView.Items.Add(item);
			}
		}

		private readonly Controller controller;
		private static Font nameFont = new Font("Microsoft Sans Serif", 9.0f, FontStyle.Bold);
		private static Font copyrightFont = new Font("Microsoft Sans Serif", 7.0f);
		private static Font descriptionFont = new Font("Microsoft Sans Serif", 8.25f);
	}
}