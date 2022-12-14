// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Control;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Abstract base class for the state of the controller.</summary>
	public abstract class State {

		public virtual void HandleEscapeKeyPress() {}

		public virtual void HandleDeleteKeyPress() {}

		public virtual void HandleLeftMouseButtonDown() { }

		public virtual void HandleLeftMouseButtonUp() {}

		public virtual void HandleLeftMouseDoubleClick() {}

		public virtual void HandleRightMouseDoubleClick() {}

		public virtual void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			if(view.GameDisplayAreaInPixels.Contains(currentMouseScreenPosition)) {
				PointF mouseModelPosition = view.ConvertScreenToModelCoordinates(currentMouseScreenPosition);
				networkClient.Send(new MouseMovedMessage(mouseModelPosition));
			}
		}

		public virtual void HandleMouseWheel(Guid executorPlayerGuid, int detents) {}

		public virtual void UpdateCursor(Form mainForm, IView view) { mainForm.Cursor = Cursors.Default; }

		public virtual bool MouseCaptured { get { return false; } }

		public virtual void HandleGrabKeyPress() {}

		public State(Controller controller) {
			this.controller = controller;
		}
		internal readonly Controller controller;
		internal IView view { get { return controller.View; } }
		internal IModel model { get { return controller.Model; } }
		internal NetworkClient networkClient { get { return controller.NetworkClient; } }
	}
}
