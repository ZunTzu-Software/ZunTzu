#include "stdafx.h"
#include <malloc.h>
#include "zlib.h"
#include "zzlib.h"

typedef unsigned short uint16_t;
typedef unsigned int uint32_t;
typedef unsigned long long uint64_t;

# define ZZIP_GET16(__p)     (*(uint16_t*)(__p))
# define ZZIP_GET32(__p)     (*(uint32_t*)(__p))
# define ZZIP_GET64(__p)     (*(uint64_t*)(__p))
# define ZZIP_SET16(__p,__x) (*(uint16_t*)(__p) = (uint16_t)(__x))
# define ZZIP_SET32(__p,__x) (*(uint32_t*)(__p) = (uint32_t)(__x))
# define ZZIP_SET64(__p,__x) (*(uint64_t*)(__p) = (uint64_t)(__x))

/* zzip_file_header - the local file header */
#define zzip_file_header_get_magic(__p)      ZZIP_GET32((__p)->z_magic)
#define zzip_file_header_set_magic(__p,__x)  ZZIP_SET32((__p)->z_magic,(__x))
#define zzip_file_header_get_flags(__p)      ZZIP_GET16((__p)->z_flags)
#define zzip_file_header_set_flags(__p,__x)  ZZIP_SET16((__p)->z_flags,(__x))
#define zzip_file_header_get_compr(__p)      ZZIP_GET16((__p)->z_compr)
#define zzip_file_header_set_compr(__p,__x)  ZZIP_SET16((__p)->z_compr,(__x))
#define zzip_file_header_get_crc32(__p)      ZZIP_GET32((__p)->z_crc32)
#define zzip_file_header_set_crc32(__p,__x)  ZZIP_SET32((__p)->z_crc32,(__x))
#define zzip_file_header_get_csize(__p)      ZZIP_GET32((__p)->z_csize)
#define zzip_file_header_set_csize(__p,__x)  ZZIP_SET32((__p)->z_csize,(__x))
#define zzip_file_header_get_usize(__p)      ZZIP_GET32((__p)->z_usize)
#define zzip_file_header_set_usize(__p,__x)  ZZIP_SET32((__p)->z_usize,(__x))
#define zzip_file_header_get_namlen(__p)     ZZIP_GET16((__p)->z_namlen)
#define zzip_file_header_set_namlen(__p,__x) ZZIP_SET16((__p)->z_namlen,(__x))
#define zzip_file_header_get_extras(__p)     ZZIP_GET16((__p)->z_extras)
#define zzip_file_header_set_extras(__p,__x) ZZIP_SET16((__p)->z_extras,(__x))
#define zzip_file_header_sizeof_tails(__p) (zzip_file_header_get_namlen(__p)+\
					    zzip_file_header_get_extras(__p) )
#define zzip_file_header_check_magic(__p)  ZZIP_FILE_HEADER_CHECKMAGIC((__p))

