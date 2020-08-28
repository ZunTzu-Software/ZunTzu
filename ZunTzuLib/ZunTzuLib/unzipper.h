#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "zzlib.h"
#include "image_loader_error.h"

class unzipper {
public:
	virtual ~unzipper() = 0 {}
	virtual void set_error_handler(const jmp_buf & error_handler) = 0;
	virtual size_t read(char * buffer, size_t bytes_to_read) = 0;
protected:
	unzipper() {}
};

#ifndef ZTDESIGNER
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
#endif

#ifdef ZTDESIGNER
class file_loader : public unzipper {
public:
	file_loader(const wchar_t * file_name);
	virtual ~file_loader();
	virtual void set_error_handler(const jmp_buf & error_handler);
	virtual size_t read(char * buffer, size_t bytes_to_read);
private:
	wchar_t * file_name;
	HANDLE file;
	HANDLE mapped_file;
	LPVOID mapping;
	ZZIP_DISK_FILE * zip_entry;
	jmp_buf error_handler;
	char * position;
	char * end_of_file;
};
#endif
