// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class MeasuringState : State {

		public MeasuringState(Controller controller) : base(controller) {}

		public override void HandleLeftMouseButtonUp() {
			controller.State = controller.IdleState;
			model.IsMeasuring = false;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			model.RulerEndPosition = view.ConvertScreenToModelCoordinates(controller.MainForm.PointToClient(Cursor.Position));
		}

		public override void UpdateCursor(Form mainForm, IView view) { mainForm.Cursor = Cursors.Cross; }

		public override bool MouseCaptured { get { return true; } }
	}
}
