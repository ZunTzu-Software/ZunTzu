// Copyright (c) 2022 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;

namespace ZunTzu.Graphics {
	/// <summary>
	/// Summary description for RectangularImage.
	/// </summary>
	public sealed class DXTexturedImage : IImage {

		/// <summary>Constructor.</summary>
		internal DXTexturedImage(DXTileSet tileSet, RectangleF imageLocation) {
			this.tileSet = tileSet;
			this.imageLocation = imageLocation;
			tesselate(-1);
		}

		/// <summary>Constructor for a one-shot only image (no mipmapping).</summary>
		internal DXTexturedImage(DXTileSet tileSet, RectangleF imageLocation, RectangleF renderingPositionAndSize) {
			this.tileSet = tileSet;
			this.imageLocation = imageLocation;

			int mipMapLevel = 0;
			float horizontalScaleFactor = renderingPositionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = renderingPositionAndSize.Height / imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int)tileSet.DetailLevel || (mipMapFactor < 0.5f && mipMapLevel < tileSet.MipMapLevelCount - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}
			
			tesselate(mipMapLevel);
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
			int mipMapLevel = 0;
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int)tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = tesselation[mipMapLevel];

			for(int i = 0; i < tess.Length; ++i) {
				DXQuad q = tileSet.Graphics.Quad;

				q.Texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);
				q.ModulationColor = modulationColor;
				q.TextureCoordinates = tess[i].TextureCoordinates;

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

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

