// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class ScrollingState : State {

		public ScrollingState(Controller controller) : base(controller) {}

		public override void HandleLeftMouseButtonUp() {
			controller.State = controller.IdleState;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			Point mouseScreenPosition = controller.MainForm.PointToClient(Cursor.Position);

			// this is the offset in model coordinates
			SizeF modelCoordinates = view.ConvertScreenToModelCoordinates(
				new SizeF(
				mouseScreenPosition.X - previousMouseScreenPosition.X,
				mouseScreenPosition.Y - previousMouseScreenPosition.Y));

			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;

			visibleArea.X -= modelCoordinates.Width;
			visibleArea.Y -= modelCoordinates.Height;
			visibleBoard.VisibleArea = visibleArea;

			// this is the model coordinates based on the new point of view referential
			PointF mouseModelPosition = view.ConvertScreenToModelCoordinates(mouseScreenPosition);

			networkClient.Send(new VisibleAreaChangedMessage(mouseModelPosition, visibleBoard.Id, visibleArea));
		}

		public override void UpdateCursor(Form mainForm, IView view) {
			mainForm.Cursor = view.FistCursor;
		}

		public override bool MouseCaptured { get { return true; } }
	}
}
