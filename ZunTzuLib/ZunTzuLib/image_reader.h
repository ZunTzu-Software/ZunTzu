#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "jpeglib.h"
#include "png.h"

class image_reader {
public:
	virtual ~image_reader() = 0 {}
	virtual void set_error_handler(const jmp_buf & error_handler) = 0;
	virtual void get_image_dimensions(unsigned int & width, unsigned int & height) = 0;
	virtual char * read_line() = 0;
protected:
	image_reader() {}
};

class unzipper;

struct my_error_mgr {
	struct jpeg_error_mgr pub;	// "public" fields
	jmp_buf setjmp_buffer;	// for return to caller
};

class jpeg_reader : public image_reader {
public:
	jpeg_reader(const wchar_t * archive_name, const char * entry_name);
	virtual ~jpeg_reader();
	virtual void set_error_handler(const jmp_buf & error_handler);
	virtual void get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual char * read_line();
private:
	void init_jpeg();

	my_error_mgr jerr;
	struct jpeg_decompress_struct cinfo;
	JSAMPARRAY pBuffer;
	unzipper * unzipper;
};

class png_reader : public image_reader {
public:
	png_reader(const wchar_t * archive_name, const char * entry_name);
	virtual ~png_reader();
	virtual void set_error_handler(const jmp_buf & error_handler);
	virtual void get_image_dimensions(unsigned int & width, unsigned int & height);
	virtual char * read_line();
private:
	void init_png();

	jmp_buf error_handler;
	png_structp png_ptr;
	png_infop info_ptr;
	png_bytep row_pointer;
	unzipper * unzipper;
	unsigned int current_scanline;
	unsigned int height;
};