/* zzip_file_trailer - data descriptor per file block */
#define zzip_file_trailer_get_magic(__p)     ZZIP_GET32((__p)->z_magic)
#define zzip_file_trailer_set_magic(__p,__x) ZZIP_SET32((__p)->z_magic,(__x))
#define zzip_file_header_get_crc32(__p)      ZZIP_GET32((__p)->z_crc32)
#define zzip_file_trailer_set_crc32(__p,__x) ZZIP_SET32((__p)->z_crc32,(__x))
#define zzip_file_trailer_get_csize(__p)     ZZIP_GET32((__p)->z_csize)
#define zzip_file_trailer_set_csize(__p,__x) ZZIP_SET32((__p)->z_csize,(__x))
#define zzip_file_trailer_get_usize(__p)     ZZIP_GET32((__p)->z_usize)
#define zzip_file_trailer_set_usize(__p,__x) ZZIP_SET32((__p)->z_usize,(__x))
#define zzip_file_trailer_sizeof_tails(__p) 0
#define zzip_file_trailer_check_magic(__p)   ZZIP_FILE_TRAILER_CHECKMAGIC((__p))
/* zzip_disk_entry (currently named zzip_root_dirent) */
#define zzip_disk_entry_get_magic(__p)      ZZIP_GET32((__p)->z_magic)
#define zzip_disk_entry_set_magic(__p,__x)  ZZIP_SET32((__p)->z_magic,(__x))
#define zzip_disk_entry_get_flags(__p)      ZZIP_GET16((__p)->z_flags)
#define zzip_disk_entry_set_flags(__p,__x)  ZZIP_SET16((__p)->z_flags,(__x))
#define zzip_disk_entry_get_compr(__p)      ZZIP_GET16((__p)->z_compr)
#define zzip_disk_entry_set_compr(__p,__x)  ZZIP_SET16((__p)->z_compr,(__x))
#define zzip_disk_entry_get_crc32(__p)      ZZIP_GET32((__p)->z_crc32)
#define zzip_disk_entry_set_crc32(__p,__x)  ZZIP_SET32((__p)->z_crc32,(__x))
#define zzip_disk_entry_get_csize(__p)      ZZIP_GET32((__p)->z_csize)
#define zzip_disk_entry_set_csize(__p,__x)  ZZIP_SET32((__p)->z_csize,(__x))
#define zzip_disk_entry_get_usize(__p)      ZZIP_GET32((__p)->z_usize)
#define zzip_disk_entry_set_usize(__p,__x)  ZZIP_SET32((__p)->z_usize,(__x))
#define zzip_disk_entry_get_namlen(__p)     ZZIP_GET16((__p)->z_namlen)
#define zzip_disk_entry_set_namlen(__p,__x) ZZIP_SET16((__p)->z_namlen,(__x))
#define zzip_disk_entry_get_extras(__p)     ZZIP_GET16((__p)->z_extras)
#define zzip_disk_entry_set_extras(__p,__x) ZZIP_SET16((__p)->z_extras,(__x))
#define zzip_disk_entry_get_comment(__p)     ZZIP_GET16((__p)->z_comment)
#define zzip_disk_entry_set_comment(__p,__x) ZZIP_SET16((__p)->z_comment,(__x))
#define zzip_disk_entry_get_diskstart(__p)     ZZIP_GET16((__p)->z_diskstart)
#define zzip_disk_entry_set_diskstart(__p,__x) ZZIP_SET16((__p)->z_diskstart,(__x))
#define zzip_disk_entry_get_filetype(__p)     ZZIP_GET16((__p)->z_filetype)
#define zzip_disk_entry_set_filetype(__p,__x) ZZIP_SET16((__p)->z_filetype,(__x))
#define zzip_disk_entry_get_filemode(__p)     ZZIP_GET32((__p)->z_filemode)
#define zzip_disk_entry_set_filemode(__p,__x) ZZIP_SET32((__p)->z_filemode,(__x))
#define zzip_disk_entry_get_offset(__p)     ZZIP_GET32((__p)->z_offset)
#define zzip_disk_entry_set_offset(__p,__x) ZZIP_SET32((__p)->z_offset,(__x))
#define zzip_disk_entry_sizeof_tails(__p) (zzip_disk_entry_get_namlen(__p) +\
					   zzip_disk_entry_get_extras(__p) +\
					   zzip_disk_entry_get_comment(__p) )
#define zzip_disk_entry_check_magic(__p)  ZZIP_DISK_ENTRY_CHECKMAGIC((__p))

/* zzip_disk_trailer - the zip archive entry point */
#define zzip_disk_trailer_get_magic(__p)      ZZIP_GET32((__p)->z_magic)
#define zzip_disk_trailer_set_magic(__p,__x)  ZZIP_SET32((__p)->z_magic,(__x))
#define zzip_disk_trailer_get_disk(__p)     ZZIP_GET16((__p)->z_disk)
#define zzip_disk_trailer_set_disk(__p,__x) ZZIP_SET16((__p)->z_disk,(__x))
#define zzip_disk_trailer_get_finaldisk(__p)     ZZIP_GET16((__p)->z_finaldisk)
#define zzip_disk_trailer_set_finaldisk(__p,__x) ZZIP_SET16((__p)->z_finaldisk,(__x))
#define zzip_disk_trailer_get_entries(__p)     ZZIP_GET16((__p)->z_entries)
#define zzip_disk_trailer_set_entries(__p,__x) ZZIP_SET16((__p)->z_entries,(__x))
#define zzip_disk_trailer_get_finalentries(__p)     ZZIP_GET16((__p)->z_finalentries)
#define zzip_disk_trailer_set_finalentries(__p,__x) ZZIP_SET16((__p)->z_finalentries,(__x))
#define zzip_disk_trailer_get_rootsize(__p)     ZZIP_GET32((__p)->z_rootsize)
#define zzip_disk_trailer_set_rootsize(__p,__x) ZZIP_SET32((__p)->z_rootsize,(__x))
#define zzip_disk_trailer_get_rootseek(__p)     ZZIP_GET32((__p)->z_rootseek)
#define zzip_disk_trailer_set_rootseek(__p,__x) ZZIP_SET32((__p)->z_rootseek,(__x))
#define zzip_disk_trailer_get_comment(__p)     ZZIP_GET16((__p)->z_comment)
#define zzip_disk_trailer_set_comment(__p,__x) ZZIP_SET16((__p)->z_comment,(__x))
#define zzip_disk_trailer_sizeof_tails(__p) ( zzip_disk_entry_get_comment(__p))
#define zzip_disk_trailer_check_magic(__p)  ZZIP_DISK_TRAILER_CHECKMAGIC((__p))

