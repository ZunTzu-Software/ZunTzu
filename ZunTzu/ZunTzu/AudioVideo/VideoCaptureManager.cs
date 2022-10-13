// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ZunTzu.AudioVideo {

	/// <summary>The class in charge of capturing the video frames.</summary>
	public sealed class VideoCaptureManager : IVideoCaptureManager, ISampleGrabberCB {

		/// <summary>A list of available video capture devices.</summary>
		public IVideoCaptureDevice[] AvailableDevices {
			get {
				ICreateDevEnum createDevEnum = (ICreateDevEnum) new SystemDeviceEnum();
				IEnumMoniker enumMoniker;
				if(1 != createDevEnum.CreateClassEnumerator(
					new Guid(0x860BB310, 0x5D01, 0x11d0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86),	// CLSID_VideoInputDeviceCategory
					out enumMoniker, 0)) {
					List<VideoCaptureDevice> devices = new List<VideoCaptureDevice>();
					IMoniker[] moniker = new IMoniker[1];
					while((enumMoniker.Next(1, moniker, IntPtr.Zero) == 0))
						devices.Add(new VideoCaptureDevice(moniker[0]));
					return devices.ToArray();
				} else {
					return new VideoCaptureDevice[0];
				}
			}
		}

		/// <summary>The video capture device to be used.</summary>
		public IVideoCaptureDevice Device {
			get { return device; }
			set {
				if(value != device) {
					device = (VideoCaptureDevice) value;
					setupDirectShowFilterGraph();
				}
			}
		}

		public void Start() {
			Debug.Assert(mediaControl != null && !running);
			if(!running) {
				mediaControl.Run();
				running = true;
			}
		}

		public void Stop() {
			Debug.Assert(mediaControl != null && running);
			if(running) {
				mediaControl.StopWhenReady();
				running = false;
			}
		}

		public bool Running { get { return running; } }

		public float FrameRate { get { return frameRate; } }

		public Size FrameSize { get { return frameSize; } }

		public event FrameCapturedHandler FrameCaptured;

		private sealed class VideoCaptureDevice : IVideoCaptureDevice {
			public VideoCaptureDevice(IMoniker moniker) {
				Debug.Assert(moniker != null);
				this.moniker = moniker;

				// retrieve name
				Guid bagRiid = typeof(IPropertyBag).GUID;
				object propertyValue = null;
				object bag = null;
				try {
					moniker.BindToStorage(null, null, ref bagRiid, out bag);
					((IPropertyBag) bag).Read("FriendlyName", out propertyValue, null);
				} catch {
				} finally {
					if(bag != null)
						Marshal.ReleaseComObject(bag);
				}
				name = propertyValue as string;
			}

			public string Name { get { return name; } }
			public IMoniker Moniker { get { return moniker; } }

			private string name;
			private IMoniker moniker;
		}

		/// <summary>Not implemented.</summary>
		int ISampleGrabberCB.SampleCB(double sampleTime, IMediaSample sample) {
			Marshal.ReleaseComObject(sample);
			return 0;
		}
		/// <summary>Called by the sample grabber on its deliver thread.</summary>
		int ISampleGrabberCB.BufferCB(double sampleTime, IntPtr buffer, int bufferSize) {
			//byte* resampledFrame = stackalloc byte[64 * 64 * 3];
			//resampleFrame(buffer, resampledFrame);
			FrameCaptured(buffer);
			return 0;
		}

		private void setupDirectShowFilterGraph() {
			if(mediaControl != null && running)
				Stop();

			if(device == null) {
				filterGraph = null;
				mediaControl = null;
			} else {
				filterGraph = (IFilterGraph2) new FilterGraph();
				mediaControl = (IMediaControl) filterGraph;
				ICaptureGraphBuilder2 captureGraphBuilder = (ICaptureGraphBuilder2) new CaptureGraphBuilder2();
				captureGraphBuilder.SetFiltergraph((IGraphBuilder) filterGraph);

				// capture filter
				IBaseFilter captureFilter;
				filterGraph.AddSourceFilterForMoniker(device.Moniker, null, device.Name, out captureFilter);

				// sample grabber
				ISampleGrabber sampleGrabber = (ISampleGrabber) new SampleGrabber();
				IBaseFilter sampleGrabberFilter = (IBaseFilter) sampleGrabber;
				{
					var mediaType = new AMMediaType
					{
						majorType = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71), // MEDIATYPE_Video
						subType = new Guid(0xe436eb7d, 0x524f, 0x11ce, 0x9f, 0x53, 0x00, 0x20, 0xaf, 0x0b, 0xa7, 0x70),   // MEDIASUBTYPE_RGB24
						formatType = new Guid(0x05589f80, 0xc356, 0x11ce, 0xbf, 0x01, 0x00, 0xaa, 0x00, 0x55, 0x59, 0x5a),    // FORMAT_VideoInfo
					};
                    sampleGrabber.SetMediaType(mediaType);
                }
                sampleGrabber.SetOneShot(false);
				sampleGrabber.SetBufferSamples(false);
				sampleGrabber.SetCallback(this, 1);
				filterGraph.AddFilter(sampleGrabberFilter, "ZunTzu Sample Grabber");

				// renderer
				IBaseFilter nullRenderer = (IBaseFilter) new NullRenderer();
				filterGraph.AddFilter(nullRenderer, "Null Renderer");

				captureGraphBuilder.RenderStream(
					new Guid(0xfb6c4281, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba),	// PIN_CATEGORY_CAPTURE
					new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71),	// MEDIATYPE_Video
					captureFilter, sampleGrabberFilter, nullRenderer);

				// retrieve frame size
				{
                    AMMediaType mediaType;
					sampleGrabber.GetConnectedMediaType(out mediaType);
                    var infoHeader = new VideoInfoHeader();
                    Marshal.PtrToStructure(mediaType.formatPtr, infoHeader);
					frameRate = 10000000.0f / infoHeader.AvgTimePerFrame;
					frameSize = new Size(infoHeader.BmiHeader.Width, infoHeader.BmiHeader.Height);
					mediaType.Free();
				}
			}
		}

		private float frameRate;
		private Size frameSize = Size.Empty;

		private VideoCaptureDevice device = null;
		private IFilterGraph2 filterGraph = null;
		private IMediaControl mediaControl = null;
		private bool running = false;
	}
}
