/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "unzipper.h"

#ifndef ZTDESIGNER

simple_unzipper::simple_unzipper(
	const wchar_t * archive_name,
	const char * entry_name) :
	archive_name(_wcsdup(archive_name)),
	entry_name(_strdup(entry_name)),
	file(0),
	mapped_file(0),
	mapping(0),
	zip_entry(0)
{
}

simple_unzipper::~simple_unzipper() {
	if(zip_entry) {
		zzip_disk_fclose(zip_entry);

		if(!UnmapViewOfFile(mapping)) {
			//_tprintf(_T("%u - Error: UnmapViewOfFile()\n"), GetLastError());
		}
		CloseHandle(mapped_file);
		CloseHandle(file);
	}
	free(entry_name);
	free(archive_name);
}

void simple_unzipper::set_error_handler(const jmp_buf & error_handler) {
	CopyMemory(&this->error_handler, &error_handler, sizeof(jmp_buf));
}

size_t simple_unzipper::read(char * buffer, size_t bytes_to_read) {
	if(!zip_entry) {
		file = CreateFile(archive_name, 
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

		ZZIP_DISK disk;
		disk.buffer = (zzip_byte_t*) mapping;
		disk.endbuf = disk.buffer + file_size;
		disk.reserved = 0;
		disk.flags = 0;
		disk.mapped = 0;

		zip_entry = zzip_disk_fopen(&disk, const_cast<LPSTR>(entry_name));
		if(!zip_entry) {
			//_tprintf(_T("Error: zzip_disk_fopen()\n"));
			if(!UnmapViewOfFile(mapping)) {
				//_tprintf(_T("%u - Error: UnmapViewOfFile()\n"), GetLastError());
			}
			CloseHandle(mapped_file);
			CloseHandle(file);
			longjmp(error_handler, UNZIPPER_ENTRY_NOT_FOUND);
		}
	}

	return zzip_disk_fread(buffer, bytes_to_read, zip_entry);
}

#endif