				tileSet.Graphics.RenderTexturedQuad();
			}
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			int mipMapLevel = 0;
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int) tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = tesselation[mipMapLevel];

			for(int i = 0; i < tess.Length; ++i) {
				DXQuad q = tileSet.Graphics.Quad;

				q.Texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);
				q.ModulationColor = color;
				q.TextureCoordinates = tess[i].TextureCoordinates;

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

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

				tileSet.Graphics.RenderTexturedQuadSilhouette();
			}
		}

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void RenderIgnoreMask(RectangleF positionAndSize) {
			int mipMapLevel = 0;
			float horizontalScaleFactor = positionAndSize.Width / imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int) tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = tesselation[mipMapLevel];

			for(int i = 0; i < tess.Length; ++i) {
				DXQuad q = tileSet.Graphics.Quad;

				q.Texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);
				q.ModulationColor = 0xFFFFFFFF;
				q.TextureCoordinates = tess[i].TextureCoordinates;

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

				PointF offset = new PointF(
					imageLocation.Width * 0.5f * horizontalScaleFactor + positionAndSize.X - 0.5f,
					imageLocation.Height * 0.5f * verticalScaleFactor + positionAndSize.Y - 0.5f);

				q.Coord0 = new PointF(left + offset.X, top + offset.Y);
				q.Coord1 = new PointF(left + offset.X, bottom + offset.Y);
				q.Coord2 = new PointF(right + offset.X, top + offset.Y);
				q.Coord3 = new PointF(right + offset.X, bottom + offset.Y);

				tileSet.Graphics.RenderTexturedQuadIgnoreMask();
			}
		}

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		public uint GetColorAtPosition(PointF position) {
			for(int mipMapLevel = 0; mipMapLevel < tesselation.Length; ++mipMapLevel) {
				if(tesselation[mipMapLevel] != null) {
					foreach(Quad q in tesselation[mipMapLevel]) {
						if(q.Tile != null && q.Coordinates.Contains(position)) {
							return q.Tile.GetTexelColorAtAddress(new PointF(
								q.TextureCoordinates.X + q.TextureCoordinates.Width * (position.X - q.Coordinates.X) / q.Coordinates.Width,
								q.TextureCoordinates.Y + q.TextureCoordinates.Height * (position.Y - q.Coordinates.Y) / q.Coordinates.Height));
						}
					}
				}
			}
			return 0x00000000;
		}

		/// <summary>Splits the image surface into textured quads.</summary>
		/// <param name="thisMipMapLevelOnly">Set to -1 for all mipmap levels.</param>
		/// <remarks>
		///
		/// +--------------------+
		/// |         T          |
		/// +---+------------+---+
		/// |   |            |   |
		/// | L |     C      | R |
		/// |   |            |   |
		/// +---+------------+---+
		/// |         B          |
		/// +--------------------+
		///
		/// In case padding is necessay, only the textured zone C is tesselated.
		/// The padding zones are rendered with a single black quad.
		/// </remarks>
		private void tesselate(int thisMipMapLevelOnly) {
			int mipMapLevels = tileSet.MipMapLevelCount;
			tesselation = new Quad[mipMapLevels][];

			// create padding quads (the padding quads are shared between all mipmap levels)

			bool topPaddingRequired = imageLocation.Top < 0;
			bool bottomPaddingRequired = imageLocation.Bottom > tileSet.Size.Height;
			bool leftPaddingRequired = imageLocation.Left < 0;
			bool rightPaddingRequired = imageLocation.Right > tileSet.Size.Width;
			int paddingQuadsCount = 
				(topPaddingRequired ? 1 : 0) + (bottomPaddingRequired ? 1 : 0) +
				(leftPaddingRequired ? 1 : 0) + (rightPaddingRequired ? 1 : 0);
			Quad[] paddingQuads = new Quad[paddingQuadsCount];

			if(topPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = imageLocation.Left;
				quad.Coordinates.Width = imageLocation.Width;
				quad.Coordinates.Y = imageLocation.Top;
				quad.Coordinates.Height = -imageLocation.Top;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= imageLocation.X + imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= imageLocation.Y + imageLocation.Height * 0.5f;
				paddingQuads[0] = quad;
			}
			if(bottomPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = imageLocation.Left;
				quad.Coordinates.Width = imageLocation.Width;
				quad.Coordinates.Y = tileSet.Size.Height;
				quad.Coordinates.Height = imageLocation.Bottom - tileSet.Size.Height;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= imageLocation.X + imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= imageLocation.Y + imageLocation.Height * 0.5f;
				paddingQuads[topPaddingRequired ? 1 : 0] = quad;
			}
			if(leftPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = imageLocation.Left;
				quad.Coordinates.Width = -imageLocation.Left;
				quad.Coordinates.Y = Math.Max(0.0f, imageLocation.Top);
				quad.Coordinates.Height = Math.Min(imageLocation.Bottom, tileSet.Size.Height) - quad.Coordinates.Y;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= imageLocation.X + imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= imageLocation.Y + imageLocation.Height * 0.5f;
				paddingQuads[paddingQuadsCount - (rightPaddingRequired ? 2 : 1)] = quad;
			}
			if(rightPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = tileSet.Size.Width;
				quad.Coordinates.Width = imageLocation.Right - tileSet.Size.Width;
				quad.Coordinates.Y = Math.Max(0.0f, imageLocation.Top);
				quad.Coordinates.Height = Math.Min(imageLocation.Bottom, tileSet.Size.Height) - quad.Coordinates.Y;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= imageLocation.X + imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= imageLocation.Y + imageLocation.Height * 0.5f;
				paddingQuads[paddingQuadsCount - 1] = quad;
			}

			// create quads for each mipmap levels

			SizeF tileSize = new SizeF(254.0f, 254.0f);
			SizeF textureSize = new SizeF(256.0f, 256.0f);
			for(int mipMapLevel = 0; mipMapLevel < mipMapLevels; ++mipMapLevel) {
				if(mipMapLevel >= (int)tileSet.DetailLevel &&
					(thisMipMapLevelOnly == -1 || thisMipMapLevelOnly == mipMapLevel))
				{
					Point upperLeftTile = new Point(
						(int) Math.Floor(Math.Max(0.0f, imageLocation.Left) / tileSize.Width),
						(int) Math.Floor(Math.Max(0.0f, imageLocation.Top) / tileSize.Height));
					Size tileCount = new Size(
						(int) (Math.Floor((Math.Min(tileSet.Size.Width, imageLocation.Right) - 1) / tileSize.Width)) - upperLeftTile.X + 1,
						(int) (Math.Floor((Math.Min(tileSet.Size.Height, imageLocation.Bottom) - 1) / tileSize.Height)) - upperLeftTile.Y + 1);
					tesselation[mipMapLevel] = new Quad[paddingQuadsCount + Math.Max(0, tileCount.Width) * Math.Max(0, tileCount.Height)];

					for(int i = 0; i < paddingQuadsCount; ++i) {
						tesselation[mipMapLevel][i] = paddingQuads[i];
					}

					for(int y = 0; y < tileCount.Height; ++y) {
						for(int x = 0; x < tileCount.Width; ++x) {
							PointF tileUpperLeftCorner = new PointF(
								(upperLeftTile.X + x) * tileSize.Width,
								(upperLeftTile.Y + y) * tileSize.Height);

							Quad quad = new Quad();

							quad.Tile = tileSet.Tiles[mipMapLevel][upperLeftTile.X + x, upperLeftTile.Y + y];

							quad.Coordinates.X = Math.Max(tileUpperLeftCorner.X, imageLocation.Left);
							quad.Coordinates.Width = Math.Min(tileUpperLeftCorner.X + tileSize.Width, imageLocation.Right) - quad.Coordinates.X;
							quad.Coordinates.Y = Math.Max(tileUpperLeftCorner.Y, imageLocation.Top);
							quad.Coordinates.Height = Math.Min(tileUpperLeftCorner.Y + tileSize.Height, imageLocation.Bottom) - quad.Coordinates.Y;

							quad.TextureCoordinates.X = (quad.Coordinates.X % tileSize.Width + (textureSize.Width - tileSize.Width)*0.5f) / textureSize.Width;
							quad.TextureCoordinates.Width = quad.Coordinates.Width / textureSize.Width;
							quad.TextureCoordinates.Y = (quad.Coordinates.Y % tileSize.Height + (textureSize.Width - tileSize.Width)*0.5f) / textureSize.Height;
							quad.TextureCoordinates.Height = quad.Coordinates.Height / textureSize.Height;

							quad.Coordinates.X -= imageLocation.X + imageLocation.Width * 0.5f;
							quad.Coordinates.Y -= imageLocation.Y + imageLocation.Height * 0.5f;

							tesselation[mipMapLevel][paddingQuadsCount + y * tileCount.Width + x] = quad;
						}
					}
				}

				tileSize.Width *= 2.0f;
				tileSize.Height *= 2.0f;
				textureSize.Width *= 2.0f;
				textureSize.Height *= 2.0f;
			}
		}

		private DXTileSet tileSet;
		private RectangleF imageLocation;

		private struct Quad {
			public DXTile Tile;
			public RectangleF Coordinates;
			public RectangleF TextureCoordinates;

			/// <summary>This is to get rid of the annoying warning "is never assigned to, and will always have its default value"</summary>
			private Quad(RectangleF dummy) { Tile = null; Coordinates = TextureCoordinates = dummy; }
		}
		private Quad[][] tesselation;
	}
}
