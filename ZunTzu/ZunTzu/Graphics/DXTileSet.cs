// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZunTzu.FileSystem;

namespace ZunTzu.Graphics {
	/// <summary>
	/// Summary description for DXTileSet.
	/// </summary>
	public sealed class DXTileSet : ITileSet {

		internal DXTileSet(DXGraphics graphics, IFile imageFile, DetailLevelType detailLevel)
			: this(graphics, imageFile, null, detailLevel) {}

		internal DXTileSet(DXGraphics graphics, IFile imageFile, IFile maskFile, DetailLevelType detailLevel) {
			this.graphics = graphics;
			this.imageFile = imageFile;
			this.maskFile = maskFile;
			this.detailLevel = detailLevel;
		}

		private sealed class BitmapResource : IDisposable {
			public BitmapResource(IFile imageFile, PixelFormat pixelFormat) {
				stream = imageFile.Open();
				bitmap = new Bitmap(stream);

				bitmapData = bitmap.LockBits(
					new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.ReadOnly,
					pixelFormat);
			}

			public BitmapData BitmapData { get { return bitmapData; } }

			public void Dispose() {
				if(bitmap != null) {
					if(bitmapData != null) {
						bitmap.UnlockBits(bitmapData);
						bitmapData = null;
					}
					bitmap.Dispose();
				}
				if(stream != null) {
					stream.Close();
				}
			}

			private Stream stream = null;
			private Bitmap bitmap = null;
			private BitmapData bitmapData = null;
		}

		/// <summary>Must be called for the icons tile.</summary>
		public void LoadIcons() {
			using(BitmapResource image = new BitmapResource(imageFile, (maskFile != null ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb))) {
				if(maskFile != null) {
					using(BitmapResource mask = new BitmapResource(maskFile, PixelFormat.Format24bppRgb)) {
						applyMask(image, mask);
					}
				}

				size = new SizeF((float) image.BitmapData.Width, (float) image.BitmapData.Height);
				for(int i = (int) detailLevel; i > 0; --i)
					size = new SizeF(size.Width * 2, size.Height * 2);

				int mipMapLevelCount = 1;
				tiles = new DXTile[mipMapLevelCount + (int) detailLevel][,];

				int mipMapLevel = (int) detailLevel;
				tiles[mipMapLevel] = new DXTile[1, 1];
				tiles[mipMapLevel][0, 0] = new DXTile(this);

				tiles[mipMapLevel][0, 0].Initialize(
					image.BitmapData.Scan0, image.BitmapData.Stride, image.BitmapData.PixelFormat,
					new Point(0 * 254 - 1, 0 * 254 - 1),
					new Point(Math.Min(0 * 254 + 255, image.BitmapData.Width), Math.Min(0 * 254 + 255, image.BitmapData.Height)),
					true);
			}
		}

