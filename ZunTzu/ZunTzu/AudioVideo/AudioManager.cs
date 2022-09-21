// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ZunTzu.AudioVideo
{

    /// <summary>An audio device.</summary>
    public class AudioManager : IAudioManager {

		private static string[] soundBank = { "Shuffle", "Die0", "Die1", "Die2", "Die3", "Die4", "Die5", "Die6" };

		/// <summary>Constructor.</summary>
		public AudioManager(Form owner, AudioProperties audioProperties) {
			_audioProperties = audioProperties;

			_audioEnabled = DS.CreateAudio(owner.Handle);

			if (!_audioEnabled)
            {
				MessageBox.Show(
					"Sound card doesn't meet ZunTzu's minimum requirements. Are the drivers installed?",
					"Can't use sound",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}

			_soundBuffers = new DSBuffer[soundBank.Length];

			restoreSoundBuffers();
		}

		/// <summary>Settings for audio recording and playback.</summary>
		public AudioProperties AudioProperties {
			get { return _audioProperties; }
			set {
				_audioProperties = value;
				if(AudioPropertiesChanged != null)
					AudioPropertiesChanged();
			}
		}

		/// <summary>Occurs when the audio settings have changed.</summary>
		public event AudioPropertiesChangedHandler AudioPropertiesChanged;

		/// <summary>Plays a WMA audio file on disk.</summary>
		/// <param name="fileName">File name.</param>
		public void PlayAudioFile(string fileName) {
			if(_audioEnabled && !_audioProperties.MuteSoundEffects) {
				string fullPath = Path.Combine(
					(ApplicationDeployment.IsNetworkDeployed ?
						ApplicationDeployment.CurrentDeployment.DataDirectory :
						Application.StartupPath),
					fileName);
				ThreadPool.QueueUserWorkItem(new WaitCallback(playAudioFile), fullPath);
			}
		}

		/// <summary>Plays a sound from the sound bank.</summary>
		/// <param name="track">Sound to play.</param>
		public void PlayAudioTrack(AudioTrack track) {
			if(_audioEnabled && !_audioProperties.MuteSoundEffects) {
				DSBuffer buffer = _soundBuffers[(int)track - 1];
				if (!buffer.Play())
                {
					restoreSoundBuffers();
					buffer = _soundBuffers[(int)track - 1];
					buffer.Play();
				}
			}
		}

		/// <summary>Sets the origin of the sound in 3D space.</summary>
		/// <param name="track">Sound to play.</param>
		/// <param name="origin">Position of the origin of the sound.</param>
		public void SetAudioTrackOrigin(AudioTrack track, PointF origin)
        {
			if (_audioEnabled)
			{
				float xCoeff = 1.0f;
				float yCoeff = -1.0f * _gameDisplayArea.Height / _gameDisplayArea.Width;
				float x = xCoeff * ((2.0f * (origin.X - _gameDisplayArea.Left) / _gameDisplayArea.Width) - 1.0f);
				float y = yCoeff * ((2.0f * (origin.Y - _gameDisplayArea.Top) / _gameDisplayArea.Height) - 1.0f);
				float z = 1.0f;

				DSBuffer buffer = _soundBuffers[(int)track - 1];
				buffer.SetSoundBuffer3DPosition(x, y, z);
			}
		}

		/// <summary>Informs the 3D positioning sound system of display area.</summary>
		/// <param name="gameDisplayArea">The game display area.</param>
		public void SetGameDisplayArea(RectangleF gameDisplayArea) {
			_gameDisplayArea = gameDisplayArea;
		}

		public void Dispose() {
			if(_audioEnabled) {
				_audioEnabled = false;
				for (int i = 0; i < _soundBuffers.Length; ++i) {
					_soundBuffers[i].Dispose();
				}
				DS.FreeAudio();
			}
		}

		static void playAudioFile(object fileName) {
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

		void restoreSoundBuffers() {
			if(_audioEnabled) {
				for(int i = 0; i < soundBank.Length; ++i) {
					if(_soundBuffers[i] != null) {
						_soundBuffers[i].Dispose();
					}
					using(Stream resourceStream = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles." + soundBank[i] + ".wav").Open()) {
						_soundBuffers[i] = DSBuffer.Create(resourceStream);
					}
				}
			}
		}

		AudioProperties _audioProperties;
		bool _audioEnabled;
		DSBuffer[] _soundBuffers;
		RectangleF _gameDisplayArea = new RectangleF(0.0f, 0.0f, 800.0f, 600.0f);
	}
}
