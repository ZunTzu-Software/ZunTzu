/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include <emmintrin.h>	// SIMD intrinsics
#include "ZunTzuLib.h"

unsigned int __fastcall set_mxcsr()
{
	unsigned int mxcsr = _mm_getcsr();
	_mm_setcsr(0x9fc0);
	return mxcsr;
}

void __fastcall restore_mxcsr(unsigned int mxcsr)
{
	_mm_setcsr(mxcsr);
}

// SIMD macros

#define HORIZONTAL_MIN(x) \
	((x) = _mm_min_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(1,0,3,2))), \
	(x) = _mm_min_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(2,3,0,1))))
#define HORIZONTAL_MAX(x) \
	((x) = _mm_max_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(1,0,3,2))), \
	(x) = _mm_max_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(2,3,0,1))))
#define HORIZONTAL_ADD(x) \
	((x) = _mm_add_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(1,0,3,2))), \
	(x) = _mm_add_ps((x), _mm_shuffle_ps((x), (x), _MM_SHUFFLE(2,3,0,1))))
#define INVERSE_SIGN(x) _mm_sub_ps(_mm_set1_ps(0.0f), (x))

// DXT1 code book

static const unsigned char DXT1_CODES_4[4 * 4] = {
	0x01 << 0, 0x03 << 0, 0x02 << 0, 0x00 << 0,
	0x01 << 2, 0x03 << 2, 0x02 << 2, 0x00 << 2,
	0x01 << 4, 0x03 << 4, 0x02 << 4, 0x00 << 4,
	0x01 << 6, 0x03 << 6, 0x02 << 6, 0x00 << 6
};

// color space perceptual model

static const __m128 PERCEPTUAL_COEFF[3] = {
	_mm_set1_ps(0.114f),
	_mm_set1_ps(0.587f),
	_mm_set1_ps(0.299f)
};

static const __m128 ZERO = _mm_set1_ps(0.0f);

static const __m128 INV_PERCEPTUAL_COEFF[3] = {
	_mm_set1_ps(31.0f / 255.0f / 0.114f),
	_mm_set1_ps(63.0f / 255.0f / 0.587f),
	_mm_set1_ps(31.0f / 255.0f / 0.299f)
};

static const __m128 CLAMP_31 = _mm_set1_ps(31.0f);
static const __m128 CLAMP_63 = _mm_set1_ps(63.0f);


