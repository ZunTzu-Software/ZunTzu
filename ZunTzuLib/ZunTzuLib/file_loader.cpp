/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "unzipper.h"

#ifdef ZTDESIGNER

file_loader::file_loader(
	const wchar_t * file_name) :
	file_name(_wcsdup(file_name)),
	file(0),
	mapped_file(0),
	mapping(0),
	position(0),
	end_of_file(0)
{
}

file_loader::~file_loader() {
	if(position) {
		if(!UnmapViewOfFile(mapping)) {
			//_tprintf(_T("%u - Error: UnmapViewOfFile()\n"), GetLastError());
		}
		CloseHandle(mapped_file);
		CloseHandle(file);
	}
	free(file_name);
}

void file_loader::set_error_handler(const jmp_buf & error_handler) {
	CopyMemory(&this->error_handler, &error_handler, sizeof(jmp_buf));
}

size_t file_loader::read(char * buffer, size_t bytes_to_read) {
	if(!position) {
		file = CreateFile(file_name, 
			GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 
			FILE_ATTRIBUTE_NORMAL, NULL);
		if(file == INVALID_HANDLE_VALUE) {
			//_tprintf(_T("File Not Opened!\n"));
			longjmp(error_handler, UNZIPPER_CANNOT_OPEN_ARCHIVE_FILE);
		}

		int file_size = GetFileSize(file, NULL);
		if(file_size == 0xffffffff) {
			//_tprintf(_T("%u - Error: GetFileSize()\n"), GetLastError());
			longjmp(error_handler, UNZIPPER_CANNOT_READ_FILE_SIZE);
		}

		mapped_file = CreateFileMapping(file, 
			NULL, PAGE_READONLY, 0, 0, NULL);
		if(mapped_file == NULL) {
			//_tprintf(_T("%u - Error: CreateFileMapping()\n"), GetLastError());
			CloseHandle(file);
			longjmp(error_handler, UNZIPPER_CANNOT_CREATE_FILE_MAPPING);
		}

		mapping = MapViewOfFile(mapped_file,
			FILE_MAP_READ, 0, 0, 0); 
		if(mapping == NULL)	{
			//_tprintf(_T("%u - Error: MapViewOfFile()\n"), GetLastError());
			CloseHandle(mapped_file);
			CloseHandle(file);
			longjmp(error_handler, UNZIPPER_CANNOT_MAP_FILE);
		}

		//_tprintf(_T("archive opened."));

		position = (char *) mapping;
		end_of_file = position + file_size;
	}

	size_t bytes_read = (bytes_to_read + position < end_of_file ? bytes_to_read : (end_of_file - position));
	CopyMemory(buffer, position, bytes_read);
	position += bytes_read;
	return bytes_read;
}

#endif