/* extra field should be type + size + data + type + size + data ... */
#define zzip_extra_block_get_datatype(__p)     ZZIP_GET16((zzip_byte_t*)(__p))
#define zzip_extra_block_set_datatype(__p,__x) ZZIP_SET16((zzip_byte_t*)(__p),__x)
#define zzip_extra_block_get_datasize(__p)     ZZIP_GET16((zzip_byte_t*)(__p)+2)
#define zzip_extra_block_set_datasize(__p,__x) ZZIP_SET16((zzip_byte_t*)(__p)+2,__x)

/* zzip64_disk_trailer - the zip64 archive entry point */
#define zzip_disk64_trailer_get_magic(__p)      ZZIP_GET32((__p)->z_magic)
#define zzip_disk64_trailer_set_magic(__p,__x)  ZZIP_SET32((__p)->z_magic,(__x))
#define zzip_disk64_trailer_get_size(__p)     ZZIP_GET64((__p)->z_size)
#define zzip_disk64_trailer_set_size(__p,__x) ZZIP_SET64((__p)->z_size,(__x))
#define zzip_disk64_trailer_get_disk(__p)     ZZIP_GET32((__p)->z_disk)
#define zzip_disk64_trailer_set_disk(__p,__x) ZZIP_SET32((__p)->z_disk,(__x))
#define zzip_disk64_trailer_get_finaldisk(__p)     ZZIP_GET32((__p)->z_finaldisk)
#define zzip_disk64_trailer_set_finaldisk(__p,__x) ZZIP_SET32((__p)->z_finaldisk,(__x))
#define zzip_disk64_trailer_get_entries(__p)     ZZIP_GET64((__p)->z_entries)
#define zzip_disk64_trailer_set_entries(__p,__x) ZZIP_SET64((__p)->z_entries,(__x))
#define zzip_disk64_trailer_get_finalentries(__p)     ZZIP_GET64((__p)->z_finalentries)
#define zzip_disk64_trailer_set_finalentries(__p,__x) ZZIP_SET64((__p)->z_finalentries,(__x))
#define zzip_disk64_trailer_get_rootsize(__p)     ZZIP_GET64((__p)->z_rootsize)
#define zzip_disk64_trailer_set_rootsize(__p,__x) ZZIP_SET64((__p)->z_rootsize,(__x))
#define zzip_disk64_trailer_get_rootseek(__p)     ZZIP_GET64((__p)->z_rootseek)
#define zzip_disk64_trailer_set_rootseek(__p,__x) ZZIP_SET64((__p)->z_rootseek,(__x))
#define zzip_disk64_trailer_check_magic(__p)  ZZIP_DISK64_TRAILER_CHECKMAGIC((__p))

/* .............. some logical typed access wrappers ....................... */

/* zzip_file_header - the local file header */
#define zzip_file_header_csize(__p)   ((zzip_size_t) \
        zzip_file_header_get_csize(__p))
#define zzip_file_header_usize(__p)   ((zzip_size_t) \
        zzip_file_header_get_usize(__p))
#define zzip_file_header_namlen(__p)   ((zzip_size_t) \
        zzip_file_header_get_namlen(__p))
#define zzip_file_header_extras(__p)   ((zzip_size_t) \
        zzip_file_header_get_extras(__p))
#define zzip_file_header_sizeof_tail(__p) ((zzip_size_t) \
        zzip_file_header_sizeof_tails(__p))
#define zzip_file_header_sizeto_end(__p)   ((zzip_size_t) \
        (zzip_file_header_sizeof_tail(__p) + zzip_file_header_headerlength))
#define zzip_file_header_skipto_end(__p)   ((void*) (__p) + \
        (zzip_file_header_sizeof_tail(__p) + zzip_file_header_headerlength))

