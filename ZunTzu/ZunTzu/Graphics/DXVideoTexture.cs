// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Graphics
{

    /// <summary>A texture that can be used to render video frames.</summary>
    internal sealed class DXVideoTexture : IVideoTexture {

		/// <summary>Constructor.</summary>
		public DXVideoTexture(Size size) {
			_size = size;
			_texture = D3DTexture.Create(size.Width, size.Height, D3DTextureFormat.A8R8G8B8);
		}

		/// <summary>Update part of the texture with a new image.</summary>
		/// <param name="location">The part of the texture that will be affected.</param>
		/// <param name="bitmapBits">An image in A8R8G8B8 format.</param>
		/// <remarks>The size of the image must be identical to the size of the location.</remarks>
		public unsafe void Update(Rectangle location, IntPtr bitmapBits) {
			if(!_texture.Disposed) {
				_texture.Lock(out int texturePitch, out byte* textureBits);

				byte* source = (byte*) bitmapBits;
				byte* dest = textureBits + texturePitch * location.Y + 4 * location.X;
				for(int y = 0; y < location.Height; ++y) {
					for(int x = 0; x < location.Width; ++x) {
						dest[0] = source[0];
						dest[1] = source[1];
						dest[2] = source[2];
						dest[3] = source[3];
						dest += 4;
						source += 4;
					}
					dest += texturePitch - location.Width * 4;
				}

				_texture.Unlock();
			}
		}

		public IImage ExtractImage(RectangleF imageLocation) {
			return new DXVideoImage(this, imageLocation);
		}

		public void Dispose() {
			_texture.Dispose();
		}

		public Size Size => _size;
		public D3DTexture Texture => _texture;

		Size _size = new Size(0, 0);
		D3DTexture _texture;
	}
}
