// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Runtime.InteropServices;
using ZunTzu.Numerics;

namespace ZunTzu.Graphics
{
    static class D3D
    {
		public static D3DDisplayMode[] GetEligibleFullscreenModes()
        {
			int count = ZunTzuLib.GetEligibleFullscreenModeCount();
			var modes = new D3DDisplayMode[count];
			for(int i = 0; i < count; ++i)
            {
				ZunTzuLib.GetEligibleFullscreenMode(
					i,
					out modes[i].Width,
					out modes[i].Height,
					out modes[i].RefreshRate,
					out var format);
				modes[i].Format = (D3DTextureFormat)format;
			}
			return modes;
		}

		public static D3DDisplayMode GetCurrentDisplayMode()
        {
			D3DDisplayMode mode;
			ZunTzuLib.GetCurrentDisplayMode(
				out mode.Width,
				out mode.Height,
				out mode.RefreshRate,
				out var format);
			mode.Format = (D3DTextureFormat)format;
			return mode;
		}

		public static bool CreateDevice(
			IntPtr hMainWnd,
			bool fullscreen,
			int width,
			int height,
			int refreshRate,
			D3DTextureFormat displayFormat,
			bool waitForVerticalBlank)
        {
			return ZunTzuLib.CreateDevice(hMainWnd, fullscreen, width, height, refreshRate, (int)displayFormat, waitForVerticalBlank);
        }

		public static void FreeDevice()
        {
			ZunTzuLib.FreeDevice();
        }

		public static D3DCooperativeLevel CheckCooperativeLevel()
        {
			return (D3DCooperativeLevel)ZunTzuLib.CheckCooperativeLevel();
        }

		public static bool ResetDevice()
        {
			return ZunTzuLib.ResetDevice();
        }

		public static bool UpdatePresentParameters(
			bool fullscreen,
			int width,
			int height,
			int refreshRate,
			D3DTextureFormat displayFormat,
			bool waitForVerticalBlank)
		{
			return ZunTzuLib.UpdatePresentParameters(fullscreen, width, height, refreshRate, (int)displayFormat, waitForVerticalBlank);
		}

		public static void BeginFrame()
		{
			ZunTzuLib.BeginFrame();
		}

		public static void EndFrame()
		{
			ZunTzuLib.EndFrame();
		}

