/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "jerror.h"
#include "image_reader.h"
#include "unzipper.h"
#include "image_loader_error.h"

// Here's the routine that will replace the standard error_exit method:

METHODDEF(void) my_error_exit(j_common_ptr cinfo) {
	// cinfo->err really points to a my_error_mgr struct, so coerce pointer
	my_error_mgr * myerr = reinterpret_cast<my_error_mgr*>(cinfo->err);

	// Return control to the setjmp point
	longjmp(myerr->setjmp_buffer, JPEG_ERRORS + myerr->pub.msg_code);
}

jpeg_reader::jpeg_reader(
	const wchar_t * archive_name,
	const char * entry_name) :
	pBuffer(0),
	unzipper(new simple_unzipper(archive_name, entry_name))
{
	cinfo.err = jpeg_std_error(&jerr.pub);
	jerr.pub.error_exit = my_error_exit;
}

jpeg_reader::~jpeg_reader() {
	delete unzipper;
	jpeg_destroy_decompress(&cinfo);
}

void jpeg_reader::set_error_handler(const jmp_buf & error_handler) {
	CopyMemory(&jerr.setjmp_buffer, &error_handler, sizeof(jmp_buf));
	unzipper->set_error_handler(error_handler);
}

GLOBAL(void) jpeg_unzipper_src(j_decompress_ptr cinfo, unzipper * unzipper);

void jpeg_reader::init_jpeg() {
	jpeg_create_decompress(&cinfo);
	jpeg_unzipper_src(&cinfo, unzipper);
	jpeg_read_header(&cinfo, TRUE);

	// throw exception in case of CMYK color space or progressive JPEG
	if(cinfo.num_components != 1 && cinfo.num_components != 3)
		ERREXIT(&cinfo, IMAGE_UNSUPPORTED_COLOR_SPACE - JPEG_ERRORS);
	if(cinfo.progressive_mode)
		ERREXIT(&cinfo, IMAGE_UNSUPPORTED_PROGRESSIVE - JPEG_ERRORS);
	cinfo.out_color_space = JCS_RGB;

	jpeg_start_decompress(&cinfo);

	pBuffer = (*cinfo.mem->alloc_sarray) ((j_common_ptr) &cinfo, JPOOL_IMAGE, cinfo.output_components * cinfo.output_width, 1);
}

void jpeg_reader::get_image_dimensions(unsigned int & width, unsigned int & height) {
	if(!pBuffer)
		init_jpeg();

	width = cinfo.output_width;
	height = cinfo.output_height;
}

char * jpeg_reader::read_line() {
	if(!pBuffer)
		init_jpeg();

	if(cinfo.output_scanline < cinfo.output_height) {
		JDIMENSION count = jpeg_read_scanlines(&cinfo, pBuffer, 1);
		return reinterpret_cast<char*>(pBuffer[0]);
	} else {
		ERREXIT(&cinfo, IMAGE_READ_PAST_LAST_SCANLINE - JPEG_ERRORS);	// throw IMAGE_READ_PAST_LAST_SCANLINE
		return 0;
	}
}
