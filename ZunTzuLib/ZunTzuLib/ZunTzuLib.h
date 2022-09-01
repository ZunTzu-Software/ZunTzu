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
}
