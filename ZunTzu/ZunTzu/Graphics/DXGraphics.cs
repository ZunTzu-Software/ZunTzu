// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.FileSystem;

namespace ZunTzu.Graphics
{

    /// <summary>Direct3D implementation of IGraphics.</summary>
    public sealed class DXGraphics : IGraphics {

		/// <summary>An eligible fullscreen mode.</summary>
		private sealed class FullscreenMode : IFullscreenMode {
			public FullscreenMode(D3DDisplayMode displayMode) {
				DisplayMode = displayMode;
			}
			/// <summary>An human readable description.</summary>
			public override string ToString() { return string.Format("{0} x {1}, {2} hertz", DisplayMode.Width, DisplayMode.Height, DisplayMode.RefreshRate); }
			public readonly D3DDisplayMode DisplayMode;
		}

		/// <summary>Game aspect ratio.</summary>
		/// <remarks>If the screen physical aspect ratio is different than the game aspect ratio, black bands will appear.</remarks>
		public AspectRatioType GameAspectRatio {
			get { return gameAspectRatio; }
			set {
				if(gameAspectRatio != value) {
					gameAspectRatio = value;
					updateGameDisplayArea();
				}
			}
		}
		private AspectRatioType gameAspectRatio;

		/// <summary>Display area.</summary>
		/// <remarks>The display area is smaller than the screen if black bars are displayed.</remarks>
		public Rectangle GameDisplayAreaInPixels { get { return gameDisplayAreaInPixels; } }
		private Rectangle gameDisplayAreaInPixels;

		/// <summary>Occurs when a the game display area has changed.</summary>
		/// <remarks>That could be caused by the window resizing or if the user has changed the game aspect ratio.</remarks>
		public event GameDisplayAreaResizedHandler GameDisplayAreaResized;

		void updatePresentParameters()
        {
			bool success = false;

			if (!fullscreen)
			{
				// try as windowed

				backBufferWidth = deviceWindow.ClientSize.Width;
				backBufferHeight = deviceWindow.ClientSize.Height;

				success = D3D.UpdatePresentParameters(
					fullscreen: fullscreen,
					width: backBufferWidth,
					height: backBufferHeight,
					refreshRate: 0,
					displayFormat: windowedDisplayMode.Format,
					waitForVerticalBlank: properties.WaitForVerticalBlank);
			}

			if (fullscreen || !success)
			{
				// try as fullscreen

				fullscreen = true;

				D3DDisplayMode displayMode = windowedDisplayMode;
				if (properties.PreferredFullscreenMode > 0)
				{
					IFullscreenMode[] allDisplayModes = EligibleFullscreenModes;
					if (properties.PreferredFullscreenMode < allDisplayModes.Length + 1)
						displayMode = ((FullscreenMode)allDisplayModes[properties.PreferredFullscreenMode - 1]).DisplayMode;
				}

				backBufferWidth = displayMode.Width;
				backBufferHeight = displayMode.Height;

				success = D3D.UpdatePresentParameters(
					fullscreen: fullscreen,
					width: backBufferWidth,
					height: backBufferHeight,
					refreshRate: displayMode.RefreshRate,
					displayFormat: displayMode.Format,
					waitForVerticalBlank: properties.WaitForVerticalBlank);

				if (!success) throw new ApplicationException("No compliant display adapter found (caps).");
			}
		}

		private void updateGameDisplayArea() {
			int width = backBufferWidth;
			int height = backBufferHeight;

			if(gameAspectRatio == AspectRatioType.FourToThree) {
				if(width * 3 < height * 4) {
					gameDisplayAreaInPixels = new Rectangle(0, (height - (width * 3) / 4) / 2, width, (width * 3) / 4);
				} else {
					gameDisplayAreaInPixels = new Rectangle((width - (height * 4) / 3) / 2, 0, (height * 4) / 3, height);
				}
			} else {	// AspectRatioType.SixteenToTen:
				if(width * 10 < height * 16) {
					gameDisplayAreaInPixels = new Rectangle(0, (height - (width * 10) / 16) / 2, width, (width * 10) / 16);
				} else {
					gameDisplayAreaInPixels = new Rectangle((width - (height * 16) / 10) / 2, 0, (height * 16) / 10, height);
				}
			}

			// notify delegates registered to the GameDisplayAreaResized event
			if(GameDisplayAreaResized != null)
				GameDisplayAreaResized();
		}

