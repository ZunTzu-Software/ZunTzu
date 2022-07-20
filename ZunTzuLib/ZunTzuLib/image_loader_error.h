#pragma once

/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

enum IMAGE_LOADER_ERROR {
	IMAGE_LOADER_ABORTED_BY_USER = -1,
	IMAGE_LOADER_OK = 0,

	UNZIPPER_CANNOT_OPEN_ARCHIVE_FILE,
	UNZIPPER_CANNOT_READ_FILE_SIZE,
	UNZIPPER_CANNOT_CREATE_FILE_MAPPING,
	UNZIPPER_CANNOT_MAP_FILE,
	UNZIPPER_ENTRY_NOT_FOUND,

	IMAGE_READ_PAST_LAST_SCANLINE,
	IMAGE_UNSUPPORTED_COLOR_SPACE,
	IMAGE_UNSUPPORTED_PROGRESSIVE,
	IMAGE_INCONSISTENT_MASK_DIMENSIONS,
	IMAGE_NOT_A_PNG_FILE,
	IMAGE_READ_ERROR,
	IMAGE_CANNOT_CREATE_PNG_READER,

	JPEG_ERRORS,	// JPEG errors start here
	JPEG_JERR_ARITH_NOTIMPL, // Sorry, there are legal restrictions on arithmetic coding
	JPEG_JERR_BAD_ALIGN_TYPE, // ALIGN_TYPE is wrong, please fix
	JPEG_JERR_BAD_ALLOC_CHUNK, // MAX_ALLOC_CHUNK is wrong, please fix
	JPEG_JERR_BAD_BUFFER_MODE, // Bogus buffer control mode
	JPEG_JERR_BAD_COMPONENT_ID, // Invalid component ID %d in SOS
	JPEG_JERR_BAD_DCT_COEF, // DCT coefficient out of range
	JPEG_JERR_BAD_DCTSIZE, // IDCT output block size %d not supported
	JPEG_JERR_BAD_HUFF_TABLE, // Bogus Huffman table definition
	JPEG_JERR_BAD_IN_COLORSPACE, // Bogus input colorspace
	JPEG_JERR_BAD_J_COLORSPACE, // Bogus JPEG colorspace
	JPEG_JERR_BAD_LENGTH, // Bogus marker length
	JPEG_JERR_BAD_LIB_VERSION, // Wrong JPEG library version: library is %d, caller expects %d
	JPEG_JERR_BAD_MCU_SIZE, // Sampling factors too large for interleaved scan
	JPEG_JERR_BAD_POOL_ID, // Invalid memory pool code %d
	JPEG_JERR_BAD_PRECISION, // Unsupported JPEG data precision %d
	JPEG_JERR_BAD_PROGRESSION, // Invalid progressive parameters Ss=%d Se=%d Ah=%d Al=%d
	JPEG_JERR_BAD_PROG_SCRIPT, // Invalid progressive parameters at scan script entry %d
	JPEG_JERR_BAD_SAMPLING, // Bogus sampling factors
	JPEG_JERR_BAD_SCAN_SCRIPT, // Invalid scan script at entry %d
	JPEG_JERR_BAD_STATE, // Improper call to JPEG library in state %d
	JPEG_JERR_BAD_STRUCT_SIZE, // JPEG parameter struct mismatch: library thinks size is %u, caller expects %u
	JPEG_JERR_BAD_VIRTUAL_ACCESS, // Bogus virtual array access
	JPEG_JERR_BUFFER_SIZE, // Buffer passed to JPEG library is too small
	JPEG_JERR_CANT_SUSPEND, // Suspension not allowed here
	JPEG_JERR_CCIR601_NOTIMPL, // CCIR601 sampling not implemented yet
	JPEG_JERR_COMPONENT_COUNT, // Too many color components: %d, max %d
	JPEG_JERR_CONVERSION_NOTIMPL, // Unsupported color conversion request
	JPEG_JERR_DAC_INDEX, // Bogus DAC index %d
	JPEG_JERR_DAC_VALUE, // Bogus DAC value 0x%x
	JPEG_JERR_DHT_INDEX, // Bogus DHT index %d
	JPEG_JERR_DQT_INDEX, // Bogus DQT index %d
	JPEG_JERR_EMPTY_IMAGE, // Empty JPEG image (DNL not supported)
	JPEG_JERR_EMS_READ, // Read from EMS failed
	JPEG_JERR_EMS_WRITE, // Write to EMS failed
	JPEG_JERR_EOI_EXPECTED, // Didn't expect more than one scan
	JPEG_JERR_FILE_READ, // Input file read error
	JPEG_JERR_FILE_WRITE, // Output file write error --- out of disk space?
	JPEG_JERR_FRACT_SAMPLE_NOTIMPL, // Fractional sampling not implemented yet
	JPEG_JERR_HUFF_CLEN_OVERFLOW, // Huffman code size table overflow
	JPEG_JERR_HUFF_MISSING_CODE, // Missing Huffman code table entry
	JPEG_JERR_IMAGE_TOO_BIG, // Maximum supported image dimension is %u pixels
	JPEG_JERR_INPUT_EMPTY, // Empty input file
	JPEG_JERR_INPUT_EOF, // Premature end of input file
	JPEG_JERR_MISMATCHED_QUANT_TABLE, // Cannot transcode due to multiple use of quantization table %d
	JPEG_JERR_MISSING_DATA, // Scan script does not transmit all data
	JPEG_JERR_MODE_CHANGE, // Invalid color quantization mode change
	JPEG_JERR_NOTIMPL, // Not implemented yet
	JPEG_JERR_NOT_COMPILED, // Requested feature was omitted at compile time
	JPEG_JERR_NO_BACKING_STORE, // Backing store not supported
	JPEG_JERR_NO_HUFF_TABLE, // Huffman table 0x%02x was not defined
	JPEG_JERR_NO_IMAGE, // JPEG datastream contains no image
	JPEG_JERR_NO_QUANT_TABLE, // Quantization table 0x%02x was not defined
	JPEG_JERR_NO_SOI, // Not a JPEG file: starts with 0x%02x 0x%02x
	JPEG_JERR_OUT_OF_MEMORY, // Insufficient memory (case %d)
	JPEG_JERR_QUANT_COMPONENTS, // Cannot quantize more than %d color components
	JPEG_JERR_QUANT_FEW_COLORS, // Cannot quantize to fewer than %d colors
	JPEG_JERR_QUANT_MANY_COLORS, // Cannot quantize to more than %d colors
	JPEG_JERR_SOF_DUPLICATE, // Invalid JPEG file structure: two SOF markers
	JPEG_JERR_SOF_NO_SOS, // Invalid JPEG file structure: missing SOS marker
	JPEG_JERR_SOF_UNSUPPORTED, // Unsupported JPEG process: SOF type 0x%02x
	JPEG_JERR_SOI_DUPLICATE, // Invalid JPEG file structure: two SOI markers
	JPEG_JERR_SOS_NO_SOF, // Invalid JPEG file structure: SOS before SOF
	JPEG_JERR_TFILE_CREATE, // Failed to create temporary file %s
	JPEG_JERR_TFILE_READ, // Read failed on temporary file
	JPEG_JERR_TFILE_SEEK, // Seek failed on temporary file
	JPEG_JERR_TFILE_WRITE, // Write failed on temporary file --- out of disk space?
	JPEG_JERR_TOO_LITTLE_DATA, // Application transferred too few scanlines
	JPEG_JERR_UNKNOWN_MARKER, // Unsupported marker type 0x%02x
	JPEG_JERR_VIRTUAL_BUG, // Virtual array controller messed up
	JPEG_JERR_WIDTH_OVERFLOW, // Image too wide for this implementation
	JPEG_JERR_XMS_READ, // Read from XMS failed
	JPEG_JERR_XMS_WRITE, // Write to XMS failed
	JPEG_JMSG_COPYRIGHT,
	JPEG_JMSG_VERSION,
	JPEG_JTRC_16BIT_TABLES, // Caution: quantization tables are too coarse for baseline JPEG
	JPEG_JTRC_ADOBE, // Adobe APP14 marker: version %d, flags 0x%04x 0x%04x, transform %d
	JPEG_JTRC_APP0, // Unknown APP0 marker (not JFIF), length %u
	JPEG_JTRC_APP14, // Unknown APP14 marker (not Adobe), length %u
	JPEG_JTRC_DAC, // Define Arithmetic Table 0x%02x: 0x%02x
	JPEG_JTRC_DHT, // Define Huffman Table 0x%02x
	JPEG_JTRC_DQT, // Define Quantization Table %d  precision %d
	JPEG_JTRC_DRI, // Define Restart Interval %u
	JPEG_JTRC_EMS_CLOSE, // Freed EMS handle %u
	JPEG_JTRC_EMS_OPEN, // Obtained EMS handle %u
	JPEG_JTRC_EOI, // End Of Image
	JPEG_JTRC_HUFFBITS, //         %3d %3d %3d %3d %3d %3d %3d %3d
	JPEG_JTRC_JFIF, // JFIF APP0 marker: version %d.%02d, density %dx%d  %d
	JPEG_JTRC_JFIF_BADTHUMBNAILSIZE, // Warning: thumbnail image size does not match data length %u
	JPEG_JTRC_JFIF_EXTENSION, // JFIF extension marker: type 0x%02x, length %u
	JPEG_JTRC_JFIF_THUMBNAIL, //     with %d x %d thumbnail image
	JPEG_JTRC_MISC_MARKER, // Miscellaneous marker 0x%02x, length %u
	JPEG_JTRC_PARMLESS_MARKER, // Unexpected marker 0x%02x
	JPEG_JTRC_QUANTVALS, //         %4u %4u %4u %4u %4u %4u %4u %4u
	JPEG_JTRC_QUANT_3_NCOLORS, // Quantizing to %d = %d*%d*%d colors
	JPEG_JTRC_QUANT_NCOLORS, // Quantizing to %d colors
	JPEG_JTRC_QUANT_SELECTED, // Selected %d colors for quantization
	JPEG_JTRC_RECOVERY_ACTION, // At marker 0x%02x, recovery action %d
	JPEG_JTRC_RST, // RST%d
	JPEG_JTRC_SMOOTH_NOTIMPL, // Smoothing not supported with nonstandard sampling ratios
	JPEG_JTRC_SOF, // Start Of Frame 0x%02x: width=%u, height=%u, components=%d
	JPEG_JTRC_SOF_COMPONENT, //     Component %d: %dhx%dv q=%d
	JPEG_JTRC_SOI, // Start of Image
	JPEG_JTRC_SOS, // Start Of Scan: %d components
	JPEG_JTRC_SOS_COMPONENT, //     Component %d: dc=%d ac=%d
	JPEG_JTRC_SOS_PARAMS, //   Ss=%d, Se=%d, Ah=%d, Al=%d
	JPEG_JTRC_TFILE_CLOSE, // Closed temporary file %s
	JPEG_JTRC_TFILE_OPEN, // Opened temporary file %s
	JPEG_JTRC_THUMB_JPEG, // JFIF extension marker: JPEG-compressed thumbnail image, length %u
	JPEG_JTRC_THUMB_PALETTE, // JFIF extension marker: palette thumbnail image, length %u
	JPEG_JTRC_THUMB_RGB, // JFIF extension marker: RGB thumbnail image, length %u
	JPEG_JTRC_UNKNOWN_IDS, // Unrecognized component IDs %d %d %d, assuming YCbCr
	JPEG_JTRC_XMS_CLOSE, // Freed XMS handle %u
	JPEG_JTRC_XMS_OPEN, // Obtained XMS handle %u
	JPEG_JWRN_ADOBE_XFORM, // Unknown Adobe color transform code %d
	JPEG_JWRN_BOGUS_PROGRESSION, // Inconsistent progression sequence for component %d coefficient %d
	JPEG_JWRN_EXTRANEOUS_DATA, // Corrupt JPEG data: %u extraneous bytes before marker 0x%02x
	JPEG_JWRN_HIT_MARKER, // Corrupt JPEG data: premature end of data segment
	JPEG_JWRN_HUFF_BAD_CODE, // Corrupt JPEG data: bad Huffman code
	JPEG_JWRN_JFIF_MAJOR, // Warning: unknown JFIF revision number %d.%02d
	JPEG_JWRN_JPEG_EOF, // Premature end of JPEG file
	JPEG_JWRN_MUST_RESYNC, // Corrupt JPEG data: found marker 0x%02x instead of RST%d
	JPEG_JWRN_NOT_SEQUENTIAL, // Invalid SOS parameters for sequential JPEG
	JPEG_JWRN_TOO_MUCH_DATA, // Application transferred too many scanlines

