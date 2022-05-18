// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class MovingState : State {

		public MovingState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			if(model.ThisPlayer.CursorLocation is IBoardCursorLocation) {
				ISelection selection = model.CurrentSelection;
				// assumption: the stack will remain unchanged in the meantime
				if(selection != null && !selection.Empty &&
					!model.AnimationManager.IsBeingAnimated(selection.Stack))
				{
					networkClient.Send(new MoveSelectionMessage(model.StateChangeSequenceNumber, model.ThisPlayer.CursorLocation.ModelPosition));
				}
			}
			controller.State = controller.IdleState;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			controller.State = controller.ScrollingState;
			controller.ScrollingState.HandleMouseMove(previousMouseScreenPosition, currentMouseScreenPosition);
		}

		public override void UpdateCursor(System.Windows.Forms.Form mainForm, IView view) {
			mainForm.Cursor = System.Windows.Forms.Cursors.Cross;
		}
	}
}
