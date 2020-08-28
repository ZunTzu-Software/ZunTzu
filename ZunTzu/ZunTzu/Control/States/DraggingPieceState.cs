// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Visualization;

namespace ZunTzu.Control.States {

	/// <summary>State used when the user is dragging a piece from the stack inspector.</summary>
	public sealed class DraggingPieceState : State {

		public DraggingPieceState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			if(model.ThisPlayer.PieceBeingDragged != null)
				networkClient.Send(new DragDropAbortedMessage());
			model.ThisPlayer.PieceBeingDragged = null;
			controller.State = controller.IdleState;
		}

		public override void HandleLeftMouseButtonUp() {
			IPlayer thisPlayer = model.ThisPlayer;
			IPiece pieceBeingDragged = thisPlayer.PieceBeingDragged;
			if(pieceBeingDragged != null) {
				ICursorLocation cursorLocation = thisPlayer.CursorLocation;
				// over the stack inspector
				if(cursorLocation is IStackInspectorCursorLocation) {
					IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
					if(model.CurrentSelection != null) {
						int insertionIndex = location.Index;
						if(pieceBeingDragged.Stack == model.CurrentSelection.Stack) {
							int currentIndex = pieceBeingDragged.IndexInStackFromBottomToTop;
							// assumption: the stack will remain unchanged in the meantime
							if(insertionIndex != currentIndex && insertionIndex != currentIndex + 1 &&
								!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack)) {
								networkClient.Send(new DragDropPieceIntoSameStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, insertionIndex));
							} else {
								networkClient.Send(new DragDropAbortedMessage());
							}
						} else {
							// TODO ? drag drop piece into other stack ? Not sure
							networkClient.Send(new DragDropAbortedMessage());
						}
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over the player's hand
				} else if(cursorLocation is IHandCursorLocation) {
					IHandCursorLocation location = (IHandCursorLocation) cursorLocation;
					// assumption: boths stacks will remain unchanged in the meantime
					IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid);
					if(playerHand != null &&
						(playerHand.Count == 0 || !model.AnimationManager.IsBeingAnimated(playerHand.Pieces[0].Stack)) &&
						!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack))
					{
						if(pieceBeingDragged.Stack.Pieces.Length == 1) {
							networkClient.Send(new DragDropStackIntoHandMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, location.Index));
						} else {
							networkClient.Send(new DragDropPieceIntoHandMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, location.Index));
						}
					} else {
						networkClient.Send(new DragDropAbortedMessage());
					}
				// over the board
				} else if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					IPiece pieceAtMousePosition = location.Piece;
					// over an unattached stack
					if(model.CurrentGameBox.CurrentGame.StackingEnabled && pieceAtMousePosition != null && !pieceAtMousePosition.Stack.AttachedToCounterSection && pieceAtMousePosition.GetType() == pieceBeingDragged.GetType() && !(pieceAtMousePosition is ITerrain)) {
						// same stack?
						if(pieceAtMousePosition.Stack == pieceBeingDragged.Stack) {
							int currentIndex = pieceBeingDragged.IndexInStackFromBottomToTop;
							// assumption: the stack will remain unchanged in the meantime
							if(currentIndex < pieceAtMousePosition.Stack.Pieces.Length - 1 &&
								!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack))
							{
								networkClient.Send(new DragDropPieceIntoSameStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, pieceAtMousePosition.Stack.Pieces.Length));
							} else {
								networkClient.Send(new DragDropAbortedMessage());
							}
						} else {
							// assumption: both stacks will remain unchanged in the meantime
							if(!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack) &&
								!model.AnimationManager.IsBeingAnimated(pieceAtMousePosition.Stack)) {
								if(pieceBeingDragged.Stack.Pieces.Length == 1)
									networkClient.Send(new DragDropStackOnTopOfOtherStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, pieceAtMousePosition.Stack.Id));
								else
									networkClient.Send(new DragDropPieceOnTopOfOtherStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, pieceAtMousePosition.Stack.Id));
							} else {
								networkClient.Send(new DragDropAbortedMessage());
							}
						}
					// over an empty space
					} else {
						// assumption: the stack will remain unchanged in the meantime
						if(!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack)) {
							PointF newPiecePosition = new PointF(
								location.ModelPosition.X - thisPlayer.DragAndDropAnchor.X,
								location.ModelPosition.Y - thisPlayer.DragAndDropAnchor.Y);
							if(pieceBeingDragged.Stack.Pieces.Length == 1) {
								if(pieceBeingDragged is ITerrainClone)
									networkClient.Send(new DragDropTerrainMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Stack.Board.Id, pieceBeingDragged.Stack.Board.GetZOrder(pieceBeingDragged.Stack), newPiecePosition));
								else
									networkClient.Send(new DragDropStackMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Stack.Id, newPiecePosition));
							} else {
								networkClient.Send(new DragDropPieceMessage(model.StateChangeSequenceNumber, pieceBeingDragged.Id, newPiecePosition));
							}
						} else {
							networkClient.Send(new DragDropAbortedMessage());
						}
					}
				// over a counter sheet tab
				// assumption: the stack will remain unchanged in the meantime
				} else if(cursorLocation is ITabsCursorLocation && ((ITabsCursorLocation) cursorLocation).Tab is ICounterSheet && !pieceBeingDragged.Stack.AttachedToCounterSection &&
					!model.AnimationManager.IsBeingAnimated(pieceBeingDragged.Stack))
				{
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
				if(cursorLocation is IBoardCursorLocation) {
					IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
					mainForm.Cursor = (!model.CurrentGameBox.CurrentGame.StackingEnabled || location.Piece == null || location.Piece.Stack.AttachedToCounterSection || location.Piece.GetType() != pieceBeingDragged.GetType() && !(location.Piece is ITerrain) ? view.FingerCursor : view.FingerAddCursor);
				} else if(cursorLocation is IStackInspectorCursorLocation) {
					mainForm.Cursor = view.FingerCursor;
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
