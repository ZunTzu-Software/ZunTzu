// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Control.Menu;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Properties;
using ZunTzu.Visualization;

// Rotation sequence:
// Rotation commands are grouped based on the measured roundtrip duration to minimize chattiness

//  A B C   Wheel             Client                               Host
//
//  0 F F     | +1 increment    |                                   |
//  1 T F     +---------------->| NOT(C) => BeginRotation           |
//  1 F T     |                 +---------------------------------->|
//            | +3 increments   |                                   |
//  4 T T     +---------------->| C => nothing                      |
//            |                 |                  ContinueRotation |
//  4 T F     |                 |<----------------------------------+
//            |                 | B => BeginRotation                |
//  4 F T     |                 +---------------------------------->|
//            |                 |                                   |
//            | No increment    |                                   |
//            |                 |                  ContinueRotation |
//  4 F F     |                 |<----------------------------------+
//            |                 | NOT(B) => RotateStack(4)          |
//  4 F F     |                 +---------------------------------->|
//            |                 |                                   |
//            |                 |                                   |
//            |                 |       ChangeAccepted(RotateStack) |
//            |                 |<----------------------------------+
//            |                 |                                   |

namespace ZunTzu.Control.States {

	/// <summary>Default state for the controller.</summary>
	public sealed class IdleState : State {

		public IdleState(Controller controller) : base(controller) {}

		public override void HandleEscapeKeyPress() {
			// close the stack inspector?
			if(model.CurrentSelection != null) {
				SelectionChangedMessage.SelectionInfo selectionInfo = new SelectionChangedMessage.SelectionInfo();
				selectionInfo.StackId = -1;
				networkClient.Send(new SelectionChangedMessage(model.StateChangeSequenceNumber, selectionInfo));
			// close the chat text box?
			} else if(view.Prompter.InputBoxVisible) {
				view.Prompter.HideInputBox();
			// toggle menu visibility?
			} else {
				view.Menu.IsVisible = !view.Menu.IsVisible;
				if(view.Menu.IsVisible) {
					view.Menu.MenuItems = new MenuItem[] {
						new SubMenuItem(Resources.MenuFile, false, new FileMenuItem()),
						new SubMenuItem(Resources.MenuMultiplayer, false, new MultiplayerMenuItem()),
						new SubMenuItem(Resources.MenuSettings, false, new SettingsMenuItem()),
						new SubMenuItem(Resources.MenuQuestionMark, false, new QuestionMarkMenuItem())
					};
				}
			}
		}

