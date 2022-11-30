// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Graphics
{
    /// <summary>
    /// Summary description for RectangularImage.
    /// </summary>
    public sealed class DXTexturedImage : IImage {

		/// <summary>Constructor.</summary>
		internal DXTexturedImage(DXTileSet tileSet, RectangleF imageLocation) {
			_tileSet = tileSet;
			_imageLocation = imageLocation;
			tesselate(-1);
		}

		/// <summary>Constructor for a one-shot only image (no mipmapping).</summary>
		internal DXTexturedImage(DXTileSet tileSet, RectangleF imageLocation, RectangleF renderingPositionAndSize) {
			_tileSet = tileSet;
			_imageLocation = imageLocation;

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
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int)_tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < _tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = _tesselation[mipMapLevel];

			for(int i = 0; i < tess.Length; ++i) {
				D3DTexture texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

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
					float sin = (float) Math.Sin(-rotationAngle);
					float cos = (float) Math.Cos(-rotationAngle);

					x0 = left * cos - top * sin + offset_x;
					y0 = left * sin + top * cos + offset_y;

					x1 = left * cos - bottom * sin + offset_x;
					y1 = left * sin + bottom * cos + offset_y;

					x2 = right * cos - top * sin + offset_x;
					y2 = right * sin + top * cos + offset_y;

					x3 = right * cos - bottom * sin + offset_x;
					y3 = right * sin + bottom * cos + offset_y;
				}

				RectangleF tex_coords = tess[i].TextureCoordinates;

				D3D.RenderTexturedQuad(
                    texture, modulationColor,
                    x0, y0, x1, y1, x2, y2, x3, y3,
                    tex_coords.Top, tex_coords.Right, tex_coords.Bottom, tex_coords.Left);
            }
		}

		/// <summary>Render the silhouette for this image at the given position and size.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this image.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderSilhouette(RectangleF positionAndSize, float rotationAngle, uint color) {
			int mipMapLevel = 0;
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int) _tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < _tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = _tesselation[mipMapLevel];

			for(int i = 0; i < tess.Length; ++i) {
				D3DTexture texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

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

				RectangleF tex_coords = tess[i].TextureCoordinates;

				D3D.RenderTexturedQuadSilhouette(
					texture, color,
					x0, y0, x1, y1, x2, y2, x3, y3,
					tex_coords.Top, tex_coords.Right, tex_coords.Bottom, tex_coords.Left);
			}
		}

		/// <summary>Render this image at the given position and size, ignoring any transparency mask.</summary>
		/// <param name="positionAndSize">Position and size of the rendered image.</param>
		public void RenderIgnoreMask(RectangleF positionAndSize) {
			int mipMapLevel = 0;
			float horizontalScaleFactor = positionAndSize.Width / _imageLocation.Width;
			float verticalScaleFactor = positionAndSize.Height / _imageLocation.Height;
			float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
			while(mipMapLevel < (int) _tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < _tesselation.Length - 1)) {
				++mipMapLevel;
				mipMapFactor *= 2.0f;
			}

			Quad[] tess = _tesselation[mipMapLevel];

			for (int i = 0; i < tess.Length; ++i)
			{
				D3DTexture texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);

				float left = tess[i].Coordinates.Left * horizontalScaleFactor;
				float right = tess[i].Coordinates.Right * horizontalScaleFactor;
				float top = tess[i].Coordinates.Top * verticalScaleFactor;
				float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

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

				RectangleF tex_coords = tess[i].TextureCoordinates;

				D3D.RenderTexturedQuadIgnoreMask(
					texture, 0xFFFFFFFF,
					x0, y0, x1, y1, x2, y2, x3, y3,
					tex_coords.Top, tex_coords.Right, tex_coords.Bottom, tex_coords.Left);
			}
		}

		/// <summary>Returns the color of the texel at the given position.</summary>
		/// <param name="position">Position in model coordinates relative to the center of this image.</param>
		/// <returns>A color in A8R8G8B8 format.</returns>
		public uint GetColorAtPosition(PointF position) {
			for(int mipMapLevel = 0; mipMapLevel < _tesselation.Length; ++mipMapLevel) {
				if(_tesselation[mipMapLevel] != null) {
					foreach(Quad q in _tesselation[mipMapLevel]) {
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

		/// <summary>Renders a block with this image as a sticker at the given position and size.</summary>
		/// <param name="blockPositionAndSize">Position of the block when lying down, in model coordinates relative to the center of this image.</param>
		/// <param name="thickness">Thickness of the block, in model coordinates.</param>
		/// <param name="stickerPositionAndSize">Position of the sticker when lying down, in model coordinates relative to the center of this image.</param>
		/// <param name="flipProgress">Progress of the 'flip' animation: 0.0 for a fully lying down block, 1.0 for a fully standing up block.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this block when lying down.</param>
		/// <param name="blockOpaqueColor">Color of the block when lying down in X8R8G8B8 format. Shading is automatically added to the edges when standing up.</param>
		/// <param name="opacity">Transparency of the block: 0.0 for a fully transparent block, 1.0 for a fully opaque block.</param>
		/// <param name="dropShadow">True to display the shadow cast by this block on the board.</param>
		public void RenderBlock(RectangleF blockPositionAndSize, float thickness, RectangleF stickerPositionAndSize, float flipProgress, float rotationAngle, uint blockOpaqueColor, float opacity, bool dropShadow)
		{
			renderBlock(BlockRenderMode.Sticker, blockPositionAndSize, thickness, stickerPositionAndSize, flipProgress, rotationAngle, blockOpaqueColor, opacity, dropShadow);
		}

		/// <summary>Renders an unlabelled block at the given position and size. This is meant to render the back of a hidden block.</summary>
		/// <param name="blockPositionAndSize">Position of the block when lying down, in model coordinates relative to the center of this image.</param>
		/// <param name="thickness">Thickness of the block, in model coordinates.</param>
		/// <param name="flipProgress">Progress of the 'flip' animation: 0.0 for a fully lying down block, 1.0 for a fully standing up block.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this block when lying down.</param>
		/// <param name="blockOpaqueColor">Color of the block when lying down in X8R8G8B8 format. Shading is automatically added to the edges when standing up.</param>
		/// <param name="opacity">Transparency of the block: 0.0 for a fully transparent block, 1.0 for a fully opaque block.</param>
		/// <param name="dropShadow">True to display the shadow cast by this block on the board.</param>
		public void RenderBlockBlank(RectangleF blockPositionAndSize, float thickness, float flipProgress, float rotationAngle, uint blockOpaqueColor, float opacity, bool dropShadow)
		{
			renderBlock(BlockRenderMode.Blank, blockPositionAndSize, thickness, RectangleF.Empty, flipProgress, rotationAngle, blockOpaqueColor, opacity, dropShadow);
		}

		/// <summary>Renders the silhouette of a block at the given position and size.</summary>
		/// <param name="blockPositionAndSize">Position of the block when lying down, in model coordinates relative to the center of this image.</param>
		/// <param name="thickness">Thickness of the block, in model coordinates.</param>
		/// <param name="flipProgress">Progress of the 'flip' animation: 0.0 for a fully lying down block, 1.0 for a fully standing up block.</param>
		/// <param name="rotationAngle">Rotation angle. The rotation axis goes through the center of this block when lying down.</param>
		/// <param name="color">Color of the silhouette in A8R8G8B8 format.</param>
		public void RenderBlockSilhouette(RectangleF blockPositionAndSize, float thickness, float flipProgress, float rotationAngle, uint color)
		{
			renderBlock(BlockRenderMode.Silhouette, blockPositionAndSize, thickness, RectangleF.Empty, flipProgress, rotationAngle, color, 1.0f, false);
		}

		enum BlockRenderMode { Sticker, Blank, Silhouette }

		void renderBlock(BlockRenderMode mode, RectangleF blockPositionAndSize, float thickness, RectangleF stickerPositionAndSize, float flipProgress, float rotationAngle, uint blockOpaqueColor, float opacity, bool dropShadow)
		{
			Debug.Assert(thickness >= 0.0f);
			Debug.Assert(flipProgress >= 0.0f && flipProgress <= 1.0f);
			Debug.Assert(rotationAngle > (float)-Math.PI && rotationAngle <= (float)Math.PI);
			Debug.Assert(opacity >= 0.0f && opacity <= 1.0f);

			//    A'                B'
			//      +-------______+
			//     /             /|         A               B
			//  A +-------______+ | B        +-------------+
			//    |  a       b  | |          |  a       b  |
			//    |   +---__+   | |          |   +-----+   |
			//    |   |     |   | |          |   |     |   |
			//    |   |     |   | |          |   |     |   |
			//    |   +---__+   | + C'       |   +-----+   |
			//    |  d       c  |/           |  d       c  |
			//  D +-------______+ C          +-------------+
			//                              D               C
			//      standing up                 lying down
			//  (flipProgress == 1.0)     (flipProgress == 0.0)

			uint opacityAlpha = (uint)(opacity * 255) << 24;
			uint blockColor = opacityAlpha | (blockOpaqueColor & 0x00ffffff);

			unsafe
			{
				PointF* p = stackalloc PointF[4 * 3]; // first A..D, then a..d, then A'..D'

				// Lying down coordinates, before rotation

				PointF* A = &p[0];
				A->X = blockPositionAndSize.Left - 0.5f;
				A->Y = blockPositionAndSize.Top - 0.5f;

				PointF* B = &p[1];
				B->X = blockPositionAndSize.Right - 0.5f;
				B->Y = blockPositionAndSize.Top - 0.5f;

				PointF* C = &p[2];
				C->X = blockPositionAndSize.Right - 0.5f;
				C->Y = blockPositionAndSize.Bottom - 0.5f;

				PointF* D = &p[3];
				D->X = blockPositionAndSize.Left - 0.5f;
				D->Y = blockPositionAndSize.Bottom - 0.5f;

				PointF* a = &p[4];
				a->X = stickerPositionAndSize.Left - 0.5f;
				a->Y = stickerPositionAndSize.Top - 0.5f;

				PointF* b = &p[5];
				b->X = stickerPositionAndSize.Right - 0.5f;
				b->Y = stickerPositionAndSize.Top - 0.5f;

				PointF* c = &p[6];
				c->X = stickerPositionAndSize.Right - 0.5f;
				c->Y = stickerPositionAndSize.Bottom - 0.5f;

				PointF* d = &p[7];
				d->X = stickerPositionAndSize.Left - 0.5f;
				d->Y = stickerPositionAndSize.Bottom - 0.5f;

				PointF* A_prime = &p[8];
				PointF* B_prime = &p[9];
				PointF* C_prime = &p[10];
				PointF* D_prime = &p[11];

				// Lying down coordinates, after rotation

				float center_x = blockPositionAndSize.Left + 0.5f * blockPositionAndSize.Width - 0.5f;
				float center_y = blockPositionAndSize.Top + 0.5f * blockPositionAndSize.Height - 0.5f;

				if (rotationAngle != 0.0f)
				{
					float sin = (float)Math.Sin(-rotationAngle);
					float cos = (float)Math.Cos(-rotationAngle);

					for (int i = 0; i < 4 * 2; ++i) // rotate A..D and a..d
					{
						float x = p[i].X - center_x;
						float y = p[i].Y - center_y;
						p[i].X = x * cos - y * sin + center_x;
						p[i].Y = x * sin + y * cos + center_y;
					}
				}

				// Standing up coordinates, axonometric projection

				float skewingFactor = 0.1f * flipProgress;
				for (int i = 0; i < 4 * 2; ++i) // skew A..D and a..d vertically
				{
					float x = p[i].X - center_x;
					p[i].Y += x * skewingFactor;
				}

				float thicknessTranslation = thickness * flipProgress;
				for (int i = 0; i < 4; ++i) // set A'..D' by translation from A..D
				{
					p[i + 8].X = p[i].X + thicknessTranslation;
					p[i + 8].Y = p[i].Y - thicknessTranslation;
				}

				// Render drop shadow
				// (unless the render mode is 'silhouette' or
				//  the block is in the middle of rotating or flipping)

				if (dropShadow &&
					opacity == 1.0f && // no shadow for drag-drop ghost
					(flipProgress == 0.0f || flipProgress == 1.0f)) // no shadow for flipping blocks
				{
					//    I'            J'
					//      +-----____+
					//     /         /|            I +--------+   J
					//  I +-----____+ | J            |        |\
					//    |         | |              |        | +
					//    |         | + K'           |        | |
					//    |         |/ \           L +--------+ | K
					//  L +-----____+ K +             \        \|
					//     \         \ /               +--------+
					//      +-----____+

					bool stillRotating = true;
					PointF* J = B;
					PointF* K = C;
					PointF* L = D;
					PointF* K_prime = C_prime;

					if (rotationAngle == 0.0f)
					{
						stillRotating = false;
					}
					else if (rotationAngle == 0.5f * (float)Math.PI)
					{
						stillRotating = false;
						J = C;
						K = D;
						L = A;
						K_prime = D_prime;
					}
					else if (rotationAngle == (float)Math.PI)
					{
						stillRotating = false;
						J = D;
						K = A;
						L = B;
						K_prime = A_prime;
					}
					else if (rotationAngle == -0.5f * (float)Math.PI)
					{
						stillRotating = false;
						J = A;
						K = B;
						L = C;
						K_prime = B_prime;
					}

					if (!stillRotating)
					{
						if (flipProgress == 0.0f)
						{
							for (int pass = 0; pass < 4 && pass * 2.0f < thickness; ++pass) // several passes for a softer shadow
							{
								float offset = pass * -2.0f;
								D3D.RenderGradientQuad(0x10000000, 0x04000000, J->X, J->Y, K->X, K->Y, J->X + thickness + offset, J->Y + thickness, K->X + thickness + offset, K->Y + thickness + offset);
								D3D.RenderGradientQuad(0x10000000, 0x04000000, K->X, K->Y, L->X, L->Y, K->X + thickness + offset, K->Y + thickness + offset, L->X + thickness, L->Y + thickness + offset);
							}
						}
						else // flipProgress == 1.0f
						{
							float h = blockPositionAndSize.Height * 0.4f;
							for (int pass = 0; pass < 4 && pass * 8.0f < h; ++pass) // several passes for a softer shadow
							{
								float offset = pass * -8.0f;
								D3D.RenderGradientQuad(0x10000000, 0x00000000, K_prime->X, K_prime->Y, K->X, K->Y, K_prime->X + h + offset, K_prime->Y + h, K->X + h + offset, K->Y + h + offset);
								D3D.RenderGradientQuad(0x10000000, 0x00000000, K->X, K->Y, L->X, L->Y, K->X + h + offset, K->Y + h + offset, L->X + h, L->Y + h + offset);
							}
						}
					}
				}

				if (mode == BlockRenderMode.Sticker)
				{
					// Render ABCD minus abcd

					//    A           B
					//     +----------+
					//     |\ a    b /|
					//     | +------+ |
					//     | |      | |
					//     | +------+ |
					//     |/ d    c \|
					//     +----------+
					//    D           C

					D3D.RenderMonochromaticQuad(blockColor, A->X, A->Y, D->X, D->Y, a->X, a->Y, d->X, d->Y);
					D3D.RenderMonochromaticQuad(blockColor, B->X, B->Y, A->X, A->Y, b->X, b->Y, a->X, a->Y);
					D3D.RenderMonochromaticQuad(blockColor, C->X, C->Y, B->X, B->Y, c->X, c->Y, b->X, b->Y);
					D3D.RenderMonochromaticQuad(blockColor, D->X, D->Y, C->X, C->Y, d->X, d->Y, c->X, c->Y);
				}
				else
				{
					// Render ABCD

					//    A           B
					//     +----------+
					//     |          |
					//     |          |
					//     |          |
					//     |          |
					//     +----------+
					//    D           C

					D3D.RenderMonochromaticQuad(blockColor, A->X, A->Y, D->X, D->Y, B->X, B->Y, C->X, C->Y);
				}

				// Render edges A'B'AB + B'C'BC + C'D'CD + D'A'DA  

				//    A'           B'
				//      +--------+
				//     /.       /|
				//  A +--------+ | B
				//    | .      | |
				// D' | +..... | + C'
				//    |.       |/
				//  D +--------+ C

				if (flipProgress > 0.0f)
				{
					uint lighterColor = blockColor;
					uint darkerColor = blockColor;

					if (mode != BlockRenderMode.Silhouette)
					{
						double mix;

						if (rotationAngle < -Math.PI * 0.75f)
						{
							// Render C'D'CD + D'A'DA
							mix = (rotationAngle + Math.PI * 1.25f) / (Math.PI * 0.5f);
						}
						else if (rotationAngle < -Math.PI * 0.25f)
						{
							// Render D'A'DA + A'B'AB
							mix = (rotationAngle + Math.PI * 0.75f) / (Math.PI * 0.5f);
						}
						else if (rotationAngle < Math.PI * 0.25f)
						{
							// Render A'B'AB + B'C'BC
							mix = (rotationAngle + Math.PI * 0.25f) / (Math.PI * 0.5f);
						}
						else if (rotationAngle < Math.PI * 0.75f)
						{
							// Render B'C'BC + C'D'CD 
							mix = (rotationAngle - Math.PI * 0.25f) / (Math.PI * 0.5f);
						}
						else
						{
							// Render C'D'CD + D'A'DA
							mix = (rotationAngle - Math.PI * 0.75f) / (Math.PI * 0.5f);
						}

						double lightMix = 1.0 - mix * 0.5;
						double darkMix = 0.5 + mix * 0.5;

						double red = ((blockColor & 0x00ff0000) >> 16) / 255.0;
						double green = ((blockColor & 0x0000ff00) >> 8) / 255.0;
						double blue = ((blockColor & 0x000000ff) >> 0) / 255.0;

						double lighterRed = (1.0 - lightMix) + lightMix * red;
						double lighterGreen = (1.0 - lightMix) + lightMix * green;
						double lighterBlue = (1.0 - lightMix) + lightMix * blue;
						lighterColor =
							opacityAlpha |
							((uint)(255 * lighterRed) << 16) |
							((uint)(255 * lighterGreen) << 8) |
							((uint)(255 * lighterBlue) << 0);

						double darkerRed = darkMix * red;
						double darkerGreen = darkMix * green;
						double darkerBlue = darkMix * blue;
						darkerColor =
							opacityAlpha |
							((uint)(255 * darkerRed) << 16) |
							((uint)(255 * darkerGreen) << 8) |
							((uint)(255 * darkerBlue) << 0);
					}

					if (rotationAngle < -Math.PI * 0.75f || rotationAngle >= Math.PI * 0.75f)
					{
						// Render C'D'CD + D'A'DA
						D3D.RenderMonochromaticQuad(lighterColor, C_prime->X, C_prime->Y, C->X, C->Y, D_prime->X, D_prime->Y, D->X, D->Y);
						D3D.RenderMonochromaticQuad(darkerColor, D_prime->X, D_prime->Y, D->X, D->Y, A_prime->X, A_prime->Y, A->X, A->Y);
					}
					else if (rotationAngle < -Math.PI * 0.25f)
					{
						// Render D'A'DA + A'B'AB
						D3D.RenderMonochromaticQuad(lighterColor, D_prime->X, D_prime->Y, D->X, D->Y, A_prime->X, A_prime->Y, A->X, A->Y);
						D3D.RenderMonochromaticQuad(darkerColor, A_prime->X, A_prime->Y, A->X, A->Y, B_prime->X, B_prime->Y, B->X, B->Y);
					}
					else if (rotationAngle < Math.PI * 0.25f)
					{
						// Render A'B'AB + B'C'BC
						D3D.RenderMonochromaticQuad(lighterColor, A_prime->X, A_prime->Y, A->X, A->Y, B_prime->X, B_prime->Y, B->X, B->Y);
						D3D.RenderMonochromaticQuad(darkerColor, B_prime->X, B_prime->Y, B->X, B->Y, C_prime->X, C_prime->Y, C->X, C->Y);
					}
					else
					{
						// Render B'C'BC + C'D'CD 
						D3D.RenderMonochromaticQuad(lighterColor, B_prime->X, B_prime->Y, B->X, B->Y, C_prime->X, C_prime->Y, C->X, C->Y);
						D3D.RenderMonochromaticQuad(darkerColor, C_prime->X, C_prime->Y, C->X, C->Y, D_prime->X, D_prime->Y, D->X, D->Y);
					}
				}

				// Render abcd sticker

				if (mode == BlockRenderMode.Sticker)
				{
					int mipMapLevel = 0;
					float horizontalScaleFactor = stickerPositionAndSize.Width / _imageLocation.Width;
					float verticalScaleFactor = stickerPositionAndSize.Height / _imageLocation.Height;
					float mipMapFactor = Math.Max(horizontalScaleFactor, verticalScaleFactor);
					while (mipMapLevel < (int)_tileSet.DetailLevel || (mipMapFactor <= 0.5f && mipMapLevel < _tesselation.Length - 1))
					{
						++mipMapLevel;
						mipMapFactor *= 2.0f;
					}

					Quad[] tess = _tesselation[mipMapLevel];

					for (int i = 0; i < tess.Length; ++i)
					{
						D3DTexture texture = (tess[i].Tile == null ? null : tess[i].Tile.Texture);

						float left = tess[i].Coordinates.Left * horizontalScaleFactor;
						float right = tess[i].Coordinates.Right * horizontalScaleFactor;
						float top = tess[i].Coordinates.Top * verticalScaleFactor;
						float bottom = tess[i].Coordinates.Bottom * verticalScaleFactor;

						float offset_x = _imageLocation.Width * 0.5f * horizontalScaleFactor + stickerPositionAndSize.X - 0.5f;
						float offset_y = _imageLocation.Height * 0.5f * verticalScaleFactor + stickerPositionAndSize.Y - 0.5f;

						float x0, y0, x1, y1, x2, y2, x3, y3;

						if (rotationAngle == 0.0f)
						{
							x0 = left + offset_x;
							y0 = top + offset_y + (x0 - center_x) * skewingFactor;

							x1 = x0;
							y1 = bottom + offset_y + (x0 - center_x) * skewingFactor;

							x2 = right + offset_x;
							y2 = top + offset_y + (x2 - center_x) * skewingFactor;

							x3 = x2;
							y3 = bottom + offset_y + (x2 - center_x) * skewingFactor;
						}
						else
						{
							// rotation:
							// x <- x * cos - y * sin
							// y <- x * sin + y * cos
							float sin = (float)Math.Sin(-rotationAngle);
							float cos = (float)Math.Cos(-rotationAngle);

							x0 = left * cos - top * sin + offset_x;
							y0 = left * sin + top * cos + offset_y + (x0 - center_x) * skewingFactor;

							x1 = left * cos - bottom * sin + offset_x;
							y1 = left * sin + bottom * cos + offset_y + (x1 - center_x) * skewingFactor;

							x2 = right * cos - top * sin + offset_x;
							y2 = right * sin + top * cos + offset_y + (x2 - center_x) * skewingFactor;

							x3 = right * cos - bottom * sin + offset_x;
							y3 = right * sin + bottom * cos + offset_y + (x3 - center_x) * skewingFactor;
						}

						RectangleF tex_coords = tess[i].TextureCoordinates;

						D3D.RenderTexturedQuadBlend(
							texture, blockColor,
							x0, y0, x1, y1, x2, y2, x3, y3,
							tex_coords.Top, tex_coords.Right, tex_coords.Bottom, tex_coords.Left);
					}
				}
			}
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
			int mipMapLevels = _tileSet.MipMapLevelCount;
			_tesselation = new Quad[mipMapLevels][];

			// create padding quads (the padding quads are shared between all mipmap levels)

			bool topPaddingRequired = _imageLocation.Top < 0;
			bool bottomPaddingRequired = _imageLocation.Bottom > _tileSet.Size.Height;
			bool leftPaddingRequired = _imageLocation.Left < 0;
			bool rightPaddingRequired = _imageLocation.Right > _tileSet.Size.Width;
			int paddingQuadsCount = 
				(topPaddingRequired ? 1 : 0) + (bottomPaddingRequired ? 1 : 0) +
				(leftPaddingRequired ? 1 : 0) + (rightPaddingRequired ? 1 : 0);
			Quad[] paddingQuads = new Quad[paddingQuadsCount];

			if(topPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = _imageLocation.Left;
				quad.Coordinates.Width = _imageLocation.Width;
				quad.Coordinates.Y = _imageLocation.Top;
				quad.Coordinates.Height = -_imageLocation.Top;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= _imageLocation.X + _imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= _imageLocation.Y + _imageLocation.Height * 0.5f;
				paddingQuads[0] = quad;
			}
			if(bottomPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = _imageLocation.Left;
				quad.Coordinates.Width = _imageLocation.Width;
				quad.Coordinates.Y = _tileSet.Size.Height;
				quad.Coordinates.Height = _imageLocation.Bottom - _tileSet.Size.Height;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= _imageLocation.X + _imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= _imageLocation.Y + _imageLocation.Height * 0.5f;
				paddingQuads[topPaddingRequired ? 1 : 0] = quad;
			}
			if(leftPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = _imageLocation.Left;
				quad.Coordinates.Width = -_imageLocation.Left;
				quad.Coordinates.Y = Math.Max(0.0f, _imageLocation.Top);
				quad.Coordinates.Height = Math.Min(_imageLocation.Bottom, _tileSet.Size.Height) - quad.Coordinates.Y;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= _imageLocation.X + _imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= _imageLocation.Y + _imageLocation.Height * 0.5f;
				paddingQuads[paddingQuadsCount - (rightPaddingRequired ? 2 : 1)] = quad;
			}
			if(rightPaddingRequired) {
				Quad quad = new Quad();
				quad.Tile = null;
				quad.Coordinates.X = _tileSet.Size.Width;
				quad.Coordinates.Width = _imageLocation.Right - _tileSet.Size.Width;
				quad.Coordinates.Y = Math.Max(0.0f, _imageLocation.Top);
				quad.Coordinates.Height = Math.Min(_imageLocation.Bottom, _tileSet.Size.Height) - quad.Coordinates.Y;
				quad.TextureCoordinates.X = 0.0f;
				quad.TextureCoordinates.Width = 1.0f;
				quad.TextureCoordinates.Y = 0.0f;
				quad.TextureCoordinates.Height = 1.0f;
				quad.Coordinates.X -= _imageLocation.X + _imageLocation.Width * 0.5f;
				quad.Coordinates.Y -= _imageLocation.Y + _imageLocation.Height * 0.5f;
				paddingQuads[paddingQuadsCount - 1] = quad;
			}

			// create quads for each mipmap levels

			SizeF tileSize = new SizeF(254.0f, 254.0f);
			SizeF textureSize = new SizeF(256.0f, 256.0f);
			for(int mipMapLevel = 0; mipMapLevel < mipMapLevels; ++mipMapLevel) {
				if(mipMapLevel >= (int)_tileSet.DetailLevel &&
					(thisMipMapLevelOnly == -1 || thisMipMapLevelOnly == mipMapLevel))
				{
					Point upperLeftTile = new Point(
						(int) Math.Floor(Math.Max(0.0f, _imageLocation.Left) / tileSize.Width),
						(int) Math.Floor(Math.Max(0.0f, _imageLocation.Top) / tileSize.Height));
					Size tileCount = new Size(
						(int) (Math.Floor((Math.Min(_tileSet.Size.Width, _imageLocation.Right) - 1) / tileSize.Width)) - upperLeftTile.X + 1,
						(int) (Math.Floor((Math.Min(_tileSet.Size.Height, _imageLocation.Bottom) - 1) / tileSize.Height)) - upperLeftTile.Y + 1);
					_tesselation[mipMapLevel] = new Quad[paddingQuadsCount + Math.Max(0, tileCount.Width) * Math.Max(0, tileCount.Height)];

					for(int i = 0; i < paddingQuadsCount; ++i) {
						_tesselation[mipMapLevel][i] = paddingQuads[i];
					}

					for(int y = 0; y < tileCount.Height; ++y) {
						for(int x = 0; x < tileCount.Width; ++x) {
							PointF tileUpperLeftCorner = new PointF(
								(upperLeftTile.X + x) * tileSize.Width,
								(upperLeftTile.Y + y) * tileSize.Height);

							Quad quad = new Quad();

							quad.Tile = _tileSet.Tiles[mipMapLevel][upperLeftTile.X + x, upperLeftTile.Y + y];

							quad.Coordinates.X = Math.Max(tileUpperLeftCorner.X, _imageLocation.Left);
							quad.Coordinates.Width = Math.Min(tileUpperLeftCorner.X + tileSize.Width, _imageLocation.Right) - quad.Coordinates.X;
							quad.Coordinates.Y = Math.Max(tileUpperLeftCorner.Y, _imageLocation.Top);
							quad.Coordinates.Height = Math.Min(tileUpperLeftCorner.Y + tileSize.Height, _imageLocation.Bottom) - quad.Coordinates.Y;

							quad.TextureCoordinates.X = (quad.Coordinates.X % tileSize.Width + (textureSize.Width - tileSize.Width)*0.5f) / textureSize.Width;
							quad.TextureCoordinates.Width = quad.Coordinates.Width / textureSize.Width;
							quad.TextureCoordinates.Y = (quad.Coordinates.Y % tileSize.Height + (textureSize.Width - tileSize.Width)*0.5f) / textureSize.Height;
							quad.TextureCoordinates.Height = quad.Coordinates.Height / textureSize.Height;

							quad.Coordinates.X -= _imageLocation.X + _imageLocation.Width * 0.5f;
							quad.Coordinates.Y -= _imageLocation.Y + _imageLocation.Height * 0.5f;

							_tesselation[mipMapLevel][paddingQuadsCount + y * tileCount.Width + x] = quad;
						}
					}
				}

				tileSize.Width *= 2.0f;
				tileSize.Height *= 2.0f;
				textureSize.Width *= 2.0f;
				textureSize.Height *= 2.0f;
			}
		}

		DXTileSet _tileSet;
		RectangleF _imageLocation;

		struct Quad {
			public DXTile Tile;
			public RectangleF Coordinates;
			public RectangleF TextureCoordinates;

			/// <summary>This is to get rid of the annoying warning "is never assigned to, and will always have its default value"</summary>
			Quad(RectangleF dummy) { Tile = null; Coordinates = TextureCoordinates = dummy; }
		}
		Quad[][] _tesselation;
	}
}
