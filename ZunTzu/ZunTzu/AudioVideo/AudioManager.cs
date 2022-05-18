// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.DirectX.DirectSound;

namespace ZunTzu.AudioVideo {

	/// <summary>An audio device.</summary>
	public class AudioManager : IAudioManager {

		private static string[] soundBank = { "Shuffle", "Die0", "Die1", "Die2", "Die3", "Die4", "Die5", "Die6" };

		/// <summary>Constructor.</summary>
		public AudioManager(Form owner, AudioProperties audioProperties) {
			this.audioProperties = audioProperties;
			try {
				device = new Device();
				device.SetCooperativeLevel(owner, CooperativeLevel.Priority);
			} catch(ApplicationException) {
				device = null;
				System.Windows.Forms.MessageBox.Show(
					"Sound card doesn't meet ZunTzu's minimum requirements. Are the drivers installed?",
					"Can't use sound",
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Warning);
			}

			secondaryBuffers = new SecondaryBuffer[soundBank.Length];
			buffers3D = new Buffer3D[soundBank.Length];

			restoreSoundBuffers();
		}

		/// <summary>Settings for audio recording and playback.</summary>
		public AudioProperties AudioProperties {
			get { return audioProperties; }
			set {
				audioProperties = value;
				if(AudioPropertiesChanged != null)
					AudioPropertiesChanged();
			}
		}

		/// <summary>Occurs when the audio settings have changed.</summary>
		public event AudioPropertiesChangedHandler AudioPropertiesChanged;
			
		public Device DirectSoundDevice { get { return device; } }

		/// <summary>Plays a WMA audio file on disk.</summary>
		/// <param name="fileName">File name.</param>
		public void PlayAudioFile(string fileName) {
			if(device != null && !audioProperties.MuteSoundEffects) {
				string fullPath = Path.Combine(
					(ApplicationDeployment.IsNetworkDeployed ?
						ApplicationDeployment.CurrentDeployment.DataDirectory :
						System.Windows.Forms.Application.StartupPath),
					fileName);
				ThreadPool.QueueUserWorkItem(new WaitCallback(playAudioFile), fullPath);
			}
		}

		/// <summary>Plays a sound from the sound bank.</summary>
		/// <param name="track">Sound to play.</param>
		public void PlayAudioTrack(AudioTrack track) {
			if(device != null && !audioProperties.MuteSoundEffects) {
				SecondaryBuffer buffer = secondaryBuffers[(int) track - 1];
				try {
					if(buffer.Status.BufferLost) {
						restoreSoundBuffers();
						buffer = secondaryBuffers[(int) track - 1];
					}
					buffer.SetCurrentPosition(0);
					buffer.Play(0, BufferPlayFlags.Default);
				} catch(Exception) {}
			}
		}

		/// <summary>Informs the 3D positioning sound system of display area.</summary>
		/// <param name="gameDisplayArea">The game display area.</param>
		public void SetGameDisplayArea(RectangleF gameDisplayArea) {
			this.gameDisplayArea = gameDisplayArea;
		}

		/// <summary>Sets the origin of the sound in 3D space.</summary>
		/// <param name="track">Sound to play.</param>
		/// <param name="origin">Position of the origin of the sound.</param>
		public void SetAudioTrackOrigin(AudioTrack track, PointF origin) {
			if(device != null) {
				Buffer3D buffer = buffers3D[(int) track - 1];
				float xCoeff = 1.0f;
				float yCoeff = -1.0f * gameDisplayArea.Height / gameDisplayArea.Width;
				buffer.Position = new Microsoft.DirectX.Vector3(
					xCoeff * ((2.0f * (origin.X - gameDisplayArea.Left) / gameDisplayArea.Width) - 1.0f),
					yCoeff * ((2.0f * (origin.Y - gameDisplayArea.Top) / gameDisplayArea.Height) - 1.0f),
					1.0f);
			}
		}

		public void Dispose() {
			if(device != null && !device.Disposed) {
				for(int i = 0; i < secondaryBuffers.Length; ++i) {
					buffers3D[i].Dispose();
					secondaryBuffers[i].Dispose();
				}
				device.Dispose();
			}
		}

		private static void playAudioFile(object fileName) {
			try {
				IFilterGraph2 filterGraph = (IFilterGraph2) new FilterGraph();
				if(0 == filterGraph.RenderFile(fileName as string, IntPtr.Zero)) {
					IMediaControl mediaControl = (IMediaControl) filterGraph;
					mediaControl.Run();

					IMediaEvent mediaEvent = (IMediaEvent) filterGraph;
					int eventCode;
					mediaEvent.WaitForCompletion(6000, out eventCode);
				}
			} catch(Exception) { }
		}

		private void restoreSoundBuffers() {
			if(device != null) {
				BufferDescription bufferDescription = new BufferDescription();
				bufferDescription.ControlEffects = false;
				bufferDescription.ControlVolume = false;
				bufferDescription.ControlFrequency = false;
				bufferDescription.GlobalFocus = true;
				bufferDescription.Control3D = true;

				for(int i = 0; i < soundBank.Length; ++i) {
					if(secondaryBuffers[i] != null) {
						buffers3D[i].Dispose();
						secondaryBuffers[i].Dispose();
					}
					using(System.IO.Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles." + soundBank[i] + ".wav").Open()) {
						secondaryBuffers[i] = new SecondaryBuffer(resourceStream, bufferDescription, device);
						buffers3D[i] = new Buffer3D(secondaryBuffers[i]);
						buffers3D[i].MinDistance = 2.0f;
					}
				}
			}
		}

		private Device device;
		private AudioProperties audioProperties;
		private SecondaryBuffer[] secondaryBuffers;
		private Buffer3D[] buffers3D;
		private RectangleF gameDisplayArea = new RectangleF(0.0f, 0.0f, 800.0f, 600.0f);
	}
}
