#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

typedef int error_code;	// no error if 0, otherwise abort

class dxt_compressor {
public:
	virtual ~dxt_compressor() = 0 {}
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height) = 0;
	virtual error_code get_next_tile(char * tile_data, unsigned int & mipmap_level, unsigned int & x, unsigned int & y) = 0;
protected:
	dxt_compressor() {}
};

class tile_layer;
class synchronized_tile_buffer;

class dxt1_compressor : public dxt_compressor {
public:
#ifdef ZTDESIGNER
	dxt1_compressor(const wchar_t * file_name, unsigned int skipped_mipmap_levels, int options);
#else
	dxt1_compressor(const wchar_t * archive_name, const char * entry_name, unsigned int skipped_mipmap_levels, int options);
#endif
	virtual ~dxt1_compressor();
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual error_code get_next_tile(char * tile_data, unsigned int & mipmap_level, unsigned int & x, unsigned int & y);
private:
	static DWORD WINAPI image_loading_loop(__in LPVOID lpParameter);
	static DWORD WINAPI compression_loop(__in LPVOID lpParameter);

	int options;
	tile_layer * tyler;
	int compression_thread_count;
	HANDLE * threads;
	synchronized_tile_buffer * tile_buffer;
	synchronized_tile_buffer * texture_buffer;
};

class dxt5_compressor : public dxt_compressor {
public:
#ifdef ZTDESIGNER
	dxt5_compressor(const wchar_t * image_file_name, const wchar_t * mask_file_name, unsigned int skipped_mipmap_levels, int options);
#else
	dxt5_compressor(const wchar_t * archive_name, const char * image_entry_name, const char * mask_entry_name, unsigned int skipped_mipmap_levels, int options);
#endif
	virtual ~dxt5_compressor();
	virtual error_code get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual error_code get_next_tile(char * tile_data, unsigned int & mipmap_level, unsigned int & x, unsigned int & y);
private:
	static DWORD WINAPI image_loading_loop(__in LPVOID lpParameter);
	static DWORD WINAPI compression_loop(__in LPVOID lpParameter);

	int options;
	tile_layer * tyler;
	int compression_thread_count;
	HANDLE * threads;
	synchronized_tile_buffer * tile_buffer;
	synchronized_tile_buffer * texture_buffer;
};
