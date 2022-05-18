// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Graphics {
	/// <summary>
	/// Summary description for DXMonochromaticImage.
	/// </summary>
	public sealed class DXMonochromaticImage : IImage {

		/// <summary>Constructor.</summary>
		internal DXMonochromaticImage(DXGraphics graphics) {
			this.graphics = graphics;
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
			DXQuad quad = graphics.Quad;

			quad.ModulationColor = modulationColor;

			if(rotationAngle == 0.0f) {
				quad.Coord0 = new PointF(positionAndSize.X - 0.5f, positionAndSize.Y - 0.5f);
				quad.Coord1 = new PointF(quad.Coord0.X, positionAndSize.Bottom - 0.5f);
				quad.Coord2 = new PointF(positionAndSize.Right - 0.5f, quad.Coord0.Y);
				quad.Coord3 = new PointF(quad.Coord2.X, quad.Coord1.Y);
			} else {
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float) Math.Sin(-rotationAngle);
				float cos = (float) Math.Cos(-rotationAngle);
				float wc = (positionAndSize.Width * 0.5f) * cos;
				float ws = (positionAndSize.Width * 0.5f) * sin;
				float hc = (positionAndSize.Height * 0.5f) * cos;
				float hs = (positionAndSize.Height * 0.5f) * sin;

				PointF rotatedCoord0 = new PointF(hs - wc, -hc - wc);
				PointF rotatedCoord1 = new PointF(hs + wc, -hc + ws);

				quad.Coord0 = new PointF(
					(rotatedCoord0.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(rotatedCoord0.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord1 = new PointF(
					(-rotatedCoord1.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(-rotatedCoord1.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord2 = new PointF(
					(rotatedCoord1.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(rotatedCoord1.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord3 = new PointF(
					(-rotatedCoord0.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(-rotatedCoord0.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
			}

			graphics.RenderMonochromaticQuad();
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			DXQuad quad = graphics.Quad;

			quad.ModulationColor = color;

			if(rotationAngle == 0.0f) {
				quad.Coord0 = new PointF(positionAndSize.X - 0.5f, positionAndSize.Y - 0.5f);
				quad.Coord1 = new PointF(quad.Coord0.X, positionAndSize.Bottom - 0.5f);
				quad.Coord2 = new PointF(positionAndSize.Right - 0.5f, quad.Coord0.Y);
				quad.Coord3 = new PointF(quad.Coord2.X, quad.Coord1.Y);
			} else {
				// rotation:
				// x <- x * cos - y * sin
				// y <- x * sin + y * cos
				float sin = (float) Math.Sin(-rotationAngle);
				float cos = (float) Math.Cos(-rotationAngle);
				float wc = (positionAndSize.Width * 0.5f) * cos;
				float ws = (positionAndSize.Width * 0.5f) * sin;
				float hc = (positionAndSize.Height * 0.5f) * cos;
				float hs = (positionAndSize.Height * 0.5f) * sin;

				PointF rotatedCoord0 = new PointF(hs - wc, -hc - wc);
				PointF rotatedCoord1 = new PointF(hs + wc, -hc + ws);

				quad.Coord0 = new PointF(
					(rotatedCoord0.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(rotatedCoord0.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord1 = new PointF(
					(-rotatedCoord1.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(-rotatedCoord1.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord2 = new PointF(
					(rotatedCoord1.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(rotatedCoord1.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
				quad.Coord3 = new PointF(
					(-rotatedCoord0.X + positionAndSize.Width * 0.5f) + (positionAndSize.X - 0.5f),
					(-rotatedCoord0.Y + positionAndSize.Height * 0.5f) + (positionAndSize.Y - 0.5f));
			}

			graphics.RenderMonochromaticQuad();
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

		private DXGraphics graphics;
	}
}