#define zzip_file_header_to_filename(__p)   ((char*) \
        ((char*)(__p) + zzip_file_header_headerlength))
#define zzip_file_header_to_extras(__p)   ((char*) \
        (zzip_file_header_to_filename(__p) + zzip_file_header_namlen(__p)))
#define zzip_file_header_to_data(__p)   ((zzip_byte_t*) \
        (zzip_file_header_to_extras(__p) + zzip_file_header_extras(__p)))
#define zzip_file_header_to_trailer(__p)   ((struct zzip_file_trailer*) \
        (zzip_file_header_to_data(__p) + zzip_file_header_csize(__p)))

/* zzip_file_trailer - data descriptor per file block */
#define zzip_file_trailer_csize(__p)   ((zzip_size_t) \
        zzip_file_trailer_get_csize(__p))
#define zzip_file_trailer_usize(__p)   ((zzip_size_t) \
        zzip_file_trailer_get_usize(__p))
#define zzip_file_trailer_sizeof_tail(__p) ((zzip_size_t) \
        zzip_file_trailer_sizeof_tails(__p))
#define zzip_file_trailer_sizeto_end(__p)   ((zzip_size_t) \
        (zzip_file_trailer_sizeof_tail(__p) + zzip_file_trailer_headerlength))
#define zzip_file_trailer_skipto_end(__p)   ((void*) (__p) + \
        (zzip_file_trailer_sizeof_tail(__p) + zzip_file_trailer_headerlength))

/* zzip_disk_entry (currently named zzip_root_dirent) */
#define zzip_disk_entry_csize(__p)   ((zzip_size_t) \
        zzip_disk_entry_get_csize(__p))
#define zzip_disk_entry_usize(__p)   ((zzip_size_t) \
        zzip_disk_entry_get_usize(__p))
#define zzip_disk_entry_namlen(__p)   ((zzip_size_t) \
        zzip_disk_entry_get_namlen(__p))
#define zzip_disk_entry_extras(__p)   ((zzip_size_t) \
        zzip_disk_entry_get_extras(__p))
#define zzip_disk_entry_comment(__p)   ((zzip_size_t) \
        zzip_disk_entry_get_comment(__p))
#define zzip_disk_entry_diskstart(__p) ((int) \
        zzip_disk_entry_get_diskstart(__p))
#define zzip_disk_entry_filetype(__p) ((int) \
        zzip_disk_entry_get_filetype(__p))
#define zzip_disk_entry_filemode(__p) ((int) \
        zzip_disk_entry_get_filemode(__p))
#define zzip_disk_entry_fileoffset(__p) ((zzip_off_t) \
        zzip_disk_entry_get_offset(__p))
#define zzip_disk_entry_sizeof_tail(__p) ((zzip_size_t) \
        zzip_disk_entry_sizeof_tails(__p))
#define zzip_disk_entry_sizeto_end(__p)   ((zzip_size_t) \
        (zzip_disk_entry_sizeof_tail(__p) + zzip_disk_entry_headerlength))
#define zzip_disk_entry_skipto_end(__p)   ((zzip_byte_t*) (__p) + \
        (zzip_disk_entry_sizeof_tail(__p) + zzip_disk_entry_headerlength))

#define zzip_disk_entry_to_filename(__p)   ((char*) \
        ((char*)(__p) + zzip_disk_entry_headerlength))
#define zzip_disk_entry_to_extras(__p)   ((char*) \
        (zzip_disk_entry_to_filename(__p) + zzip_disk_entry_namlen(__p)))
#define zzip_disk_entry_to_comment(__p)   ((char*) \
        (zzip_disk_entry_to_extras(__p) + zzip_disk_entry_extras(__p)))
#define zzip_disk_entry_to_next_entry(__p)   ((struct zzip_disk_entry*) \
        (zzip_disk_entry_to_comment(__p) + zzip_disk_entry_comment(__p)))

/* zzip_disk_trailer - the zip archive entry point */
#define zzip_disk_trailer_localdisk(__p) ((int) \
        zzip_disk_trailer_get_disk(__p))
#define zzip_disk_trailer_finaldisk(__p) ((int) \
        zzip_disk_trailer_get_finaldisk(__p))
#define zzip_disk_trailer_localentries(__p) ((int) \
        zzip_disk_trailer_get_entries(__p))
#define zzip_disk_trailer_finalentries(__p) ((int) \
        zzip_disk_trailer_get_finalentries(__p))
