// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.VideoCompression {

	/// <summary>A video encoder.</summary>
	public interface IVideoCodec {
		/// <summary>Compresses a frame.</summary>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		void Encode(IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount);
		/// <summary>Compresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		void Encode(IntPtr referenceFrameBuffer, IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount);
		/// <summary>Uncompresses a frame.</summary>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		void Decode(IntPtr compressedBuffer, IntPtr frameBuffer);
		/// <summary>Uncompresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		void Decode(IntPtr referenceFrameBuffer, IntPtr compressedBuffer, IntPtr frameBuffer);
    }
}
