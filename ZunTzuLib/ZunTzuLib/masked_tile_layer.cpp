/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "image_loader_error.h"
#include "tile_layer.h"
#include "synchronized_tile_buffer.h"
#include "image_reader.h"

// mipmap data is arranged as an array of 256 scanlines.
// each scanline is bounded by two zeroed guard bands.

masked_tile_layer::masked_tile_layer(
#ifdef ZTDESIGNER
	const wchar_t * image_file_name,
	const wchar_t * mask_file_name,
#else
	const wchar_t * archive_name,
	const char * image_entry_name,
	const char * mask_entry_name,
#endif
	unsigned int skipped_mipmap_levels)
:
	skipped_mipmap_levels(skipped_mipmap_levels),
	width(0),
	height(0),
#ifdef ZTDESIGNER
	main_image_reader(is_png(image_file_name) ?
		static_cast<image_reader*>(new png_reader(image_file_name)) :
		static_cast<image_reader*>(new jpeg_reader(image_file_name))),
	mask_reader(is_png(mask_file_name) ?
		static_cast<image_reader*>(new png_reader(mask_file_name)) :
		static_cast<image_reader*>(new jpeg_reader(mask_file_name))),
#else
	main_image_reader(is_png(image_entry_name) ?
		static_cast<image_reader*>(new png_reader(archive_name, image_entry_name)) :
		static_cast<image_reader*>(new jpeg_reader(archive_name, image_entry_name))),
	mask_reader(is_png(mask_entry_name) ?
		static_cast<image_reader*>(new png_reader(archive_name, mask_entry_name)) :
		static_cast<image_reader*>(new jpeg_reader(archive_name, mask_entry_name))),
#endif
	mipmaps(0)
{
}

masked_tile_layer::~masked_tile_layer() {
	delete [] mipmaps;
	delete mask_reader;
	delete main_image_reader;
}

error_code masked_tile_layer::get_image_dimensions(
	unsigned int & w,
	unsigned int & h)
{
	jmp_buf error_handler;
	int error = setjmp(error_handler);
	if(error != 0) {
		w = 0;
		h = 0;
		return error;
	}
	main_image_reader->set_error_handler(error_handler);
	mask_reader->set_error_handler(error_handler);

	main_image_reader->get_image_dimensions(width, height);
	w = width;
	h = height;
	mask_reader->get_image_dimensions(width, height);
	if(width != w || height != h)
		return IMAGE_INCONSISTENT_MASK_DIMENSIONS;

	// initialise buffer structure
	const double inv_log_two = 1.0 / log(2.0);
	double mipmap_count_width = ceil(log((double)(width / 254)) * inv_log_two + 1);
	double mipmap_count_height = ceil(log((double)(height / 254)) * inv_log_two + 1);
	mipmap_level_count = max(3, (unsigned int) max(mipmap_count_width, mipmap_count_height));

	size_t total_buffer_size = mipmap_level_count * sizeof(mipmap_data);
	unsigned int mipmap_width = width;
	for(unsigned int i = 0; i < mipmap_level_count; ++i) {
		total_buffer_size += ((i < skipped_mipmap_levels ? 2 : 256) * 4) * (mipmap_width + 2);	// add 2 for margin
		mipmap_width = (mipmap_width + 1) / 2;
	}

	mipmaps = reinterpret_cast<mipmap_data*>(new char[total_buffer_size]);

	unsigned int mipmap_width2 = width;
	for(unsigned int i = 0; i < mipmap_level_count; ++i) {
		mipmaps[i].start = (i == 0 ?
			reinterpret_cast<unsigned char*>(mipmaps + mipmap_level_count) :
			mipmaps[i - 1].start + (i - 1 < skipped_mipmap_levels ? 2 : 256) * mipmaps[i - 1].stride);
		mipmaps[i].stride = (mipmap_width2 + 2) * 4;	// add 2 for margin
		mipmaps[i].next_scanline_index = 1;
		mipmaps[i].scanlines_to_complete_tile = 255;
		mipmaps[i].next_tile_y = 0;

		// zero first scanline of each mipmap
		ZeroMemory(mipmaps[i].start, mipmaps[i].stride);

		mipmap_width2 = (mipmap_width2 + 1) / 2;
	}

	return 0;
}

