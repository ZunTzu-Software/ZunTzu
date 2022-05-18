// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using ZunTzu.Control;
using ZunTzu.Properties;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class OpenFileMenuItem : IMenuItem {

		private sealed class OpenFileDialogParameters {
			public OpenFileDialogParameters(string initialDirectory, string filter) {
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
			backgroundWorker.RunWorkerAsync(new OpenFileDialogParameters(
				Settings.Default.FileDirectory,
				Resources.FileOpenFilter));
		}

		private void backgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			if(e.Error == null && !e.Cancelled && e.Result != null) {
				string fileName = (string) e.Result;
				Settings.Default.FileDirectory = Path.GetDirectoryName(fileName);
				controller.ExecuteCommand("open " + fileName);
			}
			controller.State = controller.IdleState;
		}

		private void backgroundWorkerDoWork(object sender, DoWorkEventArgs e) {
			OpenFileDialogParameters parameters = (OpenFileDialogParameters) e.Argument;

			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.InitialDirectory = parameters.InitialDirectory;
			openFileDialog.Filter = parameters.Filter;
			openFileDialog.RestoreDirectory = true;

			if(openFileDialog.ShowDialog(controller.MainForm) == DialogResult.OK)
				e.Result = openFileDialog.FileName;
			else
				e.Result = null;
		}

		private Controller controller;
	}
}