		/// <summary>Must be called in a loop for the tile set to be fully loaded.</summary>
		/// <returns>Progress between 0 and 1.</returns>
		public IEnumerable<float> LoadIncrementally() {
			if(imageFile.Archive == null ||
				(!imageFile.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !imageFile.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) ||
				(maskFile != null && !maskFile.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !maskFile.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
				goto fallBack;

			// optimized native code simd-ed multithreaded pipeline
			{
				int error;
				uint skippedMipMapLevels = (uint) Math.Max(0, (int) graphics.Properties.MapsAndCountersDetailLevel - (int) detailLevel);

				IntPtr imageLoader = ZunTzuLib.CreateImageLoader(imageFile.Archive.FileName, imageFile.FileName, (maskFile != null ? maskFile.FileName : ""), skippedMipMapLevels, 1);
				try {
					uint width;
					uint height;
					if(0 != (error = ZunTzuLib.GetImageDimensions(imageLoader, out width, out height))) {
						//throw new ApplicationException(string.Format("Error while loading image: code {0}", error));
						goto fallBack;
					}

					size = new SizeF((float) width, (float) height);
					for(int i = (int) detailLevel; i > 0; --i)
						size = new SizeF(size.Width * 2, size.Height * 2);

					int mipMapLevelCount =
						Math.Max(
							3,	// at least 3 mip-maps
							Math.Max(
								(int) Math.Ceiling(Math.Log(width / 254, 2) + 1),
								(int) Math.Ceiling(Math.Log(height / 254, 2) + 1)));
					tiles = new DXTile[mipMapLevelCount + (int) detailLevel][,];

					uint tileCount = 0;
					for(int mipMapLevel = (int) detailLevel; mipMapLevel < mipMapLevelCount + (int) detailLevel; ++mipMapLevel) {
						if(mipMapLevel >= (int) graphics.Properties.MapsAndCountersDetailLevel) {
							uint columnCount = (width + 253) / 254;
							uint rowCount = (height + 253) / 254;
							tiles[mipMapLevel] = new DXTile[columnCount, rowCount];
							tileCount += columnCount * rowCount;
						}
						width = (width + 1) / 2;
						height = (height + 1) / 2;
					}

					uint mipmapLevel;	// regardless of detailLevel (i.e. first mipmap is always zero)
					uint x;
					uint y;
					for(uint i = 0; i < tileCount; ++i) {
						Texture texture = new Texture(graphics.Device, 256, 256, 1, 0, (maskFile == null ? Format.Dxt1 : Format.Dxt5), Pool.Managed);
						if(0 != (error = loadNextTexture(imageLoader, texture, out mipmapLevel, out x, out y))) {
							texture.UnlockRectangle(0);
							//throw new ApplicationException(string.Format("Error while loading image: code {0}", error));
							goto fallBack;
						}
						texture.UnlockRectangle(0);

						DXTile tile = new DXTile(this);
						tiles[mipmapLevel + (int) detailLevel][x, y] = tile;
						tile.Initialize(maskFile == null ? Format.Dxt1 : Format.Dxt5, texture);

						yield return (float) (i + 1) / (float) tileCount;
					}
				} finally {
					ZunTzuLib.FreeImageLoader(imageLoader);
				}
				yield break;
			}

			fallBack:
			{
				BitmapResource image = null;
				try {
					if(maskFile != null) {
						image = new BitmapResource(imageFile, PixelFormat.Format32bppArgb);
						BitmapResource mask = null;
						try {
							mask = new BitmapResource(maskFile, PixelFormat.Format24bppRgb);
							applyMask(image, mask);
						} finally {
							if(mask != null) mask.Dispose();
						}
					} else {
						image = new BitmapResource(imageFile, PixelFormat.Format24bppRgb);
					}

					// TODO: find the correct progress for loading the bitmaps
					yield return 0.0f;

					size = new SizeF((float) image.BitmapData.Width, (float) image.BitmapData.Height);
					for(int i = (int) detailLevel; i > 0; --i)
						size = new SizeF(size.Width * 2, size.Height * 2);

					foreach(float progress in createMipMappedTilesIncrements(image))
						yield return progress;
				} finally {
					if(image != null) image.Dispose();
				}
			}
		}

		private static unsafe int loadNextTexture(IntPtr imageLoader, Texture texture, out uint mipmapLevel, out uint x, out uint y) {
			byte* textureBits = (byte*) texture.LockRectangle(0, LockFlags.None).InternalData.ToPointer();
			return ZunTzuLib.LoadNextTile(imageLoader, (IntPtr) textureBits, out mipmapLevel, out x, out y);
		}

		private IEnumerable<float> createMipMappedTilesIncrements(BitmapResource image) {
			int width = image.BitmapData.Width;
			int height = image.BitmapData.Height;
			int stride = image.BitmapData.Stride;
			PixelFormat pixelFormat = image.BitmapData.PixelFormat;

			int mipMapLevelCount =
				Math.Max(
					3,	// at least 3 mip-maps
					Math.Max(
						(int)Math.Ceiling(Math.Log(width / 254, 2) + 1),
						(int)Math.Ceiling(Math.Log(height / 254, 2) + 1)));
			tiles = new DXTile[mipMapLevelCount + (int)detailLevel][,];

			IntPtr currentMipMapBitmap = image.BitmapData.Scan0;

			float totalProgress = 0.0f;	// between 0 and 1
			float nextProgressIncrement = 3.0f / 4.0f;
			for(int mipMapLevel = (int)detailLevel; mipMapLevel < mipMapLevelCount + (int)detailLevel; ++mipMapLevel) {
				if(mipMapLevel > (int)detailLevel) {
					createNextLowerMipMap(currentMipMapBitmap, width, height, stride, pixelFormat);
					width = (width + 1) / 2;
					height = (height + 1) / 2;
					stride = width * (pixelFormat == PixelFormat.Format24bppRgb ? 3 : 4);

					totalProgress += nextProgressIncrement;
					nextProgressIncrement *= 0.25f;
				}
				if(mipMapLevel >= (int)graphics.Properties.MapsAndCountersDetailLevel) {
					int columnCount = (width + 253) / 254;
					int rowCount = (height + 253) / 254;
					tiles[mipMapLevel] = new DXTile[columnCount, rowCount];
					foreach(float progress in createSingleMipMapTilesIncrements(tiles[mipMapLevel], currentMipMapBitmap, width, height, stride, pixelFormat))
						yield return totalProgress + progress * nextProgressIncrement;
				}
			}
		}

		private IEnumerable<float> createSingleMipMapTilesIncrements(DXTile[,] tiles, IntPtr bitmapBits, int width, int height, int stride, PixelFormat pixelFormat) {
			int columnCount = tiles.GetLength(0);
			int rowCount = tiles.GetLength(1);

			float progress = 0.0f;
			float progressIncrement = 1.0f / (rowCount * columnCount);

			for(int r = 0; r < rowCount; ++r) {
				for(int c = 0; c < columnCount; ++c) {
					tiles[c, r] = new DXTile(this);
					tiles[c, r].Initialize(
						bitmapBits, stride, pixelFormat,
						new Point(c * 254 - 1, r * 254 - 1),
						new Point(Math.Min(c * 254 + 255, width), Math.Min(r * 254 + 255, height)),
						false);
					progress += progressIncrement;
					yield return progress;
				}
			}
		}

		/// <summary>Builds a lower mipmap bitmap by resizing the map to scale 2:1.</summary>
		/// <remarks>
		/// This method is written with unsafe code.
		/// The bitmap is scanned two scan lines at a time.
		/// </remarks>
		private unsafe void createNextLowerMipMap(IntPtr bitmapBits, int width, int height, int stride, PixelFormat pixelFormat) {
			int lowerMipMapWidth = (width + 1)/2;
			int lowerMipMapHeight = (height + 1)/2;

			byte* sourceOdd = (byte*)bitmapBits;
			byte* sourceEven = (byte*)bitmapBits + stride;
			byte* dest = (byte*)bitmapBits;

			if(pixelFormat == PixelFormat.Format24bppRgb) {
				for(int y = 0; y < height/2; ++y) {
					for(int x = 0; x < width/2; ++x) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceOdd+3)) +
							((uint)*(sourceEven+0)) +
							((uint)*(sourceEven+3)) + 2
							)/4);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceOdd+4)) +
							((uint)*(sourceEven+1)) +
							((uint)*(sourceEven+4)) + 2
							)/4);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceOdd+5)) +
							((uint)*(sourceEven+2)) +
							((uint)*(sourceEven+5)) + 2
							)/4);
						dest += 3;
						sourceOdd += 6;
						sourceEven += 6;
					}
					if(width%2 == 1) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceEven+0)) + 1
							)/2);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceEven+1)) + 1
							)/2);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceEven+2)) + 1
							)/2);
						dest += 3;
						sourceOdd += 3;
						sourceEven += 3;
					}
					sourceOdd += stride * 2 - width * 3;
					sourceEven += stride * 2 - width * 3;
				}
				if(height%2 == 1) {
					for(int x = 0; x < width/2; ++x) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceOdd+3)) + 1
							)/2);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceOdd+4)) + 1
							)/2);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceOdd+5)) + 1
							)/2);
						dest += 3;
						sourceOdd += 6;
					}
					if(width%2 == 1) {
						*(dest+0) = *(sourceOdd+0);
						*(dest+1) = *(sourceOdd+1);
						*(dest+2) = *(sourceOdd+2);
					}
				}
			} else {	// pixelFormat == PixelFormat.Format32bppArgb	
				for(int y = 0; y < height/2; ++y) {
					for(int x = 0; x < width/2; ++x) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceOdd+4)) +
							((uint)*(sourceEven+0)) +
							((uint)*(sourceEven+4)) + 2
							)/4);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceOdd+5)) +
							((uint)*(sourceEven+1)) +
							((uint)*(sourceEven+5)) + 2
							)/4);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceOdd+6)) +
							((uint)*(sourceEven+2)) +
							((uint)*(sourceEven+6)) + 2
							)/4);
						*(dest+3) = (byte) ((
							((uint)*(sourceOdd+3)) +
							((uint)*(sourceOdd+7)) +
							((uint)*(sourceEven+3)) +
							((uint)*(sourceEven+7)) + 2
							)/4);
						dest += 4;
						sourceOdd += 8;
						sourceEven += 8;
					}
					if(width%2 == 1) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceEven+0)) + 1
							)/2);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceEven+1)) + 1
							)/2);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceEven+2)) + 1
							)/2);
						*(dest+3) = (byte) ((
							((uint)*(sourceOdd+3)) +
							((uint)*(sourceEven+3)) + 1
							)/2);
						dest += 4;
						sourceOdd += 4;
						sourceEven += 4;
					}
					sourceOdd += stride * 2 - width * 4;
					sourceEven += stride * 2 - width * 4;
				}
				if(height%2 == 1) {
					for(int x = 0; x < width/2; ++x) {
						*(dest+0) = (byte) ((
							((uint)*(sourceOdd+0)) +
							((uint)*(sourceOdd+4)) + 1
							)/2);
						*(dest+1) = (byte) ((
							((uint)*(sourceOdd+1)) +
							((uint)*(sourceOdd+5)) + 1
							)/2);
						*(dest+2) = (byte) ((
							((uint)*(sourceOdd+2)) +
							((uint)*(sourceOdd+6)) + 1
							)/2);
						*(dest+3) = (byte) ((
							((uint)*(sourceOdd+3)) +
							((uint)*(sourceOdd+7)) + 1
							)/2);
						dest += 4;
						sourceOdd += 8;
					}
					if(width%2 == 1) {
						*(dest+0) = *(sourceOdd+0);
						*(dest+1) = *(sourceOdd+1);
						*(dest+2) = *(sourceOdd+2);
						*(dest+3) = *(sourceOdd+3);
					}
				}
			}
		}

		/// <summary>Apply a transparency mask to an image by adding the proper alpha information.</summary>
		/// <remarks>
		/// This method is written with unsafe code.
		/// </remarks>
		private unsafe void applyMask(BitmapResource image, BitmapResource mask) {
			int width = image.BitmapData.Width;
			int height = image.BitmapData.Height;
			if(mask.BitmapData.Width != width || mask.BitmapData.Height != height)
				throw new ApplicationException("Mask size doesn't match image size");

			int imageStride = image.BitmapData.Stride;
			int maskStride = mask.BitmapData.Stride;

			byte* src = (byte*) mask.BitmapData.Scan0;
			byte* dest = (byte*) image.BitmapData.Scan0;

			for(int y = 0; y < height; ++y) {
				for(int x = 0; x < width; ++x) {
					*(dest+3) = *(src+0);	// alpha == blue channel of mask
					dest += 4;
					src += 3;
				}
				src += maskStride - width * 3;
				dest += imageStride - width * 4;
			}
		}

		public IImage ExtractImage(RectangleF imageLocation) {
			return new DXTexturedImage(this, imageLocation);
		}

		public IImage ExtractImage(RectangleF imageLocation, RectangleF renderingPositionAndSize) {
			return new DXTexturedImage(this, imageLocation, renderingPositionAndSize);
		}

		public void Dispose() {
			foreach(DXTile[,] tileArray in tiles)
				if(tileArray != null)
					foreach(DXTile tile in tileArray)
						tile.Dispose();
		}

		public SizeF Size { get { return size; } }

		private DXGraphics graphics;
		internal DXGraphics Graphics { get { return graphics; } }
		private IFile imageFile;
		private IFile maskFile;
		internal bool SupportsTransparency { get { return maskFile != null; } }
		private DetailLevelType detailLevel;
		internal DetailLevelType DetailLevel { get { return (DetailLevelType) Math.Max((int)detailLevel, (int)graphics.Properties.MapsAndCountersDetailLevel); } }
		private DXTile[][,] tiles;
		internal int MipMapLevelCount { get { return tiles.Length; } }
		internal DXTile[][,] Tiles { get { return tiles; } }
		private SizeF size = new SizeF(0.0f, 0.0f);
	}
}
