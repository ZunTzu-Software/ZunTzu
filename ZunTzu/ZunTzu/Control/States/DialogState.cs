// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Windows.Forms;

namespace ZunTzu.Control.States {

	/// <summary>State used when the user has opened a dialog box.</summary>
	public sealed class DialogState : State {

		public DialogState(Controller controller) : base(controller) {}

		public Form Dialog {
			get { return dialog; }
			set {
				dialog = value;
				if(dialog != null)
					dialog.Closed += new EventHandler(onDialogClosed);
			}
		}

		private void onDialogClosed(object sender, EventArgs e) {
			dialog.Closed -= new EventHandler(onDialogClosed);
			dialog = null;
			controller.State = controller.IdleState;
		}

		private Form dialog = null;
	}
}