		public static void RenderMonochromaticQuad(
			uint color,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
		{
			ZunTzuLib.RenderMonochromaticQuad(
				color,
				x0, y0, x1, y1, x2, y2, x3, y3);
		}

		public static void RenderTexturedQuad(
			D3DTexture texture,
			uint modulationColor,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft)
		{
			ZunTzuLib.RenderTexturedQuad(
				(texture == null ? IntPtr.Zero : texture._internal), modulationColor,
				x0, y0, x1, y1, x2, y2, x3, y3,
				texTop, texRight, texBottom, texLeft);
		}

		public static void RenderTexturedQuadSilhouette(
			D3DTexture texture,
			uint modulationColor,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft)
		{
			ZunTzuLib.RenderTexturedQuadSilhouette(
				(texture == null ? IntPtr.Zero : texture._internal), modulationColor,
				x0, y0, x1, y1, x2, y2, x3, y3,
				texTop, texRight, texBottom, texLeft);
		}

		public static void RenderTexturedQuadIgnoreMask(
			D3DTexture texture,
			uint modulationColor,
			float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
			float texTop, float texRight, float texBottom, float texLeft)
		{
			ZunTzuLib.RenderTexturedQuadIgnoreMask(
				(texture == null ? IntPtr.Zero : texture._internal), modulationColor,
				x0, y0, x1, y1, x2, y2, x3, y3,
				texTop, texRight, texBottom, texLeft);
		}

		public static void RenderDieMesh(
			D3DVertexBuffer meshVb,
			D3DIndexBuffer meshIb,
			D3DTexture meshTexture,
			int meshVertexCount,
			int meshTriangleCount,
			float x,
			float y,
			float sizeFactor,
			Quaternion rotation,
			uint dieColor,
			uint pipsColor)
		{
			ZunTzuLib.RenderDieMesh(
				meshVb._internal, meshIb._internal, meshTexture._internal,
				meshVertexCount, meshTriangleCount,
				x, y, sizeFactor,
				rotation.X, rotation.Y, rotation.Z, rotation.W,
				dieColor, pipsColor);
		}

		public static void RenderCustomDieMesh(
			D3DVertexBuffer meshVb,
			D3DIndexBuffer meshIb,
			D3DTexture meshTexture,
			int meshVertexCount,
			int meshTriangleCount,
			float x,
			float y,
			float sizeFactor,
			Quaternion rotation)
		{
			ZunTzuLib.RenderCustomDieMesh(
				meshVb._internal, meshIb._internal, meshTexture._internal,
				meshVertexCount, meshTriangleCount,
				x, y, sizeFactor,
				rotation.X, rotation.Y, rotation.Z, rotation.W);
		}

		public static void RenderDieMeshShadow(
			D3DVertexBuffer meshVb,
			D3DIndexBuffer meshIb,
			D3DTexture meshTexture,
			int meshVertexCount,
			int meshTriangleCount,
			float meshInradius,
			float x,
			float y,
			float sizeFactor,
			Quaternion rotation,
			uint shadowColor)
		{
			ZunTzuLib.RenderDieMeshShadow(
				meshVb._internal, meshIb._internal, meshTexture._internal,
				meshVertexCount, meshTriangleCount, meshInradius,
				x, y, sizeFactor,
				rotation.X, rotation.Y, rotation.Z, rotation.W,
				shadowColor);
		}
	}

	sealed class D3DTexture : IDisposable
	{
		public static D3DTexture Create(int width, int height, D3DTextureFormat format)
		{
			IntPtr tex = ZunTzuLib.CreateTexture(width, height, (int)format);
			return new D3DTexture { _internal = tex };
		}

		public void Dispose()
		{
			if (_internal != IntPtr.Zero)
			{
				ZunTzuLib.FreeTexture(_internal);
				_internal = IntPtr.Zero;
			}
		}

		public bool Disposed => _internal == IntPtr.Zero;

        public unsafe void Lock(out int pitch, out byte* bits)
        {
            ZunTzuLib.LockTexture(_internal, out pitch, out bits);
        }

        public unsafe void LockReadOnly(out int pitch, out byte* bits)
        {
            ZunTzuLib.LockTextureReadOnly(_internal, out pitch, out bits);
        }

        public void Unlock()
        {
            ZunTzuLib.UnlockTexture(_internal);
        }

        internal IntPtr _internal;
	}

	sealed class D3DVertexBuffer : IDisposable
	{
		public static D3DVertexBuffer Create(PosNormTexVertex[] vertices)
		{
			unsafe
			{
				fixed (PosNormTexVertex* data = vertices)
				{
					IntPtr vb = ZunTzuLib.CreateVertexBuffer(vertices.Length, (IntPtr)data);
					return new D3DVertexBuffer { _internal = vb };
				}
			}
		}

		public void Dispose()
		{
			if (_internal != IntPtr.Zero)
			{
				ZunTzuLib.FreeVertexBuffer(_internal);
				_internal = IntPtr.Zero;
			}
		}

		internal IntPtr _internal;

		[StructLayout(LayoutKind.Sequential, Pack = 0)]
		internal struct PosNormTexVertex
		{
			public float X;
			public float Y;
			public float Z;
			public float Nx;
			public float Ny;
			public float Nz;
			public float Tu;
			public float Tv;
		}
	}

	sealed class D3DIndexBuffer : IDisposable
	{
		public unsafe static D3DIndexBuffer Create(short[] indices)
		{
			unsafe
            {
				fixed(short* data = indices)
                {
					IntPtr ib = ZunTzuLib.CreateIndexBuffer(indices.Length / 3, data);
					return new D3DIndexBuffer { _internal = ib };
				}
			}
		}

