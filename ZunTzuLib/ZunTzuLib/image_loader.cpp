/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"
#include "dxt_compressor.h"

extern "C" void * __cdecl CreateImageLoader(
	const wchar_t * archive_name,
	const char * image_entry_name,
	const char * mask_entry_name,
	unsigned int skipped_mipmap_levels,
	int options)
{
	//_tprintf(_T("ZunTzuLib.CreateImageLoader\n"));

	// float or simd execution path?
	if(IsProcessorFeaturePresent(PF_XMMI64_INSTRUCTIONS_AVAILABLE))
		options |= 2;

	return (mask_entry_name == 0 || strlen(mask_entry_name) == 0 ?
		static_cast<dxt_compressor*>(new dxt1_compressor(archive_name, image_entry_name, skipped_mipmap_levels, options)) :
		static_cast<dxt_compressor*>(new dxt5_compressor(archive_name, image_entry_name, mask_entry_name, skipped_mipmap_levels, options)));
}

extern "C" int __cdecl GetImageDimensions(
	void * image_loader,
	unsigned int * width,
	unsigned int * height)
{
	//_tprintf(_T("ZunTzuLib.GetImageDimensions\n"));
	dxt_compressor * compressor = static_cast<dxt_compressor*>(image_loader);
	return compressor->get_image_dimensions(*width, *height);
}

extern "C" int __cdecl LoadNextTile(
	void * image_loader,
	char * tile,
	unsigned int * mipmap_level,
	unsigned int * x,
	unsigned int * y)
{
	//_tprintf(_T("ZunTzuLib.LoadNextTile\n"));
	dxt_compressor * compressor = static_cast<dxt_compressor*>(image_loader);
	return compressor->get_next_tile(tile, *mipmap_level, *x, *y);
}

extern "C" void __cdecl FreeImageLoader(
	void * image_loader)
{
	//_tprintf(_T("ZunTzuLib.FreeImageLoader\n"));
	dxt_compressor * compressor = static_cast<dxt_compressor*>(image_loader);
	delete compressor;
}
