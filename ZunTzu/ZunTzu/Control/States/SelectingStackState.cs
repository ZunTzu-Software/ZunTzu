// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class SelectingStackState : State {

		public SelectingStackState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseDoubleClick() {
			if(model.ThisPlayer.CursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) model.ThisPlayer.CursorLocation;
				IPiece stackBottom = location.Piece;
				// assumption: the stack will remain unchanged in the meantime
				if(stackBottom != null && model.CurrentGameBox.CurrentGame.Mode != Mode.Terrain && !model.AnimationManager.IsBeingAnimated(stackBottom.Stack)) {
					ISelection newSelection = stackBottom.Stack.Select();
					if(!stackBottom.Stack.Unfolded && stackBottom is ICard) {
						newSelection = newSelection.RemoveAllPieces();
					} else {
						int bottomIndex = stackBottom.IndexInStackFromBottomToTop;
						if(bottomIndex > 0) {
							IPiece[] stackPieces = stackBottom.Stack.Pieces;
							for(int i = 0; i < bottomIndex; ++i)
								newSelection = newSelection.RemovePiece(stackPieces[i]);
						}
					}

					SelectionChangedMessage.SelectionInfo selectionInfo = new SelectionChangedMessage.SelectionInfo();
					selectionInfo.StackId = newSelection.Stack.Id;
					selectionInfo.PieceIds = new int[newSelection.Pieces.Length];
					for(int i = 0; i < newSelection.Pieces.Length; ++i)
						selectionInfo.PieceIds[i] = newSelection.Pieces[i].Id;

					networkClient.Send(new SelectionChangedMessage(model.StateChangeSequenceNumber, selectionInfo));
				}
			}
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			if(StackBottomBeingSelected is ITerrainClone)
				networkClient.Send(new TerrainDraggedMessage(StackBottomBeingSelected.Stack.Board.Id, StackBottomBeingSelected.Stack.Board.GetZOrder(StackBottomBeingSelected.Stack), model.ThisPlayer.DragAndDropAnchor));
			else
				networkClient.Send(new StackDraggedMessage(StackBottomBeingSelected.Id, model.ThisPlayer.DragAndDropAnchor));

			model.ThisPlayer.StackBeingDragged = StackBottomBeingSelected;
			controller.State = controller.DraggingStackState;
			controller.DraggingStackState.HandleMouseMove(previousMouseScreenPosition, currentMouseScreenPosition);
		}

		public override void UpdateCursor(System.Windows.Forms.Form mainForm, IView view) {
			mainForm.Cursor = view.FingerCursor;
		}

		public IPiece StackBottomBeingSelected = null;
	}
}
