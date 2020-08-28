// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using ZunTzu.Networking;
using ZunTzu.AudioVideo;

namespace ZunTzu.Control {

	/// <summary>Sub-component in charge of sending and receiving video frames through the network.</summary>
	internal sealed class VideoConferencingClient {

		public VideoConferencingClient(Controller controller) {
			this.controller = controller;
		}

		/// <summary>A list of available video capture devices.</summary>
		public IVideoCaptureDevice[] AvailableDevices { get { return videoCaptureManager.AvailableDevices; } }

		/// <summary>The device used for video capture.</summary>
		public IVideoCaptureDevice Device { get { return device; } set { device = value; } }

		public bool CaptureEnabled {
			get { return captureEnabled; }
			set {
				if(value != captureEnabled) {
					captureEnabled = value;
					if(captureEnabled) {
						videoCaptureManager.FrameCaptured += new FrameCapturedHandler(onVideoFrameCaptured);
						videoCaptureManager.Device = device;
						videoCaptureManager.Start();
					} else {
						videoCaptureManager.Stop();
						videoCaptureManager.FrameCaptured -= new FrameCapturedHandler(onVideoFrameCaptured);
						videoCaptureManager.Device = null;
					}
				}
			}
		}

		public bool PlaybackEnabled {
			get { return playbackEnabled; }
			set { playbackEnabled = value; }
		}

		private unsafe void onVideoFrameCaptured(IntPtr frameBuffer) {
			byte[] resampledFrame = new byte[64 * 64 * 3];
			fixed(byte* resampledFramePtr = resampledFrame) {
				resampleFrame((byte*) frameBuffer, resampledFramePtr);
			}
			controller.MainForm.BeginInvoke(new VideoFrameCapturedDelegate(onVideoFrameCapturedThreadSafe), new object[] { resampledFrame });
		}

		private delegate void VideoFrameCapturedDelegate(byte[] frameBuffer);
		private void onVideoFrameCapturedThreadSafe(byte[] frameBuffer) {
			controller.View.PlayerView.UpdateVideoFrame(controller.Model.ThisPlayer.Id, frameBuffer);
			if(controller.Model.NetworkClient.Status != NetworkStatus.Disconnected) {
				controller.Model.NetworkClient.SendVideoFrame(frameBuffer);
			}
		}

		/// <summary>Resizes and crops a frame image to 64x64 R8G8B8.</summary>
		/// <param name="originalFrame">The original frame image in R8G8B8 format.</param>
		/// <param name="resampledFrame">A buffer that will be overwritten with the resampled frame image.</param>
		private unsafe void resampleFrame(byte* originalFrame, byte* resampledFrame) {
			int width = videoCaptureManager.FrameSize.Width;
			int height = videoCaptureManager.FrameSize.Height;
			float sampling = height / 64.0f;
			float invSqSampling = 1.0f / (sampling * sampling);

			byte* dest = resampledFrame;
			for(int y = 0; y < 64; ++y) {
				for(int x = 0; x < 64; ++x, dest += 3) {
					float red = 0.0f;
					float green = 0.0f;
					float blue = 0.0f;

					int srcY = (int) (y * sampling);
					float pixWeightY = 1.0f - y * sampling + srcY;
					float pixLeftY = sampling;
					while(true) {	// sum along y
						int srcX = (int) (x * sampling);
						float pixLeftX = sampling;
						float pixWeightX = 1.0f - x * sampling + srcX;
						srcX += (width - height) / 2;
						float pixCombinedWeight = pixWeightX * pixWeightY;
						// to turn image upside down, replace "(height - srcY)" by "srcY" in the line below
						byte* src = originalFrame + (height - srcY) * width * 3 + srcX * 3;
						while(true) {	// sum along x
							pixLeftX -= pixWeightX;
							red += *(src + 0) * pixCombinedWeight;
							green += *(src + 1) * pixCombinedWeight;
							blue += *(src + 2) * pixCombinedWeight;
							if(pixLeftX == 0.0f)
								break;
							pixWeightX = Math.Min(1.0f, pixLeftX);
							src += 3;
							pixCombinedWeight = pixWeightX * pixWeightY;
						}
						pixLeftY -= pixWeightY;
						++srcY;
						if(pixLeftY == 0.0f)
							break;
						pixWeightY = Math.Min(1.0f, pixLeftY);
					}

					*(dest + 0) = (byte) (red * invSqSampling + 0.5f);
					*(dest + 1) = (byte) (green * invSqSampling + 0.5f);
					*(dest + 2) = (byte) (blue * invSqSampling + 0.5f);
				}
			}
		}

		private Controller controller;
		private IVideoCaptureManager videoCaptureManager = new VideoCaptureManager();
		private IVideoCaptureDevice device = null;
		private bool captureEnabled = false;
		private bool playbackEnabled = true;
	}
}
