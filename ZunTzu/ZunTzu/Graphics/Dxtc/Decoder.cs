// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Graphics.Dxtc {

	/// <summary>A DXTC decoder.</summary>
	public static unsafe class Decoder {

		/// <summary>Decodes a DXT1 block.</summary>
		/// <param name="sourceBlock">A DXT1 block (8 bytes).</param>
		/// <param name="encodedBlock">An array of 16 32-bits ARGB pixels.</param>
		public static void DecodeDxt1Block(byte* sourceBlock, byte* decodedBlock) {
			decodeDxt1Block(sourceBlock, decodedBlock, false);
		}

		/// <summary>Decodes a DXT5 block.</summary>
		/// <param name="sourceBlock">A DXT5 block (16 bytes).</param>
		/// <param name="encodedBlock">An array of 16 32-bits ARGB pixels.</param>
		public static void DecodeDxt5Block(byte* sourceBlock, byte* decodedBlock) {
			byte* alphas = stackalloc byte[8];

			alphas[0] = sourceBlock[0];
			alphas[1] = sourceBlock[1];
			if(alphas[0] > alphas[1]) {
				for(int i = 0; i < 6; ++i)
					alphas[i + 2] = (byte) (((6 - i) * (uint) alphas[0] + (1 + i) * (uint) alphas[1]) / 7);
			} else {
				for(int i = 0; i < 4; ++i)
					alphas[i + 2] = (byte) (((4 - i) * (uint) alphas[0] + (1 + i) * (uint) alphas[1]) / 5);
				alphas[6] = 0x00;
				alphas[7] = 0xFF;
			}

			byte* alphaBlock = stackalloc byte[16];

			for(int i = 0; i < 2; ++i) {
				alphaBlock[i * 8 + 0] = alphas[(uint) sourceBlock[i * 3 + 2] & 0x00000007];
				alphaBlock[i * 8 + 1] = alphas[((uint) sourceBlock[i * 3 + 2] & 0x00000038) >> 3];
				alphaBlock[i * 8 + 2] = alphas[((uint) sourceBlock[i * 3 + 2] & 0x000000C0) >> 6 | ((uint) sourceBlock[i * 3 + 3] & 0x00000001) << 2];
				alphaBlock[i * 8 + 3] = alphas[((uint) sourceBlock[i * 3 + 3] & 0x0000000E) >> 1];
				alphaBlock[i * 8 + 4] = alphas[((uint) sourceBlock[i * 3 + 3] & 0x00000070) >> 4];
				alphaBlock[i * 8 + 5] = alphas[((uint) sourceBlock[i * 3 + 3] & 0x00000080) >> 7 | ((uint) sourceBlock[i * 3 + 4] & 0x00000003) << 1];
				alphaBlock[i * 8 + 6] = alphas[((uint) sourceBlock[i * 3 + 4] & 0x0000001C) >> 2];
				alphaBlock[i * 8 + 7] = alphas[((uint) sourceBlock[i * 3 + 4] & 0x000000E0) >> 5];
			}

			decodeDxt1Block(sourceBlock + 8, decodedBlock, true);

			for(int i = 0; i < 16; ++i)
				decodedBlock[4 * i + 3] = alphaBlock[i];
		}

		private static void decodeDxt1Block(byte* sourceBlock, byte* decodedBlock, bool asPartOfDxt5Block) {
			byte* colors = stackalloc byte[4 * 4];

			for(int i = 0; i < 2; ++i) {
				uint color16bits = (uint) *(ushort*) (sourceBlock + i * 2);
				colors[i * 4 + 0] = (byte) (((color16bits & 0x0000001F) * 255 + 15) / 31);
				colors[i * 4 + 1] = (byte) ((((color16bits & 0x000007E0) >> 5) * 255 + 31) / 63);
				colors[i * 4 + 2] = (byte) ((((color16bits & 0x0000F800) >> 11) * 255 + 15) / 31);
				colors[i * 4 + 3] = 0xFF;
			}

			if(asPartOfDxt5Block || *(uint*) colors > *(uint*) (colors + 2)) {
				for(int i = 0; i < 3; ++i) {
					colors[8 + i] = (byte) ((2 * (uint) colors[i] + (uint) colors[4 + i]) / 3);
					colors[12 + i] = (byte) (((uint) colors[i] + 2 * (uint) colors[4 + i]) / 3);
				}
				colors[11] = 0xFF;
				colors[15] = 0xFF;
			} else {
				for(int i = 0; i < 3; ++i) {
					colors[8 + i] = (byte) (((uint) colors[i] + (uint) colors[4 + i]) / 2);
					colors[12 + i] = 0x00;
				}
				colors[11] = 0xFF;
				colors[15] = 0x00;
			}

			uint* dest = (uint*) decodedBlock;
			for(int row = 0; row < 4; ++row) {
				byte source = sourceBlock[4 + row];
				for(int col = 0; col < 4; ++col) {
					*dest++ = ((uint*) colors)[source & 0x00000003];
					source >>= 2;
				}
			}
		}
	}
}
