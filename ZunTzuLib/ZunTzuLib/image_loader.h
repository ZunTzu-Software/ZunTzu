#pragma once

#include "jpeglib.h"

enum IMAGE_LOADER_ERROR {
	IMAGE_LOADER_OK = 0,
	UNZIPPER_CANNOT_OPEN_ARCHIVE_FILE,
	UNZIPPER_CANNOT_READ_FILE_SIZE,
	UNZIPPER_CANNOT_CREATE_FILE_MAPPING,
	UNZIPPER_CANNOT_MAP_FILE,
	UNZIPPER_ENTRY_NOT_FOUND
};

class unzipper {
public:
	virtual ~unzipper() = 0 {}
	virtual void set_error_handler(jmp_buffer error_handler) = 0;
	virtual size_t read(char * buffer, size_t bytes_to_read) = 0;
protected:
	unzipper() {}
};

class image_reader {
public:
	virtual ~image_reader() = 0 {}
	virtual void set_error_handler(jmp_buffer error_handler) = 0;
	virtual void get_image_dimensions(int & width, int & height, int & channels, int & output_channels) = 0;
	virtual char * read_line() = 0;
protected:
	image_reader() {}
};

class tile_layer {
public:
	virtual ~tile_layer() = 0 {}
	virtual void set_error_handler(jmp_buffer error_handler) = 0;
	virtual void get_image_dimensions(int & width, int & height) = 0;
	virtual void get_next_tile(char * tile_data, int & mipmap_level, int & x, int & y) = 0;
protected:
	tile_layer() {}
};

class dxt_compressor {
public:
	virtual ~dxt_compressor() = 0 {}
	virtual IMAGE_LOADER_ERROR get_image_dimensions(int & width, int & height) = 0;
	virtual IMAGE_LOADER_ERROR get_next_tile(char * tile_data, int & mipmap_level, int & x, int & y) = 0;
protected:
	dxt_compressor() {}
};
