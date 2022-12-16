// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZunTzu.AudioVideo;
using ZunTzu.Control.Dialogs;
using ZunTzu.FileSystem;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;
using ZunTzu.Timing;

namespace ZunTzu.Visualization {

	internal abstract class CursorLocation : ICursorLocation {
		/// <summary>Position of the mouse cursor in screen coordinates.</summary>
		public Point ScreenPosition { get { return screenPosition; } set { screenPosition = value; } }
		/// <summary>Position of the mouse cursor in model coordinates.</summary>
		public PointF ModelPosition { get { return modelPosition; } set { modelPosition = value; } }
		private Point screenPosition;
		private PointF modelPosition;
	}

	internal abstract class ViewElement {
		/// <summary>Constructor</summary>
		protected ViewElement(View view) {
			this.view = view;
		}
		/// <summary>Called when the game display area is resized.</summary>
		public virtual void OnGameDisplayAreaResized() {}
		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public virtual void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {}
		/// <summary>Displays the next frame.</summary>
		public virtual void Render(IGraphics graphics, long currentTimeInMicroseconds) {}
		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public virtual bool ContainsCursorLocation(ref ICursorLocation cursorLocation) { return false; }
		protected readonly View view;
	}

	/// <summary>Component in charge of displaying everything.</summary>
	public sealed class View : IView {

		private enum ViewElementType { PlayerView, StackInspector, Hand, Tabs, Prompter, DiceBag, Menu };

