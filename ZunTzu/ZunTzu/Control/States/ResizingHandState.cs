// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>State used when grabbing the hand resize handle.</summary>
	public sealed class ResizingHandState : State {

		public ResizingHandState(Controller controller) : base(controller) { }

		public override void HandleLeftMouseButtonUp() {
			controller.State = controller.IdleState;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			if(controller.Model.ThisPlayer.CursorLocation is IHandCursorLocation)
				view.Hand.UnfoldedHeight = view.Hand.UnfoldedHeight + (previousMouseScreenPosition.Y - currentMouseScreenPosition.Y);
		}

		public override void UpdateCursor(Form mainForm, IView view) {
			mainForm.Cursor = System.Windows.Forms.Cursors.HSplit;
		}

		public override bool MouseCaptured { get { return true; } }
	}
}
