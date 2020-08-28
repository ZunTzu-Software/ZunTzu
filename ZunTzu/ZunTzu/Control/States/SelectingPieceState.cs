// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class SelectingPieceState : State {

		public SelectingPieceState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			if(model.ThisPlayer.CursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) model.ThisPlayer.CursorLocation;
				// assumption: the stack will remain unchanged in the meantime
				if(location.Piece != null && location.Piece == PieceBeingSelected &&
					!model.AnimationManager.IsBeingAnimated(PieceBeingSelected.Stack))
				{
					ISelection newSelection;
					if(model.CurrentSelection != null && model.CurrentSelection.Contains(location.Piece)) {
						newSelection = model.CurrentSelection.RemovePiece(location.Piece);
					} else {
						newSelection = model.CurrentSelection.AddPiece(location.Piece);
					}

					SelectionChangedMessage.SelectionInfo selectionInfo = new SelectionChangedMessage.SelectionInfo();
					if(newSelection == null) {
						selectionInfo.StackId = -1;
					} else {
						selectionInfo.StackId = newSelection.Stack.Id;
						selectionInfo.PieceIds = new int[newSelection.Pieces.Length];
						for(int i = 0; i < newSelection.Pieces.Length; ++i)
							selectionInfo.PieceIds[i] = newSelection.Pieces[i].Id;
					}

					networkClient.Send(new SelectionChangedMessage(model.StateChangeSequenceNumber, selectionInfo));
				}
			}
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseDoubleClick() {
			controller.State = controller.IdleState;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			ICursorLocation location = model.ThisPlayer.CursorLocation;
			if(location is IStackInspectorCursorLocation) {
				networkClient.Send(new PieceDraggedMessage(PieceBeingSelected.Id, model.ThisPlayer.DragAndDropAnchor));
				model.ThisPlayer.PieceBeingDragged = PieceBeingSelected;
				controller.State = controller.DraggingPieceState;
				controller.DraggingPieceState.HandleMouseMove(previousMouseScreenPosition, currentMouseScreenPosition);
			} else if(location is IHandCursorLocation) {
				if(PieceBeingSelected is ITerrainClone)
					networkClient.Send(new TerrainDraggedMessage(-1, PieceBeingSelected.IndexInStackFromBottomToTop, model.ThisPlayer.DragAndDropAnchor));
				else
					networkClient.Send(new PieceDraggedMessage(PieceBeingSelected.Id, model.ThisPlayer.DragAndDropAnchor));
				model.ThisPlayer.PieceBeingDragged = PieceBeingSelected;
				controller.State = controller.DraggingHandPieceState;
				controller.DraggingHandPieceState.HandleMouseMove(previousMouseScreenPosition, currentMouseScreenPosition);
			} else {
				controller.State = controller.IdleState;
			}
		}

		public override void UpdateCursor(System.Windows.Forms.Form mainForm, IView view) {
			mainForm.Cursor = view.FingerCursor;
		}

		public IPiece PieceBeingSelected = null;
	}
}
