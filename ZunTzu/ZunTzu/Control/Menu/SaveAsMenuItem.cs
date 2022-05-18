// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using ZunTzu.Control;
using ZunTzu.Properties;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class SaveAsMenuItem : IMenuItem {

		public SaveAsMenuItem() : this(false) {
		}

		public SaveAsMenuItem(bool quitAfterSaving) {
			this.quitAfterSaving = quitAfterSaving;
		}

		private sealed class SaveFileDialogParameters {
			public SaveFileDialogParameters(string initialDirectory, string filter) {
				InitialDirectory = initialDirectory;
				Filter = filter;
			}
			public string InitialDirectory;
			public string Filter;
		};

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			this.controller = controller;

			controller.View.Menu.IsVisible = false;
			controller.State = controller.DialogState;

			BackgroundWorker backgroundWorker = new BackgroundWorker();
			backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorkerDoWork);
			backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorkerRunWorkerCompleted);
			backgroundWorker.RunWorkerAsync(new SaveFileDialogParameters(
				Settings.Default.FileDirectory,
				Resources.FileSaveAsFilter));
		}

		private void backgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			if(e.Error == null && !e.Cancelled && e.Result != null) {
				string fileName = (string) e.Result;
				Settings.Default.FileDirectory = Path.GetDirectoryName(fileName);
				controller.ExecuteCommand((quitAfterSaving ? "saveasquit " : "saveas ") + fileName);
			}
			controller.State = controller.IdleState;
		}

		private void backgroundWorkerDoWork(object sender, DoWorkEventArgs e) {
			SaveFileDialogParameters parameters = (SaveFileDialogParameters) e.Argument;

			SaveFileDialog saveFileDialog = new SaveFileDialog();

			saveFileDialog.InitialDirectory = parameters.InitialDirectory;
			saveFileDialog.DefaultExt = "ztg";
			saveFileDialog.Filter = parameters.Filter;
			saveFileDialog.RestoreDirectory = true;

			if(saveFileDialog.ShowDialog(controller.MainForm) == DialogResult.OK)
				e.Result = saveFileDialog.FileName;
			else
				e.Result = null;
		}

		private Controller controller;
		private bool quitAfterSaving;
	}
}