#define zzip_disk_trailer_rootsize(__p) ((zzip_off_t) \
        zzip_disk_trailer_get_rootsize(__p))
#define zzip_disk_trailer_rootseek(__p) ((zzip_off_t) \
        zzip_disk_trailer_get_rootseek(__p))
#define zzip_disk_trailer_comment(__p)   ((zzip_size_t) \
        zzip_disk_trailer_get_comment(__p))
#define zzip_disk_trailer_sizeof_tail(__p) ((zzip_size_t) \
        zzip_disk_trailer_sizeof_tails(__p))
#define zzip_disk_trailer_sizeto_end(__p)   ((zzip_size_t) \
        (zzip_disk_trailer_sizeof_tail(__p) + zzip_disk_trailer_headerlength))
#define zzip_disk_trailer_skipto_end(__p)   ((void*) (__p) \
        (zzip_disk_trailer_sizeof_tail(__p) + zzip_disk_trailer_headerlength))

#define zzip_disk_trailer_to_comment(__p)   ((char*) \
        ((char*)(__p) + zzip_disk_trailer_headerlength))
#define zzip_disk_trailer_to_endoffile(__p)   ((void*) \
        (zzip_disk_trailer_to_comment(__p) + zzip_disk_trailer_comment(__p)))

/* zzip_disk64_trailer - the zip archive entry point */
#define zzip_disk64_trailer_localdisk(__p) ((int) \
        zzip_disk64_trailer_get_disk(__p))
#define zzip_disk64_trailer_finaldisk(__p) ((int) \
        zzip_disk64_trailer_get_finaldisk(__p))
#define zzip_disk64_trailer_localentries(__p) ((int) \
        zzip_disk64_trailer_get_entries(__p))
#define zzip_disk64_trailer_finalentries(__p) ((int) \
        zzip_disk64_trailer_get_finalentries(__p))
#define zzip_disk64_trailer_rootsize(__p) ((zzip_off64_t) \
        zzip_disk64_trailer_get_rootsize(__p))
#define zzip_disk64_trailer_rootseek(__p) ((zzip_off64_t) \
        zzip_disk64_trailer_get_rootseek(__p))
#define zzip_disk64_trailer_sizeof_tail(__p)   ((zzip_size_t) \
        zzip_disk64_trailer_get_size(__p) - zzip_disk64_trailer_headerlength)
#define zzip_disk64_trailer_sizeto_end(__p)   ((zzip_size_t) \
        zzip_disk64_trailer_get_size(__p))
#define zzip_disk64_trailer_skipto_end(__p)   ((void*) \
        ((char*)(__p) + zzip_disk64_sizeto_end(__p)))

/* extra field should be type + size + data + type + size + data ... */
#define zzip_extra_block_sizeof_tail(__p)  ((zzip_size_t) \
        (zzip_extra_block_get_datasize(__p)))
#define zzip_extra_block_sizeto_end(__p)    ((zzip_size_t) \
        (zzip_extra_block_sizeof_tail(__p) + zzip_extra_block_headerlength))
#define zzip_extra_block_skipto_end(__p)    ((void*) (__p) \
        (zzip_extra_block_sizeof_tail(__p) + zzip_extra_block_headerlength))

/* ................... and put these to the next level ................ */

#define zzip_file_header_data_encrypted(__p) \
        ZZIP_IS_ENCRYPTED( zzip_file_header_get_flags(__p) )
#define zzip_file_header_data_comprlevel(__p) \
        ZZIP_IS_COMPRLEVEL( zzip_file_header_get_flags(__p) )
#define zzip_file_header_data_streamed(__p) \
        ZZIP_IS_STREAMED( zzip_file_header_get_flags(__p) )
#define zzip_file_header_data_stored(__p) \
        ( ZZIP_IS_STORED ==   zzip_file_header_get_compr(__p) )
#define zzip_file_header_data_deflated(__p) \
        ( ZZIP_IS_DEFLATED == zzip_file_header_get_compr(__p) )

#define zzip_disk_entry_data_encrypted(__p) \
        ZZIP_IS_ENCRYPTED( zzip_disk_entry_get_flags(__p) )
#define zzip_disk_entry_data_comprlevel(__p) \
        ZZIP_IS_COMPRLEVEL( zzip_disk_entry_get_flags(__p) )
#define zzip_disk_entry_data_streamed(__p) \
        ZZIP_IS_STREAMED( zzip_disk_entry_get_flags(__p) )
