// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Diagnostics;
using System.Drawing;

namespace ZunTzu.Graphics {

	/// <summary>A texture that can be used to render video frames.</summary>
	internal sealed class DXVideoTexture : IVideoTexture {

		/// <summary>Constructor.</summary>
		public DXVideoTexture(DXGraphics graphics, Size size) {
			this.graphics = graphics;
			this.size = size;
			this.texture = new Texture(graphics.Device, size.Width, size.Height, 1, 0, Format.A8R8G8B8, Pool.Managed);
		}

		/// <summary>Update part of the texture with a new image.</summary>
		/// <param name="location">The part of the texture that will be affected.</param>
		/// <param name="bitmapBits">An image in A8R8G8B8 format.</param>
		/// <remarks>The size of the image must be identical to the size of the location.</remarks>
		public unsafe void Update(Rectangle location, IntPtr bitmapBits) {
			if(!texture.Disposed) {
				int texturePitch;
				byte* textureBits = (byte*) texture.LockRectangle(0, LockFlags.None, out texturePitch).InternalData.ToPointer();

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

				texture.UnlockRectangle(0);
			}
		}

		public IImage ExtractImage(RectangleF imageLocation) {
			return new DXVideoImage(this, imageLocation);
		}

		public void Dispose() {
			texture.Dispose();
		}

		public DXGraphics Graphics { get { return graphics; } }
		public Size Size { get { return size; } }
		public Texture Texture { get { return texture; } }

		private DXGraphics graphics;
		private Size size = new Size(0, 0);
		private Texture texture;
	}
}
