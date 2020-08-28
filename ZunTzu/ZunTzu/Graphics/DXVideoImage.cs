// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;

namespace ZunTzu.Graphics {

	/// <summary>Image extracted from a video texture.</summary>
	public sealed class DXVideoImage : IImage {

		/// <summary>Constructor for a one-shot only image (no mipmapping).</summary>
		internal DXVideoImage(DXVideoTexture videoTexture, RectangleF imageLocation) {
			this.videoTexture = videoTexture;
			this.imageLocation = imageLocation;
			SizeF videoTextureSize = new SizeF(videoTexture.Size);
			this.textureCoordinates = new RectangleF(
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
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;

			DXQuad q = videoTexture.Graphics.Quad;

			q.Texture = videoTexture.Texture;
			q.ModulationColor = modulationColor;
			q.TextureCoordinates = textureCoordinates;

			float right = imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			PointF offset = new PointF(
				imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f,
				imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f);

			if(rotationAngle != 0.0f) {
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float) Math.Sin(-rotationAngle);
				float cos = (float) Math.Cos(-rotationAngle);

				q.Coord0 = new PointF(left * cos - top * sin + offset.X, left * sin + top * cos + offset.Y);
				q.Coord1 = new PointF(left * cos - bottom * sin + offset.X, left * sin + bottom * cos + offset.Y);
				q.Coord2 = new PointF(right * cos - top * sin + offset.X, right * sin + top * cos + offset.Y);
				q.Coord3 = new PointF(right * cos - bottom * sin + offset.X, right * sin + bottom * cos + offset.Y);
			} else {
				q.Coord0 = new PointF(left + offset.X, top + offset.Y);
				q.Coord1 = new PointF(left + offset.X, bottom + offset.Y);
				q.Coord2 = new PointF(right + offset.X, top + offset.Y);
				q.Coord3 = new PointF(right + offset.X, bottom + offset.Y);
			}

			videoTexture.Graphics.RenderTexturedQuad();
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;

			DXQuad q = videoTexture.Graphics.Quad;

			q.Texture = videoTexture.Texture;
			q.ModulationColor = color;
			q.TextureCoordinates = textureCoordinates;

			float right = imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			PointF offset = new PointF(
				imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f,
				imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f);

			if(rotationAngle != 0.0f) {
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float) Math.Sin(-rotationAngle);
				float cos = (float) Math.Cos(-rotationAngle);

				q.Coord0 = new PointF(left * cos - top * sin + offset.X, left * sin + top * cos + offset.Y);
				q.Coord1 = new PointF(left * cos - bottom * sin + offset.X, left * sin + bottom * cos + offset.Y);
				q.Coord2 = new PointF(right * cos - top * sin + offset.X, right * sin + top * cos + offset.Y);
				q.Coord3 = new PointF(right * cos - bottom * sin + offset.X, right * sin + bottom * cos + offset.Y);
			} else {
				q.Coord0 = new PointF(left + offset.X, top + offset.Y);
				q.Coord1 = new PointF(left + offset.X, bottom + offset.Y);
				q.Coord2 = new PointF(right + offset.X, top + offset.Y);
				q.Coord3 = new PointF(right + offset.X, bottom + offset.Y);
			}

			videoTexture.Graphics.RenderTexturedQuadSilhouette();
		}

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void RenderIgnoreMask(RectangleF positionAndSize) {
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;

			DXQuad q = videoTexture.Graphics.Quad;

			q.Texture = videoTexture.Texture;
			q.ModulationColor = 0xffffffff;
			q.TextureCoordinates = textureCoordinates;

			float right = imageLocation.Width * 0.5f * horizontalScaleFactor;
			float left = -right;
			float bottom = imageLocation.Height * 0.5f * verticalScaleFactor;
			float top = -bottom;

			PointF offset = new PointF(
				imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f,
				imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f);

			q.Coord0 = new PointF(left + offset.X, top + offset.Y);
			q.Coord1 = new PointF(left + offset.X, bottom + offset.Y);
			q.Coord2 = new PointF(right + offset.X, top + offset.Y);
			q.Coord3 = new PointF(right + offset.X, bottom + offset.Y);

			videoTexture.Graphics.RenderTexturedQuadIgnoreMask();
		}

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		public uint GetColorAtPosition(PointF position) {
			// not implemented
			return 0x00000000;
		}

		private DXVideoTexture videoTexture;
		private RectangleF imageLocation;
		private RectangleF textureCoordinates;
	}
}