void __fastcall encodeDxt1Block_fast_simd(
	const unsigned char * rgba,
	unsigned char * block)
{

	// read colors and multiply by a perceptual coefficient

	__m128 q_pixels[4][3];

	for(int i = 0; i < 4; ++i) {
		__m128i r, g, b;

		r.m128i_u32[0] = rgba[16*i + (0 + 0)];
		r.m128i_u32[1] = rgba[16*i + (4 + 0)];
		r.m128i_u32[2] = rgba[16*i + (8 + 0)];
		r.m128i_u32[3] = rgba[16*i + (12 + 0)];
		q_pixels[i][0] = _mm_mul_ps(_mm_cvtepi32_ps(r), PERCEPTUAL_COEFF[0]);

		g.m128i_u32[0] = rgba[16*i + (0 + 1)];
		g.m128i_u32[1] = rgba[16*i + (4 + 1)];
		g.m128i_u32[2] = rgba[16*i + (8 + 1)];
		g.m128i_u32[3] = rgba[16*i + (12 + 1)];
		q_pixels[i][1] = _mm_mul_ps(_mm_cvtepi32_ps(g), PERCEPTUAL_COEFF[1]);

		b.m128i_u32[0] = rgba[16*i + (0 + 2)];
		b.m128i_u32[1] = rgba[16*i + (4 + 2)];
		b.m128i_u32[2] = rgba[16*i + (8 + 2)];
		b.m128i_u32[3] = rgba[16*i + (12 + 2)];
		q_pixels[i][2] = _mm_mul_ps(_mm_cvtepi32_ps(b), PERCEPTUAL_COEFF[2]);
	}

	// calculate bounding box

	__m128 min[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
	__m128 max[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
	for(int i = 1; i < 4; ++i) {
		min[0] = _mm_min_ps(min[0], q_pixels[i][0]);
		min[1] = _mm_min_ps(min[1], q_pixels[i][1]);
		min[2] = _mm_min_ps(min[2], q_pixels[i][2]);

		max[0] = _mm_max_ps(max[0], q_pixels[i][0]);
		max[1] = _mm_max_ps(max[1], q_pixels[i][1]);
		max[2] = _mm_max_ps(max[2], q_pixels[i][2]);
	}

	// HORIZONTAL_MIN: (x, y, z, w) -> (min(x,y,z,x), min(x,y,z,x), min(x,y,z,x))
	min[0] = HORIZONTAL_MIN(min[0]);
	min[1] = HORIZONTAL_MIN(min[1]);
	min[2] = HORIZONTAL_MIN(min[2]);

	// HORIZONTAL_MAX: (x, y, z, w) -> (max(x,y,z,x), max(x,y,z,x), max(x,y,z,x))
	max[0] = HORIZONTAL_MAX(max[0]);
	max[1] = HORIZONTAL_MAX(max[1]);
	max[2] = HORIZONTAL_MAX(max[2]);

	// single color?
	if(1 == _mm_comieq_ss(min[0], max[0]) &&
	   1 == _mm_comieq_ss(min[1], max[1]) &&
	   1 == _mm_comieq_ss(min[2], max[2]))
	{
		// single color
		// write block bits
		unsigned short single_color =
			(unsigned short)(rgba[2] >> 3) |
			((unsigned short)(rgba[1] >> 2) << 5) |
			((unsigned short)(rgba[0] >> 3) << 11);

		*((unsigned short*)block) = single_color;
		*((unsigned short*)(block + 2)) = single_color;
		*((unsigned int*)(block + 4)) = 0x00000000;

	} else {
		// calculate centroid

		__m128 centroid[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
		for(int i = 1; i < 4; ++i) {
			centroid[0] = _mm_add_ps(centroid[0], q_pixels[i][0]);
			centroid[1] = _mm_add_ps(centroid[1], q_pixels[i][1]);
			centroid[2] = _mm_add_ps(centroid[2], q_pixels[i][2]);
		}

		// HORIZONTAL_ADD: (x, y, z, w) -> (x+y+z+w, x+y+z+w, x+y+z+w, x+y+z+w)
		centroid[0] = HORIZONTAL_ADD(centroid[0]);
		centroid[1] = HORIZONTAL_ADD(centroid[1]);
		centroid[2] = HORIZONTAL_ADD(centroid[2]);

		// divide by 16

		__m128 one_sixteenth = _mm_set1_ps(1.0f / 16.0f);
		centroid[0] = _mm_mul_ps(centroid[0], one_sixteenth);
		centroid[1] = _mm_mul_ps(centroid[1], one_sixteenth);
		centroid[2] = _mm_mul_ps(centroid[2], one_sixteenth);

		// move referential to centroid

		for(int i = 0; i < 4; ++i) {
			q_pixels[i][0] = _mm_sub_ps(q_pixels[i][0], centroid[0]);
			q_pixels[i][1] = _mm_sub_ps(q_pixels[i][1], centroid[1]);
			q_pixels[i][2] = _mm_sub_ps(q_pixels[i][2], centroid[2]);
		}

		// principal component analysis approximation: find best diagonal of the bounding box

		// calculate directions for all four diagonals

		__m128 any_diagonal[3] = {
			_mm_sub_ps(max[0], min[0]),
			_mm_sub_ps(max[1], min[1]),
			_mm_sub_ps(max[2], min[2])
		};
		__m128 inv_diag_norm =
			_mm_rsqrt_ps(
				_mm_add_ps(
					_mm_add_ps(
						_mm_mul_ps(any_diagonal[0], any_diagonal[0]),
						_mm_mul_ps(any_diagonal[1], any_diagonal[1])
					),
					_mm_mul_ps(any_diagonal[2], any_diagonal[2])
				)
			);
		any_diagonal[0] = _mm_mul_ps(any_diagonal[0], inv_diag_norm);
		any_diagonal[1] = _mm_mul_ps(any_diagonal[1], inv_diag_norm);
		any_diagonal[2] = _mm_mul_ps(any_diagonal[2], inv_diag_norm);

		__m128 diags[4][3] = {
			{ any_diagonal[0], any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], any_diagonal[1], INVERSE_SIGN(any_diagonal[2]) },
			{ any_diagonal[0], INVERSE_SIGN(any_diagonal[1]), any_diagonal[2] },
			{ any_diagonal[0], INVERSE_SIGN(any_diagonal[1]), INVERSE_SIGN(any_diagonal[2]) }
		};

		// for each diagonals calculate the projection of each colour

		__m128 q_dot_products[4][4];
		__m128 min_dot_product[4] = { ZERO, ZERO, ZERO, ZERO };
		__m128 max_dot_product[4] = { ZERO, ZERO, ZERO, ZERO };

		__m128 largest_dot_product_difference = ZERO;
		int best_diag_choice = 0;

		for(int diag_choice = 0; diag_choice < 4; ++diag_choice) {

			// project colours onto the diagonal

			for(int i = 0; i < 4; ++i) {
				q_dot_products[diag_choice][i] =
					_mm_add_ps(
						_mm_mul_ps(q_pixels[i][0], diags[diag_choice][0]),
						_mm_add_ps(
							_mm_mul_ps(q_pixels[i][1], diags[diag_choice][1]),
							_mm_mul_ps(q_pixels[i][2], diags[diag_choice][2])
						)
					);
				min_dot_product[diag_choice] = _mm_min_ps(min_dot_product[diag_choice], q_dot_products[diag_choice][i]);
				max_dot_product[diag_choice] = _mm_max_ps(max_dot_product[diag_choice], q_dot_products[diag_choice][i]);
			}
			min_dot_product[diag_choice] = HORIZONTAL_MIN(min_dot_product[diag_choice]);
			max_dot_product[diag_choice] = HORIZONTAL_MAX(max_dot_product[diag_choice]);

			// is this diagonal more discriminating than the others? (i.e. biggest difference between least and greatest dot products)

			__m128 dot_product_difference = _mm_sub_ps(max_dot_product[diag_choice], min_dot_product[diag_choice]);
			if(1 == _mm_comilt_ss(largest_dot_product_difference, dot_product_difference)) {
				best_diag_choice = diag_choice;
				largest_dot_product_difference = dot_product_difference;
			}
		}

		// choose extreme positions as start and end colors

		__m128 start_color[3];
		start_color[0] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[0], _mm_add_ps(centroid[0], _mm_mul_ps(max_dot_product[best_diag_choice], diags[best_diag_choice][0])));
		start_color[1] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[1], _mm_add_ps(centroid[1], _mm_mul_ps(max_dot_product[best_diag_choice], diags[best_diag_choice][1])));
		start_color[2] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[2], _mm_add_ps(centroid[2], _mm_mul_ps(max_dot_product[best_diag_choice], diags[best_diag_choice][2])));

		__m128 end_color[3];
		end_color[0] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[0], _mm_add_ps(centroid[0], _mm_mul_ps(min_dot_product[best_diag_choice], diags[best_diag_choice][0])));
		end_color[1] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[1], _mm_add_ps(centroid[1], _mm_mul_ps(min_dot_product[best_diag_choice], diags[best_diag_choice][1])));
		end_color[2] = _mm_mul_ps(INV_PERCEPTUAL_COEFF[2], _mm_add_ps(centroid[2], _mm_mul_ps(min_dot_product[best_diag_choice], diags[best_diag_choice][2])));

		// round and clamp values

		__m128i start_color_rounded[3], end_color_rounded[3];

		start_color_rounded[0] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_31, start_color[0])));
		start_color_rounded[1] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_63, start_color[1])));
		start_color_rounded[2] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_31, start_color[2])));

		end_color_rounded[0] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_31, end_color[0])));
		end_color_rounded[1] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_63, end_color[1])));
		end_color_rounded[2] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(CLAMP_31, end_color[2])));

		// convert to R5G6B5 format

		unsigned short start = (unsigned short)(
			start_color_rounded[2].m128i_u32[0] |
			(start_color_rounded[1].m128i_u32[0] << 5) |
			(start_color_rounded[0].m128i_u32[0] << 11));
		unsigned short end = (unsigned short)(
			end_color_rounded[2].m128i_u32[0] |
			(end_color_rounded[1].m128i_u32[0] << 5) |
			(end_color_rounded[0].m128i_u32[0] << 11));

		// map each color to the indice of the closest position

		__m128i indices[4];
		__m128 coeff = _mm_mul_ps(_mm_set1_ps(3.0f), _mm_rcp_ps(largest_dot_product_difference));
		for(int i = 0; i < 4; ++i) {
			indices[i] = _mm_cvtps_epi32(_mm_mul_ps(_mm_sub_ps(q_dot_products[best_diag_choice][i], min_dot_product[best_diag_choice]), coeff));
		}

		// write block bits

		// write the endpoints		
		*((unsigned short*)block) = start;
		*((unsigned short*)(block + 2)) = end;

		// write the indices
		for( int i = 0; i < 4; ++i )
		{
			block[4 + i] =
				DXT1_CODES_4[0*4 + indices[i].m128i_u32[0]] |
				DXT1_CODES_4[1*4 + indices[i].m128i_u32[1]] |
				DXT1_CODES_4[2*4 + indices[i].m128i_u32[2]] |
				DXT1_CODES_4[3*4 + indices[i].m128i_u32[3]];
		}

		if(start <= end) {	// good branch prediction (start is often > end because of the diagonals of the bounding box are oriented from low red to high red)
			if(start == end) {
				// degenerate case: the colors end up being the same when rounded down
				*((unsigned int*)(block + 4)) = 0x00000000;
			} else {
				// reverse start and end to stay in four colors mode
				*((unsigned short*)block) = end;
				*((unsigned short*)(block + 2)) = start;
				*((unsigned int*)(block + 4)) ^= 0x55555555;
			}
		}
	}
}

