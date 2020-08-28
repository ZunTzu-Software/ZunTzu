#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

typedef int error_code;	// no error if 0, otherwise abort

class synchronized_tile_buffer;

class tile_layer {
public:
	virtual ~tile_layer() = 0 {}
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height) = 0;
	virtual void get_all_tiles(synchronized_tile_buffer * tile_buffer) = 0;
protected:
	tile_layer() {}
#ifdef ZTDESIGNER
	static bool is_png(const wchar_t * file_name) {
		size_t file_name_length = wcslen(file_name);
		return file_name_length >= 4 && (0 == _wcsnicmp(L".png", file_name + (file_name_length - 4), 4));
	}
#else
	static bool is_png(const char * entry_name) {
		size_t entry_name_length = strlen(entry_name);
		return entry_name_length >= 4 && (0 == _strnicmp(".png", entry_name + (entry_name_length - 4), 4));
	}
#endif
};

class image_reader;

class simple_tile_layer : public tile_layer {
public:
#ifdef ZTDESIGNER
	simple_tile_layer(const wchar_t * file_name, unsigned int skipped_mipmap_levels);
#else
	simple_tile_layer(const wchar_t * archive_name, const char * entry_name, unsigned int skipped_mipmap_levels);
#endif
	virtual ~simple_tile_layer();
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual void get_all_tiles(synchronized_tile_buffer * tile_buffer);
private:
	unsigned int skipped_mipmap_levels;
	unsigned int width;
	unsigned int height;

	image_reader * reader;
	unsigned int mipmap_level_count;
	struct mipmap_data {
		unsigned char * start; // start of the scanline data in the buffer
		size_t stride; // size in bytes of each scanline
		unsigned int next_tile_y; // y coordinate of the next tile
		unsigned char next_scanline_index; // index of the next scanline in the buffer (from 0 to 256)
		unsigned char scanlines_to_complete_tile; // number of scanlines to read to complete a tile (from 255 to 0)
	};
	mipmap_data * mipmaps;	// mipmap counters and pixels
};

class masked_tile_layer : public tile_layer {
public:
#ifdef ZTDESIGNER
	masked_tile_layer(const wchar_t * image_file_name, const wchar_t * mask_file_name, unsigned int skipped_mipmap_levels);
#else
	masked_tile_layer(const wchar_t * archive_name, const char * image_entry_name, const char * mask_entry_name, unsigned int skipped_mipmap_levels);
#endif
	virtual ~masked_tile_layer();
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual void get_all_tiles(synchronized_tile_buffer * tile_buffer);
private:
	unsigned int skipped_mipmap_levels;
	unsigned int width;
	unsigned int height;

	image_reader * main_image_reader;
	image_reader * mask_reader;
	unsigned int mipmap_level_count;
	struct mipmap_data {
		unsigned char * start; // start of the scanline data in the buffer
		size_t stride; // size in bytes of each scanline
		unsigned int next_tile_y; // y coordinate of the next tile
		unsigned char next_scanline_index; // index of the next scanline in the buffer (from 0 to 256)
		unsigned char scanlines_to_complete_tile; // number of scanlines to read to complete a tile (from 255 to 0)
	};
	mipmap_data * mipmaps;	// mipmap counters and pixels
};
