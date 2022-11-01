// ZunTzuLib.h : main header file for the ZunTzuLib DLL
// Copyright (c) 2006-2022 ZunTzu Software and contributors

#pragma once

#include "resource.h"		// main symbols

extern "C" {
	// DXT compression routines
	__declspec(dllexport) void __cdecl CompressDxt1(const char * rgb, int top, int left, int bottom, int right, int stride, char * blocks, int option);
	__declspec(dllexport) void __cdecl CompressDxt1FromRgba(const char * rgba, int top, int left, int bottom, int right, int stride, char * blocks, int option);
	__declspec(dllexport) void __cdecl CompressDxt5(const char * rgba, int top, int left, int bottom, int right, int stride, char * blocks, int option);

	// Image loading
	__declspec(dllexport) void * __cdecl CreateImageLoader(const wchar_t * archive_name, const char * image_entry_name, const char * mask_entry_name, unsigned int skipped_mipmap_levels, int options);
	__declspec(dllexport) int __cdecl GetImageDimensions(void * image_loader, unsigned int * width, unsigned int * height);
	__declspec(dllexport) int __cdecl LoadNextTile(void * image_loader, char * tile, unsigned int * mipmap_level, unsigned int * x, unsigned int * y);
	__declspec(dllexport) void __cdecl FreeImageLoader(void * image_loader);

	// System info
	__declspec(dllexport) int __cdecl GetProcessorCoreCount();

	// Networking
	__declspec(dllexport) void* __cdecl CreatePeer();
	__declspec(dllexport) void __cdecl FreePeer(void* client_or_server);
	__declspec(dllexport) int __cdecl StartupClient(void* client, unsigned short port);
	__declspec(dllexport) int __cdecl StartupServer(void* server, unsigned short port);
	__declspec(dllexport) void __cdecl Shutdown(void* client_or_server);
	__declspec(dllexport) int __cdecl Connect(void* client, const char* host, unsigned short remote_port);
	__declspec(dllexport) int __cdecl Send(void* client, const char* data, int length, int priority, int reliability, int ordering_channel, void* system_identifier, bool broadcast);
	__declspec(dllexport) void* __cdecl Receive(void* client_or_server);
	__declspec(dllexport) void __cdecl DeallocatePacket(void* client_or_server, void* packet);
	__declspec(dllexport) unsigned long long __cdecl GetGuid(void* client);
	__declspec(dllexport) unsigned long __cdecl GetBoundAddress(void* server);

	// Direct3D
	__declspec(dllexport) int __cdecl GetEligibleFullscreenModeCount();
	__declspec(dllexport) void __cdecl GetEligibleFullscreenMode(int index, int* width, int* height, int* refresh_rate, int* format);
	__declspec(dllexport) void __cdecl GetCurrentDisplayMode(int* width, int* height, int* refresh_rate, int* format);
	__declspec(dllexport) bool __cdecl CreateDevice(void* hMainWnd, bool full_screen, int width, int height, int refresh_rate, int display_format, bool wait_for_vertical_blank);
	__declspec(dllexport) void __cdecl FreeDevice();
	__declspec(dllexport) int __cdecl CheckCooperativeLevel();
	__declspec(dllexport) bool __cdecl ResetDevice();
	__declspec(dllexport) bool __cdecl UpdatePresentParameters(bool full_screen, int width, int height, int refresh_rate, int display_format, bool wait_for_vertical_blank);
	__declspec(dllexport) void __cdecl BeginFrame();
	__declspec(dllexport) void __cdecl EndFrame();
	__declspec(dllexport) void __cdecl RenderMonochromaticQuad(unsigned int color, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3);
	__declspec(dllexport) void __cdecl RenderTexturedQuad(void* texture, unsigned int modulation_color, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float tex_top, float tex_right, float tex_bottom, float tex_left);
	__declspec(dllexport) void __cdecl RenderTexturedQuadSilhouette(void* texture, unsigned int modulation_color, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float tex_top, float tex_right, float tex_bottom, float tex_left);
	__declspec(dllexport) void __cdecl RenderTexturedQuadIgnoreMask(void* texture, unsigned int modulation_color, float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float tex_top, float tex_right, float tex_bottom, float tex_left);
	__declspec(dllexport) void __cdecl RenderDieMesh(void* mesh_vb, void* mesh_ib, void* mesh_texture, int mesh_vertex_count, int mesh_triangle_count, float x, float y, float size_factor, float rot_x, float rot_y, float rot_z, float rot_w, unsigned int die_color, unsigned int pips_color);
	__declspec(dllexport) void __cdecl RenderCustomDieMesh(void* mesh_vb, void* mesh_ib, void* mesh_texture, int mesh_vertex_count, int mesh_triangle_count, float x, float y, float size_factor, float rot_x, float rot_y, float rot_z, float rot_w);
	__declspec(dllexport) void __cdecl RenderDieMeshShadow(void* mesh_vb, void* mesh_ib, void* mesh_texture, int mesh_vertex_count, int mesh_triangle_count, float mesh_inradius, float x, float y, float size_factor, float rot_x, float rot_y, float rot_z, float rot_w, unsigned int shadow_color);
	__declspec(dllexport) void* __cdecl CreateTexture(int width, int height, int format);
	__declspec(dllexport) void __cdecl LockTexture(void* texture, int* pitch, char** bits);
	__declspec(dllexport) void __cdecl LockTextureReadOnly(void* texture, int* pitch, char** bits);
	__declspec(dllexport) void __cdecl UnlockTexture(void* texture);
	__declspec(dllexport) void __cdecl FreeTexture(void* texture);
	__declspec(dllexport) void* __cdecl CreateVertexBuffer(int vertex_count, void* data);
	__declspec(dllexport) void __cdecl FreeVertexBuffer(void* vb);
	__declspec(dllexport) void* __cdecl CreateIndexBuffer(int triangle_count, short* data);
	__declspec(dllexport) void __cdecl FreeIndexBuffer(void* ib);

	// Audio
	__declspec(dllexport) bool __cdecl CreateAudio(void* hMainWnd);
	__declspec(dllexport) void __cdecl FreeAudio();
	__declspec(dllexport) void* __cdecl CreateSoundBuffer(byte* wav_data, int wav_size);
	__declspec(dllexport) void __cdecl FreeSoundBuffer(void* sound_buffer);
	__declspec(dllexport) void __cdecl SetSoundBuffer3DPosition(void* sound_buffer, float x, float y, float z);
	__declspec(dllexport) bool __cdecl PlaySoundBuffer(void* sound_buffer);
}