		private void onResize(object sender, EventArgs e) {
			if(!fullscreen) {
				backBufferWidth = deviceWindow.ClientSize.Width;
				backBufferHeight = deviceWindow.ClientSize.Height;
				updatePresentParameters();
				updateGameDisplayArea();

				// don't reset if the window is minimized
				if(deviceWindow.WindowState != FormWindowState.Minimized) {
					// check for lost device
					if (D3D.CheckCooperativeLevel() != D3DCooperativeLevel.DeviceLost)
					{
						D3D.ResetDevice();
					}
				}
			}
		}

		/// <summary>Constructor.</summary>
		/// <param name="deviceWindow">Main form of the application.</param>
		public DXGraphics(Form deviceWindow, GraphicsProperties graphicsProperties, AspectRatioType gameAspectRatio) {
			this.deviceWindow = deviceWindow;
			deviceWindowLocation = new Rectangle(deviceWindow.Location, deviceWindow.ClientSize);
			deviceWindowState = deviceWindow.WindowState;

			windowedDisplayMode = D3D.GetCurrentDisplayMode();

			properties = graphicsProperties;
			this.gameAspectRatio = gameAspectRatio;

			// try as windowed

			fullscreen = false;
			backBufferWidth = deviceWindow.ClientSize.Width;
			backBufferHeight = deviceWindow.ClientSize.Height;

			bool success = D3D.CreateDevice(
				hMainWnd: deviceWindow.Handle,
				fullscreen: fullscreen,
				width: backBufferWidth,
				height: backBufferHeight,
				refreshRate: 0,
				displayFormat: windowedDisplayMode.Format,
				waitForVerticalBlank: properties.WaitForVerticalBlank);

			if (!success)
            {
				// try again as fullscreen

				fullscreen = true;

				D3DDisplayMode displayMode = windowedDisplayMode;
				if (properties.PreferredFullscreenMode > 0)
				{
					IFullscreenMode[] allDisplayModes = EligibleFullscreenModes;
					if (properties.PreferredFullscreenMode < allDisplayModes.Length + 1)
						displayMode = ((FullscreenMode)allDisplayModes[properties.PreferredFullscreenMode - 1]).DisplayMode;
				}

				backBufferWidth = displayMode.Width;
				backBufferHeight = displayMode.Height;

				success = D3D.CreateDevice(
					hMainWnd: deviceWindow.Handle,
					fullscreen: fullscreen,
					width: backBufferWidth,
					height: backBufferHeight,
					refreshRate: displayMode.RefreshRate,
					displayFormat: displayMode.Format,
					waitForVerticalBlank: properties.WaitForVerticalBlank);

				if (!success) throw new ApplicationException("No compliant display adapter found.");
			}

			updateGameDisplayArea();

			deviceWindow.Resize += new EventHandler(onResize);

			monochromaticImage = new DXMonochromaticImage();
		}

		/// <summary>User preferences for the display.</summary>
		/// <remarks>Setting the properties will deallocate all resources.</remarks>
		public GraphicsProperties Properties {
			get {
				return properties;
			}
			set {
				bool resetRequired =
					(properties.WaitForVerticalBlank != value.WaitForVerticalBlank ||
					(fullscreen && properties.PreferredFullscreenMode != value.PreferredFullscreenMode));

				properties = value;

				if(resetRequired) {
					updatePresentParameters();
					updateGameDisplayArea();

					// check for lost device
					if (D3D.CheckCooperativeLevel() != D3DCooperativeLevel.DeviceLost)
                    {
						deviceWindow.Resize -= new EventHandler(onResize);
						D3D.ResetDevice();
						deviceWindow.Resize += new EventHandler(onResize);
					}
				}
			}
		}
		private GraphicsProperties properties;

		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="detailLevel">Resolution at which the image was scanned.</param>
		/// <returns>A tile set.</returns>
		public ITileSet LoadTileSet(IFile imageFile, DetailLevelType detailLevel) {
			DXTileSet newTileSet = new DXTileSet(imageFile, detailLevel);
			tileSets.Add(newTileSet);
			return newTileSet;
		}

		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="maskFile">Source file for the transparency mask.</param>
		/// <param name="detailLevel">Resolution at which the image and the mask was scanned.</param>
		/// <returns>A tile set.</returns>
		public ITileSet LoadTileSet(IFile imageFile, IFile maskFile, DetailLevelType detailLevel) {
			DXTileSet newTileSet = new DXTileSet(imageFile, maskFile, detailLevel);
			tileSets.Add(newTileSet);
			return newTileSet;
		}