#define zzip_disk_entry_data_stored(__p) \
        ( ZZIP_IS_STORED ==  zzip_disk_entry_get_compr(__p) )
#define zzip_disk_entry_data_deflated(__p) \
        ( ZZIP_IS_DEFLATED ==  zzip_disk_entry_get_compr(__p) )
#define zzip_disk_entry_data_ascii(__p) \
        ( zzip_disk_entry_get_filetype(__p) & 1)

#define zzip_file_header_data_not_deflated(__p) \
        (zzip_file_header_data_stored(__p))
#define zzip_file_header_data_std_deflated(__p) \
        (zzip_file_header_data_deflated(__p) && \
	 zzip_file_header_data_comprlevel(__p) == ZZIP_DEFLATED_STD_COMPR)
#define zzip_file_header_data_max_deflated(__p) \
        (zzip_file_header_data_deflated(__p) && \
	 zzip_file_header_data_comprlevel(__p) == ZZIP_DEFLATED_MAX_COMPR)
#define zzip_file_header_data_low_deflated(__p) \
        (zzip_file_header_data_deflated(__p) && \
	 zzip_file_header_data_comprlevel(__p) == ZZIP_DEFLATED_LOW_COMPR)
#define zzip_file_header_data_min_deflated(__p) \
        (zzip_file_header_data_deflated(__p) && \
	 zzip_file_header_data_comprlevel(__p) == ZZIP_DEFLATED_MIN_COMPR)

#define zzip_disk_entry_data_not_deflated(__p) \
        (zzip_disk_entry_data_stored(__p))
#define zzip_disk_entry_data_std_deflated(__p) \
        (zzip_disk_entry_data_deflated(__p) && \
	 zzip_disk_entry_data_comprlevel(__p) == ZZIP_DEFLATED_STD_COMPR)
#define zzip_disk_entry_data_max_deflated(__p) \
        (zzip_disk_entry_data_deflated(__p) && \
	 zzip_disk_entry_data_comprlevel(__p) == ZZIP_DEFLATED_MAX_COMPR)
#define zzip_disk_entry_data_low_deflated(__p) \
        (zzip_disk_entry_data_deflated(__p) && \
	 zzip_disk_entry_data_comprlevel(__p) == ZZIP_DEFLATED_LOW_COMPR)
#define zzip_disk_entry_data_min_deflated(__p) \
        (zzip_disk_entry_data_deflated(__p) && \
	 zzip_disk_entry_data_comprlevel(__p) == ZZIP_DEFLATED_MIN_COMPR)

/**
 * This function does half the job of => zzip_disk_entry_to_data where it
 * can augment with => zzip_file_header_to_data helper from format/fetch.h
 */
struct zzip_file_header * zzip_disk_entry_to_file_header(ZZIP_DISK* disk, struct zzip_disk_entry* entry) {
    zzip_byte_t* file_header = /* (struct zzip_file_header*) */
	(disk->buffer + zzip_disk_entry_fileoffset (entry));
    if (disk->buffer > file_header || file_header >= disk->endbuf) 
	return 0;
    return (struct zzip_file_header*) file_header;
}

/**
 * This function is a big helper despite its little name: in a zip file the
 * encoded filenames are usually NOT zero-terminated but for common usage
 * with libc we need it that way. Secondly, the filename SHOULD be present
 * in the zip central directory but if not then we fallback to the filename
 * given in the file_header of each compressed data portion.
 */
char * zzip_disk_entry_name(ZZIP_DISK* disk, struct zzip_disk_entry* entry, zzip_size_t & len) {
    if (! disk || ! entry) return 0;

    char* name;
    struct zzip_file_header* file;
    if ((len = zzip_disk_entry_namlen (entry)))
	name = zzip_disk_entry_to_filename (entry);
    else if ((file = zzip_disk_entry_to_file_header (disk, entry)) &&
	     (len = zzip_file_header_namlen (file)))
	name = zzip_file_header_to_filename (file);
    else
	return 0;

    if ((zzip_byte_t*) name < disk->buffer || 
	(zzip_byte_t*) name+len > disk->endbuf)
	return 0;

	return name;
}
/*
char * zzip_disk_entry_strdup_name(ZZIP_DISK* disk, struct zzip_disk_entry* entry) {
    if (! disk || ! entry) return 0;

	zzip_size_t len;
    char* name = zzip_disk_entry_name(disk, entry, len);

	if(!name) return 0;
    
	char* dup = (char*) malloc(len + 1);
    strncpy_s(dup, len + 1, name, len);
	dup[len] = 0;
	return dup;
}
*/

