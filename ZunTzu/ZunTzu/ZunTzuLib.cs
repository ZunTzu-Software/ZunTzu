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
			[In] byte* data,
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

		[DllImport("ZunTzuLib.dll")]
		public static extern int GetEligibleFullscreenModeCount();

		[DllImport("ZunTzuLib.dll")]
		public static extern void GetEligibleFullscreenMode(
			int index, 
			[Out] out int width, 
			[Out] out int height, 
			[Out] out int refresh_rate, 
			[Out] out int format);

		[DllImport("ZunTzuLib.dll")]
		public static extern void GetCurrentDisplayMode(
			[Out] out int width, 
			[Out] out int height, 
			[Out] out int refresh_rate, 
			[Out] out int format);

		[DllImport("ZunTzuLib.dll")]
		public static extern bool CreateDevice(
			IntPtr hMainWnd, 
			bool full_screen, 
			int width, 
			int height, 
			int refresh_rate, 
			int display_format, 
			bool wait_for_vertical_blank);

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreeDevice();

		[DllImport("ZunTzuLib.dll")]
		public static extern int CheckCooperativeLevel();

		[DllImport("ZunTzuLib.dll")]
		public static extern bool ResetDevice();

		[DllImport("ZunTzuLib.dll")]
		public static extern bool UpdatePresentParameters(
			bool full_screen,
			int width,
			int height,
			int refresh_rate,
			int display_format,
			bool wait_for_vertical_blank);

		[DllImport("ZunTzuLib.dll")]
		public static extern void BeginFrame();

		[DllImport("ZunTzuLib.dll")]
		public static extern void EndFrame();

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderMonochromaticQuad(
			uint color,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderTexturedQuad(
			IntPtr texture,
			uint modulation_color,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderTexturedQuadSilhouette(
			IntPtr texture,
			uint modulation_color,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderTexturedQuadIgnoreMask(
			IntPtr texture,
			uint modulation_color,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderDieMesh(
			IntPtr mesh_vb,
			IntPtr mesh_ib,
			IntPtr mesh_texture,
			int mesh_vertex_count,
			int mesh_triangle_count,
			float x,
			float y,
			float size_factor,
			float rot_00, float rot_01, float rot_02,
			float rot_10, float rot_11, float rot_12,
			float rot_20, float rot_21, float rot_22,
			uint die_color,
			uint pips_color);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderCustomDieMesh(
			IntPtr mesh_vb,
			IntPtr mesh_ib,
			IntPtr mesh_texture,
			int mesh_vertex_count,
			int mesh_triangle_count,
			float x,
			float y,
			float size_factor,
			float rot_00, float rot_01, float rot_02,
			float rot_10, float rot_11, float rot_12,
			float rot_20, float rot_21, float rot_22);

		[DllImport("ZunTzuLib.dll")]
		public static extern void RenderDieMeshShadow(
			IntPtr mesh_vb,
			IntPtr mesh_ib,
			IntPtr mesh_texture,
			int mesh_vertex_count,
			int mesh_triangle_count,
			float mesh_inradius,
			float x,
			float y,
			float size_factor,
			float rot_00, float rot_01, float rot_02,
			float rot_10, float rot_11, float rot_12,
			float rot_20, float rot_21, float rot_22,
			uint shadow_color);

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr CreateTexture(
			int width,
			int height,
			int format);

		[DllImport("ZunTzuLib.dll")]
		public static extern void LockTexture(
			IntPtr texture,
			[Out] out int pitch,
			[Out] out byte* bits);

		[DllImport("ZunTzuLib.dll")]
		public static extern void LockTextureReadOnly(
			IntPtr texture,
			[Out] out int pitch,
			[Out] out byte* bits);

		[DllImport("ZunTzuLib.dll")]
		public static extern void UnlockTexture(
			IntPtr texture);

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreeTexture(
			IntPtr texture);

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr CreateVertexBuffer(
			int vertex_count,
			IntPtr data);

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreeVertexBuffer(
			IntPtr vb);

		[DllImport("ZunTzuLib.dll")]
		public static extern IntPtr CreateIndexBuffer(
			int triangle_count,
			[In] short* data);

		[DllImport("ZunTzuLib.dll")]
		public static extern void FreeIndexBuffer(
			IntPtr ib);
	}
}
