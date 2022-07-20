/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"
#include "dxt_compressor.h"
#include "synchronized_tile_buffer.h"
#include "tile_layer.h"

dxt5_compressor::dxt5_compressor(
	const wchar_t * archive_name,
	const char * image_entry_name,
	const char * mask_entry_name,
	unsigned int skipped_mipmap_levels,
	int options)
:
	options(options),
	tyler(new masked_tile_layer(archive_name, image_entry_name, mask_entry_name, skipped_mipmap_levels)),
	compression_thread_count(0),
	threads(0),
	tile_buffer(0),
	texture_buffer(0)
{
}

dxt5_compressor::~dxt5_compressor()
{
	if(threads != 0) {
		tile_buffer->stop_producer();
		if(texture_buffer != 0)
			texture_buffer->stop_producer();
		WaitForMultipleObjects(1 + compression_thread_count, threads, true, 10000);
		for(int i = 0; i < 1 + compression_thread_count; ++i)
			CloseHandle(threads[i]);
		delete [] threads;
	}
	delete texture_buffer;
	delete tile_buffer;
	delete tyler;
}

error_code dxt5_compressor::get_image_dimensions(
	unsigned int & width,
	unsigned int & height)
{
	return tyler->get_image_dimensions(width, height);
}

DWORD WINAPI dxt5_compressor::image_loading_loop(
  __in LPVOID lpParameter)
{
	dxt5_compressor * compressor = static_cast<dxt5_compressor*>(lpParameter);
	compressor->tyler->get_all_tiles(compressor->tile_buffer);
	return 0;
}

DWORD WINAPI dxt5_compressor::compression_loop(
  __in LPVOID lpParameter)
{
	dxt5_compressor * compressor = static_cast<dxt5_compressor*>(lpParameter);
	while(true) {
		// get write buffer
		unsigned int write_slot_index = 0;
		if(!compressor->texture_buffer->allocate_write_slot(write_slot_index)) {
			// consumer loop has issued a "stop producer loop" command
			return 0;
		}
		tile_slot * write_slot = compressor->texture_buffer->get_slot(write_slot_index);

		// get read buffer
		unsigned int read_slot_index = 0;
		error_code read_error = compressor->tile_buffer->allocate_read_slot(read_slot_index);
		if(0 == read_error) {
			tile_slot * read_slot = compressor->tile_buffer->get_slot(read_slot_index);

			// compress data
			write_slot->mipmap_level = read_slot->mipmap_level;
			write_slot->x = read_slot->x;
			write_slot->y = read_slot->y;

			CompressDxt5(read_slot->texels, 0, 0, 256, 256, 256 * 4, write_slot->texels, compressor->options);

			// yield result
			compressor->tile_buffer->free_read_slot(read_slot_index);
			compressor->texture_buffer->free_write_slot(write_slot_index);
		} else {
			compressor->texture_buffer->stop_consumer(read_error);
			return 0;
		}
	}
}

error_code dxt5_compressor::get_next_tile(
	char * tile_data,
	unsigned int & mipmap_level,
	unsigned int & x,
	unsigned int & y)
{
	if(threads == 0) {
		// if more than one core then launch one compression thread per core
		compression_thread_count = GetProcessorCoreCount();

		threads = new HANDLE[1 + compression_thread_count];

		tile_buffer = new synchronized_tile_buffer((compression_thread_count > 0 ? 1 + 2 * compression_thread_count : 3), 256 * 256 * 4);

		// create image loading thread
		threads[0] = CreateThread(
			0, //  __in_opt   LPSECURITY_ATTRIBUTES lpThreadAttributes
			0, //  __in       SIZE_T dwStackSize
			image_loading_loop, //  __in       LPTHREAD_START_ROUTINE lpStartAddress
			this, //  __in_opt   LPVOID lpParameter
			0, //  __in       DWORD dwCreationFlags
			0 //  __out_opt  LPDWORD lpThreadId
		);

		if(compression_thread_count > 0) {
			texture_buffer = new synchronized_tile_buffer(1 + 2 * compression_thread_count, 64 * 64 * 16);

			// create texture compression threads
			for(int i = 0; i < compression_thread_count; ++i) {
				threads[1 + i] = CreateThread(
					0, //  __in_opt   LPSECURITY_ATTRIBUTES lpThreadAttributes
					0, //  __in       SIZE_T dwStackSize
					compression_loop, //  __in       LPTHREAD_START_ROUTINE lpStartAddress
					this, //  __in_opt   LPVOID lpParameter
					0, //  __in       DWORD dwCreationFlags
					0 //  __out_opt  LPDWORD lpThreadId
				);
			}
		}
	}

	if(compression_thread_count == 0) {
		unsigned int slot_index = 0;
		error_code error = tile_buffer->allocate_read_slot(slot_index);
		if(0 == error) {
			tile_slot * slot = tile_buffer->get_slot(slot_index);
			mipmap_level = slot->mipmap_level;
			x = slot->x;
			y = slot->y;
			CompressDxt5(slot->texels, 0, 0, 256, 256, 256 * 4, tile_data, options);
			tile_buffer->free_read_slot(slot_index);
		}
		return error;

	} else {
		unsigned int slot_index = 0;
		error_code error = texture_buffer->allocate_read_slot(slot_index);
		if(0 == error) {
			tile_slot * slot = texture_buffer->get_slot(slot_index);
			mipmap_level = slot->mipmap_level;
			x = slot->x;
			y = slot->y;

			CopyMemory(tile_data, slot->texels, 64 * 64 * 16);

			texture_buffer->free_read_slot(slot_index);
		}
		return error;
	}
}
