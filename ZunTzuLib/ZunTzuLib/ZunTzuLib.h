// ZunTzuLib.h : main header file for the ZunTzuLib DLL
// Copyright (c) 2020 ZunTzu Software and contributors

#pragma once

#include "resource.h"		// main symbols

extern "C" {
	// DXT compression routines
	__declspec(dllexport) void __cdecl CompressDxt1(const char * rgb, int top, int left, int bottom, int right, int stride, char * blocks, int option);
	__declspec(dllexport) void __cdecl CompressDxt1FromRgba(const char * rgba, int top, int left, int bottom, int right, int stride, char * blocks, int option);
	__declspec(dllexport) void __cdecl CompressDxt5(const char * rgba, int top, int left, int bottom, int right, int stride, char * blocks, int option);

	// Image loading
#ifdef ZTDESIGNER
	__declspec(dllexport) void * __cdecl CreateImageLoader(const wchar_t * image_file_name, const wchar_t * mask_file_name, unsigned int skipped_mipmap_levels, int options);
#else
	__declspec(dllexport) void * __cdecl CreateImageLoader(const wchar_t * archive_name, const char * image_entry_name, const char * mask_entry_name, unsigned int skipped_mipmap_levels, int options);
#endif
	__declspec(dllexport) int __cdecl GetImageDimensions(void * image_loader, unsigned int * width, unsigned int * height);
	__declspec(dllexport) int __cdecl LoadNextTile(void * image_loader, char * tile, unsigned int * mipmap_level, unsigned int * x, unsigned int * y);
	__declspec(dllexport) void __cdecl FreeImageLoader(void * image_loader);

	// System info
	__declspec(dllexport) int __cdecl GetProcessorCoreCount();
}
