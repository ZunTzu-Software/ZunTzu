// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.VideoCompression {

	public class NullVideoCodec : IVideoCodec {

		/// <summary>Compresses a frame.</summary>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		public unsafe void Encode(IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount) {
			byteCount = 64 * 64 * 3;
			byte* source = (byte*) frameBuffer;
			byte* destination = (byte*) compressedBuffer;
			for(int i = 0; i < 64 * 64 * 3; ++i)
				*destination++ = *source++;
		}

		/// <summary>Compresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		public void Encode(IntPtr referenceFrameBuffer, IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount) {
			Encode(frameBuffer, compressedBuffer, out byteCount);
		}

		/// <summary>Uncompresses a frame.</summary>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		public unsafe void Decode(IntPtr compressedBuffer, IntPtr frameBuffer) {
			byte* source = (byte*) compressedBuffer;
			byte* destination = (byte*) frameBuffer;
			for(int i = 0; i < 64 * 64 * 3; ++i)
				*destination++ = *source++;
		}

		/// <summary>Uncompresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		public void Decode(IntPtr referenceFrameBuffer, IntPtr compressedBuffer, IntPtr frameBuffer) {
			Decode(compressedBuffer, frameBuffer);
		}
	}
}
