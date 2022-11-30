// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Graphics {
	/// <summary>
	/// Summary description for DXMonochromaticImage.
	/// </summary>
	public sealed class DXMonochromaticImage : IImage {

		/// <summary>Constructor.</summary>
		internal DXMonochromaticImage() {}

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
			float x0, y0, x1, y1, x2, y2, x3, y3;

			if(rotationAngle == 0.0f) {
				x0 = positionAndSize.X - 0.5f;
				y0 = positionAndSize.Y - 0.5f;

				x1 = x0;
				y1 = positionAndSize.Bottom - 0.5f;

				x2 = positionAndSize.Right - 0.5f;
				y2 = y0;

				x3 = x2;
				y3 = y1;
			} else {
				float x = positionAndSize.X - 0.5f;
				float y = positionAndSize.Y - 0.5f;
				float hw = positionAndSize.Width * 0.5f;
				float hh = positionAndSize.Height * 0.5f;

				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float) Math.Sin(-rotationAngle);
				float cos = (float) Math.Cos(-rotationAngle);
				float wc = hw * cos;
				float ws = hw * sin;
				float hc = hh * cos;
				float hs = hh * sin;

				float rotated_x0 = hs - wc;
				float rotated_y0 = -hc - wc;
				float rotated_x2 = hs + wc;
				float rotated_y2 = -hc + ws;

				x0 = (hw + x) + rotated_x0;
				y0 = (hh + y) + rotated_y0;

				x1 = (hw + x) - rotated_x2;
				y1 = (hh + y) - rotated_y2;

				x2 = (hw + x) + rotated_x2;
				y2 = (hh + y) + rotated_y2;

				x3 = (hw + x) - rotated_x0;
				y3 = (hh + y) - rotated_y0;
			}

			D3D.RenderMonochromaticQuad(
				modulationColor,
				x0, y0, x1, y1, x2, y2, x3, y3);
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			float x0, y0, x1, y1, x2, y2, x3, y3;

			if (rotationAngle == 0.0f)
			{
				x0 = positionAndSize.X - 0.5f;
				y0 = positionAndSize.Y - 0.5f;

				x1 = x0;
				y1 = positionAndSize.Bottom - 0.5f;

				x2 = positionAndSize.Right - 0.5f;
				y2 = y0;

				x3 = x2;
				y3 = y1;
			}
			else
			{
				float x = positionAndSize.X - 0.5f;
				float y = positionAndSize.Y - 0.5f;
				float hw = positionAndSize.Width * 0.5f;
				float hh = positionAndSize.Height * 0.5f;

				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float)Math.Sin(-rotationAngle);
				float cos = (float)Math.Cos(-rotationAngle);
				float wc = hw * cos;
				float ws = hw * sin;
				float hc = hh * cos;
				float hs = hh * sin;

				float rotated_x0 = hs - wc;
				float rotated_y0 = -hc - wc;
				float rotated_x2 = hs + wc;
				float rotated_y2 = -hc + ws;

				x0 = (hw + x) + rotated_x0;
				y0 = (hh + y) + rotated_y0;

				x1 = (hw + x) - rotated_x2;
				y1 = (hh + y) - rotated_y2;

				x2 = (hw + x) + rotated_x2;
				y2 = (hh + y) + rotated_y2;

				x3 = (hw + x) - rotated_x0;
				y3 = (hh + y) - rotated_y0;
			}

			D3D.RenderMonochromaticQuad(color, x0, y0, x1, y1, x2, y2, x3, y3);
		}

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void RenderIgnoreMask(RectangleF positionAndSize) {
			Render(positionAndSize);
		}

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		public uint GetColorAtPosition(PointF position) {
			return 0xFFFFFFFF;
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
	}
}