		/// <summary>Loads a textured 3D model.</summary>
		/// <param name="vertice">Geometry data.</param>
		/// <param name="triangles">Geometry data.</param>
		/// <param name="inradius">The distance between the centroid and the nearest face.</param>
		/// <param name="textureFile">Source file with texture data.</param>
		/// <returns>A mesh.</returns>
		public IDieMesh LoadDieMesh(float[,] vertice, Int16[,] triangles, float inradius, IFile textureFile, bool custom) {
			DXDieMesh newMesh = new DXDieMesh(vertice, triangles, inradius, textureFile, custom);
			meshes.Add(newMesh);
			return newMesh;
		}

		/// <summary>Must be called once at the beginning of a frame before rendering actually begins.</summary>
		/// <param name="currentTimeInMicroseconds">The current time.</param>
		/// <returns>False if the device is not ready to render.</returns>
		public bool BeginFrame(long currentTimeInMicroseconds) {

			// don't render if the window is minimized
			if(deviceWindow.WindowState == FormWindowState.Minimized)
				return false;

			// check for lost device
			switch (D3D.CheckCooperativeLevel())
			{
				case D3DCooperativeLevel.DeviceLost:
					return false;

				case D3DCooperativeLevel.DeviceNotReset:
					if (!D3D.ResetDevice()) return false;
					break;
			}

			// mark text resources for garbage collecting
			foreach(DXTextCache textCache in textCaches.Values)
				textCache.MarkAllEntriesAsUnused();

			// begin the scene
			D3D.BeginFrame();

			return true;
		}

