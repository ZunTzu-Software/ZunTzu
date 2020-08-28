// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Visualization {

	/// <summary>
	/// Summary description for Tabs.
	/// </summary>
	internal sealed class Tabs : ViewElement, ITabs {

		public Tabs(IModel model, View view, IGraphics graphics) : base(view) {
			this.model = model;
			this.graphics = graphics;
		}

		/// <summary>Position and size of the tabs in screen coordinates.</summary>
		public RectangleF Area { get { return view.TabsArea; } }

		/// <summary>Scroll to the first tab position.</summary>
		public void ShowFirstTab() {
			tabShown = 0;
		}

		/// <summary>Scroll to the previous tab position.</summary>
		public void ShowPreviousTab() {
			tabShown = Math.Max(0, tabShown - 1);
		}

		/// <summary>Scroll to the next tab position.</summary>
		public void ShowNextTab() {
			tabShown = Math.Min(lastTabStop, tabShown + 1);
		}

		/// <summary>Scroll to the last tab position.</summary>
		public void ShowLastTab() {
			tabShown = lastTabStop;
		}

		private int tabCount {
			get {
				IGame game = model.CurrentGameBox.CurrentGame;
				IBoard[] boards = game.Boards;

				if(game.Mode == Mode.Terrain) {
					return boards.Length;
				} else {
					int count = 0;
					for(int i = 0; i < boards.Length; ++i) {
						ICounterSheet counterSheet = boards[i] as ICounterSheet;
						if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain)
							++count;
					}
					return count;
				}
			}
		}

		/// <summary>Retrieves the board for the tab at the given position.</summary>
		/// <param name="position">A position in screen coordinates.</param>
		/// <returns>A board, or null if none is found.</returns>
		private IBoard getTabAtPosition(PointF position) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IBoard[] boards = game.Boards;

			int boardCount = tabCount;
			if(boardCount > 1) {
				RectangleF area = view.TabsArea;
				if(new RectangleF(area.X, area.Y + 6, area.Width, 20.0f).Contains(position)) {
					position.X -= area.X;
					position.Y -= area.Y + 6;

					if(scrollersNeeded)
						position.X += scrollOffset - 63.0f;

					IBoard visibleBoard = game.VisibleBoard;
					int boardIndex = 0;
					IBoard previousBoard = null;
					for(int i = 0; i < boards.Length; ++i) {
						IBoard board = boards[i];
						ICounterSheet counterSheet = board as ICounterSheet;
						if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain || game.Mode == Mode.Terrain) {
							if(position.X < 0.0f)
								break;
							if(position.X < 13.0f) {
								if(boardIndex == 0) {
									if(position.X >= position.Y * (13.0f / 20.0f))
										return board;
								} else {
									if(board == visibleBoard) {
										if(position.X >= position.Y * (13.0f / 20.0f))
											return board;
										else if(position.X < 16.0f - position.Y * (13.0f / 20.0f))
											return previousBoard;
									} else {
										if(position.X < 13.0f - position.Y * (13.0f / 20.0f))
											return previousBoard;
										else if(position.X >= -3.0f + position.Y * (13.0f / 20.0f))
											return board;
									}
								}
								return null;
							}
							position.X -= 13.0f;

							float textWidth = graphics.GetTextWidthInPixels(font, board.Name);
							if(position.X < textWidth)
								return board;
							position.X -= textWidth - 3.0f;

							if(boardIndex == boardCount - 1) {
								if(position.X < 13.0f - position.Y * (13.0f / 20.0f))
									return board;
								else
									return null;
							}
							previousBoard = board;
							++boardIndex;
						}
					}
				}
			}
			return null;
		}

		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			RectangleF area = view.TabsArea;
			IGame game = model.CurrentGameBox.CurrentGame;
			IBoard[] boards = game.Boards;

			int boardCount = tabCount;
			if(boardCount > 1) {
				float position = area.X;
				IBoard visibleBoard = game.VisibleBoard;

				IBoard tabAtMousePosition = null;
				if(model.ThisPlayer.CursorLocation is ITabsCursorLocation)
					tabAtMousePosition = ((ITabsCursorLocation) model.ThisPlayer.CursorLocation).Tab;

				float tabShownOffset = 0.0f;

				// calculate total width
				float totalWidth = 16.0f;
				for(int i = 0; i < boards.Length; ++i) {
					IBoard board = boards[i];
					ICounterSheet counterSheet = board as ICounterSheet;
					if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain || game.Mode == Mode.Terrain) {
						string boardName = board.Name;
						float textWidth = graphics.GetTextWidthInPixels(font, boardName);
						totalWidth += textWidth + 10.0f;
						if(i < tabShown)
							tabShownOffset += textWidth + 10.0f;
					}
				}

				// if total width exceeds screen width (minus margin), display scrollers
				float widthOverflow = totalWidth + 150.0f - area.Width;
				scrollersNeeded = (widthOverflow > 0.0f);

				if(scrollersNeeded) {
					// calculate last tab stop
					widthOverflow += 63.0f;
					for(int i = 0; i < boards.Length; ++i) {
						IBoard board = boards[i];
						ICounterSheet counterSheet = board as ICounterSheet;
						if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain || game.Mode == Mode.Terrain) {
							string boardName = board.Name;
							float textWidth = graphics.GetTextWidthInPixels(font, boardName);
							widthOverflow -= textWidth + 10.0f;
							if(widthOverflow <= 0.0f) {
								lastTabStop = i + 1;
								break;
							}
						}
					}

					scrollOffset = (scrollOffset < tabShownOffset ? Math.Min(scrollOffset + 40.0f, tabShownOffset) : Math.Max(scrollOffset - 40.0f, tabShownOffset));
					position -= scrollOffset;
					tabsImageElements[7].Render(new RectangleF(position, area.Y, 63.0f, 26.0f));
					position += 63.0f;
				} else {
					scrollOffset = 0.0f;
					lastTabStop = 0;
					tabShown = 0;
				}

				bool previousBoardIsVisible = false;
				bool previousBoardIsOwned = false;
				uint previousBoardOwnerColor = 0;
				int boardIndex = 0;
				for(int i = 0; i < boards.Length; ++i) {
					IBoard board = boards[i];
					ICounterSheet counterSheet = board as ICounterSheet;
					if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain || game.Mode == Mode.Terrain) {
						string boardName = board.Name;
						float textWidth = graphics.GetTextWidthInPixels(font, boardName);

						if(boardIndex == 0)
							tabsImageElements[board == visibleBoard ? 8 : 0].Render(new RectangleF(position, area.Y, 16.0f, 26.0f));
						else if(board != visibleBoard)
							tabsImageElements[previousBoardIsVisible ? 5 : 2].Render(new RectangleF(position, area.Y, 16.0f, 26.0f));
						if(previousBoardIsOwned)
							hiddenIcon.Render(new RectangleF(position - 9.0f, area.Y + 4.0f, 17.0f, 11.0f), previousBoardOwnerColor);
						if(boardIndex != 0 && board == visibleBoard)
							tabsImageElements[3].Render(new RectangleF(position, area.Y, 16.0f, 26.0f));
						position += 16.0f;

						tabsImageElements[board == visibleBoard ? 4 : 1].Render(new RectangleF(position, area.Y, textWidth - 6.0f, 26.0f));
						graphics.DrawText(font, (board == tabAtMousePosition ? 0xFF7FFF7F : (board == visibleBoard ? 0xFFFFFFFF : 0xFFBFBFBF)),
							new RectangleF(position - 3.0f, area.Y + 7.0f, textWidth, 26.0f), StringAlignment.Center, boardName);
						position += textWidth - 6.0f;

						if(board.Owner != Guid.Empty) {
							previousBoardIsOwned = true;
							IPlayer player = model.GetPlayerByGuid(board.Owner);
							previousBoardOwnerColor = (player != null ? player.Color : 0xffffffff);
						} else {
							previousBoardIsOwned = false;
						}

						if(boardIndex == boardCount - 1) {
							tabsImageElements[board == visibleBoard ? 9 : 6].Render(new RectangleF(position, area.Y, 16.0f, 26.0f));
						} else {
							previousBoardIsVisible = (board == visibleBoard);
						}
						++boardIndex;
					}
				}
				if(previousBoardIsOwned)
					hiddenIcon.Render(new RectangleF(position - 7.0f, area.Y + 4.0f, 17.0f, 11.0f), previousBoardOwnerColor);
				position += 16.0f;
				tabsImageElements[7].Render(new RectangleF(position, area.Y, area.Right - position, 26.0f));
			} else {
				scrollersNeeded = false;
			}

			// icons
			ITabsCursorLocation cursorLocation = model.ThisPlayer.CursorLocation as ITabsCursorLocation;
			if(scrollersNeeded) {
				for(int i = 0; i < 4; ++i) {
					tabScrollers[i].Render(new RectangleF(area.Left + i * 16.0f + 1.0f, area.Bottom - 20.0f, 15.0f, 15.0f),
					(cursorLocation != null && cursorLocation.Icon == (TabsIcon.FirstTab + i) && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
				}
			}
			hideRevealIcon.Render(new RectangleF(area.Right - 154.0f, area.Bottom - 19.0f, 21.0f, 17.0f),
				(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || model.ThisPlayer.Guid == Guid.Empty || (game.VisibleBoard.Owner != Guid.Empty && game.VisibleBoard.Owner != model.ThisPlayer.Guid) ? 0xFF7F7F7F : (cursorLocation != null && cursorLocation.Icon == TabsIcon.HideReveal && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));
			if(game.StackingEnabled) {
				stackingEnabledIcon.Render(new RectangleF(area.Right - 127.0f, area.Bottom - 19.0f, 21.0f, 18.0f),
					(cursorLocation != null && cursorLocation.Icon == TabsIcon.Stacking && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
			} else {
				stackingDisabledIcon.Render(new RectangleF(area.Right - 127.0f, area.Bottom - 19.0f, 21.0f, 18.0f),
					(cursorLocation != null && cursorLocation.Icon == TabsIcon.Stacking && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
			}
			if(game.Mode == Mode.Terrain) {
				// computes pulsation
				float pulsation = 1.15f + 0.15f * (float) Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0));
				terrainModeIcon.Render(new RectangleF(area.Right - 100.0f + 13.5f * (1.0f - pulsation), area.Bottom - 19.0f + 9.5f * (1.0f - pulsation), 27.0f * pulsation, 19.0f * pulsation),
					(cursorLocation != null && cursorLocation.Icon == TabsIcon.TerrainMode && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
			} else {
				terrainModeIcon.Render(new RectangleF(area.Right - 100.0f, area.Bottom - 19.0f, 27.0f, 19.0f),
					(cursorLocation != null && cursorLocation.Icon == TabsIcon.TerrainMode && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
			}
			handIcon.Render(new RectangleF(area.Right - 66.0f, area.Bottom - 19.0f, 17.0f, 18.0f),
				(model.ThisPlayer.Guid == Guid.Empty ? 0xFF7F7F7F : (cursorLocation != null && cursorLocation.Icon == TabsIcon.Hand && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));
			undoIcon.Render(new RectangleF(area.Right - 42.0f, area.Bottom - 19.0f, 16.0f, 16.0f),
				(!model.CommandManager.CanUndo ? 0xFF7F7F7F : (cursorLocation != null && cursorLocation.Icon == TabsIcon.Undo && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));
			redoIcon.Render(new RectangleF(area.Right - 19.0f, area.Bottom - 19.0f, 16.0f, 16.0f),
				(!model.CommandManager.CanRedo ? 0xFF7F7F7F : (cursorLocation != null && cursorLocation.Icon == TabsIcon.Redo && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));

			// tool tips
			if(cursorLocation != null && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null) {
				if(cursorLocation.Icon == TabsIcon.Undo)
					graphics.DrawText(font, (!model.CommandManager.CanUndo ? 0xFF7F7F7F : 0xff7fff7f),
						new RectangleF(area.Right - 42.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						Resources.ToolTipUndo);
				else if(cursorLocation.Icon == TabsIcon.Redo)
					graphics.DrawText(font, (!model.CommandManager.CanRedo ? 0xFF7F7F7F : 0xff7fff7f),
						new RectangleF(area.Right - 19.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						Resources.ToolTipRedo);
				else if(cursorLocation.Icon == TabsIcon.Hand)
					graphics.DrawText(font, (model.ThisPlayer.Guid == Guid.Empty ? 0xFF7F7F7F : 0xff7fff7f),
						new RectangleF(area.Right - 66.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						(view.Hand.IsVisible ? Resources.ToolTipHideHand : Resources.ToolTipShowHand));
				else if(cursorLocation.Icon == TabsIcon.TerrainMode)
					graphics.DrawText(font, 0xff7fff7f,
						new RectangleF(area.Right - 100.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						Resources.ToolTipTerrainMode);
				else if(cursorLocation.Icon == TabsIcon.Stacking)
					graphics.DrawText(font, 0xff7fff7f,
						new RectangleF(area.Right - 127.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						Resources.ToolTipStacking);
				else if(cursorLocation.Icon == TabsIcon.HideReveal)
					graphics.DrawText(font, (model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || model.ThisPlayer.Guid == Guid.Empty || (game.VisibleBoard.Owner != Guid.Empty && game.VisibleBoard.Owner != model.ThisPlayer.Guid) ? 0xFF7F7F7F : 0xff7fff7f),
						new RectangleF(area.Right - 154.0f, area.Bottom - 19.0f - 17.0f, 16.0f, 17.0f), StringAlignment.Far,
						(game.VisibleBoard.Owner == Guid.Empty ? Resources.ToolTipHideBoard : Resources.ToolTipRevealBoard));
			}
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			tabsImageElements = new IImage[10] {
				iconsTileSet.ExtractImage(new RectangleF( 74.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF( 90.0f * 4, 1.0f * 4,  1.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF( 95.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(111.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(128.0f * 4, 1.0f * 4,  1.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(132.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(150.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(204.0f * 4, 1.0f * 4,  1.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(166.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(187.0f * 4, 1.0f * 4, 16.0f * 4, 26.0f * 4))
			};
			terrainModeIcon = iconsTileSet.ExtractImage(new RectangleF(38.0f * 4, 55.0f * 4, 27.0f * 4, 19.0f * 4));
			handIcon = iconsTileSet.ExtractImage(new RectangleF(19.0f * 4, 55.0f * 4, 17.0f * 4, 18.0f * 4));
			undoIcon = iconsTileSet.ExtractImage(new RectangleF(1.0f * 4, 60.0f * 4, 16.0f * 4, 16.0f * 4));
			redoIcon = iconsTileSet.ExtractImage(new RectangleF(2.0f * 4, 77.0f * 4, 16.0f * 4, 16.0f * 4));
			stackingEnabledIcon = iconsTileSet.ExtractImage(new RectangleF(232.0f * 4, 42.0f * 4, 21.0f * 4, 18.0f * 4));
			stackingDisabledIcon = iconsTileSet.ExtractImage(new RectangleF(232.0f * 4, 61.0f * 4, 21.0f * 4, 18.0f * 4));
			hideRevealIcon = iconsTileSet.ExtractImage(new RectangleF(232.0f * 4, 152.0f * 4, 21.0f * 4, 17.0f * 4));
			hiddenIcon = iconsTileSet.ExtractImage(new RectangleF(232.0f * 4, 80.0f * 4, 17.0f * 4, 11.0f * 4));
			tabScrollers = new IImage[4] {
				iconsTileSet.ExtractImage(new RectangleF(215.0f * 4, 30.0f * 4, 15.0f * 4, 15.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(215.0f * 4, 46.0f * 4, 15.0f * 4, 15.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(215.0f * 4, 62.0f * 4, 15.0f * 4, 15.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(215.0f * 4, 78.0f * 4, 15.0f * 4, 15.0f * 4))
			};
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			PointF screenPosition = cursorLocation.ScreenPosition;
			if(Area.Contains(screenPosition)) {
				TabsCursorLocation location = cursorLocation as TabsCursorLocation;
				if(location == null) {
					location = new TabsCursorLocation();
					cursorLocation = location;
				}
				location.Icon = getIconAtPosition(screenPosition);
				location.Tab = (location.Icon != TabsIcon.None ? null : getTabAtPosition(screenPosition));
				return true;
			} else {
				return false;
			}
		}

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class TabsCursorLocation : CursorLocation, ITabsCursorLocation {
			/// <summary>The board for the tab at the mouse cursor position.</summary>
			public IBoard Tab { get { return tab; } set { tab = value; } }
			/// <summary>The icon displayed at the mouse cursor position.</summary>
			public TabsIcon Icon { get { return icon; } set { icon = value; } }
			private IBoard tab;
			private TabsIcon icon;
		}

		/// <summary>Returns the icon at the given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>A value indicating the icon.</returns>
		private TabsIcon getIconAtPosition(PointF mouseScreenPosition) {
			RectangleF area = view.TabsArea;
			if(area.Contains(mouseScreenPosition)) {
				// Undo
				RectangleF undoIconLocation = new RectangleF(area.Right - 42.0f, area.Bottom - 19.0f, 16.0f, 16.0f);
				if(undoIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.Undo;

				// Redo
				RectangleF redoIconLocation = new RectangleF(area.Right - 19.0f, area.Bottom - 19.0f, 16.0f, 16.0f);
				if(redoIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.Redo;

				// Hand
				RectangleF handIconLocation = new RectangleF(area.Right - 66.0f, area.Bottom - 19.0f, 17.0f, 18.0f);
				if(handIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.Hand;

				// Terrain mode
				RectangleF terrainModeIconLocation = new RectangleF(area.Right - 100.0f, area.Bottom - 19.0f, 27.0f, 19.0f);
				if(terrainModeIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.TerrainMode;

				// Stacking
				RectangleF stackingIconLocation = new RectangleF(area.Right - 127.0f, area.Bottom - 19.0f, 19.0f, 18.0f);
				if(stackingIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.Stacking;

				// Hide/Reveal
				RectangleF revealIconLocation = new RectangleF(area.Right - 154.0f, area.Bottom - 19.0f, 21.0f, 17.0f);
				if(revealIconLocation.Contains(mouseScreenPosition))
					return TabsIcon.HideReveal;

				// Tab scrollers
				if(scrollersNeeded) {
					for(int i = 0; i < 4; ++i) {
						RectangleF tabScrollerLocation = new RectangleF(area.Left + i * 16.0f + 1.0f, area.Bottom - 20.0f, 15.0f, 15.0f);
						if(tabScrollerLocation.Contains(mouseScreenPosition))
							return (TabsIcon) (TabsIcon.FirstTab + i);
					}
				}
			}
			return TabsIcon.None;
		}

		private readonly IModel model;
		private readonly IGraphics graphics;
		private readonly Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
		/// <summary>Tabs.</summary>
		private IImage[] tabsImageElements = null;
		private IImage undoIcon = null;
		private IImage redoIcon = null;
		private IImage handIcon = null;
		private IImage terrainModeIcon = null;
		private IImage stackingEnabledIcon = null;
		private IImage stackingDisabledIcon = null;
		private IImage hideRevealIcon = null;
		private IImage hiddenIcon = null;
		private bool scrollersNeeded = false;
		private IImage[] tabScrollers = null;
		private float scrollOffset = 0.0f;
		private int lastTabStop = 0;
		private int tabShown = 0;
	}
}