	PNG_ERRORS,	// PNG errors start here
	PNG_CALL_TO_NULL_READ_FUNCTION,	// Call to NULL read function
	PNG_CANNOT_READ_INTERLACED_IMAGE_INTERLACE_HANDLER_DISABLED,	// Cannot read interlaced image -- interlace handler disabled
	PNG_CRC_ERROR,	// CRC error
	PNG_DECOMPRESSION_ERROR,	// Decompression error
	PNG_DUPLICATE_PLTE_CHUNK,	// Duplicate PLTE chunk
	PNG_EXTRA_COMPRESSED_DATA,	// Extra compressed data
	PNG_IMAGE_IS_TOO_HIGH_TO_PROCESS_WITH_PNG_READ_PNG,	// Image is too high to process with png_read_png()
	PNG_IMAGE_SIZE_EXCEEDS_USER_LIMITS_IN_IHDR,	// image size exceeds user limits in IHDR
	PNG_IMAGE_WIDTH_OR_HEIGHT_IS_ZERO_IN_IHDR,	// Image width or height is zero in IHDR
	PNG_INCOMPATIBLE_LIBPNG_VERSION_IN_APPLICATION_AND_LIBRARY,	// Incompatible libpng version in application and library
	PNG_INSUFFICIENT_MEMORY_TO_STORE_TEXT,	// Insufficient memory to store text
	PNG_INSUFFICIENT_MEMORY_TO_STORE_ZTXT_CHUNK,	// Insufficient memory to store zTXt chunk
	PNG_INVALID_ATTEMPT_TO_READ_ROW_DATA,	// Invalid attempt to read row data
	PNG_INVALID_BIT_DEPTH,	// Invalid bit depth
	PNG_INVALID_BIT_DEPTH_IN_IHDR,	// Invalid bit depth in IHDR
	PNG_INVALID_CHUNK_TYPE,	// invalid chunk type
	PNG_INVALID_COLOR_TYPE,	// Invalid color type
	PNG_INVALID_COLOR_TYPE_IN_IHDR,	// Invalid color type in IHDR
	PNG_INVALID_COLOR_TYPE_BIT_DEPTH_COMBINATION_IN_IHDR,	// Invalid color type/bit depth combination in IHDR
	PNG_INVALID_IHDR_CHUNK,	// Invalid IHDR chunk
	PNG_INVALID_IMAGE_HEIGHT,	// Invalid image height
	PNG_INVALID_IMAGE_SIZE_IN_IHDR,	// Invalid image size in IHDR
	PNG_INVALID_IMAGE_WIDTH,	// Invalid image width
	PNG_INVALID_PALETTE_CHUNK,	// Invalid palette chunk
	PNG_INVALID_PALETTE_LENGTH,	// Invalid palette length
	PNG_MISSING_IHDR_BEFORE_BKGD,	// Missing IHDR before bKGD
	PNG_MISSING_IHDR_BEFORE_CHRM,	// Missing IHDR before cHRM
	PNG_MISSING_IHDR_BEFORE_GAMA,	// Missing IHDR before gAMA
	PNG_MISSING_IHDR_BEFORE_HIST,	// Missing IHDR before hIST
	PNG_MISSING_IHDR_BEFORE_ICCP,	// Missing IHDR before iCCP
	PNG_MISSING_IHDR_BEFORE_IDAT,	// Missing IHDR before IDAT
	PNG_MISSING_IHDR_BEFORE_OFFS,	// Missing IHDR before oFFs
	PNG_MISSING_IHDR_BEFORE_PCAL,	// Missing IHDR before pCAL
	PNG_MISSING_IHDR_BEFORE_PHYS,	// Missing IHDR before pHYs
	PNG_MISSING_IHDR_BEFORE_PLTE,	// Missing IHDR before PLTE
	PNG_MISSING_IHDR_BEFORE_SBIT,	// Missing IHDR before sBIT
	PNG_MISSING_IHDR_BEFORE_SCAL,	// Missing IHDR before sCAL
	PNG_MISSING_IHDR_BEFORE_SPLT,	// Missing IHDR before sPLT
	PNG_MISSING_IHDR_BEFORE_SRGB,	// Missing IHDR before sRGB
	PNG_MISSING_IHDR_BEFORE_TEXT,	// Missing IHDR before tEXt
	PNG_MISSING_IHDR_BEFORE_TRNS,	// Missing IHDR before tRNS
	PNG_MISSING_IHDR_BEFORE_ZTXT,	// Missing IHDR before zTXt
	PNG_MISSING_PLTE_BEFORE_IDAT,	// Missing PLTE before IDAT
	PNG_NO_IMAGE_IN_FILE,	// No image in file
	PNG_NOT_A_PNG_FILE,	// Not a PNG file
	PNG_NOT_ENOUGH_IMAGE_DATA,	// Not enough image data
	PNG_NOT_ENOUGH_MEMORY_FOR_TEXT,	// Not enough memory for text
	PNG_NOT_ENOUGH_MEMORY_TO_DECOMPRESS_CHUNK,	// Not enough memory to decompress chunk
	PNG_NULL_ROW_BUFFER,	// NULL row buffer
	PNG_OUT_OF_MEMORY,	// Out of Memory!
	PNG_OUT_OF_PLACE_IHDR,	// Out of place IHDR
	PNG_OUT_OF_PLACE_TIME_CHUNK,	// Out of place tIME chunk
	PNG_OVERFLOW_IN_PNG_MEMCPY_CHECK,	// Overflow in png_memcpy_check
	PNG_OVERFLOW_IN_PNG_MEMSET_CHECK,	// Overflow in png_memset_check
	PNG_PNG_FILE_CORRUPTED_BY_ASCII_CONVERSION,	// PNG file corrupted by ASCII conversion
	PNG_PNG_UNSIGNED_INTEGER_OUT_OF_RANGE,	// PNG unsigned integer out of range
	PNG_PNG_DO_DITHER_RETURNED_ROWBYTES_IS_ZERO,	// png_do_dither returned rowbytes=0
	PNG_PNG_DO_RGB_TO_GRAY_FOUND_NONGRAY_PIXEL,	// png_do_rgb_to_gray found nongray pixel
	PNG_ROW_HAS_TOO_MANY_BYTES_TO_ALLOCATE_IN_MEMORY,	// Row has too many bytes to allocate in memory
	PNG_ROWBYTES_OVERFLOW_IN_PNG_READ_START_ROW,	// Rowbytes overflow in png_read_start_row
	PNG_THE_INFO_STRUCT_ALLOCATED_BY_APPLICATION_FOR_READING_IS_TOO_SMALL,	// The info struct allocated by application for reading is too small
	PNG_THE_PNG_STRUCT_ALLOCATED_BY_THE_APPLICATION_FOR_READING_IS_TOO_SMALL,	// The png struct allocated by the application for reading is too small
	PNG_TOO_MANY_BYTES_FOR_PNG_SIGNATURE,	// Too many bytes for PNG signature
	PNG_TOO_MANY_IDATS_FOUND,	// Too many IDAT's found
	PNG_UNKNOWN_COMPRESSION_METHOD_IN_IHDR,	// Unknown compression method in IHDR
	PNG_UNKNOWN_CRITICAL_CHUNK,	// unknown critical chunk
	PNG_UNKNOWN_FILTER_METHOD_IN_IHDR,	// Unknown filter method in IHDR
	PNG_UNKNOWN_INTERLACE_METHOD_IN_IHDR,	// Unknown interlace method in IHDR
	PNG_UNKNOWN_ZLIB_ERROR,	// Unknown zlib error
	PNG_ZLIB_MEMORY_ERROR,	// zlib memory error
	PNG_ZLIB_VERSION_ERROR	// zlib version error
};
