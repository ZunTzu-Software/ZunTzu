// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ZunTzu.AudioVideo {

	[ComImport, Guid("0579154A-2B53-4994-B0D0-E773148EFF85"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ISampleGrabberCB {
		[PreserveSig]
		int SampleCB(double SampleTime, IMediaSample pSample);
		[PreserveSig]
		int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen);
	}

	[ComImport, Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IMediaSample {
	}

	[ComImport, Guid("36b73882-c2c8-11cf-8b46-00805f6cef60"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IFilterGraph2 {
		void AddFilter(
			[In] IBaseFilter pFilter,
			[In, MarshalAs(UnmanagedType.LPWStr)] string pName);
		void RemoveFilter( /* not used */ );
		void EnumFilters( /* not used */ );
		void FindFilterByName( /* not used */ );
		void ConnectDirect( /* not used */ );
		void Reconnect( /* not used */ );
		void Disconnect( /* not used */ );
		void SetDefaultSyncSource( /* not used */ );
		void Connect( /* not used */ );
		void Render( /* not used */ );
		[PreserveSig]
		int RenderFile(
			[In, MarshalAs(UnmanagedType.BStr)] string strFilename,
			IntPtr strPlayList);
		void AddSourceFilter( /* not used */ );
		void SetLogFile( /* not used */ );
		void Abort( /* not used */ );
		void ShouldOperationContinue( /* not used */ );
		void AddSourceFilterForMoniker(
			[In] IMoniker pMoniker,
			[In] IBindCtx pCtx,
			[In, MarshalAs(UnmanagedType.LPWStr)] string lpcwstrFilterName,
			[Out] out IBaseFilter ppFilter);
		void ReconnectEx( /* not used */ );
		void RenderEx( /* not used */ );
	}

	[ComImport, Guid("56a868a9-0ad4-11ce-b03a-0020af0ba770"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IGraphBuilder {
	}

	[ComImport, Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"),
	InterfaceType(ComInterfaceType.InterfaceIsDual)]
	internal interface IMediaControl {
		void Run();
		void Pause( /* not used */ );
		void Stop( /* not used */ );
		void GetState( /* not used */ );
		void RenderFile( /* not used */ );
		void AddSourceFilter( /* not used */ );
		void get_FilterCollection( /* not used */ );
		void get_RegFilterCollection( /* not used */ );
		void StopWhenReady();
	}

	[ComImport, Guid("56a868b6-0ad4-11ce-b03a-0020af0ba770"),
	InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IMediaEvent {
		void GetEventHandle( /* not used */ );
		void GetEvent( /* not used */ );
		[PreserveSig]
		int WaitForCompletion(
			[In] int msTimeout,
			[Out] out int pEvCode);
		void CancelDefaultHandling( /* not used */ );
		void RestoreDefaultHandling( /* not used */ );
		void FreeEventParams( /* not used */ );
	}

	[ComImport, Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IBaseFilter {
	}

	[ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ICreateDevEnum {
		[PreserveSig]
		int CreateClassEnumerator(
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid pType,
			[Out] out IEnumMoniker ppEnumMoniker,
			[In] int dwFlags);
	}

	[ComImport, Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86")]
	internal class SystemDeviceEnum {
	}

	[ComImport, Guid("e436ebb3-524f-11ce-9f53-0020af0ba770")]
	internal class FilterGraph {
	}

	[ComImport, Guid("93E5A4E0-2D50-11d2-ABFA-00A0C9C6E38D"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ICaptureGraphBuilder2 {
		void SetFiltergraph([In] IGraphBuilder pfg);
		void GetFiltergraph( /* not used */ );
		void SetOutputFileName( /* not used */ );
		void FindInterface(
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid pCategory,
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid pType,
			[In] IBaseFilter pbf,
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
			[Out, MarshalAs(UnmanagedType.IUnknown)] out object ppint);
		void RenderStream(
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid PinCategory,
			[In, MarshalAs(UnmanagedType.LPStruct)] Guid MediaType,
			[In, MarshalAs(UnmanagedType.IUnknown)] object pSource,
			[In] IBaseFilter pfCompressor,
			[In] IBaseFilter pfRenderer);
		void ControlStream( /* not used */ );
		void AllocCapFile( /* not used */ );
		void CopyCaptureFile( /* not used */ );
		void FindPin( /* not used */ );
	}

	[ComImport, Guid("BF87B6E1-8C27-11d0-B3F0-00AA003761C5")]
	internal class CaptureGraphBuilder2 {
	}

	[ComImport, Guid("6B652FFF-11FE-4fce-92AD-0266B5D7C78F"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface ISampleGrabber {
		void SetOneShot([In, MarshalAs(UnmanagedType.Bool)] bool OneShot);
		void SetMediaType([In, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt);
		void GetConnectedMediaType([Out, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt);
		void SetBufferSamples([In, MarshalAs(UnmanagedType.Bool)] bool BufferThem);
		void GetCurrentBuffer( /* not used */ );
		void GetCurrentSample( /* not used */ );
		void SetCallback(ISampleGrabberCB pCallback, int WhichMethodToCallback);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal class AMMediaType {
		public Guid majorType;
		public Guid subType;
		[MarshalAs(UnmanagedType.Bool)]
		public bool fixedSizeSamples;
		[MarshalAs(UnmanagedType.Bool)]
		public bool temporalCompression;
		public int sampleSize;
		public Guid formatType;
		public IntPtr unkPtr;
		public int formatSize;
		public IntPtr formatPtr;

		public void Free() {
			if(formatSize != 0) {
				Marshal.FreeCoTaskMem(formatPtr);
				formatSize = 0;
				formatPtr = IntPtr.Zero;
			}
			if(unkPtr != IntPtr.Zero) {
				Marshal.Release(unkPtr);
				unkPtr = IntPtr.Zero;
			}
		}
	}

	[ComImport, Guid("C1F400A0-3F08-11d3-9F0B-006008039E37")]
	internal class SampleGrabber {
	}

	[ComImport, Guid("C6E13340-30AC-11d0-A18C-00A0C9118956"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAMStreamConfig {
		void SetFormat([In, MarshalAs(UnmanagedType.LPStruct)] AMMediaType pmt);
		void GetFormat([Out] out AMMediaType pmt);
		void GetNumberOfCapabilities( /* not used */ );
		void GetStreamCaps( /* not used */ );
	}

	[StructLayout(LayoutKind.Sequential)]
	internal class VideoInfoHeader {
		public Rect SrcRect;
		public Rect TargetRect;
		public int BitRate;
		public int BitErrorRate;
		public long AvgTimePerFrame;
		public BitmapInfoHeader BmiHeader;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal class Rect {
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class BitmapInfoHeader {
		public int Size;
		public int Width;
		public int Height;
		public short Planes;
		public short BitCount;
		public int Compression;
		public int ImageSize;
		public int XPelsPerMeter;
		public int YPelsPerMeter;
		public int ClrUsed;
		public int ClrImportant;
	}

	[ComImport, Guid("C1F400A4-3F08-11d3-9F0B-006008039E37")]
	internal class NullRenderer {
	}

	[ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IPropertyBag {
		void Read(
			[In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
			[Out, MarshalAs(UnmanagedType.Struct)] out object pVar,
			[In] object pErrorLog);
		void Write( /* not used */ );
	}
}