void masked_tile_layer::get_all_tiles(synchronized_tile_buffer * tile_buffer)
{
	// get the image dimensions, if it was not already done
	if(width == 0) {
		error_code error = get_image_dimensions(width, height);
		if(error != 0) {
			tile_buffer->stop_consumer(error);
			return;
		}
	}

	// setup an error handling frame
	jmp_buf error_handler;
	int error = setjmp(error_handler);
	if(error != 0) {
		tile_buffer->stop_consumer(error);
		return;
	}
	main_image_reader->set_error_handler(error_handler);
	mask_reader->set_error_handler(error_handler);

	// do until all scanlines have been read
	unsigned int unread_scanlines = height;
	while(unread_scanlines > 0) {
		// read next scanline
		char * image_scanline = main_image_reader->read_line();
		char * mask_scanline = mask_reader->read_line();
		--unread_scanlines;

		// copy scanline data to the first level mipmap
		unsigned char * destination = mipmaps[0].start + mipmaps[0].stride * mipmaps[0].next_scanline_index;
		destination[0] = 0;
		destination[1] = 0;
		destination[2] = 0;
		destination[3] = 0;
		for(unsigned int i = 0; i < width; ++i) {
			destination[i * 4 + 4] = image_scanline[i * 3 + 0];
			destination[i * 4 + 5] = image_scanline[i * 3 + 1];
			destination[i * 4 + 6] = image_scanline[i * 3 + 2];
			destination[i * 4 + 7] = mask_scanline[i * 3 + 1];	// alpha = green component of mask
		}
		destination[width * 4 + 4] = 0;
		destination[width * 4 + 5] = 0;
		destination[width * 4 + 6] = 0;
		destination[width * 4 + 7] = 0;

		// for each mipmap level...
		for(unsigned int level = 0; level < mipmap_level_count; ++level) {
			mipmap_data & mipmap = mipmaps[level];

			if(level < skipped_mipmap_levels) {
				mipmap.next_scanline_index = 1 - mipmap.next_scanline_index;
			} else {
				++mipmap.next_scanline_index;
				--mipmap.scanlines_to_complete_tile;

				// are some tiles ready to be output (every 254 scanlines or at the end of the image)?
				if(mipmap.scanlines_to_complete_tile == 0 || unread_scanlines == 0) {
					// a band of tiles is ready
					for(unsigned int tile_x = 0; tile_x * (254 * 4) < mipmap.stride - (2 * 4); ++tile_x) {
						// last tile of the row?
						bool last_tile_of_row = ((tile_x * (254 * 4)) + (254 * 4) > mipmap.stride - (2 * 4));

						// get write buffer
						unsigned int slot_index = 0;
						if(!tile_buffer->allocate_write_slot(slot_index)) {
							// consumer loop has issued a "stop producer loop" command
							return;
						}
						tile_slot * slot = tile_buffer->get_slot(slot_index);
						slot->mipmap_level = level;
						slot->x = tile_x;
						slot->y = mipmap.next_tile_y;

						// copy tile data to write buffer
						unsigned int y;
						for(y = 0; y < (unsigned int)256 - mipmap.scanlines_to_complete_tile; ++y) {
							unsigned char * source = mipmap.start + mipmap.stride * (unsigned char)(mipmap.next_scanline_index + (unsigned char)y + mipmap.scanlines_to_complete_tile) + (254 * 4) * tile_x;	// cyclic 8-bit arithmetic
							size_t bytes_to_copy = 256 * 4;
							if(last_tile_of_row) {
								bytes_to_copy = mipmap.stride - tile_x * (254 * 4);
								ZeroMemory(slot->texels + (256 * 4) * y + bytes_to_copy, 256 * 4 - bytes_to_copy);
							}
							CopyMemory(
								slot->texels + (256 * 4) * y,
								source,
								bytes_to_copy);
						}
						// area below the image is drawn black (if there is one)
						for(; y < 256; ++y) {
							ZeroMemory(
								slot->texels + (256 * 4) * y,
								256 * 4);
						}

						// yield tile
						tile_buffer->free_write_slot(slot_index);
					}

					// we need 254 additional scanlines to output the next tile band
					mipmap.scanlines_to_complete_tile = 254;
					++mipmap.next_tile_y;
				}
			}

			// downsample every even line (downsample when last line, even if odd)
			if((level == mipmap_level_count - 1) || (unread_scanlines > 0 && (mipmap.next_scanline_index & 0x01) == 0))
				break;

			// downsample
			unsigned char * source_odd;
			unsigned char * source_even;
			if(level < skipped_mipmap_levels) {
				source_odd = mipmap.start + 4;
				source_even = source_odd + mipmap.stride;
			} else {
				source_odd = mipmap.start + mipmap.stride * (unsigned char)(mipmap.next_scanline_index + (unsigned char)254) + 4;
				source_even = mipmap.start + mipmap.stride * (unsigned char)(mipmap.next_scanline_index + (unsigned char)255) + 4;
			}
			mipmap_data & lower_mipmap = mipmaps[level + 1];
			unsigned char * dest = lower_mipmap.start + lower_mipmap.stride * lower_mipmap.next_scanline_index;
			unsigned char * lower_mipmap_scanline_end = dest + lower_mipmap.stride;

			// left guard band
			*(dest + 3) = *(dest + 2) = *(dest + 1) = *(dest + 0) = 0;
			dest += 4;

			// copy downsampled data to next level mipmap
			while(dest < lower_mipmap_scanline_end - 4) {
				*(dest+0) = ((unsigned int) *(source_odd+0) + (unsigned int) *(source_odd+4) + (unsigned int) *(source_even+0) + (unsigned int) *(source_even+4) + (unsigned int) 2) >> 2;
				*(dest+1) = ((unsigned int) *(source_odd+1) + (unsigned int) *(source_odd+5) + (unsigned int) *(source_even+1) + (unsigned int) *(source_even+5) + (unsigned int) 2) >> 2;
				*(dest+2) = ((unsigned int) *(source_odd+2) + (unsigned int) *(source_odd+6) + (unsigned int) *(source_even+2) + (unsigned int) *(source_even+6) + (unsigned int) 2) >> 2;
				*(dest+3) = ((unsigned int) *(source_odd+3) + (unsigned int) *(source_odd+7) + (unsigned int) *(source_even+3) + (unsigned int) *(source_even+7) + (unsigned int) 2) >> 2;
				dest += 4;
				source_odd += 8;
				source_even += 8;
			}

			// right guard band
			*(dest + 3) = *(dest + 2) = *(dest + 1) = *(dest + 0) = 0;
		}
	}
}