		public override void HandleDeleteKeyPress() {
			ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
			// over the board
			if(cursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
				IPiece stackBottom = location.Piece;
				// over a unattached stack
				if(stackBottom != null && !stackBottom.Stack.AttachedToCounterSection) {
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(stackBottom.Stack)) {
						if(stackBottom is ITerrainClone)
							networkClient.Send(new RemoveTerrainMessage(model.StateChangeSequenceNumber, stackBottom.Stack.Board.Id, stackBottom.Stack.Board.GetZOrder(stackBottom.Stack)));
						else
							networkClient.Send(new UnpunchStackMessage(model.StateChangeSequenceNumber, stackBottom.Id));
					}
				}
			// over the stack inspector
			} else if(cursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
				IPiece piece = location.Piece;
				// over an unattached piece
				if(piece != null && !piece.Stack.AttachedToCounterSection)
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(piece.Stack))
						networkClient.Send(new UnpunchPieceMessage(model.StateChangeSequenceNumber, piece.Id));
			// over the hand
			} else if(cursorLocation is IHandCursorLocation) {
				IHandCursorLocation location = (IHandCursorLocation) cursorLocation;
				IPiece piece = location.Piece;
				// over a piece
				if(piece != null)
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(piece.Stack))
						if(piece is ITerrainClone)
							networkClient.Send(new RemoveTerrainMessage(model.StateChangeSequenceNumber, -1, piece.IndexInStackFromBottomToTop));
						else
							networkClient.Send(new UnpunchPieceMessage(model.StateChangeSequenceNumber, piece.Id));
			}
		}

		public override void HandleLeftMouseButtonDown() {
			// over the board
			if(model.ThisPlayer.CursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) model.ThisPlayer.CursorLocation;
				// over a piece
				if(location.Piece != null) {
					PointF piecePosition = location.Piece.Position;
					model.ThisPlayer.DragAndDropAnchor = new PointF(
						location.ModelPosition.X - piecePosition.X,
						location.ModelPosition.Y - piecePosition.Y);
					controller.SelectingStackState.StackBottomBeingSelected = location.Piece;
					controller.State = controller.SelectingStackState;
				// over an empty space with the stack inspector visible
				} else if(model.CurrentSelection != null && !model.CurrentSelection.Empty) {
					controller.State = controller.MovingState;
				// over an empty space (no stack inspector)
				} else {
					controller.State = controller.ScrollingState;
				}
			// over the tabs
			} else if(model.ThisPlayer.CursorLocation is ITabsCursorLocation) {
				ITabsCursorLocation location = (ITabsCursorLocation) model.ThisPlayer.CursorLocation;
				// over an icon
				if(location.Icon != TabsIcon.None) {
					// Undo
					if(location.Icon == TabsIcon.Undo) {
						if(model.CommandManager.CanUndo)
							networkClient.Send(new UndoMessage(model.StateChangeSequenceNumber));
					// Redo
					} else if(location.Icon == TabsIcon.Redo) {
						if(model.CommandManager.CanRedo)
							networkClient.Send(new RedoMessage(model.StateChangeSequenceNumber));
					// Show/Hide hand
					} else if(location.Icon == TabsIcon.Hand) {
						if(model.ThisPlayer.Guid != Guid.Empty) {
							IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid);
							if(controller.View.Hand.IsVisible) {
								controller.View.Hand.IsVisible = false;
								if(playerHand != null && playerHand.Count == 0)
									networkClient.Send(new RemovePlayerHandMessage(model.StateChangeSequenceNumber));
							} else {
								controller.View.Hand.IsVisible = true;
								if(playerHand == null)
									networkClient.Send(new AddPlayerHandMessage(model.StateChangeSequenceNumber));
							}
						}
					// Terrain mode
					} else if(location.Icon == TabsIcon.TerrainMode) {
						networkClient.Send(new ChangeModeMessage(model.StateChangeSequenceNumber,
							(model.CurrentGameBox.CurrentGame.Mode == Mode.Terrain ? Mode.Default : Mode.Terrain)));
					// Stacking
					} else if(location.Icon == TabsIcon.Stacking) {
						networkClient.Send(new ChangeStackingMessage(model.StateChangeSequenceNumber));
					// Hide/reveal board
					} else if(location.Icon == TabsIcon.HideReveal) {
						Guid visibleBoardOwner = model.CurrentGameBox.CurrentGame.VisibleBoard.Owner;
						if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox && model.ThisPlayer.Guid != Guid.Empty) {
							if(visibleBoardOwner == Guid.Empty || visibleBoardOwner == model.ThisPlayer.Guid) {
								networkClient.Send(new HideRevealBoardMessage(model.StateChangeSequenceNumber));
							}
						}
					// Tab scrollers
					} else if(location.Icon == TabsIcon.FirstTab) {
						view.Tabs.ShowFirstTab();
					} else if(location.Icon == TabsIcon.PreviousTab) {
						view.Tabs.ShowPreviousTab();
					} else if(location.Icon == TabsIcon.NextTab) {
						view.Tabs.ShowNextTab();
					} else if(location.Icon == TabsIcon.LastTab) {
						view.Tabs.ShowLastTab();
					}
				// over a tab
				} else if(location.Tab != null) {
					networkClient.Send(new VisibleBoardChangedMessage(model.StateChangeSequenceNumber, location.Tab.Id));
				}
			// over the stack inspector
			} else if(model.ThisPlayer.CursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) model.ThisPlayer.CursorLocation;
				// over an icon
				if(location.Icon != StackInspectorIcon.None) {
					// Close
					if(location.Icon == StackInspectorIcon.Close) {
						if(model.CurrentSelection != null) {
							SelectionChangedMessage.SelectionInfo selectionInfo = new SelectionChangedMessage.SelectionInfo();
							selectionInfo.StackId = -1;
							networkClient.Send(new SelectionChangedMessage(model.StateChangeSequenceNumber, selectionInfo));
						}
					// Recycle
					} else if(location.Icon == StackInspectorIcon.Recycle) {
						if(model.CurrentSelection != null && !model.CurrentSelection.Empty && !model.CurrentSelection.Stack.AttachedToCounterSection)
							// assumption: the stack will remain unchanged in the meantime
							if(!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack))
								networkClient.Send(new UnpunchSelectionMessage(model.StateChangeSequenceNumber));
					// Shuffle
					} else if(location.Icon == StackInspectorIcon.Shuffle) {
						if(model.CurrentSelection != null && model.CurrentSelection.Stack.Pieces.Length > 1)
							// assumption: the stack will remain unchanged in the meantime
							if(!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack))
								networkClient.Send(new ShuffleMessage(model.StateChangeSequenceNumber));
					// Invert
					} else if(location.Icon == StackInspectorIcon.Invert) {
						if(model.CurrentSelection != null && model.CurrentSelection.Stack.Pieces.Length > 1)
							// assumption: the stack will remain unchanged in the meantime
							if(!model.AnimationManager.IsBeingAnimated(model.CurrentSelection.Stack))
								networkClient.Send(new InvertMessage(model.StateChangeSequenceNumber));
					}
				// over a piece
				} else if(location.Piece != null) {
					model.ThisPlayer.DragAndDropAnchor = location.AnchorPosition;
					controller.SelectingPieceState.PieceBeingSelected = location.Piece;
					controller.State = controller.SelectingPieceState;
				}
			// over the hand
			} else if(model.ThisPlayer.CursorLocation is IHandCursorLocation) {
				IHandCursorLocation location = (IHandCursorLocation) model.ThisPlayer.CursorLocation;
				// over an icon
				if(location.Icon != HandIcon.None) {
					// Resize handle
					if(location.Icon == HandIcon.Resize) {
						controller.State = controller.ResizingHandState;
					// Pin/Unpin
					} else if(location.Icon == HandIcon.Pin) {
						view.Hand.IsPinned = !view.Hand.IsPinned;
					}
				// over a piece
				} else if(location.Piece != null) {
					model.ThisPlayer.DragAndDropAnchor = location.AnchorPosition;
					controller.SelectingPieceState.PieceBeingSelected = location.Piece;
					controller.State = controller.SelectingPieceState;
				}
			// over a menu item
			} else if(model.ThisPlayer.CursorLocation is IMenuCursorLocation) {
				IMenuCursorLocation location = (IMenuCursorLocation) model.ThisPlayer.CursorLocation;
				if(!location.Item.IsDisabled && location.Item.UserData != null)
					((ZunTzu.Control.Menu.IMenuItem) location.Item.UserData).Select(controller);
			}
		}

		public override void HandleLeftMouseDoubleClick() {
			// over the dice bag
			if(model.ThisPlayer.CursorLocation is IDiceBagCursorLocation) {
				IDiceBagCursorLocation location = (IDiceBagCursorLocation) model.ThisPlayer.CursorLocation;
				// over dice
				if(location.DiceCount > 0) {
					networkClient.Send(new DiceCastMessage(location.DiceHandIndex, location.DiceCount));
				}
			}
		}

		public override void HandleRightMouseDoubleClick() {
			// over the board
			if(model.ThisPlayer.CursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) model.ThisPlayer.CursorLocation;
				IPiece stackBottom = location.Piece;
				// over a stack
				if(stackBottom != null && !stackBottom.Stack.AttachedToCounterSection) {
					bool pieceIsEligible = false;
					foreach(IPiece piece in stackBottom.Stack.Pieces) {
						if(piece == stackBottom)
							pieceIsEligible = true;
						if(pieceIsEligible && (!piece.CounterSection.IsSingleSided || piece.IsBlock)) {
							// assumption: the stack will remain unchanged in the meantime
							if(!model.AnimationManager.IsBeingAnimated(stackBottom.Stack))
								if(stackBottom is ITerrainClone)
									networkClient.Send(new FlipTerrainMessage(model.StateChangeSequenceNumber, stackBottom.Stack.Board.Id, stackBottom.Stack.Board.GetZOrder(stackBottom.Stack)));
								else
									networkClient.Send(new FlipStackMessage(model.StateChangeSequenceNumber, stackBottom.Id));
							break;
						}
					}
				// over an empty space
				} else {
					ICounterSheet visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard as ICounterSheet;
					if(visibleBoard != null && visibleBoard.Properties.BackImageFileName != null)
						networkClient.Send(new FlipCounterSheetMessage(model.StateChangeSequenceNumber));
				}
			// over the stack inspector
			} else if(model.ThisPlayer.CursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) model.ThisPlayer.CursorLocation;
				IPiece piece = location.Piece;
				if(piece != null && (!piece.CounterSection.IsSingleSided || piece.IsBlock))
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(piece.Stack))
						networkClient.Send(new FlipPieceMessage(model.StateChangeSequenceNumber, piece.Id));
			// over the hand
			} else if(model.ThisPlayer.CursorLocation is IHandCursorLocation) {
				IHandCursorLocation location = (IHandCursorLocation) model.ThisPlayer.CursorLocation;
				IPiece piece = location.Piece;
				if(piece != null && (!piece.CounterSection.IsSingleSided || piece.IsBlock)) {
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(piece.Stack)) {
						if(piece is ITerrainClone)
							networkClient.Send(new FlipTerrainMessage(model.StateChangeSequenceNumber, -1, piece.IndexInStackFromBottomToTop));
						else
							networkClient.Send(new FlipPieceMessage(model.StateChangeSequenceNumber, piece.Id));
					}
				}
			}
		}

		public override void HandleGrabKeyPress() {
			ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
			// over the board
			if(cursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
				IPiece stackBottom = location.Piece;
				// assumption: the stack will remain unchanged in the meantime
				if(stackBottom != null && !model.AnimationManager.IsBeingAnimated(stackBottom.Stack)) {
					if(stackBottom is ITerrainClone)
						networkClient.Send(new GrabTerrainMessage(model.StateChangeSequenceNumber, stackBottom.Stack.Board.Id, stackBottom.Stack.Board.GetZOrder(stackBottom.Stack)));
					else
						networkClient.Send(new GrabStackMessage(model.StateChangeSequenceNumber, stackBottom.Id));
				}
			// over the stack inspector
			} else if(cursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
				IPiece piece = location.Piece;
				// assumption: the stack will remain unchanged in the meantime
				if(piece != null && !model.AnimationManager.IsBeingAnimated(piece.Stack)) {
					if(piece.Stack.Pieces.Length == 1)
						networkClient.Send(new GrabStackMessage(model.StateChangeSequenceNumber, piece.Id));
					else
						networkClient.Send(new GrabPieceMessage(model.StateChangeSequenceNumber, piece.Id));
				}
			}
		}

		public override void HandleMouseWheel(int detents) {
			IPiece[] piecesBeingRotated = null;
			// over the board
			if(model.ThisPlayer.CursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) model.ThisPlayer.CursorLocation;
				// over an unattached stack
				if(location.Piece != null && !location.Piece.Stack.AttachedToCounterSection) {
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(location.Piece.Stack)) {
						int indexOfStackBottom = location.Piece.IndexInStackFromBottomToTop;
						IPiece[] stackPieces = location.Piece.Stack.Pieces;
						piecesBeingRotated = new IPiece[stackPieces.Length - indexOfStackBottom];
						for(int i = 0; i < piecesBeingRotated.Length; ++i)
							piecesBeingRotated[i] = stackPieces[i + indexOfStackBottom];
					}
				}
			// over the stack inspector
			} else if(model.ThisPlayer.CursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) model.ThisPlayer.CursorLocation;
				// over an unattached piece
				if(location.Piece != null && !location.Piece.Stack.AttachedToCounterSection)
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(location.Piece.Stack))
						piecesBeingRotated = new IPiece[1] { location.Piece };
			// over the hand
			} else if(model.ThisPlayer.CursorLocation is IHandCursorLocation) {
				IHandCursorLocation location = (IHandCursorLocation) model.ThisPlayer.CursorLocation;
				if(location.Piece != null)
					// assumption: the stack will remain unchanged in the meantime
					if(!model.AnimationManager.IsBeingAnimated(location.Piece.Stack))
						piecesBeingRotated = new IPiece[1] { location.Piece };
			}
			if(piecesBeingRotated != null) {
				// if another rotation is in progress, ignore this new rotation
				if(PiecesBeingRotated != null) {
					if(PiecesBeingRotated.Length != piecesBeingRotated.Length)
						return;
					else
						for(int i = 0; i < PiecesBeingRotated.Length; ++i)
							if(piecesBeingRotated[i] != PiecesBeingRotated[i])
								return;
				}

				PiecesBeingRotated = piecesBeingRotated;
				RotationInProgress = true;
				RotationIncrements += (detents / 120) * 120;

				model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(PiecesBeingRotated, (detents / 120) * 120));

				// NOT(C) => BeginRotation
				if(!WaitingForContinueRotationMessage) {
					WaitingForContinueRotationMessage = true;
					RotationInProgress = false;
					networkClient.Send(new BeginRotationMessage());
				}
			}
		}

		public void ContinueRotation() {
			// Does it need to be made more robust against collisions from different actions?
			// I don't think so, because the state change will get rejected anyway.
			WaitingForContinueRotationMessage = false;
			// B => BeginRotation
			// NOT(B) => RotateStack
			if (RotationInProgress) {
				WaitingForContinueRotationMessage = true;
				RotationInProgress = false;
				networkClient.Send(new BeginRotationMessage());
			} else {
				if(RotationIncrements != 0) {
					IPiece stackBottom = PiecesBeingRotated[0];
					if(PiecesBeingRotated.Length == 1) {
						if(stackBottom is ITerrainClone) {
							if(stackBottom.Stack.Board == null)
								networkClient.Send(new RotateTerrainMessage(model.StateChangeSequenceNumber, -1, stackBottom.IndexInStackFromBottomToTop, RotationIncrements));
							else
								networkClient.Send(new RotateTerrainMessage(model.StateChangeSequenceNumber, stackBottom.Stack.Board.Id, stackBottom.Stack.Board.GetZOrder(stackBottom.Stack), RotationIncrements));
						} else {
							networkClient.Send(new RotatePieceMessage(model.StateChangeSequenceNumber, stackBottom.Id, RotationIncrements));
						}
					} else {
						networkClient.Send(new RotateStackMessage(model.StateChangeSequenceNumber, stackBottom.Id, RotationIncrements));
					}
					RotationIncrements = 0;
				} else {
					PiecesBeingRotated = null;
				}
			}
		}

		public void AcceptRotation() {
			if(!RotationInProgress && !WaitingForContinueRotationMessage)
				PiecesBeingRotated = null;
		}

		public void RejectRotation(int rotationIncrements) {
			// rollback rotation
			model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(PiecesBeingRotated, -rotationIncrements));

			if(!RotationInProgress && !WaitingForContinueRotationMessage)
				PiecesBeingRotated = null;
		}

		public override void UpdateCursor(System.Windows.Forms.Form mainForm, IView view) {
			ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
			if(cursorLocation is IBoardCursorLocation) {
				IBoardCursorLocation location = (IBoardCursorLocation) cursorLocation;
				mainForm.Cursor = (location.Piece == null ? (model.CurrentSelection == null || model.CurrentSelection.Empty ? view.HandCursor : System.Windows.Forms.Cursors.Cross) : view.FingerCursor);
				/*
				mainForm.Cursor = (location.Piece == null ?
					(model.CurrentSelection == null || model.CurrentSelection.Empty ?
						(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox ? System.Windows.Forms.Cursors.WaitCursor : view.HandCursor) :
						System.Windows.Forms.Cursors.Cross) :
					view.FingerCursor);
				 */
			} else if(cursorLocation is IStackInspectorCursorLocation) {
				IStackInspectorCursorLocation location = (IStackInspectorCursorLocation) cursorLocation;
				mainForm.Cursor = (location.Piece == null && location.Icon == StackInspectorIcon.None ? System.Windows.Forms.Cursors.Default : view.FingerCursor);
			} else if(cursorLocation is IDiceBagCursorLocation) {
				IDiceBagCursorLocation location = (IDiceBagCursorLocation) cursorLocation;
				mainForm.Cursor = (location.DiceCount == 0 ? System.Windows.Forms.Cursors.Default : view.FingerDoubleCursor);
			} else if(cursorLocation is ITabsCursorLocation) {
				ITabsCursorLocation location = (ITabsCursorLocation) cursorLocation;
				mainForm.Cursor = (location.Tab == null && location.Icon == TabsIcon.None ? System.Windows.Forms.Cursors.Default : view.FingerCursor);
			} else if(cursorLocation is IMenuCursorLocation) {
				IMenuCursorLocation location = (IMenuCursorLocation) cursorLocation;
				mainForm.Cursor = (location.Item == null ? System.Windows.Forms.Cursors.Default : view.FingerCursor);
			} else if(cursorLocation is IHandCursorLocation) {
				IHandCursorLocation location = (IHandCursorLocation) cursorLocation;
				mainForm.Cursor = (location.Piece == null && location.Icon == HandIcon.None ? System.Windows.Forms.Cursors.Default : (location.Icon == HandIcon.Resize ? System.Windows.Forms.Cursors.HSplit : view.FingerCursor));
			} else {
				mainForm.Cursor = System.Windows.Forms.Cursors.Default;
			}
		}

		public IPiece[] PiecesBeingRotated = null;
		public int RotationIncrements = 0;	// A
		public bool RotationInProgress = false;	// B
		public bool WaitingForContinueRotationMessage = false;	// C
	}
}
