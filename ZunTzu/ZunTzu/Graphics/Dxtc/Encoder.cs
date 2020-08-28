// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Runtime.InteropServices;

namespace ZunTzu.Graphics.Dxtc {

	/// <summary>A DXTC encoder.</summary>
	public static unsafe class Encoder {

		public static void CompressDxt1(byte* rgb, int top, int left, int bottom, int right, int stride, byte* blocks, int option) {
			if(useSse2)
				option |= 2;
			ZunTzuLib.CompressDxt1(rgb, top, left, bottom, right, stride, blocks, option);
		}

		public static void CompressDxt1FromRgba(byte* rgba, int top, int left, int bottom, int right, int stride, byte* blocks, int option) {
			if(useSse2)
				option |= 2;
			ZunTzuLib.CompressDxt1FromRgba(rgba, top, left, bottom, right, stride, blocks, option);
		}

		public static void CompressDxt5(byte* rgba, int top, int left, int bottom, int right, int stride, byte* blocks, int option) {
			if(useSse2)
				option |= 2;
			ZunTzuLib.CompressDxt5(rgba, top, left, bottom, right, stride, blocks, option);
		}

		private static bool useSse2 = IsProcessorFeaturePresent(10);

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsProcessorFeaturePresent(uint processorFeature);
	}
}
