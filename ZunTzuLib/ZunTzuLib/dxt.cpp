/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"

// options: FAVOR_SPEED = 0, FAVOR_QUALITY = 1
// options: USE_FLOAT = 0, USE_SIMD = 2

// float execution path
void __fastcall encodeDxt1Block_fast_float(const unsigned char * rgba, unsigned char * block);
void __fastcall encodeDxt1Block_quality_float(const unsigned char * rgba, unsigned char * block);
void __fastcall encodeDxt5Block_float(const unsigned char * rgba,	unsigned char * block);
// simd execution path
unsigned int __fastcall set_mxcsr();
void __fastcall restore_mxcsr(unsigned int mxcsr);
void __fastcall encodeDxt1Block_fast_simd(const unsigned char * rgba, unsigned char * block);
void __fastcall encodeDxt1Block_quality_simd(const unsigned char * rgba, unsigned char * block);
void __fastcall encodeDxt5Block_simd(const unsigned char * rgba,	unsigned char * block);

extern "C" void __cdecl CompressDxt1(
	const char * rgb,
	int top, int left, int bottom, int right,
	int stride, char * blocks, int option)
{
	unsigned int mxcsr;
	if(option & 2)
		mxcsr = set_mxcsr();

	const char * const source = rgb + top * stride + left * 3;
	const int width = right - left;
	const int height = bottom - top;

	const char * lines[4];
	for(int i = 0; i < 4; ++i)
		lines[i] = source + stride * i;

	unsigned int ptr[16];

	for(int blockY = 0; blockY < 64; ++blockY) {
		for(int blockX = 0; blockX < 64; ++blockX) {
			unsigned int * p = ptr;
			for(int row = 0; row < 4; ++row) {
				for(int col = 0; col < 4; ++col) {
					if((blockY == 0 && row == 0 && top < 0) ||
						((blockY * 4) + row >= height) ||
						(blockX == 0 && col == 0 && left < 0) ||
						((blockX * 4) + col >= width))
					{
						*p = (unsigned int) 0x00000000;
					} else {
						((char*) p)[0] = *(lines[row] + col * 3 + 2);
						((char*) p)[1] = *(lines[row] + col * 3 + 1);
						((char*) p)[2] = *(lines[row] + col * 3 + 0);
						((char*) p)[3] = (char) 0xff;
					}
					++p;
				}
			}
			switch(option) {
				case 0:
					encodeDxt1Block_fast_float((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 1:
					encodeDxt1Block_quality_float((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 2:
					encodeDxt1Block_fast_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 3:
					encodeDxt1Block_quality_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					break;
			}
			blocks += 8;
			for(int i = 0; i < 4; ++i)
				lines[i] += 4 * 3;
		}
		for(int i = 0; i < 4; ++i)
			lines[i] += stride * 4 - 256 * 3;
	}

	if(option & 2)
		restore_mxcsr(mxcsr);
}

extern "C" void __cdecl CompressDxt1FromRgba(
	const char * rgba,
	int top, int left, int bottom, int right,
	int stride, char * blocks, int option)
{
	unsigned int mxcsr;
	if(option & 2)
		mxcsr = set_mxcsr();

	const char * const source = rgba + top * stride + left * 4;
	const int width = right - left;
	const int height = bottom - top;

	const char * lines[4];
	for(int i = 0; i < 4; ++i)
		lines[i] = source + stride * i;

	unsigned int ptr[16];

	for(int blockY = 0; blockY < 64; ++blockY) {
		for(int blockX = 0; blockX < 64; ++blockX) {
			unsigned int * p = ptr;
			for(int row = 0; row < 4; ++row) {
				for(int col = 0; col < 4; ++col) {
					if((blockY == 0 && row == 0 && top < 0) ||
						((blockY * 4) + row >= height) ||
						(blockX == 0 && col == 0 && left < 0) ||
						((blockX * 4) + col >= width))
					{
						*p = (unsigned int) 0x00000000;
					} else {
						((char*) p)[0] = *(lines[row] + col * 3 + 2);
						((char*) p)[1] = *(lines[row] + col * 3 + 1);
						((char*) p)[2] = *(lines[row] + col * 3 + 0);
						((char*) p)[3] = *(lines[row] + col * 3 + 3);
					}
					++p;
				}
			}
			switch(option) {
				case 0:
					encodeDxt1Block_fast_float((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 1:
					encodeDxt1Block_quality_float((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 2:
					encodeDxt1Block_fast_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					break;

				case 3:
					encodeDxt1Block_quality_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					break;
			}
			blocks += 8;
			for(int i = 0; i < 4; ++i)
				lines[i] += 4 * 4;
		}
		for(int i = 0; i < 4; ++i)
			lines[i] += stride * 4 - 256 * 4;
	}

	if(option & 2)
		restore_mxcsr(mxcsr);
}

extern "C" void __cdecl CompressDxt5(
	const char * rgba,
	int top, int left, int bottom, int right,
	int stride, char * blocks, int option)
{
	unsigned int mxcsr;
	if(option & 2)
		mxcsr = set_mxcsr();

	const char * const source = rgba + top * stride + left * 4;
	const int width = right - left;
	const int height = bottom - top;

	const char * lines[4];
	for(int i = 0; i < 4; ++i)
		lines[i] = source + stride * i;

	unsigned int ptr[16];

	for(int blockY = 0; blockY < 64; ++blockY) {
		for(int blockX = 0; blockX < 64; ++blockX) {
			unsigned int * p = ptr;
			for(int row = 0; row < 4; ++row) {
				for(int col = 0; col < 4; ++col) {
					if((blockY == 0 && row == 0 && top < 0) ||
						((blockY * 4) + row >= height) ||
						(blockX == 0 && col == 0 && left < 0) ||
						((blockX * 4) + col >= width))
					{
						*p = (unsigned int) 0x00000000;
					} else {
						((char*) p)[0] = *(lines[row] + col * 4 + 2);
						((char*) p)[1] = *(lines[row] + col * 4 + 1);
						((char*) p)[2] = *(lines[row] + col * 4 + 0);
						((char*) p)[3] = *(lines[row] + col * 4 + 3);
					}
					++p;
				}
			}
			switch(option) {
				case 0:
					encodeDxt5Block_float((const unsigned char *) ptr, (unsigned char *) blocks);
					encodeDxt1Block_fast_float((const unsigned char *) ptr, (unsigned char *) (blocks + 8));
					break;

				case 1:
					encodeDxt5Block_float((const unsigned char *) ptr, (unsigned char *) blocks);
					encodeDxt1Block_quality_float((const unsigned char *) ptr, (unsigned char *) (blocks + 8));
					break;

				case 2:
					encodeDxt5Block_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					encodeDxt1Block_fast_simd((const unsigned char *) ptr, (unsigned char *) (blocks + 8));
					break;

				case 3:
					encodeDxt5Block_simd((const unsigned char *) ptr, (unsigned char *) blocks);
					encodeDxt1Block_quality_simd((const unsigned char *) ptr, (unsigned char *) (blocks + 8));
					break;
			}
			blocks += 16;
			for(int i = 0; i < 4; ++i)
				lines[i] += 4 * 4;
		}
		for(int i = 0; i < 4; ++i)
			lines[i] += stride * 4 - 256 * 4;
	}

	if(option & 2)
		restore_mxcsr(mxcsr);
}
