// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using ZunTzu.FileSystem;
using ZunTzu.Numerics;

//
//  +------------+     +---------------+     +---------+
//  | Networking |<----+ Modelization  |<----+         |
//  +------------+     +---------------+     |         |     
//                             ^             |         |
//                             |             | Control |
//                             |             |         |
//  +------------+     +-------+-------+     |         |     
//  |  Graphics  |<----+ Visualization |<----+         |
//  +------------+     +---------------+     +---------+
// 

// Black bands in fullscreen mode:
//  - wide screen game in 16:10 fullscreen resolution    -> no bars
//  - wide screen game in 4:3 fullscreen resolution      -> horizontal bars on top and bottom
//  - regular screen game in 16:10 fullscreen resolution -> vertical bars left and right
//  - regular screen game in 4:3 fullscreen resolution   -> no bars

namespace ZunTzu.Graphics {

	public enum AspectRatioType { FourToThree, SixteenToTen }
	public enum ColorDepthType { ThirtyTwoBits, SixteenBits }
	public enum TextureQualityType { ThirtyTwoBits = 0, SixteenBits = 1, CompressedEightBitsQuality = 2, CompressedFourBits = 3, CompressedEightBitsFast = 4 }
	public enum DetailLevelType { High = 0, Medium = 1, Low = 2 }

	public delegate void GameDisplayAreaResizedHandler();

	/// <summary>An eligible fullscreen mode.</summary>
	public interface IFullscreenMode {
		/// <summary>An human readable description.</summary>
		string ToString();
	}

	/// <summary>Properties of the display.</summary>
	public struct GraphicsProperties {
		/// <summary>Indicates if the display adapter should wait for the vertical blank between two frames.</summary>
		public bool WaitForVerticalBlank;
		/// <summary>Indicates the preferred mode when operating in fullscreen</summary>
		/// <remarks>If 0, the current desktop mode will be used.</remarks>
		public int PreferredFullscreenMode;
	}

	/// <summary>Component in charge of the rendering of 2D and 3D graphics on screen.</summary>
	public interface IGraphics {
		/// <summary>Game aspect ratio.</summary>
		/// <remarks>If the screen physical aspect ratio is different than the game aspect ratio, black bands will appear.</remarks>
		AspectRatioType GameAspectRatio { get; set; }
		/// <summary>Display area.</summary>
		/// <remarks>The display area is smaller than the screen if black bars are displayed.</remarks>
		Rectangle GameDisplayAreaInPixels { get; }
		/// <summary>Occurs when a the game display area has changed.</summary>
		/// <remarks>That could be caused by the window resizing or if the user has changed the game aspect ratio.</remarks>
		event GameDisplayAreaResizedHandler GameDisplayAreaResized;
		/// <summary>User preferences for the display.</summary>
		/// <remarks>Setting the properties will deallocate all resources.</remarks>
		GraphicsProperties Properties { get; set; }
		/// <summary>All eligible fullscreen modes for this display adapter.</summary>
		IFullscreenMode[] EligibleFullscreenModes { get; }
		/// <summary>Toggles between fullscreen and windowed mode.</summary>
		bool Fullscreen { get; set; }
		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="detailLevel">Resolution at which the image was scanned.</param>
		/// <returns>A tile set.</returns>
		ITileSet LoadTileSet(IFile imageFile, DetailLevelType detailLevel);
		/// <summary>Loads a scanned map or counter sheet as a set of textured tiles.</summary>
		/// <param name="imageFile">Source image file.</param>
		/// <param name="maskFile">Source file for the transparency mask.</param>
		/// <param name="detailLevel">Resolution at which the image and the mask was scanned.</param>
		/// <returns>A tile set.</returns>
		ITileSet LoadTileSet(IFile imageFile, IFile maskFile, DetailLevelType detailLevel);
		/// <summary>Loads a textured 3D model.</summary>
		/// <param name="vertice">Geometry data.</param>
		/// <param name="triangles">Geometry data.</param>
		/// <param name="inradius">The distance between the centroid and the nearest face.</param>
		/// <param name="textureFile">Source file with texture data.</param>
		/// <returns>A mesh.</returns>
		IDieMesh LoadDieMesh(float[,] vertice, Int16[,] triangles, float inradius, IFile textureFile, bool custom);
		/// <summary>Must be called once at the beginning of a frame before rendering actually begins.</summary>
		/// <param name="currentTimeInMicroseconds">The current time.</param>
		/// <returns>False if the device is not ready to render.</returns>
		bool BeginFrame(long currentTimeInMicroseconds);
		/// <summary>Must be called once at the end of a frame after all rendering was done.</summary>
		void EndFrame();
		/// <summary>An image colored 0xffffffff.</summary>
		IImage MonochromaticImage { get; }
		/// <summary>Renders text on screen.</summary>
		/// <param name="font">Font with which the text will be rendered.</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="positionAndSize">Position and size of the text bounding box.</param>
		/// <param name="alignment">Line alignment.</param>
		/// <param name="text">Text to be rendered.</param>
		void DrawText(Font font, uint color, RectangleF positionAndSize, StringAlignment alignment, string text);
		/// <summary>Renders text on screen, and downsizes it to fit inside a rectangle.</summary>
		/// <param name="font">Font with which the text will be rendered.</param>
		/// <param name="color">Color of the text.</param>
		/// <param name="positionAndSize">Position and size of the text bounding box.</param>
		/// <param name="alignment">Line alignment.</param>
		/// <param name="text">Text to be rendered.</param>
		void DrawTextToFit(Font font, uint color, RectangleF positionAndSize, StringAlignment alignment, string text);
		/// <summary>Returns the width required to render a text.</summary>
		/// <param name="font">Font with which the text would be rendered.</param>
		/// <param name="text">Text to be rendered.</param>
		/// <returns>A width in pixels.</returns>
		int GetTextWidthInPixels(Font font, string text);
		/// <summary>Deallocates all graphics resources.</summary>
		/// <remarks>Must be called after a game is loaded.</remarks>
		void FreeResources();
		/// <summary>Allocates a texture resource to render video frames.</summary>
		/// <param name="size">Size of the texture.</param>
		/// <returns>A video texture.</returns>
		IVideoTexture CreateVideoTexture(Size size);
	}

