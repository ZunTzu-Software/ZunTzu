/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "image_reader.h"
#include "unzipper.h"
#include "image_loader_error.h"

// here are the routines that will replace the standard png_error and png_warning methods:

static void PNGAPI user_error_fn(png_structp png_ptr, png_int_32 error_code) {
	// return control to the setjmp point
	int * error_handler = static_cast<int*>(png_get_error_ptr(png_ptr));
	longjmp(error_handler, PNG_ERRORS + error_code);
}

// replacement function for png_read
static void PNGAPI user_read_fn(png_structp png_ptr, png_bytep data, png_size_t length) {
	unzipper * reader_unzipper = static_cast<unzipper*>(png_get_io_ptr(png_ptr));
	size_t bytes_read = reader_unzipper->read(reinterpret_cast<char*>(data), length);
	if(bytes_read != length)
		png_error(png_ptr, IMAGE_READ_ERROR - PNG_ERRORS);
}

png_reader::png_reader(
#ifdef ZTDESIGNER
	const wchar_t * file_name) :
#else
	const wchar_t * archive_name,
	const char * entry_name) :
#endif
	png_ptr(0),
	info_ptr(0),
	row_pointer(0),
#ifdef ZTDESIGNER
	unzipper(new file_loader(file_name)),
#else
	unzipper(new simple_unzipper(archive_name, entry_name)),
#endif
	current_scanline(0),
	height(0)
{
}

png_reader::~png_reader() {
	png_free(png_ptr, row_pointer);
	png_destroy_read_struct(&png_ptr, &info_ptr, 0);
	delete unzipper;
}

void png_reader::set_error_handler(const jmp_buf & error_handler) {
	CopyMemory(&this->error_handler, &error_handler, sizeof(jmp_buf));
	unzipper->set_error_handler(error_handler);
}

void png_reader::init_png() {
	png_ptr = png_create_read_struct(
		PNG_LIBPNG_VER_STRING,
		static_cast<int*>(error_handler),
		user_error_fn,
		0);
	if(png_ptr == NULL)
		longjmp(error_handler, IMAGE_CANNOT_CREATE_PNG_READER);

	info_ptr = png_create_info_struct(png_ptr);
	if(info_ptr == NULL)
		longjmp(error_handler, IMAGE_CANNOT_CREATE_PNG_READER);

	png_set_read_fn(png_ptr, unzipper, user_read_fn);

	// compare the first PNG_BYTES_TO_CHECK bytes of the signature
	const int signature_bytes_to_check = 8;
	unsigned char signature[signature_bytes_to_check];
	user_read_fn(png_ptr, signature, signature_bytes_to_check);
	if(0 != png_sig_cmp(signature, 0, signature_bytes_to_check))
		longjmp(error_handler, IMAGE_NOT_A_PNG_FILE);

	// we have already read some of the signature
	png_set_sig_bytes(png_ptr, signature_bytes_to_check);

	// the call to png_read_info() gives us all of the information from the
	// PNG file before the first IDAT (image data chunk)
	png_read_info(png_ptr, info_ptr);

	unsigned int width;
	int bit_depth;
	int color_type;
	int interlace_type;
	png_get_IHDR(png_ptr, info_ptr, reinterpret_cast<png_uint_32*>(&width), reinterpret_cast<png_uint_32*>(&height), &bit_depth, &color_type, &interlace_type, int_p_NULL, int_p_NULL);

	// throw exception in case of progressive PNG
	if(interlace_type != PNG_INTERLACE_NONE)
		longjmp(error_handler, IMAGE_UNSUPPORTED_PROGRESSIVE);

	// setup transform filters
	png_set_strip_alpha(png_ptr);
	png_set_palette_to_rgb(png_ptr);
	png_set_gray_to_rgb(png_ptr);
	png_set_strip_16(png_ptr);
	png_set_bgr(png_ptr);
	png_ptr->transformations &= ~0x2000000L;	// ~PNG_EXPAND_tRNS

	// allocate a buffer to hold one row
	row_pointer = (png_bytep) png_malloc(png_ptr, 3 * width);
}

void png_reader::get_image_dimensions(unsigned int & width, unsigned int & height) {
	if(!row_pointer)
		init_png();

	width = png_get_image_width(png_ptr, info_ptr);
	height = this->height;
}

char * png_reader::read_line() {
	if(!row_pointer)
		init_png();

	if(current_scanline < height) {
		png_read_row(png_ptr, row_pointer, 0);
		++current_scanline;
		return reinterpret_cast<char*>(row_pointer);
	} else {
		longjmp(error_handler, IMAGE_READ_PAST_LAST_SCANLINE);
		return 0;
	}
}