		/// <summary>Must be called once at the end of a frame after all rendering was done.</summary>
		public void EndFrame() {
			// Render black bars for aspect ratio differences
			if(gameDisplayAreaInPixels.X > 0) {
				// vertical bars
				monochromaticImage.Render(new RectangleF(0.0f, 0.0f, gameDisplayAreaInPixels.X, backBufferHeight), 0xFF000000);
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.Right, 0.0f, backBufferWidth - gameDisplayAreaInPixels.Right, backBufferHeight), 0xFF000000);
			} else if(gameDisplayAreaInPixels.Y > 0) {
				// horizontal bars
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.X, 0.0f, gameDisplayAreaInPixels.Width, gameDisplayAreaInPixels.Y), 0xFF000000);
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.X, gameDisplayAreaInPixels.Bottom, gameDisplayAreaInPixels.Width, backBufferHeight - gameDisplayAreaInPixels.Bottom), 0xFF000000);
			}

			// End the scene, and show the result
			D3D.EndFrame();

			// sweep text resources for garbage collecting
			bool emptyTextCacheFound;
			do {
				emptyTextCacheFound = false;
				foreach(KeyValuePair<Font, DXTextCache> textCacheEntry in textCaches) {
					DXTextCache cache = textCacheEntry.Value;
					cache.RemoveUnusedEntries();
					if(cache.Empty) {
						cache.Dispose();
						textCaches.Remove(textCacheEntry.Key);
						emptyTextCacheFound = true;
						break;	// the iterator is invalidated -> begin a new foreach loop
					}
				}
			} while(emptyTextCacheFound);
		}

		/// <summary>An image colored 0xffffffff.</summary>
		public IImage MonochromaticImage { get { return monochromaticImage; } }

		/// <summary>Renders text on screen.</summary>
		/// <param name="font">Font with which the text will be rendered.</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="positionAndSize">Position and size of the text bounding box.</param>
		/// <param name="alignment">Line alignment.</param>
		/// <param name="text">Text to be rendered.</param>
		public void DrawText(System.Drawing.Font font, uint modulationColor, RectangleF positionAndSize, StringAlignment alignment, string text) {
			if(text != null && text != "") {

				// retrieve the textures from the text cache, or create them if they don't exist
				DXTextCache cache;
				if(!textCaches.TryGetValue(font, out cache)) {
					cache = new DXTextCache(font);
					textCaches.Add(font, cache);
				}

				// render the text textures
				int textWidthInPixels;
				DXCachedTextFragment[] textFragments = cache.GetText(text, out textWidthInPixels);
				if(alignment != StringAlignment.Near) {
					positionAndSize.X += (positionAndSize.Width - textWidthInPixels) *
						(alignment == StringAlignment.Center ? 0.5f : 1.0f);
				}
				for (int i = 0; i < textFragments.Length; ++i) {
					float left = positionAndSize.Left;
					float top = positionAndSize.Top;

					float x0 = left + i * 256.0f - 0.5f;
					float y0 = top - 0.5f;

					float x1 = x0;
					float y1 = top + cache.TextHeight - 0.5f;

					float x2 = left + 256.0f + i * 256.0f - 0.5f;
					float y2 = y0;

					float x3 = x2;
					float y3 = y1;

					RectangleF tex = textFragments[i].TextureCoordinates;

					D3D.RenderTexturedQuad(
						textFragments[i].Texture,
						modulationColor,
						x0, y0, x1, y1, x2, y2, x3, y3,
						tex.Top, tex.Right, tex.Bottom, tex.Left);
				}
			}
		}

		/// <summary>Renders text on screen, and downsizes it to fit inside a rectangle.</summary>
		/// <param name="font">Font with which the text will be rendered.</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="positionAndSize">Position and size of the text bounding box.</param>
		/// <param name="alignment">Line alignment.</param>
		/// <param name="text">Text to be rendered.</param>
		public void DrawTextToFit(System.Drawing.Font font, uint modulationColor, RectangleF positionAndSize, StringAlignment alignment, string text) {
			if(text != null && text != "") {

				// retrieve the textures from the text cache, or create them if they don't exist
				DXTextCache cache;
				if(!textCaches.TryGetValue(font, out cache)) {
					cache = new DXTextCache(font);
					textCaches.Add(font, cache);
				}

				// render the text textures
				int textWidthInPixels;
				DXCachedTextFragment[] textFragments = cache.GetText(text, out textWidthInPixels);
				float downsizing = Math.Min(1.0f, Math.Min(positionAndSize.Height / cache.TextHeight, positionAndSize.Width / textWidthInPixels));
				if(alignment != StringAlignment.Near) {
					positionAndSize.X += (positionAndSize.Width - textWidthInPixels * downsizing) *
						(alignment == StringAlignment.Center ? 0.5f : 1.0f);
				}
				positionAndSize.Y += (positionAndSize.Height - cache.TextHeight * downsizing) * 0.5f;
				for(int i = 0; i < textFragments.Length; ++i) {
					float left = positionAndSize.Left;
					float top = positionAndSize.Top;

					float x0 = left + i * 256.0f * downsizing - 0.5f;
					float y0 = top - 0.5f;

					float x1 = x0;
					float y1 = top + cache.TextHeight * downsizing - 0.5f;

					float x2 = left + (i + 1) * 256.0f * downsizing - 0.5f;
					float y2 = y0;

					float x3 = x2;
					float y3 = y1;

					RectangleF tex = textFragments[i].TextureCoordinates;

					D3D.RenderTexturedQuad(
						textFragments[i].Texture,
						modulationColor,
						x0, y0, x1, y1, x2, y2, x3, y3,
						tex.Top, tex.Right, tex.Bottom, tex.Left);
				}
			}
		}

		/// <summary>Returns the width required to render a text.</summary>
		/// <param name="font">Font with which the text would be rendered.</param>
		/// <param name="text">Text to be rendered.</param>
		/// <returns>A width in pixels.</returns>
		public int GetTextWidthInPixels(System.Drawing.Font font, string text) {
			// retrieve the textures from the text cache, or create them if they don't exist
			DXTextCache cache;
			if(!textCaches.TryGetValue(font, out cache)) {
				cache = new DXTextCache(font);
				textCaches.Add(font, cache);
			}

			int textWidthInPixels;
			cache.GetText(text, out textWidthInPixels);

			return textWidthInPixels;
		}

		/// <summary>Deallocates all graphics resources.</summary>
		/// <remarks>Must be called after a game is loaded.</remarks>
		public void FreeResources() {
			foreach(DXTileSet tileSet in tileSets)
				tileSet.Dispose();
			tileSets.Clear();
			foreach(DXDieMesh mesh in meshes)
				mesh.Dispose();
			meshes.Clear();
			foreach(DXVideoTexture videoTexture in videoTextures)
				videoTexture.Dispose();
			videoTextures.Clear();
			foreach(DXTextCache textCache in textCaches.Values)
				textCache.Dispose();
			textCaches.Clear();
		}

		/// <summary>All eligible fullscreen modes for this display adapter.</summary>
		public IFullscreenMode[] EligibleFullscreenModes {
			get {
				D3DDisplayMode[] modes = D3D.GetEligibleFullscreenModes();
				
				var eligibleModes = new IFullscreenMode[modes.Length];
				for(int i = 0; i < modes.Length; ++i)
                {
					eligibleModes[i] = new FullscreenMode(modes[i]);
				}
				return eligibleModes;
			}
		}

		/// <summary>Toggles between fullscreen and windowed mode.</summary>
		public bool Fullscreen {
			get { return fullscreen; }
			set {
				if(fullscreen != value) {
					// toggle between fullscreen and windowed
					fullscreen = value;

					updatePresentParameters();

					if (fullscreen)
					{   // fullscreen
						deviceWindowState = deviceWindow.WindowState;
						deviceWindow.WindowState = FormWindowState.Normal;
						deviceWindowLocation = new Rectangle(deviceWindow.Location, deviceWindow.ClientSize);
						deviceWindow.FormBorderStyle = FormBorderStyle.None;
					}
					else
					{   // windowed
						deviceWindow.FormBorderStyle = FormBorderStyle.Sizable;
						deviceWindow.TopMost = false;
					}

					updateGameDisplayArea();

					deviceWindow.Resize -= new EventHandler(onResize);

					// check for lost device
					if (D3D.CheckCooperativeLevel() != D3DCooperativeLevel.DeviceLost)
					{
						D3D.ResetDevice();
					}

					if (!fullscreen)
					{   // windowed
						deviceWindow.Location = deviceWindowLocation.Location;
						deviceWindow.ClientSize = deviceWindowLocation.Size;
					}

					deviceWindow.Resize += new EventHandler(onResize);
				}
			}
		}

		/// <summary>Allocates a texture resource to render video frames.</summary>
		/// <param name="size">Size of the texture.</param>
		/// <returns>A video texture.</returns>
		public IVideoTexture CreateVideoTexture(Size size) {
			DXVideoTexture newVideoTexture = new DXVideoTexture(size);
			videoTextures.Add(newVideoTexture);
			return newVideoTexture; ;
		}

		private unsafe DXTile createMonochromaticTile(uint color) {
			var texture = D3DTexture.Create(1, 1, D3DTextureFormat.X8R8G8B8);
			texture.Lock(out var pitch, out var bits);
			*(uint*)bits = (color & 0x00FFFFFF);
			texture.Unlock();

			return new DXTile(texture);
		}

		readonly Form deviceWindow;
		D3DDisplayMode windowedDisplayMode;
		Rectangle deviceWindowLocation;	// used to restore window state when toggling from fullscreen to windowed mode
		FormWindowState deviceWindowState;
		bool fullscreen;
		int backBufferWidth;
		int backBufferHeight;
		IList<DXTileSet> tileSets = new List<DXTileSet>();
		IList<DXDieMesh> meshes = new List<DXDieMesh>();
		IList<DXVideoTexture> videoTextures = new List<DXVideoTexture>();
		sealed class FontComparer : Comparer<Font> {
			public override int Compare(Font x, Font y) { return x.GetHashCode() - y.GetHashCode(); }
		}
		IDictionary<Font, DXTextCache> textCaches = new SortedList<Font, DXTextCache>(new FontComparer());
		IImage monochromaticImage = null;
	}
}
