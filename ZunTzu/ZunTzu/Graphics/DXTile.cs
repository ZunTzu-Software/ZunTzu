// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ZunTzu.Graphics
{
    /// <summary>
    /// Summary description for Tile.
    /// </summary>
    public sealed class DXTile : IDisposable {
		internal DXTile() {
			_texture = null;
		}

		internal DXTile(D3DTexture texture) {
			_texture = texture;
		}

		internal unsafe void Initialize(D3DTextureFormat textureFormat, D3DTexture texture) {
			_textureFormat = textureFormat;
			_texture = texture;
		}

		internal void Initialize(
			IntPtr bitmapBits, int stride, PixelFormat pixelFormat,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner)
		{
			if(pixelFormat == PixelFormat.Format24bppRgb) {
				InitializeDxt1From24bits(bitmapBits, stride, sourceUpperLeftCorner, sourceLowerRightCorner, 1);
			} else {
				InitializeDxt5From32bits(bitmapBits, stride, sourceUpperLeftCorner, sourceLowerRightCorner, 1);
			}
		}

		/// <summary>Returns the color of the texel at the given address.</summary>
		/// <param name="textureAddress">An address in the rectangle [0,0,1,1].</param>
		/// <returns>A color value.</returns>
		internal uint GetTexelColorAtAddress(PointF textureAddress) {
			if(_texture.Disposed)
				return 0x00000000;
			Point position = new Point((int)(textureAddress.X * 256.0f), (int)(textureAddress.Y * 256.0f));
			uint color;
			switch(_textureFormat) {
				case D3DTextureFormat.R5G6B5:
					color = (uint) get16bitsTexel(position);
					return (((color & 0x0000001F)*255+15)/31) |
						((((color & 0x000007E0)>>5)*255+31)/63)<<8 |
						((((color & 0x0000F800)>>11)*255+15)/31)<<16 |
						0xFF000000;
				case D3DTextureFormat.A1R5G5B5:
					color = (uint) get16bitsTexel(position);
					return (((color & 0x0000001F)*255+15)/31) |
						((((color & 0x000003E0)>>5)*255+15)/31)<<8 |
						((((color & 0x00007C00)>>10)*255+15)/31)<<16 |
						((color & 0x00008000) == 0x00008000 ? 0xFF000000 : 0x00000000);
				case D3DTextureFormat.X8R8G8B8:
					return get32bitsTexel(position) | 0xFF000000;
				case D3DTextureFormat.A8R8G8B8:
					return get32bitsTexel(position);
				case D3DTextureFormat.DXT1:
					return getDxt1Texel(position);
				case D3DTextureFormat.DXT5:
					return getDxt5Texel(position);
				default:
					return 0x00000000;
			}
		}

		/// <summary>
		/// Creates a 256x256 texture from a rectangular part of a bitmap.
		/// </summary>
		/// <remarks>
		/// TODO : add a Floyd-Steinberg error diffusion dithering algorithm.
		/// 
		/// +----------+----------+----------+
		/// |          |     x    |   7/16   |
		/// +----------+----------+----------+
		/// |   3/16   |   5/16   |   1/16   |
		/// +----------+----------+----------+
		/// </remarks>
		private unsafe void Initialize16bitsFrom24bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner)
		{
			_textureFormat = D3DTextureFormat.R5G6B5;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out int texturePitch, out byte* textureBits);

			byte* source = (byte*)bitmapBits + sourceUpperLeftCorner.Y * stride + sourceUpperLeftCorner.X * 3;
			int sourceWidth = sourceLowerRightCorner.X - sourceUpperLeftCorner.X;
			int sourceHeight = sourceLowerRightCorner.Y - sourceUpperLeftCorner.Y;
			ushort* dest = (ushort*)textureBits;
			for(int y = 0; y < 256; ++y) {
				for(int x = 0; x < 256; ++x) {
					*dest = (ushort)
						((y == 0 && sourceUpperLeftCorner.Y < 0) ||
						(y >= sourceHeight) ||
						(x == 0 && sourceUpperLeftCorner.X < 0) ||
						(x >= sourceWidth) ?
						0x00000000 :
						(((uint)source[0]*31+127)/255) |
							(((uint)source[1]*63+127)/255)<<5 |
							(((uint)source[2]*31+127)/255)<<11);
					++dest;
					source += 3;
				}
				dest += (texturePitch / 2) - 256;
				source += stride - 256 * 3;
			}

			_texture.Unlock();
		}

		/// <summary>
		/// Creates a 256x256 texture from a rectangular part of a bitmap.
		/// </summary>
		/// <remarks>
		/// TODO : add a Floyd-Steinberg error diffusion dithering algorithm.
		/// 
		/// +----------+----------+----------+
		/// |          |     x    |   7/16   |
		/// +----------+----------+----------+
		/// |   3/16   |   5/16   |   1/16   |
		/// +----------+----------+----------+
		/// </remarks>
		private unsafe void Initialize16bitsFrom32bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner)
		{
			_textureFormat = D3DTextureFormat.A1R5G5B5;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out int texturePitch, out byte* textureBits);

			byte* source = (byte*)bitmapBits + sourceUpperLeftCorner.Y * stride + sourceUpperLeftCorner.X * 4;
			int sourceWidth = sourceLowerRightCorner.X - sourceUpperLeftCorner.X;
			int sourceHeight = sourceLowerRightCorner.Y - sourceUpperLeftCorner.Y;
			ushort* dest = (ushort*)textureBits;
			for(int y = 0; y < 256; ++y) {
				for(int x = 0; x < 256; ++x) {
					*dest = (ushort)
						((y == 0 && sourceUpperLeftCorner.Y < 0) ||
						(y >= sourceHeight) ||
						(x == 0 && sourceUpperLeftCorner.X < 0) ||
						(x >= sourceWidth) ?
						0x00000000 :
						(((uint)source[0]*31+127)/255) |
							(((uint)source[1]*31+127)/255)<<5 |
							(((uint)source[2]*31+127)/255)<<10 |
							((uint)source[3]>0xEF ? (uint)0x8000 : (uint)0x0000));
					++dest;
					source += 4;
				}
				dest += (texturePitch / 2) - 256;
				source += stride - 256 * 4;
			}

			_texture.Unlock();
		}

		/// <summary>
		/// Creates a 256x256 texture from a rectangular part of a bitmap.
		/// </summary>
		private unsafe void Initialize32bitsFrom24bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner)
		{
			_textureFormat = D3DTextureFormat.X8R8G8B8;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out int texturePitch, out byte* textureBits);

			byte* source = (byte*)bitmapBits + sourceUpperLeftCorner.Y * stride + sourceUpperLeftCorner.X * 3;
			int sourceWidth = sourceLowerRightCorner.X - sourceUpperLeftCorner.X;
			int sourceHeight = sourceLowerRightCorner.Y - sourceUpperLeftCorner.Y;
			byte* dest = textureBits;
			for(int y = 0; y < 256; ++y) {
				for(int x = 0; x < 256; ++x) {
					if((y == 0 && sourceUpperLeftCorner.Y < 0) ||
						(y >= sourceHeight) ||
						(x == 0 && sourceUpperLeftCorner.X < 0) ||
						(x >= sourceWidth))
					{
						*(uint*)dest = 0xFF000000;
					} else {
						dest[0] = source[0];
						dest[1] = source[1];
						dest[2] = source[2];
						dest[3] = 0xFF;
					}
					dest += 4;
					source += 3;
				}
				dest += texturePitch - 256 * 4;
				source += stride - 256 * 3;
			}

			_texture.Unlock();
		}

		/// <summary>
		/// Creates a 256x256 texture from a rectangular part of a bitmap.
		/// </summary>
		private unsafe void Initialize32bitsFrom32bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner)
		{
			_textureFormat = D3DTextureFormat.A8R8G8B8;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out int texturePitch, out byte* textureBits);

			byte* source = (byte*)bitmapBits + sourceUpperLeftCorner.Y * stride + sourceUpperLeftCorner.X * 4;
			int sourceWidth = sourceLowerRightCorner.X - sourceUpperLeftCorner.X;
			int sourceHeight = sourceLowerRightCorner.Y - sourceUpperLeftCorner.Y;
			uint* dest = (uint*)textureBits;
			for(int y = 0; y < 256; ++y) {
				for(int x = 0; x < 256; ++x) {
					*dest =
						((y == 0 && sourceUpperLeftCorner.Y < 0) ||
						(y >= sourceHeight) ||
						(x == 0 && sourceUpperLeftCorner.X < 0) ||
						(x >= sourceWidth) ?
						0x00000000 :
						*(uint*)source);
					++dest;
					source += 4;
				}
				dest += (texturePitch / 4) - 256;
				source += stride - 256 * 4;
			}

			_texture.Unlock();
		}

		/// <remarks>
		/// There is a bug in the implementation of DirectX "SurfaceLoader.FromSurface" function.
		/// To avoid visible artifacts, we have to implement the compression ourselves.
		/// </remarks>
		private unsafe void InitializeDxt1From24bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner,
			int qualityOption)
		{
			_textureFormat = D3DTextureFormat.DXT1;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out _, out byte* textureBits);

			ZunTzu.Graphics.Dxtc.Encoder.CompressDxt1((byte*) bitmapBits, sourceUpperLeftCorner.Y, sourceUpperLeftCorner.X, sourceLowerRightCorner.Y, sourceLowerRightCorner.X, stride, textureBits, qualityOption);
			
			_texture.Unlock();
		}

		/// <remarks>
		/// There is a bug in the implementation of DirectX "SurfaceLoader.FromSurface" function.
		/// To avoid visible artifacts, we have to implement the compression ourselves.
		/// </remarks>
		private unsafe void InitializeDxt1From32bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner,
			int qualityOption)
		{
			_textureFormat = D3DTextureFormat.DXT1;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out _, out byte* textureBits);

			ZunTzu.Graphics.Dxtc.Encoder.CompressDxt1FromRgba((byte*) bitmapBits, sourceUpperLeftCorner.Y, sourceUpperLeftCorner.X, sourceLowerRightCorner.Y, sourceLowerRightCorner.X, stride, textureBits, qualityOption);

			_texture.Unlock();
		}

		/// <remarks>
		/// There is a bug in the implementation of DirectX "SurfaceLoader.FromSurface" function.
		/// To avoid visible artifacts, we have to implement the compression ourselves.
		/// </remarks>
		private unsafe void InitializeDxt5From32bits(
			IntPtr bitmapBits, int stride,
			Point sourceUpperLeftCorner,
			Point sourceLowerRightCorner,
			int qualityOption)
		{
			_textureFormat = D3DTextureFormat.DXT5;
			_texture = D3DTexture.Create(256, 256, _textureFormat);
			_texture.Lock(out _, out byte* textureBits);

			ZunTzu.Graphics.Dxtc.Encoder.CompressDxt5((byte*) bitmapBits, sourceUpperLeftCorner.Y, sourceUpperLeftCorner.X, sourceLowerRightCorner.Y, sourceLowerRightCorner.X, stride, textureBits, qualityOption);

			_texture.Unlock();
		}

		private unsafe ushort get16bitsTexel(Point position) {
			_texture.Lock(out int texturePitch, out byte* textureBits);

			int address = position.Y * texturePitch + position.X * 2;
			ushort texel = (ushort)((uint)textureBits[address] | (uint)textureBits[address + 1]<<8);

			_texture.Unlock();
			return texel;
		}

		private unsafe uint get32bitsTexel(Point position) {
			_texture.Lock(out int texturePitch, out byte* textureBits);

			int address = position.Y * texturePitch + position.X * 4;
			uint texel = (uint)textureBits[address] | (uint)textureBits[address + 1]<<8 | (uint)textureBits[address + 2]<<16 | (uint)textureBits[address + 3]<<24;

			_texture.Unlock();
			return texel;
		}

		private unsafe uint getDxt1Texel(Point position) {
			byte* pixels = stackalloc byte[16*4];

			_texture.Lock(out _, out byte* textureBits);

			int blockAddress = (position.Y / 4) * 64 * 8 + (position.X / 4) * 8;
			Dxtc.Decoder.DecodeDxt1Block(textureBits + blockAddress, pixels);

			_texture.Unlock();

			return *(uint*)(pixels + (position.Y % 4) * 4 * 4 + (position.X % 4) * 4);
		}

		private unsafe uint getDxt5Texel(Point position) {
			byte* pixels = stackalloc byte[16*4];

			_texture.Lock(out _, out byte* textureBits);

			int blockAddress = (position.Y / 4) * 64 * 16 + (position.X / 4) * 16;
			Dxtc.Decoder.DecodeDxt5Block(textureBits + blockAddress, pixels);

			_texture.Unlock();

			return *(uint*)(pixels + (position.Y % 4) * 4 * 4 + (position.X % 4) * 4);
		}

		public void Dispose() {
			_texture.Dispose();
		}

		internal D3DTexture Texture => _texture;

		D3DTexture _texture;
		D3DTextureFormat _textureFormat;
	}

}
