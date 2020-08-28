// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>State used when the user is dragging a piece from the hand.</summary>
	public sealed class DraggingHandPieceState : State {

		public DraggingHandPieceState(Controller controller) : base(controller) { }

		public override void HandleEscapeKeyPress() {
			if(model.ThisPlayer.PieceBeingDragged != null)
				networkClient.Send(new DragDropAbortedMessage());
			model.ThisPlayer.PieceBeingDragged = null;
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			IPlayer thisPlayer = model.ThisPlayer;
			IPiece pieceBeingDragged = thisPlayer.PieceBeingDragged;
			IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(thisPlayer.Guid);
			if(pieceBeingDragged != null && playerHand.Count > 0 && playerHand.Pieces[0].Stack == pieceBeingDragged.Stack) {
				ICursorLocation cursorLocation = thisPlayer.CursorLocation;
				// over the hand
				if(cursorLocation is IHandCursorLocation) {
					IHandCursorLocation location = (IHandCursorLocation) cursorLocation;
					int insertionIndex = location.Index;
					int currentIndex = pieceBeingDragged.IndexInStackFromBottomToTop;
					// assumption: the stack will remain unchanged in the meantime
					if(insertionIndex != currentIndex && insertionIndex != currentIndex + 1 &&
						!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack)) {
						networkClient.Send(new RearrangePlayerHandMessage(model.StateChangeSequenceNumber, currentIndex, insertionIndex));
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over the board
				} else if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					IPiece pieceAtMousePosition = location.Piece;
					// over an unattached stack
					if(model.CurrentGameBox.CurrentGame.StackingEnabled && pieceAtMousePosition != null && !pieceAtMousePosition.Stack.AttachedToCounterSection && pieceAtMousePosition.GetType() == pieceBeingDragged.GetType() && !(pieceBeingDragged is ITerrainClone)) {
						// assumption: both stacks will remain unchanged in the meantime
						if(pieceAtMousePosition.Stack != pieceBeingDragged.Stack &&
							!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack) &&
							!model.AnimationManager.IsBeingAnimated(pieceAtMousePosition.Stack))
						{
							networkClient.Send(new DragDropPieceOnTopOfOtherStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, pieceAtMousePosition.Stack.Id));
						} else {
							networkClient.Send(new DragDropAbortedMessage());
						}
					// over an empty space
					} else {
						// assumption: the stack will remain unchanged in the meantime
						if(!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack)) {
							PointF newPiecePosition = new PointF(
								location.ModelPosition.X - thisPlayer.DragAndDropAnchor.X,
								location.ModelPosition.Y - thisPlayer.DragAndDropAnchor.Y);
							if(pieceBeingDragged is ITerrainClone)
								networkClient.Send(new DragDropTerrainMessage(model.StateChangeSequenceNumber, -1, pieceBeingDragged.IndexInStackFromBottomToTop, newPiecePosition));
							else
								networkClient.Send(new DragDropPieceMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, newPiecePosition));
						} else {
							networkClient.Send(new DragDropAbortedMessage());
						}
					}
				// over the stack inspector
				} else if(cursorLocation is IStackInspectorCursorLocation) {
					IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
					// assumption: boths stacks will remain unchanged in the meantime
					if(model.CurrentSelection != null && model.CurrentSelection.Stack != pieceBeingDragged.Stack && model.CurrentSelection.Stack.Pieces[0].GetType() == pieceBeingDragged.GetType() &&
						!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack) &&
						!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack))
					{
						networkClient.Send(new DragDropPieceIntoOtherStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, location.Index));
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over a counter sheet tab
				// assumption: the stack will remain unchanged in the meantime
				} else if(cursorLocation is ITabsCursorLocation && ((ITabsCursorLocation) cursorLocation).Tab is ICounterSheet &&
					!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack))
				{
					if(pieceBeingDragged is ITerrainClone)
						networkClient.Send(new RemoveTerrainMessage(model.StateChangeSequenceNumber, -1, pieceBeingDragged.IndexInStackFromBottomToTop));
					else
						networkClient.Send(new UnpunchPieceMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id));
				} else {
					networkClient.Send(new DragDropAbortedMessage());
				}
			}
			thisPlayer.PieceBeingDragged = null;
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
			IPiece pieceBeingDragged = model.ThisPlayer.PieceBeingDragged;
			if(pieceBeingDragged == null) {
				controller.State = controller.IdleState;
				controller.IdleState.UpdateCursor(mainForm, view);
			} else {
				ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
				if(cursorLocation is IHandCursorLocation) {
					mainForm.Cursor = view.FingerCursor;
				} else if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					mainForm.Cursor = (!model.CurrentGameBox.CurrentGame.StackingEnabled || location.Piece == null || location.Piece.Stack.AttachedToCounterSection || location.Piece.GetType() != pieceBeingDragged.GetType() || pieceBeingDragged is ITerrainClone ? view.FingerCursor : view.FingerAddCursor);
				} else if(cursorLocation is IStackInspectorCursorLocation) {
					mainForm.Cursor = (model.CurrentSelection == null || model.CurrentSelection.Stack.Pieces[0].GetType() != pieceBeingDragged.GetType() ?
						System.Windows.Forms.Cursors.No : view.FingerAddCursor);
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
