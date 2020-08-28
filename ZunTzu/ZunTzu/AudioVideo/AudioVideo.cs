// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.AudioVideo {

	public struct AudioProperties {
		public bool MuteAll;
		public bool MuteSoundEffects;
		public bool MuteRecording;
		public bool MutePlayback;
		public bool UseVoiceActivation;
		public bool AdjustActivationThresholdAutomatically;
		///<remarks>This value can range from 0 through 99</remarks>
		public int ActivationThreshold;
		public bool ActivateEchoSuppression;
		public bool UseAutomaticJitterControl;
		///<remarks>This value can range from 0 through 99</remarks>
		public int JitterControl;
		public bool DisableAutomaticGainControl;
		///<remarks>This value can range from -10,000 through 0</remarks>
		public int MicrophoneInputLevel;
		public bool DisableAutoconfiguration;
	}

	public enum AudioTrack { None, Shuffle, Die0, Die1, Die2, Die3, Die4, Die5, Die6 }

	public delegate void AudioPropertiesChangedHandler();

	/// <summary>An audio device.</summary>
	public interface IAudioManager : IDisposable {
		/// <summary>Settings for audio recording and playback.</summary>
		AudioProperties AudioProperties { get; set; }
		/// <summary>Occurs when the audio settings have changed.</summary>
		event AudioPropertiesChangedHandler AudioPropertiesChanged;
		/// <summary>Plays a WMA audio file on disk.</summary>
		/// <param name="fileName">File name.</param>
		void PlayAudioFile(string fileName);
		/// <summary>Plays a sound from the sound bank.</summary>
		/// <param name="track">Sound to play.</param>
		void PlayAudioTrack(AudioTrack track);
		/// <summary>Informs the 3D positioning sound system of display area.</summary>
		/// <param name="gameDisplayArea">The game display area.</param>
		void SetGameDisplayArea(RectangleF gameDisplayArea);
		/// <summary>Sets the origin of the sound in 3D space.</summary>
		/// <param name="track">Sound to play.</param>
		/// <param name="origin">Position of the origin of the sound.</param>
		void SetAudioTrackOrigin(AudioTrack track, PointF origin);
	}

	/// <summary>A video capture device.</summary>
	public interface IVideoCaptureDevice {
		/// <summary>Friendly name of the device.</summary>
		string Name { get; }
	}

	/// <summary>Delegate for the FrameCaptured event.</summary>
	/// <param name="frameBuffer">This is a buffer of pixels in RGB24 format.</param>
	public delegate void FrameCapturedHandler(IntPtr frameBuffer);

	public interface IVideoCaptureManager {
		/// <summary>A list of available video capture devices.</summary>
		IVideoCaptureDevice[] AvailableDevices { get; }
		/// <summary>The video capture device to be used.</summary>
		IVideoCaptureDevice Device { get; set; }
		/// <summary>Starts the capture.</summary>
		void Start();
		/// <summary>Stops the capture.</summary>
		void Stop();
		/// <summary>True if the capture has been started.</summary>
		bool Running { get; }
		/// <summary>Number of frames per second.</summary>
		float FrameRate { get; }
		/// <summary>Size of the bitmap of each frame.</summary>
		Size FrameSize { get; }
		/// <summary>Occurs when a new frame is ready.</summary>
		event FrameCapturedHandler FrameCaptured;
	}
}