/**
 * This function is the first call of all the zip access functions here.
 * It contains the code to find the first entry of the zip central directory. 
 * Here we require the mmapped block to represent a real zip file where the
 * disk_trailer is _last_ in the file area, so that its position would be at 
 * a fixed offset from the end of the file area if not for the comment field 
 * allowed to be of variable length (which needs us to do a little search
 * for the disk_tailer). However, in this simple implementation we disregard 
 * any disk_trailer info telling about multidisk archives, so we just return
 * a pointer to the zip central directory.
 * 
 * For an actual means, we are going to search backwards from the end 
 * of the mmaped block looking for the PK-magic signature of a 
 * disk_trailer. If we see one then we check the rootseek value to
 * find the first disk_entry of the root central directory. If we find
 * the correct PK-magic signature of a disk_entry over there then we 
 * assume we are done and we are going to return a pointer to that label.
 *
 * The return value is a pointer to the first zzip_disk_entry being checked
 * to be within the bounds of the file area specified by the arguments. If
 * no disk_trailer was found then null is returned, and likewise we only 
 * accept a disk_trailer with a seekvalue that points to a disk_entry and 
 * both parts have valid PK-magic parts. Beyond some sanity check we try to
 * catch a common brokeness with zip archives that still allows us to find
 * the start of the zip central directory.
 */
struct zzip_disk_entry*
zzip_disk_findfirst(ZZIP_DISK* disk)
{
    if (disk->buffer > disk->endbuf-sizeof(struct zzip_disk_trailer))
	return 0;
    zzip_byte_t* p = disk->endbuf-sizeof(struct zzip_disk_trailer);
    for (; p >= disk->buffer ; p--)
    {
	zzip_byte_t* root; /* (struct zzip_disk_entry*) */
	if (zzip_disk_trailer_check_magic(p)) {
	    root =  disk->buffer + zzip_disk_trailer_get_rootseek (
		(struct zzip_disk_trailer*)p);
	    if (root > p) 
	    {   /* the first disk_entry is after the disk_trailer? can't be! */
		zzip_size_t rootsize = zzip_disk_trailer_get_rootsize (
		    (struct zzip_disk_trailer*)p);
		if (disk->buffer+rootsize > p) continue;
		/* a common brokeness that can be fixed: we just assume the
		 * central directory was written directly before the trailer:*/
		root = p - rootsize;
	    }
	} else if (zzip_disk64_trailer_check_magic(p)) {
	    if (sizeof(void*) < 8) return 0; /* EOVERFLOW */
	    root =  disk->buffer + zzip_disk64_trailer_get_rootseek (
		(struct zzip_disk64_trailer*)p);
	    if (root > p) continue; 
	} else continue;

	if (root < disk->buffer) continue;
	if (zzip_disk_entry_check_magic(root)) 
	    return (struct zzip_disk_entry*) root;
    };
    return 0;
}

/**
 * This function takes an existing disk_entry in the central root directory
 * (e.g. from zzip_disk_findfirst) and returns the next entry within in
 * the given bounds of the mmapped file area.
 */
struct zzip_disk_entry*
zzip_disk_findnext(ZZIP_DISK* disk, struct zzip_disk_entry* entry)
{
    if ((zzip_byte_t*)entry < disk->buffer || 
	(zzip_byte_t*)entry > disk->endbuf-sizeof(entry) ||
	! zzip_disk_entry_check_magic (entry) ||
	zzip_disk_entry_sizeto_end (entry) > 64*1024)
	return 0;
    entry = zzip_disk_entry_to_next_entry (entry);
    if ((zzip_byte_t*)entry > disk->endbuf-sizeof(entry) ||
	! zzip_disk_entry_check_magic (entry) ||
	zzip_disk_entry_sizeto_end (entry) > 64*1024 ||
	zzip_disk_entry_skipto_end (entry) + sizeof(entry) > disk->endbuf)
	return 0;
    else
	return entry;
}

/** search for files in the (mmapped) zip central directory
 *
 * This function is given a filename as an additional argument, to find the 
 * disk_entry matching a given filename. The compare-function is usually 
 * strcmp or strcasecmp or perhaps strcoll, if null then strcmp is used. 
 * - use null as argument for "after"-entry when searching the first 
 * matching entry, otherwise the last returned value if you look for other
 * entries with a special "compare" function (if null then a doubled search
 * is rather useless with this variant of _findfile).
 */
