// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Control.Dialogs {

	[System.Reflection.ObfuscationAttribute(Exclude = true)]
	public partial class LibraryDialog : Form {
		public LibraryDialog(Controller controller) {
			InitializeComponent();
			this.controller = controller;
		}

		private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e) {
			openButton.Enabled = (listView.SelectedItems.Count == 1);
			removeButton.Enabled = (listView.SelectedItems.Count > 0);
		}

		private void openButton_Click(object sender, EventArgs e) {
			if(listView.SelectedItems.Count == 1) {
				IGameBoxReference reference = (IGameBoxReference) listView.SelectedItems[0].Tag;
				Close();
				controller.ExecuteCommand("open " + reference.FileName);
			}
		}

		private sealed class OpenFileDialogParameters {
			public OpenFileDialogParameters(string initialDirectory, string filter) {
				InitialDirectory = initialDirectory;
				Filter = filter;
			}
			public string InitialDirectory;
			public string Filter;
		};

		private void addButton_Click(object sender, EventArgs e) {
			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorkerDoWork);
			backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerRunWorkerCompleted);
			backgroundWorker.RunWorkerAsync(new OpenFileDialogParameters(
				Settings.Default.LibraryDirectory,
				Resources.LibraryAddFilter));
		}

		private void backgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			if(e.Error == null && !e.Cancelled && e.Result != null) {
				string[] fileNames = (string[]) e.Result;
				if(fileNames.Length > 0) {
					Settings.Default.LibraryDirectory = Path.GetDirectoryName(fileNames[0]);
					List<string> filesWithErrors = new List<string>();
					foreach(string fileName in fileNames) {
						if(controller.Model.VerifyGameBox(fileName).GetEnumerator().MoveNext()) {
							filesWithErrors.Add(fileName);
						} else {
							controller.Model.GameLibrary.AddReference(fileName);
						}
					}
					if(filesWithErrors.Count > 0) {
						GameBoxErrorDialog errorDialog = new GameBoxErrorDialog(controller, filesWithErrors.ToArray());
						Close();
						controller.DialogState.Dialog = errorDialog;
						controller.View.ShowDialog(errorDialog);
					} else {
						smallImageList.Images.Clear();
						largeImageList.Images.Clear();
						listView.Items.Clear();
						buildList();
					}
				}
			}
		}

		private void backgroundWorkerDoWork(object sender, DoWorkEventArgs e) {
			OpenFileDialogParameters parameters = (OpenFileDialogParameters) e.Argument;

			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.InitialDirectory = parameters.InitialDirectory;
			openFileDialog.Filter = parameters.Filter;
			openFileDialog.Multiselect = true;
			openFileDialog.RestoreDirectory = true;

			if(openFileDialog.ShowDialog(controller.MainForm) == DialogResult.OK)
				e.Result = openFileDialog.FileNames;
			else
				e.Result = null;
		}

		private void removeButton_Click(object sender, EventArgs e) {
			if(listView.SelectedItems.Count > 0) {
				openButton.Enabled = false;
				removeButton.Enabled = false;
				foreach(ListViewItem selectedItem in listView.SelectedItems) {
					IGameBoxReference reference = (IGameBoxReference) selectedItem.Tag;
					controller.Model.GameLibrary.RemoveReference(reference);
				}
				smallImageList.Images.Clear();
				largeImageList.Images.Clear();
				listView.Items.Clear();
				buildList();
			}
		}

		private void closeButton_Click(object sender, EventArgs e) {
			Close();
		}

		private void LibraryDialog_Load(object sender, EventArgs e) {
			buildList();
		}

		private void buildList() {
			smallImageList.Images.Add(ZunTzu.Properties.Resources.ZunTzuBox);
			largeImageList.Images.Add(ZunTzu.Properties.Resources.ZunTzuBox);

			foreach(IGameBoxReference reference in controller.Model.GameLibrary.GameBoxes) {
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

				if(reference.Icon != null) {
					Bitmap iconImage = new Bitmap(48, 48, PixelFormat.Format16bppRgb565);

					BitmapData bitmapData = iconImage.LockBits(
						new Rectangle(0, 0, iconImage.Width, iconImage.Height),
						ImageLockMode.WriteOnly,
						PixelFormat.Format16bppRgb565);
					byte[] iconBytes = reference.Icon;
					unsafe {
						byte* ptr = (byte*) bitmapData.Scan0;
						for(int i = 0; i < iconBytes.Length; ++i)
							*ptr++ = iconBytes[i];
					}
					iconImage.UnlockBits(bitmapData);

					item.ImageIndex = largeImageList.Images.Count;
					largeImageList.Images.Add(iconImage);
				} else {
					item.ImageIndex = 0;
				}

				listView.Items.Add(item);
			}
		}

		private void listView_KeyUp(object sender, KeyEventArgs e) {
			if(e.KeyData == Keys.Delete) {
				// Del was pressed
				removeButton_Click(null, null);
				e.Handled = true;
			}
		}

		private readonly Controller controller;
		private static Font nameFont = new Font("Microsoft Sans Serif", 9.0f, FontStyle.Bold);
		private static Font copyrightFont = new Font("Microsoft Sans Serif", 7.0f);
		private static Font descriptionFont = new Font("Microsoft Sans Serif", 8.25f);
	}
}