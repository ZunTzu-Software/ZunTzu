/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"
#include <mmreg.h>
#include <DSound.h>

LPDIRECTSOUND8 direct_sound = nullptr;

extern "C" bool __cdecl CreateAudio(void* hMainWnd)
{
	HWND main_window = static_cast<HWND>(hMainWnd);

    if (FAILED(DirectSoundCreate8(nullptr, &direct_sound, nullptr))) return false;
	if (FAILED(direct_sound->SetCooperativeLevel(main_window, DSSCL_PRIORITY))) return false;

	return true;
}

extern "C" void __cdecl FreeAudio()
{
	if (direct_sound != nullptr) {
		direct_sound->Release();
		direct_sound = nullptr;
	}
}

extern "C" void* __cdecl CreateSoundBuffer(byte* wav_data, int wav_size)
{
	// locate chunks in the file (it is in a RIFF format)

	byte* ptr = wav_data;

	DWORD riff_chunk_type = (DWORD)ptr[0] | ((DWORD)ptr[1] << 8) | ((DWORD)ptr[2] << 16) | ((DWORD)ptr[3] << 24);
	if (riff_chunk_type != 'FFIR') return nullptr;

	DWORD riff_chunk_size = (DWORD)ptr[4] | ((DWORD)ptr[5] << 8) | ((DWORD)ptr[6] << 16) | ((DWORD)ptr[7] << 24);

	DWORD file_type = (DWORD)ptr[8] | ((DWORD)ptr[9] << 8) | ((DWORD)ptr[10] << 16) | ((DWORD)ptr[11] << 24);
	if (file_type != 'EVAW') return nullptr;

	DWORD fmt_chunk_type = (DWORD)ptr[12] | ((DWORD)ptr[13] << 8) | ((DWORD)ptr[14] << 16) | ((DWORD)ptr[15] << 24);
	if (fmt_chunk_type != ' tmf') return nullptr;

	DWORD fmt_chunk_size = (DWORD)ptr[16] | ((DWORD)ptr[17] << 8) | ((DWORD)ptr[18] << 16) | ((DWORD)ptr[19] << 24);
	if (fmt_chunk_size > sizeof(WAVEFORMATEX)) return nullptr;

	DWORD fmt_chunk_offset = 20;

	ptr += fmt_chunk_offset + fmt_chunk_size;

	DWORD data_chunk_type = (DWORD)ptr[0] | ((DWORD)ptr[1] << 8) | ((DWORD)ptr[2] << 16) | ((DWORD)ptr[3] << 24);
	if (data_chunk_type != 'atad') return nullptr;

	DWORD data_chunk_size = (DWORD)ptr[4] | ((DWORD)ptr[5] << 8) | ((DWORD)ptr[6] << 16) | ((DWORD)ptr[7] << 24);
	DWORD data_chunk_offset = fmt_chunk_offset + fmt_chunk_size + 8;

	// load WAV data into a sound buffer

	WAVEFORMATEX format = {};
	memcpy(&format, wav_data + fmt_chunk_offset, fmt_chunk_size);

	DSBUFFERDESC desc = {};
	desc.dwSize = sizeof(DSBUFFERDESC);
	desc.dwFlags = DSBCAPS_GLOBALFOCUS | DSBCAPS_CTRL3D;
	desc.dwBufferBytes = (DWORD)data_chunk_size;
	desc.lpwfxFormat = &format;

	IDirectSoundBuffer* sb = nullptr;
	if (FAILED(direct_sound->CreateSoundBuffer(&desc, &sb, nullptr))) return nullptr;

	void* ptr1 = nullptr;
	DWORD size1 = 0;
	if (FAILED(sb->Lock(0, 0, &ptr1, &size1, nullptr, nullptr, DSBLOCK_ENTIREBUFFER))) return nullptr;
	if (size1 != data_chunk_size) return nullptr;

	memcpy(ptr1, wav_data + data_chunk_offset, data_chunk_size);

	if (FAILED(sb->Unlock(ptr1, wav_size, nullptr, 0))) return nullptr;

	IDirectSound3DBuffer* sb3D = nullptr;
	if (FAILED(sb->QueryInterface(IID_IDirectSound3DBuffer, (LPVOID*)&sb3D))) return false;
	sb3D->SetMinDistance(2.0f, DS3D_IMMEDIATE);
	sb3D->Release();

	return sb;
}

extern "C" void __cdecl FreeSoundBuffer(void* sound_buffer)
{
	IDirectSoundBuffer* sb = static_cast<IDirectSoundBuffer*>(sound_buffer);
	sb->Release();
}

extern "C" void __cdecl SetSoundBuffer3DPosition(void* sound_buffer, float x, float y, float z)
{
	IDirectSoundBuffer* sb = static_cast<IDirectSoundBuffer*>(sound_buffer);
	
	IDirectSound3DBuffer* sb3D = nullptr;
	if (SUCCEEDED(sb->QueryInterface(IID_IDirectSound3DBuffer, (LPVOID*)&sb3D)))
	{
		sb3D->SetPosition(x, y, z, DS3D_IMMEDIATE);
		sb3D->Release();
	}
}

extern "C" bool __cdecl PlaySoundBuffer(void* sound_buffer)
{
	IDirectSoundBuffer* sb = static_cast<IDirectSoundBuffer*>(sound_buffer);

	DWORD status = 0;
	if (FAILED(sb->GetStatus(&status)) || (status & DSBSTATUS_BUFFERLOST)) return false;

	sb->SetCurrentPosition(0);
	sb->Play(0, 0, 0);
}