struct zzip_disk_entry*
zzip_disk_findfile(ZZIP_DISK* disk, char* filename, 
		    struct zzip_disk_entry* after, zzip_strcmp_fn_t compare)
{
    struct zzip_disk_entry* entry = (! after ? zzip_disk_findfirst (disk) 
				     : zzip_disk_findnext (disk, after));
	size_t filename_length = strlen(filename);
    for (; entry ; entry = zzip_disk_findnext (disk, entry)) {
		/* filenames within zip files are often not null-terminated! */
		zzip_size_t len;
		char* realname = zzip_disk_entry_name (disk, entry, len);
		if (realname && 0 == _strnicmp(filename, realname, min(len, filename_length))) {
			return entry;
		}
    }
    return 0;
}

/**
 * the ZZIP_DISK_FILE* is rather simple in just encapsulating the
 * arguments given to this function plus a zlib deflate buffer.
 * Note that the ZZIP_DISK pointer does already contain the full
 * mmapped file area of a zip disk, so open()ing a file part within
 * that area happens to be a lookup of its bounds and encoding. That
 * information is memorized on the ZZIP_DISK_FILE so that subsequent
 * _read() operations will be able to get the next data portion or
 * return an eof condition for that file part wrapped in the zip archive.
 */
ZZIP_DISK_FILE * zzip_disk_entry_fopen(ZZIP_DISK* disk, ZZIP_DISK_ENTRY* entry) {
    /* keep this in sync with zzip_mem_entry_fopen */
    struct zzip_file_header* header = 
	zzip_disk_entry_to_file_header (disk, entry);
    if (! header) return 0;
    ZZIP_DISK_FILE* file = (ZZIP_DISK_FILE*) malloc(sizeof(ZZIP_DISK_FILE));
    if (! file) return file;
    file->buffer = disk->buffer;
    file->endbuf = disk->endbuf;
    file->avail = zzip_file_header_usize (header);

    if (! file->avail || zzip_file_header_data_stored (header))
    { file->stored = zzip_file_header_to_data (header); return file; }

    file->stored = 0;
    file->zlib.opaque = 0;
    file->zlib.zalloc = Z_NULL;
    file->zlib.zfree = Z_NULL;
    file->zlib.avail_in = (uInt) zzip_file_header_csize (header);
    file->zlib.next_in = zzip_file_header_to_data (header);

    if (! zzip_file_header_data_deflated (header) ||
	inflateInit2 (& file->zlib, -MAX_WBITS) != Z_OK)
    { free (file); return 0; }

    return file;
}

/** openening a file part wrapped within a (mmapped) zip archive
 *
 * This function opens a file found by name, so it does a search into
 * the zip central directory with => zzip_disk_findfile and whatever
 * is found first is given to => zzip_disk_entry_fopen
 */
ZZIP_DISK_FILE * zzip_disk_fopen(ZZIP_DISK * disk, char * filename) {
    ZZIP_DISK_ENTRY* entry = zzip_disk_findfile (disk, filename, 0, 0);
    if (! entry) return 0; else return zzip_disk_entry_fopen (disk, entry);
}

/**
 * This function releases any zlib decoder info needed for decompression
 * and dumps the ZZIP_DISK_FILE* then.
 */
void zzip_disk_fclose(ZZIP_DISK_FILE * file) {
	if(!file->stored)
		inflateEnd(&file->zlib);
	free(file);
}

/**
 * This function reads more bytes into the output buffer specified as
 * arguments. The return value is null on eof or error, the stdio-like
 * interface can not distinguish between these so you need to check
 * with => zzip_disk_feof for the difference.
 */
zzip_size_t zzip_disk_fread(void* ptr, zzip_size_t size, ZZIP_DISK_FILE* file) {
    if (size > file->avail) size = file->avail;
    if (file->stored) {
		memcpy (ptr, file->stored, size);
		file->stored += size;
		file->avail -= size;
		return size;
    }
    
    file->zlib.avail_out = (uInt) size;
    file->zlib.next_out = (Bytef*) ptr;
    zzip_size_t total_old = file->zlib.total_out;
    int err = inflate (& file->zlib, Z_NO_FLUSH);
    if (err == Z_STREAM_END)
		file->avail = 0;
    else if (err == Z_OK)
		file->avail -= file->zlib.total_out - total_old;
    else
		return 0;
    return file->zlib.total_out - total_old;
}
