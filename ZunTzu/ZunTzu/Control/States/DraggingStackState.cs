// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class DraggingStackState : State {

		public DraggingStackState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			if(model.ThisPlayer.StackBeingDragged != null)
				networkClient.Send(new DragDropAbortedMessage());
			model.ThisPlayer.StackBeingDragged = null;
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			IPlayer thisPlayer = model.ThisPlayer;
			IPiece stackBottomBeingDragged = thisPlayer.StackBeingDragged;
			if(stackBottomBeingDragged != null) {
				ICursorLocation cursorLocation = thisPlayer.CursorLocation;
				// over the stack inspector
				if(cursorLocation is IStackInspectorCursorLocation) {
					IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
					// assumption: boths stacks will remain unchanged in the meantime
					if(model.CurrentSelection != null && model.CurrentSelection.Stack != stackBottomBeingDragged.Stack && model.CurrentSelection.Stack.Pieces[0].GetType() == thisPlayer.StackBeingDragged.GetType() &&
						!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack) &&
						!model.AnimationManager.IsBeingAnimated(stackBottomBeingDragged.Stack))
					{
						networkClient.Send(new DragDropStackIntoOtherStackMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Id, location.Index));
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over the board
				} else if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					IPiece pieceAtMousePosition = location.Piece;
					// over another stack
					if(model.CurrentGameBox.CurrentGame.StackingEnabled && pieceAtMousePosition != null && pieceAtMousePosition.Stack != stackBottomBeingDragged.Stack && !pieceAtMousePosition.Stack.AttachedToCounterSection && pieceAtMousePosition.GetType() == stackBottomBeingDragged.GetType() && !(pieceAtMousePosition is ITerrain)) {
						// assumption: boths stacks will remain unchanged in the meantime
						if(!model.AnimationManager.IsBeingAnimated(pieceAtMousePosition.Stack) &&
							!model.AnimationManager.IsBeingAnimated(stackBottomBeingDragged.Stack)) {
							networkClient.Send(new DragDropStackOnTopOfOtherStackMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Id, pieceAtMousePosition.Stack.Id));
						} else {
							networkClient.Send(new DragDropAbortedMessage());
						}
					// over an empty space
					} else {
						PointF newStackPosition = new PointF(
							location.ModelPosition.X - thisPlayer.DragAndDropAnchor.X,
							location.ModelPosition.Y - thisPlayer.DragAndDropAnchor.Y);
						// assumption: the stack will remain unchanged in the meantime
						if(!model.AnimationManager.IsBeingAnimated(stackBottomBeingDragged.Stack)) {
							if(stackBottomBeingDragged is ITerrainClone)
								networkClient.Send(new DragDropTerrainMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Stack.Board.Id, stackBottomBeingDragged.Stack.Board.GetZOrder(stackBottomBeingDragged.Stack), newStackPosition));
							else
								networkClient.Send(new DragDropStackMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Id, newStackPosition));
						} else {
							networkClient.Send(new DragDropAbortedMessage());
						}
					}
				// over the player's hand
				} else if(cursorLocation is IHandCursorLocation) {
					IHandCursorLocation location = (IHandCursorLocation) cursorLocation;
					// assumption: boths stacks will remain unchanged in the meantime
					IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid);
					if(playerHand != null &&
						(playerHand.Count == 0 || !model.AnimationManager.IsBeingAnimated(playerHand.Pieces[0].Stack)) &&
						!model.AnimationManager.IsBeingAnimated(stackBottomBeingDragged.Stack))
					{
						if(stackBottomBeingDragged is ITerrainClone)
							networkClient.Send(new DragDropTerrainIntoHandMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Stack.Board.Id, stackBottomBeingDragged.Stack.Board.GetZOrder(stackBottomBeingDragged.Stack), location.Index));
						else
							networkClient.Send(new DragDropStackIntoHandMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Id, location.Index));
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over a counter sheet tab
				// assumption: the stack will remain unchanged in the meantime
				} else if(cursorLocation is ITabsCursorLocation && ((ITabsCursorLocation) cursorLocation).Tab is ICounterSheet && !stackBottomBeingDragged.Stack.AttachedToCounterSection &&
					 !model.AnimationManager.IsBeingAnimated(stackBottomBeingDragged.Stack))
				{
					if(stackBottomBeingDragged is ITerrainClone)
						networkClient.Send(new RemoveTerrainMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Stack.Board.Id, stackBottomBeingDragged.Stack.Board.GetZOrder(stackBottomBeingDragged.Stack)));
					else
						networkClient.Send(new UnpunchStackMessage(model.StateChangeSequenceNumber, stackBottomBeingDragged.Id));
				} else {
					networkClient.Send(new DragDropAbortedMessage());
				}
			}
			thisPlayer.StackBeingDragged = null;
			controller.State = controller.IdleState;
		}

		public override void HandleMouseMove(Point previousMouseScreenPosition, Point currentMouseScreenPosition) {
			base.HandleMouseMove(previousMouseScreenPosition, currentMouseScreenPosition);

			ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
			if(!WaitingForBoardChange && cursorLocation is ITabsCursorLocation) {
				ITabsCursorLocation location = (ITabsCursorLocation) cursorLocation;
				IMap tab = location.Tab as IMap;
				if(tab != null && tab != model.CurrentGameBox.CurrentGame.VisibleBoard) {
					WaitingForBoardChange = true;
					networkClient.Send(new VisibleBoardChangedMessage(model.StateChangeSequenceNumber, tab.Id));
				}
			}
		}

		public override void UpdateCursor(System.Windows.Forms.Form mainForm, IView view) {
			IPiece stackBeingDragged = model.ThisPlayer.StackBeingDragged;
			if(stackBeingDragged == null) {
				controller.State = controller.IdleState;
				controller.IdleState.UpdateCursor(mainForm, view);
			} else {
				ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
				if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					mainForm.Cursor = (model.CurrentGameBox.CurrentGame.StackingEnabled && location.Piece != null && location.Piece.Stack != stackBeingDragged.Stack && !location.Piece.Stack.AttachedToCounterSection && location.Piece.GetType() == stackBeingDragged.GetType() && !(location.Piece is ITerrain) ? view.FingerAddCursor : view.FingerCursor);
				} else if(cursorLocation is IStackInspectorCursorLocation) {
					mainForm.Cursor = (model.CurrentSelection == null || model.CurrentSelection.Stack == stackBeingDragged.Stack || model.CurrentSelection.Stack.Pieces[0].GetType() != stackBeingDragged.GetType() ?
						System.Windows.Forms.Cursors.No : view.FingerAddCursor);
				} else if(cursorLocation is IHandCursorLocation) {
					mainForm.Cursor = view.FingerAddCursor;
				} else if(cursorLocation is ITabsCursorLocation) {
					ITabsCursorLocation location = (ITabsCursorLocation) cursorLocation;
					mainForm.Cursor = (location.Tab is ICounterSheet ? view.FingerRemoveCursor : view.FingerCursor);
				} else {
					mainForm.Cursor = System.Windows.Forms.Cursors.No;
				}
			}
		}

		public override bool MouseCaptured { get { return true; } }

		public bool WaitingForBoardChange = false;
	}
}