void __fastcall encodeDxt1Block_quality_simd(
	const unsigned char * rgba,
	unsigned char * block)
{

	// read colors (no perceptual coefficient)

	__m128 q_pixels[4][3];

	for(int i = 0; i < 4; ++i) {
		__m128i r, g, b;

		r.m128i_u32[0] = rgba[16*i + (0 + 0)];
		r.m128i_u32[1] = rgba[16*i + (4 + 0)];
		r.m128i_u32[2] = rgba[16*i + (8 + 0)];
		r.m128i_u32[3] = rgba[16*i + (12 + 0)];
		q_pixels[i][0] = _mm_cvtepi32_ps(r);

		g.m128i_u32[0] = rgba[16*i + (0 + 1)];
		g.m128i_u32[1] = rgba[16*i + (4 + 1)];
		g.m128i_u32[2] = rgba[16*i + (8 + 1)];
		g.m128i_u32[3] = rgba[16*i + (12 + 1)];
		q_pixels[i][1] = _mm_cvtepi32_ps(g);

		b.m128i_u32[0] = rgba[16*i + (0 + 2)];
		b.m128i_u32[1] = rgba[16*i + (4 + 2)];
		b.m128i_u32[2] = rgba[16*i + (8 + 2)];
		b.m128i_u32[3] = rgba[16*i + (12 + 2)];
		q_pixels[i][2] = _mm_cvtepi32_ps(b);
	}

	// calculate bounding box

	__m128 min[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
	__m128 max[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
	for(int i = 1; i < 4; ++i) {
		min[0] = _mm_min_ps(min[0], q_pixels[i][0]);
		min[1] = _mm_min_ps(min[1], q_pixels[i][1]);
		min[2] = _mm_min_ps(min[2], q_pixels[i][2]);

		max[0] = _mm_max_ps(max[0], q_pixels[i][0]);
		max[1] = _mm_max_ps(max[1], q_pixels[i][1]);
		max[2] = _mm_max_ps(max[2], q_pixels[i][2]);
	}

	// HORIZONTAL_MIN: (x, y, z, w) -> (min(x,y,z,x), min(x,y,z,x), min(x,y,z,x))
	min[0] = HORIZONTAL_MIN(min[0]);
	min[1] = HORIZONTAL_MIN(min[1]);
	min[2] = HORIZONTAL_MIN(min[2]);

	// HORIZONTAL_MAX: (x, y, z, w) -> (max(x,y,z,x), max(x,y,z,x), max(x,y,z,x))
	max[0] = HORIZONTAL_MAX(max[0]);
	max[1] = HORIZONTAL_MAX(max[1]);
	max[2] = HORIZONTAL_MAX(max[2]);

	// single color?
	if(1 == _mm_comieq_ss(min[0], max[0]) &&
	   1 == _mm_comieq_ss(min[1], max[1]) &&
	   1 == _mm_comieq_ss(min[2], max[2]))
	{
		// single color
		// write block bits
		unsigned short single_color =
			(unsigned short)(rgba[2] >> 3) |
			((unsigned short)(rgba[1] >> 2) << 5) |
			((unsigned short)(rgba[0] >> 3) << 11);

		*((unsigned short*)block) = single_color;
		*((unsigned short*)(block + 2)) = single_color;
		*((unsigned int*)(block + 4)) = 0x00000000;

	} else {
		// calculate centroid

		__m128 centroid[3] = { q_pixels[0][0], q_pixels[0][1], q_pixels[0][2] };
		for(int i = 1; i < 4; ++i) {
			centroid[0] = _mm_add_ps(centroid[0], q_pixels[i][0]);
			centroid[1] = _mm_add_ps(centroid[1], q_pixels[i][1]);
			centroid[2] = _mm_add_ps(centroid[2], q_pixels[i][2]);
		}

		// HORIZONTAL_ADD: (x, y, z, w) -> (x+y+z+w, x+y+z+w, x+y+z+w, x+y+z+w)
		centroid[0] = HORIZONTAL_ADD(centroid[0]);
		centroid[1] = HORIZONTAL_ADD(centroid[1]);
		centroid[2] = HORIZONTAL_ADD(centroid[2]);

		// divide by 16

		__m128 one_sixteenth = _mm_set1_ps(1.0f / 16.0f);
		centroid[0] = _mm_mul_ps(centroid[0], one_sixteenth);
		centroid[1] = _mm_mul_ps(centroid[1], one_sixteenth);
		centroid[2] = _mm_mul_ps(centroid[2], one_sixteenth);

		// move referential to centroid

		for(int i = 0; i < 4; ++i) {
			q_pixels[i][0] = _mm_sub_ps(q_pixels[i][0], centroid[0]);
			q_pixels[i][1] = _mm_sub_ps(q_pixels[i][1], centroid[1]);
			q_pixels[i][2] = _mm_sub_ps(q_pixels[i][2], centroid[2]);
		}

		// principal component analysis approximation: find best diagonal of the bounding box

		// calculate directions for all four diagonals

		__m128 any_diagonal[3] = {
			_mm_sub_ps(max[0], min[0]),
			_mm_sub_ps(max[1], min[1]),
			_mm_sub_ps(max[2], min[2])
		};
		__m128 inv_diag_norm =
			_mm_rsqrt_ps(
				_mm_add_ps(
					_mm_add_ps(
						_mm_mul_ps(any_diagonal[0], any_diagonal[0]),
						_mm_mul_ps(any_diagonal[1], any_diagonal[1])
					),
					_mm_mul_ps(any_diagonal[2], any_diagonal[2])
				)
			);
		any_diagonal[0] = _mm_mul_ps(any_diagonal[0], inv_diag_norm);
		any_diagonal[1] = _mm_mul_ps(any_diagonal[1], inv_diag_norm);
		any_diagonal[2] = _mm_mul_ps(any_diagonal[2], inv_diag_norm);

		__m128 diags[4][3] = {
			{ any_diagonal[0], any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], any_diagonal[1], INVERSE_SIGN(any_diagonal[2]) },
			{ any_diagonal[0], INVERSE_SIGN(any_diagonal[1]), any_diagonal[2] },
			{ any_diagonal[0], INVERSE_SIGN(any_diagonal[1]), INVERSE_SIGN(any_diagonal[2]) }
		};

		// for each diagonals calculate the projection of each colour

		__m128 q_dot_products[4][4];
		__m128 min_dot_product[4] = { ZERO, ZERO, ZERO, ZERO };
		__m128 max_dot_product[4] = { ZERO, ZERO, ZERO, ZERO };

		__m128 largest_dot_product_difference = ZERO;
		int best_diag_choice = 0;

		for(int diag_choice = 0; diag_choice < 4; ++diag_choice) {

			// project colours onto the diagonal

			for(int i = 0; i < 4; ++i) {
				q_dot_products[diag_choice][i] =
					_mm_add_ps(
						_mm_mul_ps(q_pixels[i][0], diags[diag_choice][0]),
						_mm_add_ps(
							_mm_mul_ps(q_pixels[i][1], diags[diag_choice][1]),
							_mm_mul_ps(q_pixels[i][2], diags[diag_choice][2])
						)
					);
				min_dot_product[diag_choice] = _mm_min_ps(min_dot_product[diag_choice], q_dot_products[diag_choice][i]);
				max_dot_product[diag_choice] = _mm_max_ps(max_dot_product[diag_choice], q_dot_products[diag_choice][i]);
			}
			min_dot_product[diag_choice] = HORIZONTAL_MIN(min_dot_product[diag_choice]);
			max_dot_product[diag_choice] = HORIZONTAL_MAX(max_dot_product[diag_choice]);

			// is this diagonal more discrimining than the others? (i.e. bigger difference between least and greatest dot products)

			__m128 dot_product_difference = _mm_sub_ps(max_dot_product[diag_choice], min_dot_product[diag_choice]);
			if(1 == _mm_comilt_ss(largest_dot_product_difference, dot_product_difference)) {
				best_diag_choice = diag_choice;
				largest_dot_product_difference = dot_product_difference;
			}
		}

		// map each color to the indice of the closest position

		__m128i indices[4];
		__m128 coeff = _mm_mul_ps(_mm_set1_ps(3.0f), _mm_rcp_ps(largest_dot_product_difference));
		for(int i = 0; i < 4; ++i) {
			indices[i] = _mm_cvtps_epi32(_mm_mul_ps(_mm_sub_ps(q_dot_products[best_diag_choice][i], min_dot_product[best_diag_choice]), coeff));
		}

		__m128 clamp_min[3] = {
			INVERSE_SIGN(centroid[0]),
			INVERSE_SIGN(centroid[1]),
			INVERSE_SIGN(centroid[2])
		};
		__m128 clamp_max[3] = {
			_mm_sub_ps(_mm_set1_ps(255.0f), centroid[0]),
			_mm_sub_ps(_mm_set1_ps(255.0f), centroid[1]),
			_mm_sub_ps(_mm_set1_ps(255.0f), centroid[2])
		};

		__m128 start_color[3], end_color[3];

		// refine using Least Squares
		for(int iteration = 0; iteration < 3; ++iteration) {
			// We have 16 equations like this:
			// alpha[i] * start + beta[i] * end = pixel[i]
			// with { alpha[i], beta[i] } equal to {0, 1}, {1/3, 2/3}, {2/3, 1/3} or {1, 0}.
			//
			// Least Squares solution:
			// ( start )   ( S{alpha[i]^2}       S{alpha[i]*beta[i]} )^-1   ( S{alpha[i]*pixel[i]} )
			// (  end  ) = ( S{alpha[i]*beta[i]} S{beta[i]^2}        )    x ( S{beta[i]*pixel[i]}  )

			__m128 alpha = _mm_mul_ps(_mm_cvtepi32_ps(indices[0]), _mm_set1_ps(1.0f / 3.0f));
			__m128 S_alpha = alpha;
			__m128 S_alpha_2 = _mm_mul_ps(alpha, alpha);
			__m128 S_alpha_pixel[3] = {
				_mm_mul_ps(alpha, q_pixels[0][0]),
				_mm_mul_ps(alpha, q_pixels[0][1]),
				_mm_mul_ps(alpha, q_pixels[0][2])
			};
			for(int i = 1; i < 4; ++i) {
				alpha = _mm_mul_ps(_mm_cvtepi32_ps(indices[i]), _mm_set1_ps(1.0f / 3.0f));
				
				S_alpha = _mm_add_ps(S_alpha, alpha);
				S_alpha_2 = _mm_add_ps(S_alpha_2, _mm_mul_ps(alpha, alpha));

				S_alpha_pixel[0] = _mm_add_ps(S_alpha_pixel[0], _mm_mul_ps(alpha, q_pixels[i][0]));
				S_alpha_pixel[1] = _mm_add_ps(S_alpha_pixel[1], _mm_mul_ps(alpha, q_pixels[i][1]));
				S_alpha_pixel[2] = _mm_add_ps(S_alpha_pixel[2], _mm_mul_ps(alpha, q_pixels[i][2]));
			}
			S_alpha = HORIZONTAL_ADD(S_alpha);
			S_alpha_2 = HORIZONTAL_ADD(S_alpha_2);
			S_alpha_pixel[0] = HORIZONTAL_ADD(S_alpha_pixel[0]);
			S_alpha_pixel[1] = HORIZONTAL_ADD(S_alpha_pixel[1]);
			S_alpha_pixel[2] = HORIZONTAL_ADD(S_alpha_pixel[2]);

			// calculations are simplified by the fact that beta[i] == (1 - alpha[i])
			__m128 S_alpha_beta = _mm_sub_ps(S_alpha, S_alpha_2);
			__m128 S_beta_2 = _mm_sub_ps(_mm_sub_ps(_mm_set1_ps(16.0f), S_alpha), S_alpha_beta);

			__m128 factor = _mm_rcp_ps(_mm_sub_ps(_mm_mul_ps(S_alpha_2, S_beta_2), _mm_mul_ps(S_alpha_beta, S_alpha_beta)));
			__m128 start_factor = _mm_mul_ps(_mm_add_ps(S_beta_2, S_alpha_beta), factor);
			__m128 end_factor = _mm_sub_ps(ZERO, _mm_mul_ps(_mm_add_ps(S_alpha_2, S_alpha_beta), factor));

			// compute start and end color, and clamp to [0, 255]

			start_color[0] = _mm_max_ps(clamp_min[0], _mm_min_ps(clamp_max[0], _mm_mul_ps(start_factor, S_alpha_pixel[0])));
			start_color[1] = _mm_max_ps(clamp_min[1], _mm_min_ps(clamp_max[1], _mm_mul_ps(start_factor, S_alpha_pixel[1])));
			start_color[2] = _mm_max_ps(clamp_min[2], _mm_min_ps(clamp_max[2], _mm_mul_ps(start_factor, S_alpha_pixel[2])));

			end_color[0] = _mm_max_ps(clamp_min[0], _mm_min_ps(clamp_max[0], _mm_mul_ps(end_factor, S_alpha_pixel[0])));
			end_color[1] = _mm_max_ps(clamp_min[1], _mm_min_ps(clamp_max[1], _mm_mul_ps(end_factor, S_alpha_pixel[1])));
			end_color[2] = _mm_max_ps(clamp_min[2], _mm_min_ps(clamp_max[2], _mm_mul_ps(end_factor, S_alpha_pixel[2])));

			// recalculate indexes
			// map each color to the indice of the closest position

			__m128 segment[3] = {
				_mm_sub_ps(start_color[0], end_color[0]),
				_mm_sub_ps(start_color[1], end_color[1]),
				_mm_sub_ps(start_color[2], end_color[2])
			};
			__m128 segment_norm_2 =
				_mm_add_ps(
					_mm_add_ps(
						_mm_mul_ps(segment[0], segment[0]),
						_mm_mul_ps(segment[1], segment[1])
					),
					_mm_mul_ps(segment[2], segment[2])
				);
			// coefficient used to pre-multiply dot product result == 3/(norm^2)
			__m128 coeff = _mm_mul_ps(_mm_set1_ps(3.0f), _mm_rcp_ps(segment_norm_2));
			segment[0] = _mm_mul_ps(segment[0], coeff);
			segment[1] = _mm_mul_ps(segment[1], coeff);
			segment[2] = _mm_mul_ps(segment[2], coeff);

			for(int i = 0; i < 4; ++i) {
				// calculate dot product (pre-multiplied by 3/norm)
				__m128 dot_product =
					_mm_add_ps(
						_mm_mul_ps(_mm_sub_ps(q_pixels[i][0], end_color[0]), segment[0]),
						_mm_add_ps(
							_mm_mul_ps(_mm_sub_ps(q_pixels[i][1], end_color[1]), segment[1]),
							_mm_mul_ps(_mm_sub_ps(q_pixels[i][2], end_color[2]), segment[2])
						)
					);
				// round and clamp values
				indices[i] = _mm_cvtps_epi32(_mm_max_ps(ZERO, _mm_min_ps(_mm_set1_ps(3.0f), dot_product)));
			}
		}

		// reset referential away from centroid and round (no need to clamp, it's already done)

		__m128i start_color_rounded[3], end_color_rounded[3];

		start_color_rounded[0] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(31.0f / 255.0f), _mm_add_ps(centroid[0], start_color[0])));
		start_color_rounded[1] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(63.0f / 255.0f), _mm_add_ps(centroid[1], start_color[1])));
		start_color_rounded[2] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(31.0f / 255.0f), _mm_add_ps(centroid[2], start_color[2])));

		end_color_rounded[0] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(31.0f / 255.0f), _mm_add_ps(centroid[0], end_color[0])));
		end_color_rounded[1] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(63.0f / 255.0f), _mm_add_ps(centroid[1], end_color[1])));
		end_color_rounded[2] = _mm_cvtps_epi32(_mm_mul_ps(_mm_set1_ps(31.0f / 255.0f), _mm_add_ps(centroid[2], end_color[2])));

		// convert to R5G6B5 format

		unsigned short start = (unsigned short)(
			start_color_rounded[2].m128i_u32[0] |
			(start_color_rounded[1].m128i_u32[0] << 5) |
			(start_color_rounded[0].m128i_u32[0] << 11));
		unsigned short end = (unsigned short)(
			end_color_rounded[2].m128i_u32[0] |
			(end_color_rounded[1].m128i_u32[0] << 5) |
			(end_color_rounded[0].m128i_u32[0] << 11));

		// write block bits

		// write the endpoints		
		*((unsigned short*)block) = start;
		*((unsigned short*)(block + 2)) = end;

		// write the indices
		for( int i = 0; i < 4; ++i )
		{
			block[4 + i] =
				DXT1_CODES_4[0*4 + indices[i].m128i_u32[0]] |
				DXT1_CODES_4[1*4 + indices[i].m128i_u32[1]] |
				DXT1_CODES_4[2*4 + indices[i].m128i_u32[2]] |
				DXT1_CODES_4[3*4 + indices[i].m128i_u32[3]];
		}

		if(start <= end) {	// note that start is often >= end because of the diagonals of the bounding box are oriented from low red to high red
			if(start == end) {
				// degenerate case: the colors end up being the same when rounded down
				*((unsigned int*)(block + 4)) = 0x00000000;
			} else {
				// reverse start and end to stay in four colors mode
				*((unsigned short*)block) = end;
				*((unsigned short*)(block + 2)) = start;
				*((unsigned int*)(block + 4)) ^= 0x55555555;
			}
		}
	}
}

