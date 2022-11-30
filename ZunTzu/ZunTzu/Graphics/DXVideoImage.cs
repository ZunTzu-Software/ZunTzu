// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Graphics
{

    /// <summary>Image extracted from a video texture.</summary>
    public sealed class DXVideoImage : IImage {

		/// <summary>Constructor for a one-shot only image (no mipmapping).</summary>
		internal DXVideoImage(DXVideoTexture videoTexture, RectangleF imageLocation) {
			_videoTexture = videoTexture;
			_imageLocation = imageLocation;
			SizeF videoTextureSize = new SizeF(videoTexture.Size);
			_textureCoordinates = new RectangleF(
				imageLocation.X / videoTextureSize.Width,
				imageLocation.Y / videoTextureSize.Height,
				imageLocation.Width / videoTextureSize.Width,
				imageLocation.Height / videoTextureSize.Height);
		}

		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void Render(RectangleF positionAndSize) {
			Render(positionAndSize, 0.0f, 0xFFFFFFFF);
		}

		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		public void Render(RectangleF positionAndSize, float rotationAngle) {
			Render(positionAndSize, rotationAngle, 0xFFFFFFFF);
		}

		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="modulationColor">Modulation color in A8R8G8B8 format.</param>
		public void Render(RectangleF positionAndSize, uint modulationColor) {
			Render(positionAndSize, 0.0f, modulationColor);
		}

		/// <summary>Render this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="modulationColor">Modulation color in A8R8G8B8 format.</param>
		public void Render(RectangleF positionAndSize, float rotationAngle, uint modulationColor) {
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;

			float right = _imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = _imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			float offset_x = _imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f;
			float offset_y = _imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f;

			float x0, y0, x1, y1, x2, y2, x3, y3;

			if (rotationAngle == 0.0f)
			{
				x0 = left + offset_x;
				y0 = top + offset_y;

				x1 = x0;
				y1 = bottom + offset_y;

				x2 = right + offset_x;
				y2 = y0;

				x3 = x2;
				y3 = y1;
			}
			else
			{
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float)Math.Sin(-rotationAngle);
				float cos = (float)Math.Cos(-rotationAngle);

				x0 = left * cos - top * sin + offset_x;
				y0 = left * sin + top * cos + offset_y;

				x1 = left * cos - bottom * sin + offset_x;
				y1 = left * sin + bottom * cos + offset_y;

				x2 = right * cos - top * sin + offset_x;
				y2 = right * sin + top * cos + offset_y;

				x3 = right * cos - bottom * sin + offset_x;
				y3 = right * sin + bottom * cos + offset_y;
			}

			D3D.RenderTexturedQuad(
				_videoTexture.Texture, modulationColor,
				x0, y0, x1, y1, x2, y2, x3, y3,
				_textureCoordinates.Top, _textureCoordinates.Right, _textureCoordinates.Bottom, _textureCoordinates.Left);
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;

			float right = _imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = _imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			float offset_x = _imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f;
			float offset_y = _imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f;

			float x0, y0, x1, y1, x2, y2, x3, y3;

			if (rotationAngle == 0.0f)
			{
				x0 = left + offset_x;
				y0 = top + offset_y;

				x1 = x0;
				y1 = bottom + offset_y;

				x2 = right + offset_x;
				y2 = y0;

				x3 = x2;
				y3 = y1;
			}
			else
			{
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float)Math.Sin(-rotationAngle);
				float cos = (float)Math.Cos(-rotationAngle);

				x0 = left * cos - top * sin + offset_x;
				y0 = left * sin + top * cos + offset_y;

				x1 = left * cos - bottom * sin + offset_x;
				y1 = left * sin + bottom * cos + offset_y;

				x2 = right * cos - top * sin + offset_x;
				y2 = right * sin + top * cos + offset_y;

				x3 = right * cos - bottom * sin + offset_x;
				y3 = right * sin + bottom * cos + offset_y;
			}

			D3D.RenderTexturedQuadSilhouette(
				_videoTexture.Texture, color,
				x0, y0, x1, y1, x2, y2, x3, y3,
				_textureCoordinates.Top, _textureCoordinates.Right, _textureCoordinates.Bottom, _textureCoordinates.Left);
		}

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void RenderIgnoreMask(RectangleF positionAndSize) {
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;

			float right = _imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = _imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			float offset_x = _imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f;
			float offset_y = _imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f;

			float x0 = left + offset_x;
			float y0 = top + offset_y;

			float x1 = x0;
			float y1 = bottom + offset_y;

			float x2 = right + offset_x;
			float y2 = y0;

			float x3 = x2;
			float y3 = y1;

			D3D.RenderTexturedQuadIgnoreMask(
				_videoTexture.Texture, 0xffffffff,
				x0, y0, x1, y1, x2, y2, x3, y3,
				_textureCoordinates.Top, _textureCoordinates.Right, _textureCoordinates.Bottom, _textureCoordinates.Left);
		}

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		public uint GetColorAtPosition(PointF position) {
			// not implemented
			return 0x00000000;
		}

		/// <summary>Unsupported (only for textured images)</summary>
		public void RenderBlock(RectangleF blockPositionAndSize, float thickness, RectangleF stickerPositionAndSize, float flipProgress, float rotationAngle, uint blockOpaqueColor, float opacity, bool dropShadow)
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported (only for textured images)</summary>
		public void RenderBlockBlank(RectangleF blockPositionAndSize, float thickness, float flipProgress, float rotationAngle, uint blockOpaqueColor, float opacity, bool dropShadow)
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported (only for textured images)</summary>
		public void RenderBlockSilhouette(RectangleF blockPositionAndSize, float thickness, float flipProgress, float rotationAngle, uint color)
		{
			throw new NotSupportedException();
		}

		DXVideoTexture _videoTexture;
		RectangleF _imageLocation;
		RectangleF _textureCoordinates;
	}
}