		/// <summary>Constructor.</summary>
		/// <param name="model">Current state of the program.</param>
		/// <param name="mainForm">Windows form that hosts the display.</param>
		public View(IModel model, Form mainForm, DisplayProperties displayProperties) {
			this.mainForm = mainForm;
			this.model = model;

			GraphicsProperties graphicsProperties;
			graphicsProperties.WaitForVerticalBlank = displayProperties.WaitForVerticalBlank;
			graphicsProperties.PreferredFullscreenMode = displayProperties.PreferredFullscreenMode;
			graphics = new DXGraphics(mainForm, graphicsProperties, displayProperties.GameAspectRatio);

			viewElements = new ViewElement[] {
				new PlayerView(model, this),
				new StackInspector(model, this),
				new Hand(model, this),
				new Tabs(model, this, graphics),
				new Prompter(this, mainForm),
				new DiceBag(model, this),
				new Menu(model, this)
			};

			graphics.GameDisplayAreaResized += new GameDisplayAreaResizedHandler(onGameDisplayAreaResized);

			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.Hand.cur").Open()) {
				handCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.Fist.cur").Open()) {
				fistCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.Zoom.cur").Open()) {
				zoomCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.Finger.cur").Open()) {
				fingerCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.FingerAdd.cur").Open()) {
				fingerAddCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.FingerRemove.cur").Open()) {
				fingerRemoveCursor = new Cursor(resourceStream);
			}
			using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.FingerDouble.cur").Open()) {
				fingerDoubleCursor = new Cursor(resourceStream);
			}
		}

		private float panelBarMargin { get { return (0.005f * 1.6f) * gameDisplayArea.Height; } }
		private float panelBarWidth { get { return (195.0f / 1200.0f) * gameDisplayArea.Height; } }
		internal RectangleF TabsArea {
			get {
				float height = 26.0f;
				return new RectangleF(
					gameDisplayArea.X,
					gameDisplayArea.Y + gameDisplayArea.Height - height,
					gameDisplayArea.Width,
					height);
			}
		}
		internal RectangleF HandArea {
			get {
				float height = ((Hand) Hand).Height;
				return new RectangleF(
					gameDisplayArea.X,
					gameDisplayArea.Y + gameDisplayArea.Height - height,
					gameDisplayArea.Width,
					height);
			}
		}
		internal RectangleF PlayerViewArea {
			get {
				float playerViewHeight = ((PlayerView)PlayerView).GetHeight(gameDisplayArea.Height);
				if(playerViewHeight == 0.0f) {
					return new RectangleF(
						gameDisplayArea.X + panelBarMargin,
						DiceBagArea.Bottom,
						panelBarWidth,
						0.0f);
				} else {
					return new RectangleF(
						gameDisplayArea.X + panelBarMargin,
						DiceBagArea.Bottom + panelBarMargin,
						panelBarWidth,
						playerViewHeight);
				}
			}
		}
		internal RectangleF StackInspectorArea {
			get {
				RectangleF playerViewArea = PlayerViewArea;
				return new RectangleF(
					gameDisplayArea.X + panelBarMargin,
					playerViewArea.Bottom + panelBarMargin,
					panelBarWidth,
					gameDisplayArea.Y + gameDisplayArea.Height - TabsArea.Height - 2.0f * panelBarMargin - playerViewArea.Bottom);
			}
		}
		internal RectangleF PrompterArea {
			get {
				return new RectangleF(
					gameDisplayArea.X + 2 * panelBarMargin + panelBarWidth,
					gameDisplayArea.Y + panelBarMargin,
					gameDisplayArea.Width - 3 * panelBarMargin - panelBarWidth,
					gameDisplayArea.Height - TabsArea.Height - 2 * panelBarMargin);
			}
		}
		internal RectangleF DiceBagArea {
			get {
				float diceBagHeight = ((DiceBag)DiceBag).GetHeight(gameDisplayArea.Height);
				if(diceBagHeight == 0) {
					return new RectangleF(
						gameDisplayArea.X + panelBarMargin,
						gameDisplayArea.Y,
						panelBarWidth,
						0.0f);
				} else {
					return new RectangleF(
						gameDisplayArea.X + panelBarMargin,
						gameDisplayArea.Y + panelBarMargin,
						panelBarWidth,
						diceBagHeight);
				}
			}
		}
		internal RectangleF MenuArea {
			get {
				return new RectangleF(
					gameDisplayArea.Right - 150.0f - panelBarMargin,
					gameDisplayArea.Y + panelBarMargin,
					150.0f,
					gameDisplayArea.Height - 2 * panelBarMargin);
			}
		}

		/// <summary>Display a dialog box.</summary>
		/// <param name="dialog">Dialog box.</param>
		public void ShowDialog(Form dialog) {
			dialog.TopLevel = false;
			dialog.Parent = mainForm;
			dialog.Location = new Point(gameDisplayArea.X + gameDisplayArea.Width / 2 - dialog.Width / 2, gameDisplayArea.Y + gameDisplayArea.Height / 2 - dialog.Height / 2);
			dialog.Show();
			dialog.Focus();

			currentDialog = dialog;
			dialog.Closed += new EventHandler(onDialogClosed);
		}

		/// <summary>User preferences for the display.</summary>
		/// <remarks>Setting the properties will deallocate all resources.</remarks>
		public DisplayProperties DisplayProperties {
			get {
				GraphicsProperties graphicsProperties = graphics.Properties;
				DisplayProperties properties;
				properties.WaitForVerticalBlank = graphicsProperties.WaitForVerticalBlank;
				properties.PreferredFullscreenMode = graphicsProperties.PreferredFullscreenMode;
				properties.GameAspectRatio = graphics.GameAspectRatio;
				return properties;
			}
			set {
				GraphicsProperties graphicsProperties = graphics.Properties;
				graphics.GameAspectRatio = value.GameAspectRatio;
				graphicsProperties.WaitForVerticalBlank = value.WaitForVerticalBlank;
				graphicsProperties.PreferredFullscreenMode = value.PreferredFullscreenMode;
				graphics.Properties = graphicsProperties;
			}
		}

		/// <summary>All eligible fullscreen modes for this display adapter.</summary>
		public IFullscreenMode[] EligibleFullscreenModes { get { return graphics.EligibleFullscreenModes; } }

		/// <summary>Toggles between fullscreen and windowed mode.</summary>
		public bool Fullscreen { get { return graphics.Fullscreen; } set { graphics.Fullscreen = value; } }

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public void ResetGraphicsElements() {
			graphics.FreeResources();

			// update display area
			gameDisplayArea = graphics.GameDisplayAreaInPixels;

			// invalidate cached background image
			cachedBackgroundBoard = null;

			// load cursor and other icons
			IFile iconsImageFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.Icons.png");
			IFile iconsMaskFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.IconsMask.png");
			ITileSet iconsTileSet = graphics.LoadTileSet(iconsImageFile, iconsMaskFile, DetailLevelType.Low);
			iconsTileSet.LoadIcons();
			arrowCursorImage = iconsTileSet.ExtractImage(new RectangleF(0.0f * 4, 0.0f * 4, 12.0f * 4, 21.0f * 4));
			fingerCursorImage = iconsTileSet.ExtractImage(new RectangleF(1.0f * 4, 23.0f * 4, 17.0f * 4, 22.0f * 4));
			rulerExtremityImage = iconsTileSet.ExtractImage(new RectangleF(31.0f * 4, 1.0f * 4, 22.0f * 4, 22.0f * 4));
			rulerLineImage = iconsTileSet.ExtractImage(new RectangleF(52.0f * 4, 7.0f * 4, 1.0f * 4, 10.0f * 4));
			borderImageElements = new IImage[8] {
				iconsTileSet.ExtractImage(new RectangleF(33.0f * 4, 26.0f * 4, 12.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(50.0f * 4, 26.0f * 4, 1.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(55.0f * 4, 26.0f * 4, 12.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(55.0f * 4, 39.0f * 4, 12.0f * 4, 1.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(55.0f * 4, 41.0f * 4, 12.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(50.0f * 4, 41.0f * 4, 1.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(33.0f * 4, 41.0f * 4, 12.0f * 4, 12.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(33.0f * 4, 39.0f * 4, 12.0f * 4, 1.0f * 4))
			};
			progressBarImage = iconsTileSet.ExtractImage(new RectangleF(0.0f * 4, 239.0f * 4, 254.0f * 4, 15.0f * 4));
			hintIcons = new IImage[4] {
				iconsTileSet.ExtractImage(new RectangleF(68.0f * 4, 30.0f * 4, 63.0f * 4, 63.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(133.0f * 4, 30.0f * 4, 25.0f * 4, 63.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(159.0f * 4, 30.0f * 4, 25.0f * 4, 63.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(185.0f * 4, 30.0f * 4, 25.0f * 4, 63.0f * 4))
			};

			// reload all graphics resource in view elements
			foreach(ViewElement element in viewElements)
				element.ResetGraphicsElements(graphics, iconsTileSet);

			// load maps and pieces
			IGameBox gameBox = model.CurrentGameBox;
			if(gameBox.Reference == model.GameLibrary.DefaultGameBox) {
				// load title
				IFile titleImageFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.ZunTzuTitle.png");
				ITileSet titleTileSet = graphics.LoadTileSet(titleImageFile, DetailLevelType.Low);
				titleTileSet.LoadIcons();
				title = titleTileSet.ExtractImage(new RectangleF(0.0f, 0.0f, 254.0f * 4, 55.0f * 4));
			} else {
				title = null;
				loadingGraphics = true;
				loadingGraphicsProgress = loadGraphicsIncrements().GetEnumerator();
			}
		}

		public void UpdateMouseLocations() {
			// adjust visible area
			IGame game = model.CurrentGameBox.CurrentGame;
			IBoard visibleBoard = game.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			switch(graphics.GameAspectRatio) {
				case AspectRatioType.FourToThree:
					visibleArea.Height = visibleArea.Width * (3.0f / 4.0f);
					break;
				case AspectRatioType.SixteenToTen:
					visibleArea.Height = visibleArea.Width * (10.0f / 16.0f);
					break;
			}
			visibleBoard.VisibleArea = visibleArea;

			Guid visibleBoardOwner = visibleBoard.Owner;

			// determine mouse locations
			model.ThisPlayer.CursorLocation.ScreenPosition = (mainForm.IsDisposed ? Point.Empty : mainForm.PointToClient(Cursor.Position));
			IPlayer[] players = model.Players;
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				ICursorLocation location = player.CursorLocation; 
				Point mouseScreenPosition = location.ScreenPosition;
				PointF mouseModelPosition = ConvertScreenToModelCoordinates(mouseScreenPosition);
				if(GameDisplayAreaInPixels.Contains(mouseScreenPosition)) {
					bool cursorIsOverBoard = true;
					for(int i = viewElements.Length - 1; i >= 0; --i) {
						if(player == model.ThisPlayer || (i != (int) ViewElementType.Menu && i != (int) ViewElementType.Hand)) {
							ViewElement element = viewElements[i];
							if(element.ContainsCursorLocation(ref location)) {
								cursorIsOverBoard = false;
								break;
							}
						}
					}
					if(cursorIsOverBoard) {
						if(!(location is BoardCursorLocation))
							location = new BoardCursorLocation();
						((BoardCursorLocation) location).Piece =
							(visibleBoardOwner == Guid.Empty || visibleBoardOwner == player.Guid ?
								(game.Mode == Mode.Terrain ?
									visibleBoard.GetTerrainAtPosition(location.ModelPosition) :
									visibleBoard.GetPieceAtPosition(location.ModelPosition)) :
								null);
					}
				} else {
					if(!(location is OutOfFrameCursorLocation))
						location = new OutOfFrameCursorLocation();
				}
				location.ScreenPosition = mouseScreenPosition;
				location.ModelPosition = mouseModelPosition;
				player.CursorLocation = location;
			}

			// find out which unfolded stacks must be folded back
			bool someFoldingOccured = false;
			visibleBoard.FillListWithUnfoldedStacksWithinAreaFrontToBack(unfoldedStackList, visibleArea);
			foreach(IStack stack in unfoldedStackList) {
				bool stillUnfolded = false;
				for(int p = 0; p < players.Length; ++p) {
					IPlayer player = players[p];
					if(player.IsCursorVisible && player.StackBeingDragged == null && player.PieceBeingDragged == null && (visibleBoardOwner == Guid.Empty || visibleBoardOwner == player.Guid)) {
						BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
						if(cursor != null && cursor.Piece != null && cursor.Piece.Stack == stack &&
							(player.DeckAutoInspect || !(cursor.Piece is ICard)))
						{
							stillUnfolded = true;
							break;
						}
					}
				}
				if(!stillUnfolded) {
					stack.Unfolded = false;
					if(stack.Pieces.Length > 1)
						someFoldingOccured = true;
				}
			}
			// recompute mouse locations in case of folding
			if(someFoldingOccured) {
				for(int p = 0; p < players.Length; ++p) {
					IPlayer player = players[p];
					BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
					if(cursor != null)
						cursor.Piece =
							(visibleBoardOwner == Guid.Empty || visibleBoardOwner == player.Guid ?
								(game.Mode == Mode.Terrain ?
									visibleBoard.GetTerrainAtPosition(cursor.ModelPosition) :
									visibleBoard.GetPieceAtPosition(cursor.ModelPosition)) :
								null);
				}
			}
			// find out which folded stacks must be unfolded
			bool someUnfoldingOccured = false;
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				if(player.IsCursorVisible && player.StackBeingDragged == null && player.PieceBeingDragged == null && (visibleBoardOwner == Guid.Empty || visibleBoardOwner == player.Guid)) {
					BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
					if(cursor != null && cursor.Piece != null) {
						IStack stack = cursor.Piece.Stack;
						if(!stack.Unfolded && (player.DeckAutoInspect || !(cursor.Piece is ICard))) {
							stack.Unfolded = true;
							if(stack.Pieces.Length > 1)
								someUnfoldingOccured = true;
						}
					}
				}
			}
			// recompute mouse locations in case of unfolding
			if(someUnfoldingOccured) {
				for(int p = 0; p < players.Length; ++p) {
					IPlayer player = players[p];
					BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
					if(cursor != null)
						cursor.Piece =
							(visibleBoardOwner == Guid.Empty || visibleBoardOwner == player.Guid ?
								(game.Mode == Mode.Terrain ?
									visibleBoard.GetTerrainAtPosition(cursor.ModelPosition) :
									visibleBoard.GetPieceAtPosition(cursor.ModelPosition)) :
								null);
				}
			}

			// update rolled-over flags for cards
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
				if(cursor != null && cursor.Piece != null && cursor.Piece is ICard && cursor.Piece.Stack.Unfolded)
					foreach(IPiece piece in cursor.Piece.Stack.Pieces)
						if(piece != cursor.Piece)
							piece.RolledOver = false;
			}
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				BoardCursorLocation cursor = player.CursorLocation as BoardCursorLocation;
				if(cursor != null && cursor.Piece != null && cursor.Piece is ICard && cursor.Piece.Stack.Unfolded)
					cursor.Piece.RolledOver = true;
			}
		}

		/// <summary>Displays the next frame.</summary>
		/// <param name="currentTimeInMicroseconds">Time of this frame.</param>
		public void Render(long currentTimeInMicroseconds) {
			float loadingProgress = 1.0f;
			if(loadingGraphics) {
				// load increments in a tight loop
				IPrecisionTimer timer = new PrecisionTimer();
				long start = timer.NowInMicroseconds;
				do {
					if(loadingGraphicsProgress.MoveNext()) {
						loadingProgress = loadingGraphicsProgress.Current;
					} else {
						loadingGraphics = false;
						loadingGraphicsProgress.Dispose();
						loadingGraphicsProgress = null;
						break;
					}
				} while(timer.NowInMicroseconds - start < 100000L);

				// render or continue loading?
				if(loadingGraphics && previousFrameTimeInMicroseconds != 0 && currentTimeInMicroseconds - previousFrameTimeInMicroseconds < (long) 30000)
					return;
			}
			if(previousFrameTimeInMicroseconds != 0 && currentTimeInMicroseconds - previousFrameTimeInMicroseconds < (long) 3000)
				System.Threading.Thread.Sleep(1);	// give up some CPU time to other apps
			previousFrameTimeInMicroseconds = currentTimeInMicroseconds;
			if(graphics.BeginFrame(currentTimeInMicroseconds)) {
				if(loadingGraphics) {
					// render blank screen "Loading graphics..."
					// black background
					graphics.MonochromaticImage.Render(gameDisplayArea, 0xFF000000);

					// text
					graphics.DrawText(
						font,
						0xFFFFFFFF,
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 10.0f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.45f - font.Height * 0.4f,
							20.0f,
							font.Height * 0.4f),
						StringAlignment.Center,
						Resources.LoadingGraphics);

					// progress bar
					graphics.MonochromaticImage.Render(
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 254.0f * 0.5f + 4.0f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.45f + font.Height * 0.8f + 4.0f,
							(254.0f - 8.0f) * loadingProgress,
							7.0f),
						0xFF39DE39);
					progressBarImage.Render(
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 254.0f * 0.5f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.45f + font.Height * 0.8f,
							254.0f,
							15.0f));
					float intervalBetweenHints = (gameDisplayArea.Width * 0.6f - 63.0f * hintIcons.Length) / (hintIcons.Length - 1);
					for(int i = 0; i < hintIcons.Length; ++i) {
						hintIcons[i].Render(
							new RectangleF(
								gameDisplayArea.X + gameDisplayArea.Width * 0.2f + (63.0f + intervalBetweenHints) * i + (i != 0 ? 19.0f : 0.0f),
								gameDisplayArea.Y + gameDisplayArea.Height * 0.65f,
								(i == 0 ? 63.0f : 25.0f),
								63.0f));
						graphics.DrawText(
							font,
							0xFFFFFFFF,
							new RectangleF(
								gameDisplayArea.X + gameDisplayArea.Width * 0.2f + (63.0f + intervalBetweenHints) * i,
								gameDisplayArea.Y + gameDisplayArea.Height * 0.65f + 73.0f,
								63.0f,
								font.Height * 0.4f),
							StringAlignment.Center,
							Resources.ResourceManager.GetString(hints[i], Resources.Culture));
					}

				} 
				else if(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox) {
					// black background
					graphics.MonochromaticImage.Render(gameDisplayArea, 0xFF000000);

					// display title and copyright notice
					title.Render(new RectangleF(
						gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 254.0f * 0.5f,
						gameDisplayArea.Y + gameDisplayArea.Height * 0.7f - 55.0f * 0.5f,
						254.0f, 55.0f));

					graphics.DrawText(font, 0xFFFFFFFF,
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 20.0f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.7f + 40.0f,
							40.0f, 10.0f),
						StringAlignment.Center,
						Resources.Version);
					graphics.DrawText(font, 0xFFFFFFFF,
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 20.0f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.7f + 60.0f,
							40.0f, 10.0f),
						StringAlignment.Center,
						"Copyright© ZunTzu Software 2006-2022");
				} 
				else if(model.CurrentGameBox.CurrentGame.VisibleBoard.Owner != Guid.Empty && model.CurrentGameBox.CurrentGame.VisibleBoard.Owner != model.ThisPlayer.Guid) {
					// black background
					graphics.MonochromaticImage.Render(gameDisplayArea, 0xFF000000);

					// display "hidden" notice
					graphics.DrawText(font, 0xFFFFFFFF,
						new RectangleF(
							gameDisplayArea.X + gameDisplayArea.Width * 0.5f - 20.0f,
							gameDisplayArea.Y + gameDisplayArea.Height * 0.5f - 10.0f,
							40.0f, 10.0f),
						StringAlignment.Center,
						Resources.BoardIsHidden);
				} 
				else {
					renderBackground();
					renderPieces(currentTimeInMicroseconds);
				}

				// render each view element
				foreach(ViewElement element in viewElements)
					if(!loadingGraphics || element != Hand)
					element.Render(graphics, currentTimeInMicroseconds);

				//performanceGraph.Render(graphics, currentTimeInMicroseconds);
				renderRuler();
				renderCursors();

				if(currentDialog != null) {
					Rectangle bounds = currentDialog.Bounds;
					borderImageElements[0].Render(new RectangleF(bounds.Left - 12, bounds.Top - 12, 12, 12));
					borderImageElements[1].Render(new RectangleF(bounds.Left, bounds.Top - 12, bounds.Width, 12));
					borderImageElements[2].Render(new RectangleF(bounds.Right, bounds.Top - 12, 12, 12));
					borderImageElements[3].Render(new RectangleF(bounds.Right, bounds.Top, 12, bounds.Height));
					borderImageElements[4].Render(new RectangleF(bounds.Right, bounds.Bottom, 12, 12));
					borderImageElements[5].Render(new RectangleF(bounds.Left, bounds.Bottom, bounds.Width, 12));
					borderImageElements[6].Render(new RectangleF(bounds.Left - 12, bounds.Bottom, 12, 12));
					borderImageElements[7].Render(new RectangleF(bounds.Left - 12, bounds.Top, 12, bounds.Height));
				}

				graphics.EndFrame();
			}
		}

		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <remarks>
		/// Screen referential is the referential of the mouse cursor and the display pixels.
		/// Model referential is the referential of the map textures.
		/// </remarks>
		public PointF ConvertScreenToModelCoordinates(PointF screenCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new PointF(
				visibleArea.X + (screenCoordinates.X - gameDisplayArea.X) * visibleArea.Width / gameDisplayArea.Width,
				visibleArea.Y + (screenCoordinates.Y - gameDisplayArea.Y) * visibleArea.Height / gameDisplayArea.Height);
		}

		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		public SizeF ConvertScreenToModelCoordinates(SizeF screenCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new SizeF(
				screenCoordinates.Width * visibleArea.Width / gameDisplayArea.Width,
				screenCoordinates.Height * visibleArea.Height / gameDisplayArea.Height);
		}

		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		public RectangleF ConvertScreenToModelCoordinates(RectangleF screenCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new RectangleF(
				visibleArea.X + (screenCoordinates.X - gameDisplayArea.X) * visibleArea.Width / gameDisplayArea.Width,
				visibleArea.Y + (screenCoordinates.Y - gameDisplayArea.Y) * visibleArea.Height / gameDisplayArea.Height,
				screenCoordinates.Width * visibleArea.Width / gameDisplayArea.Width,
				screenCoordinates.Height * visibleArea.Height / gameDisplayArea.Height);
		}

		/// <summary>Converts coordinates from model referential to screen referential.</summary>
		public PointF ConvertModelToScreenCoordinates(PointF modelCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new PointF(
				gameDisplayArea.X + (modelCoordinates.X - visibleArea.X) * gameDisplayArea.Width / visibleArea.Width,
				gameDisplayArea.Y + (modelCoordinates.Y - visibleArea.Y) * gameDisplayArea.Height / visibleArea.Height);
		}

		/// <summary>Converts coordinates from model referential to screen referential.</summary>
		public SizeF ConvertModelToScreenCoordinates(SizeF modelCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new SizeF(
				modelCoordinates.Width * gameDisplayArea.Width / visibleArea.Width,
				modelCoordinates.Height * gameDisplayArea.Height / visibleArea.Height);
		}

		/// <summary>Converts coordinates from model referential to screen referential.</summary>
		public RectangleF ConvertModelToScreenCoordinates(RectangleF modelCoordinates) {
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			return new RectangleF(
				gameDisplayArea.X + (modelCoordinates.X - visibleArea.X) * gameDisplayArea.Width / visibleArea.Width,
				gameDisplayArea.Y + (modelCoordinates.Y - visibleArea.Y) * gameDisplayArea.Height / visibleArea.Height,
				modelCoordinates.Width * gameDisplayArea.Width / visibleArea.Width,
				modelCoordinates.Height * gameDisplayArea.Height / visibleArea.Height);
		}

		/// <summary>Display area in pixels.</summary>
		/// <remarks>The display area is smaller than the screen if black bars are displayed.</remarks>
		public Rectangle GameDisplayAreaInPixels { get { return gameDisplayArea; } }

		/// <summary>Sub-component in charge of the tabs used to change the visible board.</summary>
		public ITabs Tabs { get { return (ITabs) viewElements[(int)ViewElementType.Tabs]; } }
		/// <summary>Sub-component in charge of the display of the player names.</summary>
		public IPlayerView PlayerView { get { return (IPlayerView) viewElements[(int)ViewElementType.PlayerView]; } }
		/// <summary>Sub-component in charge of the display of the content of the selected stack.</summary>
		public IStackInspector StackInspector { get { return (IStackInspector) viewElements[(int) ViewElementType.StackInspector]; } }
		/// <summary>Sub-component in charge of the display of chat, and text notifications.</summary>
		public IPrompter Prompter { get { return (IPrompter) viewElements[(int)ViewElementType.Prompter]; } }
		/// <summary>Sub-component in charge of the display of dice.</summary>
		public IDiceBag DiceBag { get { return (IDiceBag) viewElements[(int)ViewElementType.DiceBag]; } }
		/// <summary>Sub-component in charge of the menu.</summary>
		public IMenu Menu { get { return (IMenu) viewElements[(int)ViewElementType.Menu]; } }
		/// <summary>Sub-component in charge of the hand.</summary>
		public IHand Hand { get { return (IHand) viewElements[(int) ViewElementType.Hand]; } }

		private IImage cachedBackgroundImage = null;
		private IBoard cachedBackgroundBoard = null;
		private RectangleF cachedBackgroundArea = RectangleF.Empty;
		private Side cachedBackgroundSide = Side.Front;

		private void renderBackground() {

			// render background image
			IGame game = model.CurrentGameBox.CurrentGame;
			IBoard visibleBoard = game.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			Side side = (visibleBoard is IMap ? Side.Front : ((ICounterSheet)visibleBoard).Side);
			if(visibleBoard != cachedBackgroundBoard ||
				visibleArea != cachedBackgroundArea ||
				side != cachedBackgroundSide)
			{
				if(visibleBoard is IMap) {
					IMap visibleMap = (IMap) visibleBoard;
					if(visibleMap.BackgroundGraphics != null) {
						cachedBackgroundImage = visibleMap.BackgroundGraphics.ExtractImage(visibleArea, gameDisplayArea);
					} else {
						// black background
						graphics.MonochromaticImage.Render(gameDisplayArea, 0xFF000000);
						cachedBackgroundBoard = null;
						return;
					}
				} else {
					ICounterSheet counterSheet = (ICounterSheet) visibleBoard;
					ITileSet tileSet = (side == Side.Front ? counterSheet.FrontGraphics : counterSheet.BackGraphics);
					cachedBackgroundImage = tileSet.ExtractImage(visibleArea, gameDisplayArea);
				}
				cachedBackgroundBoard = visibleBoard;
				cachedBackgroundArea = visibleArea;
				cachedBackgroundSide = side;
			}
			cachedBackgroundImage.RenderIgnoreMask(gameDisplayArea);

			// in case of a counter sheet, render a gray overlay to mark the location of cut-off sections
			if(game.Mode != Mode.Terrain && visibleBoard is ICounterSheet) {
				ICounterSheet counterSheet = (ICounterSheet) visibleBoard;
				ITileSet tileSet = (side == Side.Front ? counterSheet.FrontGraphics : counterSheet.BackGraphics);
				foreach(ICounterSection counterSection in counterSheet.CounterSections) {
					RectangleF imageLocation = Rectangle.Empty;
					if(counterSection.ContainsCounters) {
						// counters
						imageLocation = (side == Side.Front ? counterSection.FrontImageLocation : counterSection.BackImageLocation);
					} else {
						// cards
						if((side == Side.Front) == counterSection.HasCardFaceOnFront) {
							imageLocation = counterSection.FrontImageLocation;
						}
					}
					if(imageLocation.IntersectsWith(visibleArea)) {
						imageLocation.Intersect(visibleArea);
						IImage counterSectionImage = tileSet.ExtractImage(imageLocation);
						counterSectionImage.RenderSilhouette(ConvertModelToScreenCoordinates(imageLocation), 0.0f, 0xb2000000);
					}
				}
			}
		}

		private void renderPieces(long currentTimeInMicroseconds) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IBoard visibleBoard = game.VisibleBoard;
			RectangleF visibleArea = visibleBoard.VisibleArea;
			float scaling = (float) gameDisplayArea.Width / visibleArea.Width;

			visibleBoard.FillListWithFoldedStacksWithinAreaBackToFront(foldedStackList, visibleArea);
			visibleBoard.FillListWithUnfoldedStacksWithinAreaBackToFront(unfoldedStackList, visibleArea);
			if(game.Mode != Mode.Terrain) {
				// for each layer (bottom layer is for unpunched pieces, next layer is for punched terrains, next layer is for punched cards, top layer is for punched counters)
				for(int layer = 0; layer < 4; ++layer) {
					// render each selection hint
					ISelection selection = model.CurrentSelection;
					if(selection != null) {
						IStack stack = selection.Stack;
						if(visibleBoard == stack.Board &&
							((layer == 0 && stack.AttachedToCounterSection) ||
							(layer == 2 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICard) ||
							(layer == 3 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICounter)) &&
							visibleArea.IntersectsWith(stack.BoundingBox))
						{
							// computes modulation color for blinking
							uint blinkFactor = (uint) (128 * Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0)) + 128);
							uint blinkModulationColor = 0xFF000000 | blinkFactor << 16 | blinkFactor << 8 | blinkFactor;

							// render each selection hint
							foreach(IPiece piece in stack.Pieces) {
								// don't render attached pieces on the opposite side
								ICounterSection counterSection = piece.CounterSection;
								CounterSectionType type = counterSection.Type;
								if(selection.Contains(piece) &&
									(!stack.AttachedToCounterSection ||
									(counterSection.ContainsCounters && ((int) type & ((int) ((ICounterSheet) visibleBoard).Side + 1)) != 0) ||
									(!counterSection.ContainsCounters && counterSection.HasCardFaceOnFront == (((ICounterSheet) visibleBoard).Side == Side.Front))))
								{
									PointF position = piece.Position;
									SizeF size = piece.Size;
									float flipAngleCosinus = piece.FlipAngleCosinus;
									RectangleF localisation = new RectangleF(
										gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling - 2.0f,
										gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling - 2.0f,
										(size.Width * flipAngleCosinus) * scaling + 4.0f,
										size.Height * scaling + 4.0f);
									if (!piece.IsBlock)
										piece.Graphics.RenderSilhouette(localisation, piece.RotationAngle, blinkModulationColor);
									else if (piece.Owner == Guid.Empty)
										piece.Graphics.RenderBlockSilhouette(localisation, piece.BlockThickness * scaling, 0.0f, piece.RotationAngle, blinkModulationColor);
									else
										piece.Graphics.RenderBlockSilhouette(localisation, piece.BlockThickness * scaling, 1.0f, piece.RotationAngle, blinkModulationColor);
								}
							}
						}
					}

					// render roll-over hints
					IPlayer thisPlayer = model.ThisPlayer;
					IBoardCursorLocation cursor = thisPlayer.CursorLocation as IBoardCursorLocation;
					if(cursor != null && cursor.Piece != null && thisPlayer.IsCursorVisible) {
						IStack stack = cursor.Piece.Stack;
						if(((layer == 0 && stack.AttachedToCounterSection) ||
							(layer == 2 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICard) ||
							(layer == 3 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICounter)) &&
							(thisPlayer.StackBeingDragged == null || thisPlayer.StackBeingDragged.GetType() == stack.Pieces[0].GetType()) &&
							(thisPlayer.PieceBeingDragged == null || thisPlayer.PieceBeingDragged.GetType() == stack.Pieces[0].GetType()))
						{
							// computes modulation color for blinking
							uint blinkFactor = (uint) (128 * Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0)) + 128);
							uint blinkModulationColor = 0xFF000000 | blinkFactor << 16 | blinkFactor << 8 | blinkFactor;

							// render each roll-over hint
							bool pieceIsEligible = false;
							foreach(IPiece piece in stack.Pieces) {
								if(piece == cursor.Piece)
									pieceIsEligible = true;
								// don't render hint twice for selected pieces
								if(pieceIsEligible && (selection == null || !selection.Contains(piece))) {
									PointF position = piece.Position;
									SizeF size = piece.Size;
									float flipAngleCosinus = piece.FlipAngleCosinus;
									RectangleF localisation = new RectangleF(
										gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling - 2.0f,
										gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling - 2.0f,
										(size.Width * flipAngleCosinus) * scaling + 4.0f,
										size.Height * scaling + 4.0f);
									
									if (!piece.IsBlock)
										piece.Graphics.RenderSilhouette(localisation, piece.RotationAngle, blinkModulationColor);
									else if (piece.Owner == Guid.Empty)
										piece.Graphics.RenderBlockSilhouette(localisation, piece.BlockThickness * scaling, 0.0f, piece.RotationAngle, blinkModulationColor);
									else
										piece.Graphics.RenderBlockSilhouette(localisation, piece.BlockThickness * scaling, 1.0f, piece.RotationAngle, blinkModulationColor);
								}
							}
						}
					}

					// three passes: folded stacks pass, 2 x unfolded stacks pass (last pass is 50% transparent)
					for(int pass = 0; pass < 3; ++pass) {
						List<IStack> stackList = (pass == 0 ? foldedStackList : unfoldedStackList);
						foreach(IStack stack in stackList) {
							if(((layer == 0 && stack.AttachedToCounterSection) ||
								(layer == 1 && stack.Pieces[0] is ITerrain) ||
								(layer == 2 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICard) ||
								(layer == 3 && !stack.AttachedToCounterSection && stack.Pieces[0] is ICounter)))
							{
								foreach(IPiece piece in stack.Pieces) {
									// don't render attached pieces on the opposite side
									ICounterSection counterSection = piece.CounterSection;
									CounterSectionType type = counterSection.Type;
									if(!stack.AttachedToCounterSection ||
										(counterSection.ContainsCounters && ((int) type & ((int) ((ICounterSheet) visibleBoard).Side + 1)) != 0) ||
										(!counterSection.ContainsCounters && counterSection.HasCardFaceOnFront == (((ICounterSheet) visibleBoard).Side == Side.Front)))
									{
										PointF position = piece.Position;
										SizeF size = piece.Size;
										float flipAngleCosinus = piece.FlipAngleCosinus;
										RectangleF localisation = new RectangleF(
											gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling,
											gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling,
											(size.Width * flipAngleCosinus) * scaling,
											size.Height * scaling);

										// render shadow
										if(pass < 2 && !stack.AttachedToCounterSection && counterSection.ShadowLength > 0.0f) {																				
											float shadowLength = counterSection.ShadowLength * scaling;
											RectangleF shadowLocalisation = localisation;
											shadowLocalisation.Offset(shadowLength, shadowLength);											
											if(scaling > 0.3f) {
												// soft shadows
												if (!piece.IsBlock)
												{
													shadowLocalisation.Inflate(2 * scaling, 2 * scaling);
													piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x08000000);
													shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
													piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x10000000);
													shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
													piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x18000000);
													shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
													piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x10000000);
												}
											} else {
												// hard shadows
												if (!piece.IsBlock) 
													piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x40000000);
											}
										}

										// render piece
										if (!piece.IsBlock)
										{
											if (pass < 2)
											{
												piece.Graphics.Render(localisation, piece.RotationAngle, 0xffffffff);
											}
											else if (piece is ICounter)
											{
												piece.Graphics.Render(localisation, piece.RotationAngle, 0x7fffffff);
											}
											else if (piece.RolledOver)
											{
												piece.Graphics.Render(localisation, piece.RotationAngle, 0x3fffffff);
											}
										}
                                        else
                                        {
											RectangleF localisationFramedSticker = new RectangleF(
											gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling + size.Width * scaling * piece.BlockStickerReduction/2,
											gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling + size.Height * scaling * piece.BlockStickerReduction/2,
											(size.Width * flipAngleCosinus) * scaling * (1.0f - piece.BlockStickerReduction),
											size.Height * scaling * (1.0f - piece.BlockStickerReduction));											

											if (pass < 2)
											{
												renderBlockByOwnership(localisation, localisationFramedSticker, scaling, piece, 0xff000000 | piece.BlockColor);												
											}
											else if (piece is ICounter)
											{
												renderBlockByOwnership(localisation, localisationFramedSticker, scaling, piece, 0x7f000000 | piece.BlockColor);
											}
											else if (piece.RolledOver)
											{
												renderBlockByOwnership(localisation, localisationFramedSticker, scaling, piece, 0x3f000000 | piece.BlockColor);
											}
										}
									}
								}
							}
						}
					}
				}

			} 
			else {	// game.Mode == Mode.Terrain
				// render roll-over hints
				IPlayer thisPlayer = model.ThisPlayer;
				IBoardCursorLocation cursor = thisPlayer.CursorLocation as IBoardCursorLocation;
				if(cursor != null && cursor.Piece != null &&
					thisPlayer.IsCursorVisible && thisPlayer.StackBeingDragged == null && thisPlayer.PieceBeingDragged == null &&
					cursor.Piece.CounterSection.CounterSheet.Properties.Type == CounterSheetType.Terrain)
				{
					// computes modulation color for blinking
					uint blinkFactor = (uint) (128 * Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0)) + 128);
					uint blinkModulationColor = 0xFF000000 | blinkFactor << 16 | blinkFactor << 8 | blinkFactor;

					// render roll-over hint
					IPiece piece = cursor.Piece;
					PointF position = piece.Position;
					SizeF size = piece.Size;
					float flipAngleCosinus = piece.FlipAngleCosinus;
					RectangleF localisation = new RectangleF(
						gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling - 2.0f,
						gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling - 2.0f,
						(size.Width * flipAngleCosinus) * scaling + 4.0f,
						size.Height * scaling + 4.0f);
					piece.Graphics.RenderSilhouette(localisation, piece.RotationAngle, blinkModulationColor);
				}

				// two passes: folded stacks pass, unfolded stacks pass
				for(int pass = 0; pass < 2; ++pass) {
					List<IStack> stackList = (pass == 0 ? foldedStackList : unfoldedStackList);
					foreach(IStack stack in stackList) {
						IPiece piece = stack.Pieces[0];
						if(piece.CounterSection.CounterSheet.Properties.Type == CounterSheetType.Terrain) {
							// don't render attached pieces except the one pointed at
							CounterSectionType type = piece.CounterSection.Type;
							if(!stack.AttachedToCounterSection || (cursor != null && cursor.Piece == piece)) {
								PointF position = piece.Position;
								SizeF size = piece.Size;
								float flipAngleCosinus = piece.FlipAngleCosinus;
								RectangleF localisation = new RectangleF(
									gameDisplayArea.X + (position.X - visibleArea.X - (size.Width * flipAngleCosinus) * 0.5f) * scaling,
									gameDisplayArea.Y + (position.Y - visibleArea.Y - size.Height * 0.5f) * scaling,
									(size.Width * flipAngleCosinus) * scaling,
									size.Height * scaling);

								// render shadow
								if(!stack.AttachedToCounterSection && piece.CounterSection.ShadowLength > 0.0f) {
									float shadowLength = piece.CounterSection.ShadowLength * scaling;
									RectangleF shadowLocalisation = localisation;
									shadowLocalisation.Offset(shadowLength, shadowLength);
									if(scaling > 0.3f) {
										// soft shadows
										shadowLocalisation.Inflate(2 * scaling, 2 * scaling);
										piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x08000000);
										shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
										piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x10000000);
										shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
										piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x18000000);
										shadowLocalisation.Inflate(-2 * scaling, -2 * scaling);
										piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x10000000);
									} else {
										// hard shadows
										piece.Graphics.RenderSilhouette(shadowLocalisation, piece.RotationAngle, 0x40000000);
									}
								}

								// render piece
								piece.Graphics.Render(localisation, piece.RotationAngle, 0xffffffff);
							}
						}
					}
				}
			}
		}

		private void renderBlockByOwnership(RectangleF localisation, RectangleF localisationFramedSticker, float scaling, IPiece piece, uint opaqueColor) {
			float flipProgress = 1.0f;

			if (piece.Owner == Guid.Empty)
			{
				flipProgress = 0.0f;
				((Piece)piece).Side = Side.Front;
				piece.Graphics.RenderBlock(localisation, piece.BlockThickness * scaling, localisationFramedSticker, flipProgress, piece.RotationAngle, opaqueColor, 1.0f, true);
			}
			else if (piece.Owner != model.ThisPlayer.Guid)
			{
				if (piece.CounterSection.IsSingleSided)
					piece.Graphics.RenderBlockBlank(localisation, piece.BlockThickness * scaling, flipProgress, piece.RotationAngle, opaqueColor, 1.0f, true);
				else
				{
					((Piece)piece).Side = Side.Back;
					piece.Graphics.RenderBlock(localisation, piece.BlockThickness * scaling, localisationFramedSticker, flipProgress, piece.RotationAngle, opaqueColor, 1.0f, true);
				}
			}
			else
			{
				((Piece)piece).Side = Side.Front;  
				piece.Graphics.RenderBlock(localisation, piece.BlockThickness * scaling, localisationFramedSticker, flipProgress, piece.RotationAngle, opaqueColor, 1.0f, true);
			}
		}


		private void renderRuler() {
			if(model.IsMeasuring) {
				PointF start = ConvertModelToScreenCoordinates(model.RulerStartPosition);
				PointF end = ConvertModelToScreenCoordinates(model.RulerEndPosition);
				if(start == end) {
					// render starting extremity
					rulerExtremityImage.Render(
						new RectangleF(start.X - (11.0f * 0.5f), start.Y - (11.0f * 0.5f), 11.0f, 11.0f),
						0.25f * (float)Math.PI);
					// render text
					graphics.DrawText(font, 0xFFFFFFFF, new RectangleF(end.X + 12.0f, end.Y, 20.0f, 10.0f), StringAlignment.Near, "0 mm");
				} else {
					float angle = (float)Math.Atan2(start.Y - end.Y, end.X - start.X);

					// render starting extremity
					rulerExtremityImage.Render(
						new RectangleF(start.X - (11.0f * 0.5f), start.Y - (11.0f * 0.5f), 11.0f, 11.0f),
						angle);

					// render ending extremity
					rulerExtremityImage.Render(
						new RectangleF(end.X - (11.0f * 0.5f), end.Y - (11.0f * 0.5f), 11.0f, 11.0f),
						angle + (float)Math.PI);

					// render line
					float length = (float)Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
					if(length > 1.0f) {
						rulerLineImage.Render(
							new RectangleF(
								(start.X + end.X) * 0.5f - (length - 8.0f) * 0.5f,
								(start.Y + end.Y) * 0.5f - (5.0f * 0.5f),
								length - 8.0f,
								5.0f),
							angle);
					}

					// render text
					SizeF modelLength = ConvertScreenToModelCoordinates(new SizeF(length, 0.0f));
					graphics.DrawText(font, 0xFFFFFFFF, new RectangleF(end.X + 12.0f, end.Y, 20.0f, 10.0f), StringAlignment.Near,
						((int)(modelLength.Width * (25.4f / 600.0f))).ToString() + " mm");
				}
			}
		}

		private void renderCursors() {
			IPlayer thisPlayer = model.ThisPlayer;
			Guid visibleBoardOwner = model.CurrentGameBox.CurrentGame.VisibleBoard.Owner;
			if(visibleBoardOwner == Guid.Empty || visibleBoardOwner == thisPlayer.Guid) {
				IPlayer[] players = model.Players;
				for(int p = 0; p < players.Length; ++p) {
					IPlayer player = players[p];
					if(player != thisPlayer && player.IsCursorVisible) {
						PointF cursorPosition = player.CursorLocation.ScreenPosition;

						if(player.StackBeingDragged != null || player.PieceBeingDragged != null) {
							if(player.StackBeingDragged != null) {
								renderDragAndDropStackGhost(player.CursorLocation, player.DragAndDropAnchor, player.StackBeingDragged);
							} else {	// player.PieceBeingDragged != null
								renderDragAndDropPieceGhost(player.CursorLocation, player.DragAndDropAnchor, player.PieceBeingDragged);
							}
							fingerCursorImage.Render(
								new RectangleF((float) Math.Floor(cursorPosition.X) - 11.0f, (float) Math.Floor(cursorPosition.Y), 17.0f, 22.0f),
								player.Color);
						} else {
							arrowCursorImage.Render(
								new RectangleF((float) Math.Floor(cursorPosition.X) - 11.0f, (float) Math.Floor(cursorPosition.Y), 12.0f, 21.0f),
								player.Color);
						}
					}
				}
			}

			if(thisPlayer.StackBeingDragged != null) {
				renderDragAndDropStackGhost(thisPlayer.CursorLocation, thisPlayer.DragAndDropAnchor, thisPlayer.StackBeingDragged);
			} else if(thisPlayer.PieceBeingDragged != null) {
				renderDragAndDropPieceGhost(thisPlayer.CursorLocation, thisPlayer.DragAndDropAnchor, thisPlayer.PieceBeingDragged);
			}
		}

		private void renderDragAndDropStackGhost(ICursorLocation cursorLocation, PointF anchorModelPosition, IPiece stackBottom) {
			PointF stackBottomPosition = stackBottom.Position;
			Point cursorScreenPosition = cursorLocation.ScreenPosition;

			float scaling;
			if(cursorLocation is IStackInspectorCursorLocation) {
				scaling = StackInspector.PieceScaling;
			} else if(cursorLocation is IHandCursorLocation) {
				scaling = Hand.PieceScaling;
			} else {
				IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
				RectangleF visibleArea = visibleBoard.VisibleArea;
				scaling = (float) gameDisplayArea.Width / visibleArea.Width;
			}

			bool pieceEligible = false;
			foreach(IPiece piece in stackBottom.Stack.Pieces) {
				if(piece == stackBottom)
					pieceEligible = true;
				if(pieceEligible) {
					PointF position = piece.Position;
					SizeF size = piece.Size;
					RectangleF localisation = new RectangleF(
						cursorScreenPosition.X + (position.X - stackBottomPosition.X - anchorModelPosition.X - size.Width * 0.5f) * scaling,
						cursorScreenPosition.Y + (position.Y - stackBottomPosition.Y - anchorModelPosition.Y - size.Height * 0.5f) * scaling,
						size.Width * scaling,
						size.Height * scaling);

					if (piece.IsBlock && piece.CounterSection.IsSingleSided && piece.Owner != Guid.Empty && piece.Owner != model.ThisPlayer.Guid)
						piece.Graphics.RenderBlockBlank(localisation, 0.0f, 0.0f, piece.RotationAngle, 0x7f000000 | piece.BlockColor, 1.0f, true);
					else
						piece.Graphics.Render(localisation, piece.RotationAngle, 0x7FFFFFFF);
				}
			}
		}

		private void renderDragAndDropPieceGhost(ICursorLocation cursorLocation, PointF anchorModelPosition, IPiece piece) {
			Point cursorScreenPosition = cursorLocation.ScreenPosition;
			float scaling;
			if(cursorLocation is IStackInspectorCursorLocation) {
				scaling = StackInspector.PieceScaling;
			} else if(cursorLocation is IHandCursorLocation) {
				scaling = Hand.PieceScaling;
			} else {
				IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
				RectangleF visibleArea = visibleBoard.VisibleArea;
				scaling = (float) gameDisplayArea.Width / visibleArea.Width;
			}
			SizeF size = piece.Size;
			RectangleF localisation = new RectangleF(
				cursorScreenPosition.X + (-anchorModelPosition.X - size.Width * 0.5f) * scaling,
				cursorScreenPosition.Y + (-anchorModelPosition.Y - size.Height * 0.5f) * scaling,
				size.Width * scaling,
				size.Height * scaling);
			
			if (piece.IsBlock && piece.CounterSection.IsSingleSided && piece.Owner != Guid.Empty && piece.Owner != model.ThisPlayer.Guid)
				piece.Graphics.RenderBlockBlank(localisation, 0.0f, 0.0f, piece.RotationAngle, 0x7f000000 | piece.BlockColor, 1.0f, true);
			else
				piece.Graphics.Render(localisation, piece.RotationAngle, 0x7FFFFFFF);
		}

		private void onGameDisplayAreaResized() {
			// stop all animations, because some of them rely on screen coordinates
			model.AnimationManager.EndAllAnimations();

			// first convert all screen coordinates to model coordinates...
			IPlayer[] players = model.Players;
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				if(player != model.ThisPlayer)
					player.CursorLocation.ModelPosition = ConvertScreenToModelCoordinates(player.CursorLocation.ScreenPosition);
			}

			// update gameDisplayArea
			gameDisplayArea = graphics.GameDisplayAreaInPixels;

			//... and then back to screen coordinates
			for(int p = 0; p < players.Length; ++p) {
				IPlayer player = players[p];
				if(player != model.ThisPlayer)
					player.CursorLocation.ScreenPosition = Point.Truncate(ConvertModelToScreenCoordinates(player.CursorLocation.ModelPosition));
			}

			// invalidate cached background image
			cachedBackgroundBoard = null;

			// notify panels
			foreach(ViewElement element in viewElements)
				element.OnGameDisplayAreaResized();

			// notify audio manager for 3D positioning
			IAudioManager audioManager = model.AudioManager;
			audioManager.SetGameDisplayArea(gameDisplayArea);
			RectangleF stackInspectorArea = StackInspectorArea;
			audioManager.SetAudioTrackOrigin(AudioTrack.Shuffle,
				new PointF(stackInspectorArea.X + 0.5f * stackInspectorArea.Width,
					stackInspectorArea.Y + 0.5f * stackInspectorArea.Height));
		}

		private void onDialogClosed(object sender, EventArgs e) {
			currentDialog.Closed -= new EventHandler(onDialogClosed);
			currentDialog = null;
		}

		private IEnumerable<float> loadGraphicsIncrements() {
			IGameBox gameBox = model.CurrentGameBox;
			IArchive archive = new Archive(gameBox.Reference.FileName);
			IGame game = gameBox.CurrentGame;

			int currentProgress = 0;
			yield return 0.0f;

			// determine total graphics data size
			int totalGraphicsDataSize = 0;
			foreach(IBoard board in game.Boards) {
				if(board is IMap) {
					// load the image for this map
					IMap map = (IMap) board;
					if(map.Properties != null) {
						IFile imageFile = archive.GetFile(map.Properties.ImageFileName);
						totalGraphicsDataSize += imageFile.SizeInBytes;
					}
				} else {
					// load the whole images for this counter sheet (both sides)
					ICounterSheet counterSheet = (ICounterSheet) board;
					IFile frontImageFile = archive.GetFile(counterSheet.Properties.FrontImageFileName);
					totalGraphicsDataSize += frontImageFile.SizeInBytes;
					if(counterSheet.Properties.FrontMaskFileName != null) {
						IFile frontMaskFile = archive.GetFile(counterSheet.Properties.FrontMaskFileName);
						totalGraphicsDataSize += frontMaskFile.SizeInBytes;
					}
					if(counterSheet.Properties.BackImageFileName != null) {
						IFile backImageFile = archive.GetFile(counterSheet.Properties.BackImageFileName);
						totalGraphicsDataSize += backImageFile.SizeInBytes;
						if(counterSheet.Properties.BackMaskFileName != null) {
							IFile backMaskFile = archive.GetFile(counterSheet.Properties.BackMaskFileName);
							totalGraphicsDataSize += backMaskFile.SizeInBytes;
						}
					}
				}
			}

			// load map graphics
			foreach(IBoard board in game.Boards) {
				RectangleF visibleArea = board.VisibleArea;

				if(board is IMap) {
					// load the image for this map
					IMap map = (IMap) board;
					if(map.Properties != null) {
						IFile imageFile = archive.GetFile(map.Properties.ImageFileName);
						int nextProgressIncrement = imageFile.SizeInBytes;
						map.BackgroundGraphics = graphics.LoadTileSet(imageFile, (DetailLevelType) map.Properties.ImageResolution);
						foreach(float progress in map.BackgroundGraphics.LoadIncrementally())
							yield return (currentProgress + progress * nextProgressIncrement) / (float) totalGraphicsDataSize;
						currentProgress += nextProgressIncrement;
					}

					// by default, display the whole area at startup
					if(visibleArea.IsEmpty)
						visibleArea = map.BackgroundGraphics != null ?
							new RectangleF(0.0f, 0.0f, map.BackgroundGraphics.Size.Width, 0.0f) :
							new RectangleF(0.0f, 0.0f, 1600.0f, 0.0f);
				} else {
					// load the whole images for this counter sheet (both sides)
					ICounterSheet counterSheet = (ICounterSheet) board;
					IFile frontImageFile = archive.GetFile(counterSheet.Properties.FrontImageFileName);
					int nextProgressIncrement = frontImageFile.SizeInBytes;
					if(counterSheet.Properties.FrontMaskFileName != null) {
						IFile frontMaskFile = archive.GetFile(counterSheet.Properties.FrontMaskFileName);
						nextProgressIncrement += frontMaskFile.SizeInBytes;
						counterSheet.FrontGraphics = graphics.LoadTileSet(frontImageFile, frontMaskFile, (DetailLevelType)counterSheet.Properties.FrontImageResolution);
					} else {
						counterSheet.FrontGraphics = graphics.LoadTileSet(frontImageFile, (DetailLevelType)counterSheet.Properties.FrontImageResolution);
					}
					foreach(float progress in counterSheet.FrontGraphics.LoadIncrementally())
						yield return (currentProgress + progress * nextProgressIncrement) / (float)totalGraphicsDataSize;
					currentProgress += nextProgressIncrement;

					if(counterSheet.Properties.BackImageFileName != null) {
						IFile backImageFile = archive.GetFile(counterSheet.Properties.BackImageFileName);
						nextProgressIncrement = backImageFile.SizeInBytes;
						if(counterSheet.Properties.BackMaskFileName != null) {
							IFile backMaskFile = archive.GetFile(counterSheet.Properties.BackMaskFileName);
							nextProgressIncrement += backMaskFile.SizeInBytes;
							counterSheet.BackGraphics = graphics.LoadTileSet(backImageFile, backMaskFile, (DetailLevelType)counterSheet.Properties.BackImageResolution);
						} else {
							counterSheet.BackGraphics = graphics.LoadTileSet(backImageFile, (DetailLevelType)counterSheet.Properties.BackImageResolution);
						}
						foreach(float progress in counterSheet.BackGraphics.LoadIncrementally())
							yield return (currentProgress + progress * nextProgressIncrement) / (float)totalGraphicsDataSize;
						currentProgress += nextProgressIncrement;
					} else {
						counterSheet.BackGraphics = null;
					}

					// by default, display the whole area at startup
					if(visibleArea.IsEmpty)
						visibleArea = new RectangleF(0.0f, 0.0f, counterSheet.FrontGraphics.Size.Width, 0.0f);

					// cut a piece of the whole image off for each piece (both sides)
					foreach(ICounterSection counterSection in counterSheet.CounterSections) {
						RectangleF counterSectionFrontImageLocation = counterSection.FrontImageLocation;
						RectangleF counterSectionBackImageLocation = counterSection.BackImageLocation;
						SizeF pieceFrontSize = counterSection.PieceFrontSize;
						SizeF pieceBackSize = counterSection.PieceBackSize;
						for(int i = 0; i < counterSection.Pieces.Length; i += counterSection.Supply) {
							IPiece piece = counterSection.Pieces[i];
							if(piece is ICard) {
								RectangleF pieceLocation = new RectangleF(
									counterSectionFrontImageLocation.X + piece.Column * pieceFrontSize.Width,
									counterSectionFrontImageLocation.Y + piece.Row * pieceFrontSize.Height,
									pieceFrontSize.Width,
									pieceFrontSize.Height);
								IImage frontGraphics = (counterSection.HasCardFaceOnFront ? counterSheet.FrontGraphics : counterSheet.BackGraphics).ExtractImage(pieceLocation);
								for(int copy = 0; copy < counterSection.Supply; ++copy)
									counterSection.Pieces[i + copy].FrontGraphics = frontGraphics;
								if(!counterSection.IsSingleSided) {
									IImage backGraphics = (counterSection.HasCardBackOnFront ? counterSheet.FrontGraphics : counterSheet.BackGraphics).ExtractImage(counterSectionBackImageLocation);
									for(int copy = 0; copy < counterSection.Supply; ++copy)
										counterSection.Pieces[i + copy].BackGraphics = backGraphics;
								}
							} else {
								if(((int) counterSection.Type & (int) CounterSectionType.FrontSideOnly) != 0) {
									RectangleF pieceLocation = new RectangleF(
										counterSectionFrontImageLocation.X + piece.Column * pieceFrontSize.Width,
										counterSectionFrontImageLocation.Y + piece.Row * pieceFrontSize.Height,
										pieceFrontSize.Width,
										pieceFrontSize.Height);
									IImage image = counterSheet.FrontGraphics.ExtractImage(pieceLocation);
									for(int copy = 0; copy < counterSection.Supply; ++copy)
										counterSection.Pieces[i + copy].FrontGraphics = image;
								}
								if(((int) counterSection.Type & (int) CounterSectionType.BackSideOnly) != 0) {
									RectangleF pieceLocation = new RectangleF(
										counterSectionBackImageLocation.Right - (piece.Column + 1) * pieceBackSize.Width,
										counterSectionBackImageLocation.Y + piece.Row * pieceBackSize.Height,
										pieceBackSize.Width,
										pieceBackSize.Height);
									IImage image = counterSheet.BackGraphics.ExtractImage(pieceLocation);
									for(int copy = 0; copy < counterSection.Supply; ++copy)
										counterSection.Pieces[i + copy].BackGraphics = image;
								}
							}
						}
					}
				}

				// adjust visible area
				visibleArea.Height = visibleArea.Width * gameDisplayArea.Height / gameDisplayArea.Width;
				board.VisibleArea = visibleArea;
			}
		}

		/// <summary>Current mouse cursor location, providing it is out of the main frame boundaries.</summary>
		private class OutOfFrameCursorLocation : CursorLocation, IOutOfFrameCursorLocation {}

		/// <summary>Current mouse cursor location, providing it is above the visible board.</summary>
		private class BoardCursorLocation : CursorLocation, IBoardCursorLocation {
			/// <summary>The top-most piece at a mouse cursor position on the visible board.</summary>
			public IPiece Piece { get { return piece; } set { piece = value; } }
			private IPiece piece;
		}

		/// <summary>Mouse cursor used when scrolling.</summary>
		public Cursor HandCursor { get { return handCursor; } }
		private readonly Cursor handCursor;
		/// <summary>Mouse cursor used when scrolling.</summary>
		public Cursor FistCursor { get { return fistCursor; } }
		private readonly Cursor fistCursor;
		/// <summary>Mouse cursor used when zooming.</summary>
		public Cursor ZoomCursor { get { return zoomCursor; } }
		private readonly Cursor zoomCursor;
		/// <summary>Mouse cursor used when pointing.</summary>
		public Cursor FingerCursor { get { return fingerCursor; } }
		private readonly Cursor fingerCursor;
		/// <summary>Mouse cursor used when dragging.</summary>
		public Cursor FingerAddCursor { get { return fingerAddCursor; } }
		private readonly Cursor fingerAddCursor;
		/// <summary>Mouse cursor used when dragging.</summary>
		public Cursor FingerRemoveCursor { get { return fingerRemoveCursor; } }
		private readonly Cursor fingerRemoveCursor;
		/// <summary>Mouse cursor used when hovering above dice.</summary>
		public Cursor FingerDoubleCursor { get { return fingerDoubleCursor; } }
		private readonly Cursor fingerDoubleCursor;

		private Form mainForm;
		private IGraphics graphics;
		private IModel model;
		private PerformanceGraph performanceGraph = new PerformanceGraph();
		private IImage arrowCursorImage = null;
		private IImage fingerCursorImage = null;
		private IImage rulerExtremityImage = null;
		private IImage rulerLineImage = null;
		private IImage title = null;
		private IImage[] borderImageElements = null;
		private IImage progressBarImage = null;
		private IImage[] hintIcons = null;
		private string[] hints = new string[4] { "HintScroll", "HintZoom", "HintRotate", "HintFlip" };
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
		private Form currentDialog = null;
		private Rectangle gameDisplayArea;
		private ViewElement[] viewElements;
		private bool loadingGraphics = false;
		private IEnumerator<float> loadingGraphicsProgress = null;
		private long previousFrameTimeInMicroseconds = 0;
		private List<IStack> foldedStackList = new List<IStack>();
		private List<IStack> unfoldedStackList = new List<IStack>();
	}
}