static const unsigned int DXT5_CODES_8[8] = {
	0x00000001, 0x00000007, 0x00000006, 0x00000005,
	0x00000004, 0x00000003, 0x00000002, 0x00000000
};

static const unsigned int DXT5_CODES_6[8] = {
	0x00000000, 0x00000002, 0x00000003,
	0x00000004, 0x00000005, 0x00000001,
	0x00000006, 0x00000007
};

void __fastcall encodeDxt5Block_simd(
	const unsigned char * rgba,
	unsigned char * block)
{
	// read alpha, calculate bounds

	__m128 alpha[4];

	const __m128 const_0 = _mm_set1_ps(0.0f);
	const __m128 const_255 = _mm_set1_ps(255.0f);

	__m128 min = const_255;
	__m128 max = const_0;
	__m128 min_non_0 = const_255;
	__m128 max_non_255 = const_0;

	for(int i = 0; i < 4; ++i) {
		__m128i a;
		a.m128i_u32[0] = rgba[16*i + (0 + 3)];
		a.m128i_u32[1] = rgba[16*i + (4 + 3)];
		a.m128i_u32[2] = rgba[16*i + (8 + 3)];
		a.m128i_u32[3] = rgba[16*i + (12 + 3)];

		alpha[i] = _mm_cvtepi32_ps(a);

		min = _mm_min_ps(min, alpha[i]);
		max = _mm_max_ps(max, alpha[i]);

		__m128 a_is_0 = _mm_cmpeq_ps(alpha[i], const_0);
		min_non_0 = _mm_or_ps(
			_mm_and_ps(a_is_0, min_non_0),
			_mm_andnot_ps(a_is_0, _mm_min_ps(min_non_0, alpha[i]))
		);

		max_non_255 = _mm_max_ps(max_non_255,
			_mm_and_ps(alpha[i], _mm_cmpneq_ps(alpha[i], const_255)));
	}

	// HORIZONTAL_MIN: (x, y, z, w) -> (min(x,y,z,x), min(x,y,z,x), min(x,y,z,x))
	min = HORIZONTAL_MIN(min);
	min_non_0 = HORIZONTAL_MIN(min_non_0);

	// HORIZONTAL_MAX: (x, y, z, w) -> (max(x,y,z,x), max(x,y,z,x), max(x,y,z,x))
	max = HORIZONTAL_MAX(max);
	max_non_255 = HORIZONTAL_MAX(max_non_255);

	// single alpha?
	if(1 == _mm_comieq_ss(min, max)) {

		// single alpha
		// write block bits
		unsigned char single_alpha = rgba[3];

		block[1] = block[0] = single_alpha;
		block[3] = block[2] = (unsigned char) 0x00;
		*((unsigned int*)(block + 4)) = 0x00000000;

	} else {
		
		unsigned int codes[16];

		// min == 0 && max == 255 ?
		if(1 == _mm_comieq_ss(min, const_0) &&
			1 == _mm_comieq_ss(max, const_255))
		{
			__m128 min_non_0_is_255 = _mm_cmpeq_ps(min_non_0, const_255);
			min_non_0 = _mm_andnot_ps(min_non_0_is_255, min_non_0);
			max_non_255 = _mm_or_ps(
				_mm_and_ps(min_non_0_is_255, const_255),
				_mm_andnot_ps(min_non_0_is_255, max_non_255));

			block[0] = _mm_cvtss_si32(min_non_0);
			block[1] = _mm_cvtss_si32(max_non_255);

			// coeff = min_non_0 == max_non_255 ? 0.0f : 5.0f / (max_non_255 - min_non_0)
			__m128 coeff = _mm_and_ps(
				_mm_cmpneq_ps(max_non_255, min_non_0),
				_mm_div_ps(_mm_set1_ps(5.0f), _mm_sub_ps(max_non_255, min_non_0)));
			for(int i = 0; i < 4; ++i) {
				__m128 a = alpha[i];
				__m128 a_is_0 = _mm_cmpeq_ps(a, const_0);
				__m128 a_is_255 = _mm_cmpeq_ps(a, const_255);
				__m128i indices = _mm_cvtps_epi32(
					_mm_or_ps(
						_mm_and_ps(a_is_0, _mm_set1_ps(6.0f)),
						_mm_andnot_ps(a_is_0,
							_mm_or_ps(
								_mm_and_ps(a_is_255, _mm_set1_ps(7.0f)),
								_mm_andnot_ps(a_is_255,
									_mm_mul_ps(_mm_sub_ps(a, min_non_0), coeff)
								)
							)
						)
					)
				);

				codes[i*4 + 0] = DXT5_CODES_6[indices.m128i_u32[0]];
				codes[i*4 + 1] = DXT5_CODES_6[indices.m128i_u32[1]];
				codes[i*4 + 2] = DXT5_CODES_6[indices.m128i_u32[2]];
				codes[i*4 + 3] = DXT5_CODES_6[indices.m128i_u32[3]];
			}

		} else {

			block[0] = _mm_cvtss_si32(max);
			block[1] = _mm_cvtss_si32(min);

			__m128 coeff = _mm_div_ps(_mm_set1_ps(7.0f), _mm_sub_ps(max, min));
			for(int i = 0; i < 4; ++i) {
				__m128 a = alpha[i];
				__m128i indices = _mm_cvtps_epi32(_mm_mul_ps(_mm_sub_ps(a, min), coeff));

				codes[i*4 + 0] = DXT5_CODES_8[indices.m128i_u32[0]];
				codes[i*4 + 1] = DXT5_CODES_8[indices.m128i_u32[1]];
				codes[i*4 + 2] = DXT5_CODES_8[indices.m128i_u32[2]];
				codes[i*4 + 3] = DXT5_CODES_8[indices.m128i_u32[3]];
			}
		}

		for(int i = 0; i < 2; ++i) {
			block[i*3 + 2] = (unsigned char) (
				codes[i*8 + 0] |
				(codes[i*8 + 1] << 3) |
				((0x00000003 & codes[i*8 + 2]) << 6));
			block[i*3 + 3] = (unsigned char) (
				((0x00000004 & codes[i*8 + 2]) >> 2) |
				(codes[i*8 + 3] << 1) |
				(codes[i*8 + 4] << 4) |
				((0x00000001 & codes[i*8 + 5]) << 7));
			block[i*3 + 4] = (unsigned char) (
				((0x00000006 & codes[i*8 + 5]) >> 1) |
				(codes[i*8 + 6] << 2) |
				(codes[i*8 + 7] << 5));
		}
	}
}
