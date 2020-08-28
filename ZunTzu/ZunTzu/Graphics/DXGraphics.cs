// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.FileSystem;
using ZunTzu.Timing;

namespace ZunTzu.Graphics {

	/// <summary>Direct3D implementation of IGraphics.</summary>
	public sealed class DXGraphics : IGraphics {

		/// <summary>An eligible fullscreen mode.</summary>
		private sealed class FullscreenMode : IFullscreenMode {
			public FullscreenMode(DisplayMode displayMode) {
				DisplayMode = displayMode;
			}
			/// <summary>An human readable description.</summary>
			public override string ToString() { return string.Format("{0} x {1}, {2}, {3} hertz", DisplayMode.Width, DisplayMode.Height, DisplayMode.Format, DisplayMode.RefreshRate); }
			public readonly DisplayMode DisplayMode;
		}

		/// <summary>Indicates that the display adapter supports that texture format.</summary>
		/// <param name="textureQuality">Setting to use for the textures.</param>
		/// <returns>True if the format is supported.</returns>
		public bool SupportsTextureQuality(TextureQualityType textureQuality) {
			int adapter = Manager.Adapters.Default.Adapter;
			Format displayFormat = (presentParams.BackBufferFormat == Format.Unknown ? windowedDisplayMode.Format : presentParams.BackBufferFormat);
			return
				Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.X8R8G8B8) &&
				Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.A8R8G8B8) &&
				(textureQuality == TextureQualityType.SixteenBits ?
					Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.R5G6B5) &&
					Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.A1R5G5B5) :
					(textureQuality == TextureQualityType.CompressedFourBits ?
						Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.Dxt1) :
						(textureQuality == TextureQualityType.CompressedEightBitsQuality || textureQuality == TextureQualityType.CompressedEightBitsFast ?
							Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.Dxt1) &&
							Manager.CheckDeviceFormat(adapter, DeviceType.Hardware, displayFormat, 0, ResourceType.Textures, Format.Dxt5) :
							true)));
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

		private void updateGameDisplayArea() {
			int width = presentParams.BackBufferWidth;
			int height = presentParams.BackBufferHeight;

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
			if(presentParams.Windowed) {
				presentParams.BackBufferWidth = deviceWindow.ClientSize.Width;
				presentParams.BackBufferHeight = deviceWindow.ClientSize.Height;
				updateGameDisplayArea();

				// don't reset if the window is minimized
				if(deviceWindow.WindowState != FormWindowState.Minimized) {
					// check for lost device
					int checkResult;
					if(device.CheckCooperativeLevel(out checkResult) ||
						checkResult == (int)ResultCode.DeviceNotReset) {
						try {
							device.Reset(presentParams);
						} catch(DeviceLostException) {}
					}
				}
			}
		}

		/// <summary>Constructor.</summary>
		/// <param name="deviceWindow">Main form of the application.</param>
		public DXGraphics(Form deviceWindow, GraphicsProperties graphicsProperties, AspectRatioType gameAspectRatio) {
			if(!isDeviceCompliant())
				throw new ApplicationException("No compliant display adapter found (caps).");

			this.deviceWindow = deviceWindow;
			windowedDisplayMode = Manager.Adapters.Default.CurrentDisplayMode;
			deviceWindowLocation = new Rectangle(deviceWindow.Location, deviceWindow.ClientSize);
			deviceWindowState = deviceWindow.WindowState;

			properties = graphicsProperties;

			this.gameAspectRatio = gameAspectRatio;

			presentParams = new PresentParameters();
			presentParams.DeviceWindow = deviceWindow;
			presentParams.SwapEffect = SwapEffect.Discard;	// Discard the frames
			presentParams.PresentationInterval = (properties.WaitForVerticalBlank ? PresentInterval.Default : PresentInterval.Immediate);
			presentParams.BackBufferCount = 1;

			presentParams.Windowed = true;

			updatePresentParameters();
			updateGameDisplayArea();

			// Hardware vertex processing produces lower frame rates (even with a pure device), so we stick to software processing
			device = new Device(0, DeviceType.Hardware, deviceWindow, CreateFlags.SoftwareVertexProcessing, presentParams);

			device.DeviceCreated +=	new EventHandler(onCreateDevice);	// not sure it is useful at all
			device.DeviceResizing += new CancelEventHandler(onDeviceResizing);
			deviceWindow.Resize += new EventHandler(onResize);

			onCreateDevice(device, null);	// must be called explicitly since the delegate was registered with the event after the device was created
		}

		private void updatePresentParameters() {
			int adapter = Manager.Adapters.Default.Adapter;
			if(presentParams.Windowed && Manager.CheckDeviceType(adapter, DeviceType.Hardware, windowedDisplayMode.Format, Format.Unknown, true)) {	// windowed
				presentParams.BackBufferFormat = Format.Unknown;
				presentParams.BackBufferWidth = deviceWindow.ClientSize.Width;
				presentParams.BackBufferHeight = deviceWindow.ClientSize.Height;
				presentParams.FullScreenRefreshRateInHz = 0;
				presentParams.PresentFlag = PresentFlag.None;
			} else {	// fullscreen
				presentParams.Windowed = false;
				DisplayMode displayMode = windowedDisplayMode;
				if(properties.PreferredFullscreenMode > 0) {
					IFullscreenMode[] allDisplayModes = EligibleFullscreenModes;
					if(properties.PreferredFullscreenMode < allDisplayModes.Length + 1)
						displayMode = ((FullscreenMode) allDisplayModes[properties.PreferredFullscreenMode - 1]).DisplayMode;
				}
				if(!Manager.CheckDeviceType(adapter, DeviceType.Hardware, displayMode.Format, displayMode.Format, false))
					throw new ApplicationException("No compliant display adapter found (display mode).");
				presentParams.BackBufferFormat = displayMode.Format;
				presentParams.BackBufferWidth = displayMode.Width;
				presentParams.BackBufferHeight = displayMode.Height;
				presentParams.FullScreenRefreshRateInHz = displayMode.RefreshRate;
				presentParams.PresentFlag = PresentFlag.LockableBackBuffer;	// required for controls rendering
			}

			// check device support
			if(!SupportsTextureQuality(properties.TextureQuality)) {
				if(properties.TextureQuality != TextureQualityType.ThirtyTwoBits && SupportsTextureQuality(TextureQualityType.ThirtyTwoBits))
					properties.TextureQuality = TextureQualityType.ThirtyTwoBits;
				else if(properties.TextureQuality != TextureQualityType.SixteenBits && SupportsTextureQuality(TextureQualityType.SixteenBits))
					properties.TextureQuality = TextureQualityType.SixteenBits;
				else
					throw new ApplicationException("No compliant display adapter found (texture).");
			}
		}

		private bool isDeviceCompliant() {
			Caps caps = Manager.GetDeviceCaps(Manager.Adapters.Default.Adapter, DeviceType.Hardware);
			return
				// device type, display format and back buffer format?
				//SupportsBackBufferColorDepth(properties.BackBufferColorDepth) &&
				// supports texture formats?
				//SupportsTextureQuality(properties.TextureQuality) &&
				// supports PresentInterval.Immediate?
				//(presentParams.PresentationInterval != PresentInterval.Immediate || (caps.PresentationIntervals & PresentInterval.Immediate) != 0) &&
				// supports clockwise triangle culling?
				caps.PrimitiveMiscCaps.SupportsCullClockwise &&
				// supports per-stage linear filtering for minifying textures?
				caps.TextureFilterCaps.SupportsMinifyLinear &&
				// supports per-stage bilinear interpolation filtering for magnifying mipmaps?
				caps.TextureFilterCaps.SupportsMagnifyLinear &&
				// supports per-stage point-sample filtering for mipmaps (not needed anymore)
				//caps.TextureFilterCaps.SupportsMipMapPoint &&
				// can clamp textures to addresses?
				caps.TextureAddressCaps.SupportsClamp &&
				// supports the modulate texture-blending operation?
				caps.TextureOperationCaps.SupportsModulate &&
				// supports alpha-blending operations? (not reliable)
				//caps.PrimitiveMiscCaps.SupportsBlendOperation &&
				// can respect the D3DRS_ALPHABLENDENABLE render state in full-screen mode while using the FLIP or DISCARD swap effect? (not reliable)
				//(presentParams.Windowed || caps.DriverCaps.SupportsAlphaFullscreenFlipOrDiscard) &&
				// source-blending capabilities?
				caps.SourceBlendCaps.SupportsSourceAlpha &&
				// destination-blending capabilities?
				caps.DestinationBlendCaps.SupportsInverseSourceAlpha &&
				// alpha in texture pixels is supported?
				caps.TextureCaps.SupportsAlpha &&
				// supports light? (not reliable)
				//caps.MaxActiveLights >= 1 &&
				// supports directional lights? (not reliable)
				//caps.VertexProcessingCaps.SupportsDirectionAllLights &&
				// supports Gouraud lighting?
				caps.ShadeCaps.SupportsColorGouraudRgb &&
				// supports specular highlights?
				caps.ShadeCaps.SupportsSpecularGouraudRgb &&
				// supports material source? (not reliable)
				//caps.VertexProcessingCaps.SupportsMaterialSource &&
				// supports streams?
				caps.MaxStreams >= 1 &&
				// maximum number of texture-blending stages supported in the fixed function pipeline
				caps.MaxTextureBlendStages >= 1 &&
				// maximum number of textures that can be simultaneously bound to the fixed function pipeline texture blending stages
				caps.MaxSimultaneousTextures >= 1;
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
					(Fullscreen && properties.PreferredFullscreenMode != value.PreferredFullscreenMode));

				properties = value;

				if(resetRequired) {
					presentParams.PresentationInterval = (properties.WaitForVerticalBlank ? PresentInterval.Default : PresentInterval.Immediate);
					updatePresentParameters();
					updateGameDisplayArea();

					deviceWindow.Resize -= new EventHandler(onResize);

					// check for lost device
					int checkResult;
					if(device.CheckCooperativeLevel(out checkResult) ||
						checkResult == (int)ResultCode.DeviceNotReset) {
						try {
							device.Reset(presentParams);
						} catch(DeviceLostException) {}
					}

					deviceWindow.Resize += new EventHandler(onResize);
				}
			}
		}
		private GraphicsProperties properties;

		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="detailLevel">Resolution at which the image was scanned.</param>
		/// <returns>A tile set.</returns>
		public ITileSet LoadTileSet(IFile imageFile, DetailLevelType detailLevel) {
			DXTileSet newTileSet = new DXTileSet(this, imageFile, detailLevel);
			tileSets.Add(newTileSet);
			return newTileSet;
		}

		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="maskFile">Source file for the transparency mask.</param>
		/// <param name="detailLevel">Resolution at which the image and the mask was scanned.</param>
		/// <returns>A tile set.</returns>
		public ITileSet LoadTileSet(IFile imageFile, IFile maskFile, DetailLevelType detailLevel) {
			DXTileSet newTileSet = new DXTileSet(this, imageFile, maskFile, detailLevel);
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
			DXDieMesh newMesh = new DXDieMesh(this, vertice, triangles, inradius, textureFile, custom);
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
			int checkResult;
			if(!device.CheckCooperativeLevel(out checkResult)) {
				if(checkResult == (int)ResultCode.DeviceNotReset) {
					try {
						device.Reset(presentParams);
					} catch(DeviceLostException) {
						return false;
					}
				} else {
					return false;
				}
			}

			// mark text resources for garbage collecting
			foreach(DXTextCache textCache in textCaches.Values)
				textCache.MarkAllEntriesAsUnused();

			//Begin the scene
			device.BeginScene();

			// 2D settings
			device.RenderState.CullMode = Cull.Clockwise;
			device.RenderState.Lighting = false;
			device.SamplerState[0].MinFilter = TextureFilter.Linear;
			device.SamplerState[0].MagFilter = TextureFilter.Linear;
			device.SamplerState[0].AddressU = TextureAddress.Clamp;
			device.SamplerState[0].AddressV = TextureAddress.Clamp;

			device.VertexFormat = CustomVertex.PositionColoredTextured.Format;
			device.TextureState[0].ColorOperation = TextureOperation.Modulate;
			device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
			device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;

			device.RenderState.AlphaBlendEnable = true;
			device.RenderState.SourceBlend = Blend.SourceAlpha;
			device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
			device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
			device.TextureState[0].AlphaArgument1 = TextureArgument.Diffuse;
			device.TextureState[0].AlphaArgument2 = TextureArgument.TextureColor;

			device.TextureState[1].ColorOperation = TextureOperation.Disable;
			device.TextureState[1].AlphaOperation = TextureOperation.Disable;

			device.Transform.View = Matrix.Identity;
			device.Transform.Projection = Matrix.Multiply(
				Matrix.Scaling(2.0f/presentParams.BackBufferWidth, -2.0f/presentParams.BackBufferHeight, 1.0f),
				Matrix.Translation(-1.0f, 1.0f, 0.0f));
			device.Transform.World = Matrix.Identity;

			// 3D settings that can coexist with the 2D settings (so we can declare them ahead of time)
			device.Lights[0].Type = LightType.Directional;
			device.Lights[0].Direction = new Vector3(1.0f/1.5f, -1.0f/1.5f, 0.5f/1.5f);
			device.Lights[0].Ambient = Color.FromArgb(0xFF, 0x7F, 0x7F, 0x7F); //Color.FromArgb(0x003F3F3F);
			device.Lights[0].Diffuse = Color.FromArgb(0xFF, 0x80, 0x80, 0x80); //Color.FromArgb(0x00C0C0C0);
			device.Lights[0].Specular = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
			device.Lights[0].Commit();
			device.Lights[0].Enabled = true;
			device.RenderState.SpecularEnable = true;
			device.RenderState.NormalizeNormals = true;
			device.RenderState.AmbientMaterialSource = ColorSource.Material;
			device.RenderState.DiffuseMaterialSource = ColorSource.Material;
			device.RenderState.SpecularMaterialSource = ColorSource.Material;
			device.RenderState.ShadeMode = ShadeMode.Gouraud;
			device.RenderState.ColorVertex = false;

			renderingMode = RenderingMode.Default;

			device.SetStreamSource(0, vb, 0);

			return true;
		}

		/// <summary>Must be called once at the end of a frame after all rendering was done.</summary>
		public void EndFrame() {
			// Render black bars for aspect ratio differences
			switchToDefaultRendering();
			if(gameDisplayAreaInPixels.X > 0) {
				// vertical bars
				monochromaticImage.Render(new RectangleF(0.0f, 0.0f, gameDisplayAreaInPixels.X, presentParams.BackBufferHeight), 0xFF000000);
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.Right, 0.0f, presentParams.BackBufferWidth - gameDisplayAreaInPixels.Right, presentParams.BackBufferHeight), 0xFF000000);
			} else if(gameDisplayAreaInPixels.Y > 0) {
				// horizontal bars
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.X, 0.0f, gameDisplayAreaInPixels.Width, gameDisplayAreaInPixels.Y), 0xFF000000);
				monochromaticImage.Render(new RectangleF(gameDisplayAreaInPixels.X, gameDisplayAreaInPixels.Bottom, gameDisplayAreaInPixels.Width, presentParams.BackBufferHeight - gameDisplayAreaInPixels.Bottom), 0xFF000000);
			}

			// End the scene, and show the result
			device.EndScene();
			try {
				device.Present();
			} catch(DeviceLostException) {}

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
				switchToDefaultRendering();

				// retrieve the textures from the text cache, or create them if they don't exist
				DXTextCache cache;
				if(!textCaches.TryGetValue(font, out cache)) {
					cache = new DXTextCache(this, font);
					textCaches.Add(font, cache);
				}

				// render the text textures
				int textWidthInPixels;
				DXCachedTextFragment[] textFragments = cache.GetText(text, out textWidthInPixels);
				if(alignment != StringAlignment.Near) {
					positionAndSize.X += (positionAndSize.Width - textWidthInPixels) *
						(alignment == StringAlignment.Center ? 0.5f : 1.0f);
				}
				for(int i = 0; i < textFragments.Length; ++i) {
					Quad.Texture = textFragments[i].Texture;
					Quad.ModulationColor = modulationColor;
					Quad.Coord0 = new PointF(positionAndSize.Left + i * 256.0f - 0.5f, positionAndSize.Top - 0.5f);
					Quad.Coord1 = new PointF(positionAndSize.Left + i * 256.0f - 0.5f, positionAndSize.Top + cache.TextHeight - 0.5f);
					Quad.Coord2 = new PointF(positionAndSize.Left + 256.0f + i * 256.0f - 0.5f, positionAndSize.Top - 0.5f);
					Quad.Coord3 = new PointF(positionAndSize.Left + 256.0f + i * 256.0f - 0.5f, positionAndSize.Top + cache.TextHeight - 0.5f);
					Quad.TextureCoordinates = textFragments[i].TextureCoordinates;
					renderQuad();
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
				switchToDefaultRendering();

				// retrieve the textures from the text cache, or create them if they don't exist
				DXTextCache cache;
				if(!textCaches.TryGetValue(font, out cache)) {
					cache = new DXTextCache(this, font);
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
					Quad.Texture = textFragments[i].Texture;
					Quad.ModulationColor = modulationColor;
					Quad.Coord0 = new PointF(positionAndSize.Left + i * 256.0f * downsizing - 0.5f, positionAndSize.Top - 0.5f);
					Quad.Coord1 = new PointF(positionAndSize.Left + i * 256.0f * downsizing - 0.5f, positionAndSize.Top + cache.TextHeight * downsizing - 0.5f);
					Quad.Coord2 = new PointF(positionAndSize.Left + (i + 1) * 256.0f * downsizing - 0.5f, positionAndSize.Top - 0.5f);
					Quad.Coord3 = new PointF(positionAndSize.Left + (i + 1) * 256.0f * downsizing - 0.5f, positionAndSize.Top + cache.TextHeight * downsizing - 0.5f);
					Quad.TextureCoordinates = textFragments[i].TextureCoordinates;
					renderQuad();
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
				cache = new DXTextCache(this, font);
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
				List<IFullscreenMode> eligibleModes = new List<IFullscreenMode>();
				AdapterInformation adapter = Manager.Adapters.Default;
				foreach(DisplayMode displayMode in adapter.SupportedDisplayModes) {
					if(displayMode.Height >= 600 && Manager.CheckDeviceType(adapter.Adapter, DeviceType.Hardware, displayMode.Format, displayMode.Format, false)) {
						eligibleModes.Add(new FullscreenMode(displayMode));
					}
				}
				return eligibleModes.ToArray();
			}
		}

		/// <summary>Toggles between fullscreen and windowed mode.</summary>
		public bool Fullscreen {
			get { return !presentParams.Windowed; }
			set {
				if(presentParams.Windowed == value) {
					// toggle between fullscreen and windowed
					presentParams.Windowed = !value;

					updatePresentParameters();
					if(presentParams.Windowed) {	// windowed
						deviceWindow.FormBorderStyle = FormBorderStyle.Sizable;
						deviceWindow.TopMost = false;
					} else {	// fullscreen
						deviceWindowState = deviceWindow.WindowState;
						deviceWindow.WindowState = FormWindowState.Normal;
						deviceWindowLocation = new Rectangle(deviceWindow.Location, deviceWindow.ClientSize);
						deviceWindow.FormBorderStyle = FormBorderStyle.None;
					}
					updateGameDisplayArea();

					deviceWindow.Resize -= new EventHandler(onResize);

					// check for lost device
					int checkResult;
					if(device.CheckCooperativeLevel(out checkResult) ||
						checkResult == (int)ResultCode.DeviceNotReset) {
						try {
							device.Reset(presentParams);
						} catch(DeviceLostException) {}
					}

					if(presentParams.Windowed) {	// windowed
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
			DXVideoTexture newVideoTexture = new DXVideoTexture(this, size);
			videoTextures.Add(newVideoTexture);
			return newVideoTexture; ;
		}

		internal Device Device { get { return device; } }
		internal readonly DXQuad Quad = new DXQuad();

		internal void RenderTexturedQuad() {
			switchToDefaultRendering();
			if(Quad.Texture == null)
				Quad.Texture = blackTile.Texture;
			renderQuad();
		}

		internal void RenderMonochromaticQuad() {
			switchToDefaultRendering();
			Quad.Texture = whiteTile.Texture;
			Quad.TextureCoordinates = new RectangleF(0.0f, 0.0f, 1.0f, 1.0f);
			renderQuad();
		}

		internal void RenderDieMesh(DXDieMesh mesh, PointF position, float sizeFactor, float [,] rotationMatrix, uint dieColor, uint pipsColor) {
			switchToMeshRendering(mesh);

			Material material = new Material();
			material.Specular = Color.White;
			material.SpecularSharpness = 20.0f;

			float scalingFactor = 100.0f / (float)presentParams.BackBufferWidth;
			float scaling = sizeFactor * scalingFactor;

			Matrix rotation = new Matrix();
			rotation.M11 = rotationMatrix[0,0];
			rotation.M12 = rotationMatrix[0,1];
			rotation.M13 = rotationMatrix[0,2];
			rotation.M14 = 0.0f;
			rotation.M21 = rotationMatrix[1,0];
			rotation.M22 = rotationMatrix[1,1];
			rotation.M23 = rotationMatrix[1,2];
			rotation.M24 = 0.0f;
			rotation.M31 = rotationMatrix[2,0];
			rotation.M32 = rotationMatrix[2,1];
			rotation.M33 = rotationMatrix[2,2];
			rotation.M34 = 0.0f;
			rotation.M41 = 0.0f;
			rotation.M42 = 0.0f;
			rotation.M43 = 0.0f;
			rotation.M44 = 1.0f;

			device.Transform.World =
				Matrix.Multiply(
					Matrix.Multiply(
						rotation,
						Matrix.Scaling(scaling, scaling, scaling)
					),
					Matrix.Translation(
						(position.X - (float)presentParams.BackBufferWidth * 0.5f) * scalingFactor,
						((float)presentParams.BackBufferHeight * 0.5f - position.Y) * scalingFactor,
						0.0f)
				);

			material.Ambient = Color.FromArgb((int) dieColor);
			material.Diffuse = Color.FromArgb((int) dieColor);
			device.Material = material;
			device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.VertexCount, 0, mesh.TriangleCount);

			material.Ambient = Color.FromArgb((int) pipsColor);
			material.Diffuse = Color.FromArgb((int) pipsColor);
			device.Material = material;
			device.TextureState[0].AlphaOperation = TextureOperation.SelectArg2;
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.VertexCount, 0, mesh.TriangleCount);
		}

		internal void RenderCustomDieMesh(DXDieMesh mesh, PointF position, float sizeFactor, float[,] rotationMatrix) {
			switchToMeshRendering(mesh);

			Material material = new Material();
			material.Specular = Color.White;
			material.SpecularSharpness = 20.0f;

			float scalingFactor = 100.0f / (float) presentParams.BackBufferWidth;
			float scaling = sizeFactor * scalingFactor;

			Matrix rotation = new Matrix();
			rotation.M11 = rotationMatrix[0, 0];
			rotation.M12 = rotationMatrix[0, 1];
			rotation.M13 = rotationMatrix[0, 2];
			rotation.M14 = 0.0f;
			rotation.M21 = rotationMatrix[1, 0];
			rotation.M22 = rotationMatrix[1, 1];
			rotation.M23 = rotationMatrix[1, 2];
			rotation.M24 = 0.0f;
			rotation.M31 = rotationMatrix[2, 0];
			rotation.M32 = rotationMatrix[2, 1];
			rotation.M33 = rotationMatrix[2, 2];
			rotation.M34 = 0.0f;
			rotation.M41 = 0.0f;
			rotation.M42 = 0.0f;
			rotation.M43 = 0.0f;
			rotation.M44 = 1.0f;

			device.Transform.World =
				Matrix.Multiply(
					Matrix.Multiply(
						rotation,
						Matrix.Scaling(scaling, scaling, scaling)
					),
					Matrix.Translation(
						(position.X - (float) presentParams.BackBufferWidth * 0.5f) * scalingFactor,
						((float) presentParams.BackBufferHeight * 0.5f - position.Y) * scalingFactor,
						0.0f)
				);

			material.Ambient = Color.White;
			material.Diffuse = Color.White;
			device.Material = material;
			device.TextureState[0].AlphaOperation = TextureOperation.SelectArg2;
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.VertexCount, 0, mesh.TriangleCount);
		}

		internal void RenderDieMeshShadow(DXDieMesh mesh, PointF position, float sizeFactor, float[,] rotationMatrix, uint shadowColor) {
			switchToMeshRendering(mesh);

			Material material = new Material();
			material.Specular = Color.Black;
			material.SpecularSharpness = 0.0f;

			float scalingFactor = 100.0f / (float) presentParams.BackBufferWidth;
			float scaling = sizeFactor * scalingFactor;

			Matrix rotation = new Matrix();
			rotation.M11 = rotationMatrix[0, 0];
			rotation.M12 = rotationMatrix[0, 1];
			rotation.M13 = rotationMatrix[0, 2];
			rotation.M14 = 0.0f;
			rotation.M21 = rotationMatrix[1, 0];
			rotation.M22 = rotationMatrix[1, 1];
			rotation.M23 = rotationMatrix[1, 2];
			rotation.M24 = 0.0f;
			rotation.M31 = rotationMatrix[2, 0];
			rotation.M32 = rotationMatrix[2, 1];
			rotation.M33 = rotationMatrix[2, 2];
			rotation.M34 = 0.0f;
			rotation.M41 = 0.0f;
			rotation.M42 = 0.0f;
			rotation.M43 = 0.0f;
			rotation.M44 = 1.0f;

			Matrix projectionOnTable = new Matrix();
			projectionOnTable.M11 = 1.0f;
			projectionOnTable.M12 = 0.0f;
			projectionOnTable.M13 = 0.0f;
			projectionOnTable.M14 = 0.0f;
			projectionOnTable.M21 = 0.0f;
			projectionOnTable.M22 = 1.0f;
			projectionOnTable.M23 = 0.0f;
			projectionOnTable.M24 = 0.0f;
			projectionOnTable.M31 = -1.0f / 1.5f;
			projectionOnTable.M32 = 1.0f / 1.5f;
			projectionOnTable.M33 = 0.0f;
			projectionOnTable.M34 = 0.0f;
			projectionOnTable.M41 = mesh.Inradius / 1.5f;
			projectionOnTable.M42 = -(mesh.Inradius / 1.5f);
			projectionOnTable.M43 = mesh.Inradius;
			projectionOnTable.M44 = 1.0f;

			device.Transform.World =
				Matrix.Multiply(
					Matrix.Multiply(
						Matrix.Multiply(
							rotation,
							projectionOnTable
						),
						Matrix.Scaling(scaling, scaling, scaling)
					),
					Matrix.Translation(
						(position.X - (float) presentParams.BackBufferWidth * 0.5f) * scalingFactor,
						((float) presentParams.BackBufferHeight * 0.5f - position.Y) * scalingFactor,
						0.0f)
				);

			material.Ambient = Color.FromArgb((int) shadowColor);
			material.Diffuse = Color.FromArgb((int) shadowColor);
			device.Material = material;
			device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.VertexCount, 0, mesh.TriangleCount);
		}

		internal void RenderTexturedQuadSilhouette() {
			switchToSilhouetteRendering();
			if(Quad.Texture == null)
				Quad.Texture = blackTile.Texture;
			renderQuad();
		}

		internal void RenderTexturedQuadIgnoreMask() {
			switchToIgnoreMaskRendering();
			if(Quad.Texture == null)
				Quad.Texture = blackTile.Texture;
			renderQuad();
		}

		private void onCreateDevice(object sender, EventArgs e) {
			device.SetDialogBoxesEnabled(true);	// required to render controls in fullscreen
			vb = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), 4, device, Usage.WriteOnly, CustomVertex.PositionColoredTextured.Format, Pool.Managed);
			blackTile = createMonochromaticTile(0xFF000000);
			whiteTile = createMonochromaticTile(0xFFFFFFFF);
			monochromaticImage = new DXMonochromaticImage(this);
		}

		private void onDeviceResizing(object sender, CancelEventArgs e) {
			// cancels the automatic device reset on resize
			e.Cancel = true;
		}

		private unsafe DXTile createMonochromaticTile(uint color) {
			Texture texture;
			if(properties.TextureQuality == TextureQualityType.SixteenBits) {
				texture = new Texture(device, 1, 1, 1, 0, Format.R5G6B5, Pool.Managed);
				byte * textureBits = (byte*) texture.LockRectangle(0, LockFlags.None).InternalData.ToPointer();
				*(ushort*)textureBits = (ushort) (
					((((color & 0x00FF0000) >> 16)*31+127)/255) |
					((((color & 0x0000FF00) >> 8)*63+127)/255)<<5 |
					(((color & 0x000000FF)*31+127)/255)<<11);
			} else {
				texture = new Texture(device, 1, 1, 1, 0, Format.X8R8G8B8, Pool.Managed);
				byte * textureBits = (byte*) texture.LockRectangle(0, LockFlags.None).InternalData.ToPointer();
				*(uint*)textureBits = (color & 0x00FFFFFF);
			}
			texture.UnlockRectangle(0);

			return new DXTile(texture);
		}

		private CustomVertex.PositionColoredTextured[] verts = new CustomVertex.PositionColoredTextured[4];
		private unsafe void renderQuad() {
			// set texture
			device.SetTexture(0, Quad.Texture);

			// draw quad
			verts[0].X = Quad.Coord0.X;
			verts[0].Y = Quad.Coord0.Y;
			verts[0].Z = 0.0f;
			verts[0].Color = (int)Quad.ModulationColor;
			verts[0].Tu = Quad.TextureCoordinates.Left;
			verts[0].Tv = Quad.TextureCoordinates.Top;
			verts[1].X = Quad.Coord1.X;
			verts[1].Y = Quad.Coord1.Y;
			verts[1].Z = 0.0f;
			verts[1].Color = (int)Quad.ModulationColor;
			verts[1].Tu = Quad.TextureCoordinates.Left;
			verts[1].Tv = Quad.TextureCoordinates.Bottom;
			verts[2].X = Quad.Coord2.X;
			verts[2].Y = Quad.Coord2.Y;
			verts[2].Z = 0.0f;
			verts[2].Color = (int)Quad.ModulationColor;
			verts[2].Tu = Quad.TextureCoordinates.Right;
			verts[2].Tv = Quad.TextureCoordinates.Top;
			verts[3].X = Quad.Coord3.X;
			verts[3].Y = Quad.Coord3.Y;
			verts[3].Z = 0.0f;
			verts[3].Color = (int)Quad.ModulationColor;
			verts[3].Tu = Quad.TextureCoordinates.Right;
			verts[3].Tv = Quad.TextureCoordinates.Bottom;

			vb.SetData(verts, 0, LockFlags.None);
			device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
		}

		/// <summary>Prepares for 2D rendering.</summary>
		private void switchTo2DRendering() {
			if(renderingMode == RenderingMode.Mesh) {
				// reset state
				device.RenderState.Lighting = false;
				device.VertexFormat = CustomVertex.PositionColoredTextured.Format;

				// reset transform matrices
				device.Transform.View = Matrix.Identity;
				device.Transform.Projection = Matrix.Multiply(
					Matrix.Scaling(2.0f / presentParams.BackBufferWidth, -2.0f / presentParams.BackBufferHeight, 1.0f),
					Matrix.Translation(-1.0f, 1.0f, 0.0f));
				device.Transform.World = Matrix.Identity;

				device.SetStreamSource(0, vb, 0);
			}
		}

		/// <summary>Prepares for 2D rendering.</summary>
		private void switchToDefaultRendering() {
			if(renderingMode != RenderingMode.Default) {
				switchTo2DRendering();
				device.TextureState[0].ColorOperation = TextureOperation.Modulate;
				device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
				renderingMode = RenderingMode.Default;
			}
		}

		/// <summary>Prepares for 2D rendering of a silhouette.</summary>
		private void switchToSilhouetteRendering() {
			if(renderingMode != RenderingMode.Silhouette) {
				switchTo2DRendering();
				device.TextureState[0].ColorOperation = TextureOperation.SelectArg2;
				device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
				renderingMode = RenderingMode.Silhouette;
			}
		}

		/// <summary>Prepares for 2D rendering.</summary>
		private void switchToIgnoreMaskRendering() {
			if(renderingMode != RenderingMode.IgnoreMask) {
				switchTo2DRendering();
				device.TextureState[0].ColorOperation = TextureOperation.Modulate;
				device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1; 
				renderingMode = RenderingMode.IgnoreMask;
			}
		}

		/// <summary>Prepares for 3D rendering.</summary>
		private void switchToMeshRendering(DXDieMesh mesh) {
			if(renderingMode != RenderingMode.Mesh) {
				if(renderingMode != RenderingMode.Default) {
					device.TextureState[0].ColorOperation = TextureOperation.Modulate;
					device.TextureState[0].AlphaOperation = TextureOperation.Modulate;
				}
				device.RenderState.Lighting = true;
				device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
				latestUsedMesh = null;

				device.Transform.View = Matrix.Translation(0.0f, 0.0f, 100.0f);
				device.Transform.Projection = Matrix.PerspectiveLH(
					1.0f,
					(float) presentParams.BackBufferHeight / (float) presentParams.BackBufferWidth,
					1.0f, 200.0f);

				renderingMode = RenderingMode.Mesh;
			}
			// do we need to set up a new stream source?
			if(latestUsedMesh != mesh) {
				// yes
				device.SetStreamSource(0, mesh.VertexBuffer, 0);
				device.Indices = mesh.IndexBuffer;
				device.SetTexture(0, mesh.Texture);
				latestUsedMesh = mesh;
			}
		}

		private enum RenderingMode { Default, Silhouette, IgnoreMask, Mesh };	// The rendering mode is used to minimize the need to switch rendering states.
		private RenderingMode renderingMode;
		private DXDieMesh latestUsedMesh = null;	// Used to avoid setting up a new stream source if the mesh hasn't changed.
		private readonly Form deviceWindow;
		private DisplayMode windowedDisplayMode;
		private Rectangle deviceWindowLocation;	// used to restore window state when toggling from fullscreen to windowed mode
		private FormWindowState deviceWindowState;
		private readonly Device device;
		private PresentParameters presentParams;
		private IList<DXTileSet> tileSets = new List<DXTileSet>();
		private IList<DXDieMesh> meshes = new List<DXDieMesh>();
		private IList<DXVideoTexture> videoTextures = new List<DXVideoTexture>();
		private VertexBuffer vb = null;	// resources reused everytime a quad is rendered
		private sealed class FontComparer : Comparer<Font> {
			public override int Compare(Font x, Font y) { return x.GetHashCode() - y.GetHashCode(); }
		}
		private IDictionary<Font, DXTextCache> textCaches = new SortedList<Font, DXTextCache>(new FontComparer());
		private DXTile blackTile = null;
		private DXTile whiteTile = null;
		private IImage monochromaticImage = null;
	}
}
