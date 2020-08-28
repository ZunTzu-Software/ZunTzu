// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Windows.Forms;
using ZunTzu.Control;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item that opens a dialog.</summary>
	public sealed class DialogMenuItem : IMenuItem {

		/// <summary>Constructor.</summary>
		/// <param name="dialog">A Windows form to show when the user clicks this menu item.</param>
		public DialogMenuItem(Form dialog) {
			this.dialog = dialog;
		}

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.DialogState.Dialog = dialog;
			controller.State = controller.DialogState;
			controller.View.Menu.IsVisible = false;
			controller.View.ShowDialog(dialog);
		}

		private readonly Form dialog;
	}
}