		public void Dispose()
		{
			if (_internal != IntPtr.Zero)
			{
				ZunTzuLib.FreeIndexBuffer(_internal);
				_internal = IntPtr.Zero;
			}
		}

		internal IntPtr _internal;
	}

	enum D3DTextureFormat
    {
        UNKNOWN = 0,

        R8G8B8 = 20,
        A8R8G8B8 = 21,
        X8R8G8B8 = 22,
        R5G6B5 = 23,
        X1R5G5B5 = 24,
        A1R5G5B5 = 25,
        A4R4G4B4 = 26,
        R3G3B2 = 27,
        A8 = 28,
        A8R3G3B2 = 29,
        X4R4G4B4 = 30,
        A2B10G10R10 = 31,
        A8B8G8R8 = 32,
        X8B8G8R8 = 33,
        G16R16 = 34,
        A2R10G10B10 = 35,
        A16B16G16R16 = 36,

        A8P8 = 40,
        P8 = 41,

        L8 = 50,
        A8L8 = 51,
        A4L4 = 52,

        V8U8 = 60,
        L6V5U5 = 61,
        X8L8V8U8 = 62,
        Q8W8V8U8 = 63,
        V16U16 = 64,
        A2W10V10U10 = 67,

        UYVY = ((int)'U' | ((int)'Y' << 8) | ((int)'V' << 16) | ((int)'Y' << 24)),
        R8G8_B8G8 = ((int)'R' | ((int)'G' << 8) | ((int)'B' << 16) | ((int)'G' << 24)),
        YUY2 = ((int)'Y' | ((int)'U' << 8) | ((int)'Y' << 16) | ((int)'2' << 24)),
        G8R8_G8B8 = ((int)'G' | ((int)'R' << 8) | ((int)'G' << 16) | ((int)'B' << 24)),
        DXT1 = ((int)'D' | ((int)'X' << 8) | ((int)'T' << 16) | ((int)'1' << 24)),
        DXT2 = ((int)'D' | ((int)'X' << 8) | ((int)'T' << 16) | ((int)'2' << 24)),
        DXT3 = ((int)'D' | ((int)'X' << 8) | ((int)'T' << 16) | ((int)'3' << 24)),
        DXT4 = ((int)'D' | ((int)'X' << 8) | ((int)'T' << 16) | ((int)'4' << 24)),
        DXT5 = ((int)'D' | ((int)'X' << 8) | ((int)'T' << 16) | ((int)'5' << 24)),

        D16_LOCKABLE = 70,
        D32 = 71,
        D15S1 = 73,
        D24S8 = 75,
        D24X8 = 77,
        D24X4S4 = 79,
        D16 = 80,

        D32F_LOCKABLE = 82,
        D24FS8 = 83,

        D32_LOCKABLE = 84,
        S8_LOCKABLE = 85,

        L16 = 81,

        VERTEXDATA = 100,
        INDEX16 = 101,
        INDEX32 = 102,

        Q16W16V16U16 = 110,

        MULTI2_ARGB8 = ((int)'M' | ((int)'E' << 8) | ((int)'T' << 16) | ((int)'1' << 24)),

        R16F = 111,
        G16R16F = 112,
        A16B16G16R16F = 113,

        R32F = 114,
        G32R32F = 115,
        A32B32G32R32F = 116,

        CxV8U8 = 117,

        A1 = 118,

        A2B10G10R10_XR_BIAS = 119,

        BINARYBUFFER = 199,

        FORCE_DWORD = 0x7fffffff,
    }

	enum D3DCooperativeLevel
    {
		DeviceOk,
		DeviceNotReset,
		DeviceLost,
    }

	struct D3DDisplayMode
    {
        public int Width;
        public int Height;
        public int RefreshRate;
        public D3DTextureFormat Format;
    }
}
