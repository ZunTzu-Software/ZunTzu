// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Runtime.InteropServices;

namespace ZunTzu {

	/// <summary>A external C++ high performance library.</summary>
	public static unsafe class ZunTzuLib {

		// Raw DXT compression services

		[DllImport("ZunTzuLib.dll")]
		public static extern void CompressDxt1(
			[In] byte* rgb,
			[In] int top,
			[In] int left,
			[In] int bottom,
			[In] int right,
			[In] int stride,
			[In] byte* blocks,
			[In] int option);

		[DllImport("ZunTzuLib.dll")]
		public static extern void CompressDxt1FromRgba(
			[In] byte* rgb,
			[In] int top,
			[In] int left,
			[In] int bottom,
			[In] int right,
			[In] int stride,
			[In] byte* blocks,
			[In] int option);

		[DllImport("ZunTzuLib.dll")]
		public static extern void CompressDxt5(
			[In] byte* rgb,
			[In] int top,
			[In] int left,
			[In] int bottom,
			[In] int right,
			[In] int stride,
			[In] byte* blocks,
			[In] int option);

		// Image loading and compression services

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr CreateImageLoader(
			[MarshalAs(UnmanagedType.LPWStr)] string archiveName,
			[MarshalAs(UnmanagedType.LPStr)] string imageEntryName,
			[MarshalAs(UnmanagedType.LPStr)] string maskEntryName,
			uint skippedMipmapLevels,
			int option);

		[DllImport("ZunTzuLib.dll")]
		public static extern int GetImageDimensions(
			IntPtr imageLoader,
			[Out] out uint width,
			[Out] out uint height);

		[DllImport("ZunTzuLib.dll")]
		public static extern int LoadNextTile(
			IntPtr imageLoader,
			IntPtr tile,
			[Out] out uint mipmapLevel,
			[Out] out uint x,
			[Out] out uint y);

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreeImageLoader(
			IntPtr imageLoader);

		// Networking

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr CreatePeer();

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreePeer(
			IntPtr clientOrServer);

		[DllImport("ZunTzuLib.dll")]
		public static extern int StartupClient(
			IntPtr client,
			ushort port);

		[DllImport("ZunTzuLib.dll")]
		public static extern int StartupServer(
			IntPtr server, 
			ushort port);

		[DllImport("ZunTzuLib.dll")]
		public static extern void Shutdown(
			IntPtr clientOrServer);

		[DllImport("ZunTzuLib.dll")]
		public static extern int Connect(
			IntPtr client,
			[MarshalAs(UnmanagedType.LPStr)] string host, 
			ushort remotePort);

		[DllImport("ZunTzuLib.dll")]
		public static extern int Send(
			IntPtr client,
			byte* data,
			int length,
			int priority,
			int reliability,
			int orderingChannel,
			IntPtr addressOrGuid,
			bool broadcast);

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr Receive(
			IntPtr clientOrServer);

		[DllImport("ZunTzuLib.dll")]
		public static extern void DeallocatePacket(
			IntPtr clientOrServer,
			IntPtr packet);

		[DllImport("ZunTzuLib.dll")]
		public static extern UInt64 GetGuid(
			IntPtr client);
	}
}