	/// <summary>A texture that can be used to render video frames.</summary>
	public interface IVideoTexture : IDisposable {
		IImage ExtractImage(RectangleF imageLocation);
		/// <summary>Update part of the texture with a new image.</summary>
		/// <param name="location">The part of the texture that will be affected.</param>
		/// <param name="bitmapBits">An image in A8R8G8B8 format.</param>
		/// <remarks>The size of the image must be identical to the size of the location.</remarks>
		void Update(Rectangle location, IntPtr bitmapBits);
	}

	/// <summary>A set of textured tiles.</summary>
	public interface ITileSet : IDisposable {
		/// <summary>Must be called to load the icons tile.</summary>
		void LoadIcons();
		/// <summary>Must be called in a loop for the tile set to be fully loaded.</summary>
		/// <returns>Progress between 0 and 1.</returns>
		IEnumerable<float> LoadIncrementally();
		SizeF Size { get; }
		IImage ExtractImage(RectangleF imageLocation);
		IImage ExtractImage(RectangleF imageLocation, RectangleF renderingPositionAndSize);
	}

	/// <summary>An image, built from a set of textured tiles.</summary>
	public interface IImage {
		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		void Render(RectangleF positionAndSize);
		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		void Render(RectangleF positionAndSize, float rotationAngle);
		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="modulationColor">Modulation color in A8R8G8B8 format.</param>
		void Render(RectangleF positionAndSize, uint modulationColor);
		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="modulationColor">Modulation color in A8R8G8B8 format.</param>
		void Render(RectangleF positionAndSize, float rotationAngle, uint modulationColor);

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color);

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		void RenderIgnoreMask(RectangleF positionAndSize);

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		uint GetColorAtPosition(PointF position);
	}

	/// <summary>A 3D object.</summary>
	public interface IDieMesh : IDisposable {
		/// <summary>Render this die.</summary>
		void Render(PointF position, float sizeFactor, Quaternion rotation, uint dieColor, uint pipsColor);
		/// <summary>Render the shadow of this die.</summary>
		void RenderShadow(PointF position, float sizeFactor, Quaternion rotation, uint shadowColor);
	}
}
