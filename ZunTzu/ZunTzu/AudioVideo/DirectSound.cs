// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.AudioVideo
{
    static class DS
    {
		public static bool CreateAudio(IntPtr hMainWnd)
        {
            return ZunTzuLib.CreateAudio(hMainWnd);
        }

		public static void FreeAudio()
        {
            ZunTzuLib.FreeAudio();
        }
	}

    sealed class DSBuffer : IDisposable
    {
        public static DSBuffer Create(System.IO.Stream wavStream)
        {
            byte[] buffer = new byte[wavStream.Length];
            wavStream.Read(buffer, 0, buffer.Length);
            unsafe
            {
                fixed (byte* data = buffer)
                {
                    IntPtr sb = ZunTzuLib.CreateSoundBuffer(data, buffer.Length);
                    return new DSBuffer { _internal = sb };
                }
            }
        }

        public void SetSoundBuffer3DPosition(float x, float y, float z)
        {
            ZunTzuLib.SetSoundBuffer3DPosition(_internal, x, y, z);
        }

        public bool Play()
        {
            return ZunTzuLib.PlaySoundBuffer(_internal);
        }

        public void Dispose()
        {
            if (_internal != IntPtr.Zero)
            {
                ZunTzuLib.FreeSoundBuffer(_internal);
                _internal = IntPtr.Zero;
            }
        }

        IntPtr _internal;
    }
}
