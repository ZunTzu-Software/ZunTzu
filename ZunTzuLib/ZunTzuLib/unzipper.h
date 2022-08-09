#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#define _ZZIP_DISK_FILE_STRUCT 1
#include "zzip/mmapped.h"

#include "image_loader_error.h"

class unzipper {
public:
	virtual ~unzipper() = 0 {}
	virtual void set_error_handler(const jmp_buf & error_handler) = 0;
	virtual size_t read(char * buffer, size_t bytes_to_read) = 0;
protected:
	unzipper() {}
};

class simple_unzipper : public unzipper {
public:
	simple_unzipper(const wchar_t * archive_name, const char * entry_name);
	virtual ~simple_unzipper();
	virtual void set_error_handler(const jmp_buf & error_handler);
	virtual size_t read(char * buffer, size_t bytes_to_read);
private:
	wchar_t * archive_name;
	char * entry_name;
	HANDLE file;
	HANDLE mapped_file;
	LPVOID mapping;
	ZZIP_DISK_FILE * zip_entry;
	jmp_buf error_handler;
};
