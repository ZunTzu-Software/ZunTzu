/* -----------------------------------------------------------------------------
	  Copyright (c) 2020 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"

#define ROUND(x) ((unsigned int)((x)+0.5f))
#define ROUND_AND_CLAMP31(x) ((x)>=31.0f?(unsigned int)31:((x)<0.0f?(unsigned int)0:(unsigned int)((x)+0.5f)))
#define ROUND_AND_CLAMP63(x) ((x)>=63.0f?(unsigned int)63:((x)<0.0f?(unsigned int)0:(unsigned int)((x)+0.5f)))
#define ROUND_AND_CLAMP3(x) ((x)>=3.0f?(unsigned char)3:((x)<0.0f?(unsigned char)0:(unsigned char)((x)+0.5f)))
#define MIN(x,y) ((y)<(x)?(y):(x))
#define MAX(x,y) ((y)>(x)?(y):(x))
#define ROUND_TO_BYTE(x) ((unsigned char)((x)+0.5f))

// DXT1 code book

static const unsigned char DXT1_CODES_4[4 * 4] = {
	0x01 << 0, 0x03 << 0, 0x02 << 0, 0x00 << 0,
	0x01 << 2, 0x03 << 2, 0x02 << 2, 0x00 << 2,
	0x01 << 4, 0x03 << 4, 0x02 << 4, 0x00 << 4,
	0x01 << 6, 0x03 << 6, 0x02 << 6, 0x00 << 6
};

// color space perceptual model

static const float PERCEPTUAL_COEFF[3] = {
	0.114f,
	0.587f,
	0.299f
};

void __fastcall encodeDxt1Block_fast_float(
	const unsigned char * rgba,
	unsigned char * block)
{

	// read colors and multiply by a perceptual coefficient

	float pixels[16][3];

	for(int i = 0; i < 16; ++i) {
		pixels[i][0] = rgba[4*i + 0] * PERCEPTUAL_COEFF[0];
		pixels[i][1] = rgba[4*i + 1] * PERCEPTUAL_COEFF[1];
		pixels[i][2] = rgba[4*i + 2] * PERCEPTUAL_COEFF[2];
	}

	// calculate bounding box

	float min[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
	float max[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
	for(int i = 1; i < 16; ++i) {
		min[0] = MIN(min[0], pixels[i][0]);
		min[1] = MIN(min[1], pixels[i][1]);
		min[2] = MIN(min[2], pixels[i][2]);

		max[0] = MAX(max[0], pixels[i][0]);
		max[1] = MAX(max[1], pixels[i][1]);
		max[2] = MAX(max[2], pixels[i][2]);
	}

	// single color?
	if(min[0] == max[0] && min[1] == max[1] && min[2] == max[2]) {

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

		float centroid[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
		for(int i = 1; i < 16; ++i) {
			centroid[0] += pixels[i][0];
			centroid[1] += pixels[i][1];
			centroid[2] += pixels[i][2];
		}
		centroid[0] /= 16;
		centroid[1] /= 16;
		centroid[2] /= 16;

		// move referential to centroid

		for(int i = 0; i < 16; ++i) {
			pixels[i][0] -= centroid[0];
			pixels[i][1] -= centroid[1];
			pixels[i][2] -= centroid[2];
		}

		// principal component analysis approximation: find best diagonal of the bounding box

		// calculate directions for all four diagonals

		float any_diagonal[3] = {
			max[0] - min[0],
			max[1] - min[1],
			max[2] - min[2]
		};
		float inv_diag_norm = 1 / sqrt(
			any_diagonal[0] * any_diagonal[0] +
			any_diagonal[1] * any_diagonal[1] +
			any_diagonal[2] * any_diagonal[2]);
		any_diagonal[0] *= inv_diag_norm;
		any_diagonal[1] *= inv_diag_norm;
		any_diagonal[2] *= inv_diag_norm;

		float diags[4][3] = {
			{ any_diagonal[0], any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], any_diagonal[1], -any_diagonal[2] },
			{ any_diagonal[0], -any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], -any_diagonal[1], -any_diagonal[2] }
		};

		// for each diagonals calculate the projection of each colour

		float dot_products[4][16];
		float min_dot_product[4] = { 0, 0, 0, 0 };
		float max_dot_product[4] = { 0, 0, 0, 0 };

		float largest_dot_product_difference = 0;
		int best_diag_choice = 0;

		for(int diag_choice = 0; diag_choice < 4; ++diag_choice) {

			// project colors onto the diagonal

			for(int i = 0; i < 16; ++i) {
				dot_products[diag_choice][i] =
					pixels[i][0] * diags[diag_choice][0] +
					pixels[i][1] * diags[diag_choice][1] +
					pixels[i][2] * diags[diag_choice][2];
				min_dot_product[diag_choice] = MIN(min_dot_product[diag_choice], dot_products[diag_choice][i]);
				max_dot_product[diag_choice] = MAX(max_dot_product[diag_choice], dot_products[diag_choice][i]);
			}

			// is this diagonal more discriminating than the others?
			// (i.e. biggest difference between least and greatest dot products)

			float dot_product_difference = max_dot_product[diag_choice] - min_dot_product[diag_choice];
			if(dot_product_difference > largest_dot_product_difference) {
				best_diag_choice = diag_choice;
				largest_dot_product_difference = dot_product_difference;
			}
		}

		// choose extreme positions as start and end colors

		unsigned int start_color_rounded[3];
		start_color_rounded[0] = ROUND_AND_CLAMP31((centroid[0] + max_dot_product[best_diag_choice] * diags[best_diag_choice][0]) * (31.0f / 255.0f / PERCEPTUAL_COEFF[0]));
		start_color_rounded[1] = ROUND_AND_CLAMP63((centroid[1] + max_dot_product[best_diag_choice] * diags[best_diag_choice][1]) * (63.0f / 255.0f / PERCEPTUAL_COEFF[1]));
		start_color_rounded[2] = ROUND_AND_CLAMP31((centroid[2] + max_dot_product[best_diag_choice] * diags[best_diag_choice][2]) * (31.0f / 255.0f / PERCEPTUAL_COEFF[2]));

		unsigned int end_color_rounded[3];
		end_color_rounded[0] = ROUND_AND_CLAMP31((centroid[0] + min_dot_product[best_diag_choice] * diags[best_diag_choice][0]) * (31.0f / 255.0f / PERCEPTUAL_COEFF[0]));
		end_color_rounded[1] = ROUND_AND_CLAMP63((centroid[1] + min_dot_product[best_diag_choice] * diags[best_diag_choice][1]) * (63.0f / 255.0f / PERCEPTUAL_COEFF[1]));
		end_color_rounded[2] = ROUND_AND_CLAMP31((centroid[2] + min_dot_product[best_diag_choice] * diags[best_diag_choice][2]) * (31.0f / 255.0f / PERCEPTUAL_COEFF[2]));

		// convert to R5G6B5 format

		unsigned short start = (unsigned short)(start_color_rounded[2] | (start_color_rounded[1] << 5) | (start_color_rounded[0] << 11));
		unsigned short end = (unsigned short)(end_color_rounded[2] | (end_color_rounded[1] << 5) | (end_color_rounded[0] << 11));

		// map each color to the indice of the closest position

		unsigned char indices[16];
		float coeff = 3 / largest_dot_product_difference;
		for(int i = 0; i < 16; ++i) {
			indices[i] = ROUND_TO_BYTE((dot_products[best_diag_choice][i] - min_dot_product[best_diag_choice]) * coeff);
		}

		// write block bits

		// write the endpoints		
		*((unsigned short*)block) = start;
		*((unsigned short*)(block + 2)) = end;

		// write the indices
		for( int i = 0; i < 4; ++i )
		{
			unsigned char* ind = indices + 4*i;
			block[4 + i] =
				DXT1_CODES_4[0*4 + ind[0]] |
				DXT1_CODES_4[1*4 + ind[1]] |
				DXT1_CODES_4[2*4 + ind[2]] |
				DXT1_CODES_4[3*4 + ind[3]];
		}

		if(start <= end) {	// good branch prediction (start is often > end because the diagonals of the bounding box are oriented from low red to high red)
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

void __fastcall encodeDxt1Block_quality_float(
	const unsigned char * rgba,
	unsigned char * block)
{

	// read colors (no perceptual coefficient)

	float pixels[16][3];

	for(int i = 0; i < 16; ++i) {
		pixels[i][0] = rgba[4*i + 0];
		pixels[i][1] = rgba[4*i + 1];
		pixels[i][2] = rgba[4*i + 2];
	}

	// calculate bounding box

	float min[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
	float max[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
	for(int i = 1; i < 16; ++i) {
		min[0] = MIN(min[0], pixels[i][0]);
		min[1] = MIN(min[1], pixels[i][1]);
		min[2] = MIN(min[2], pixels[i][2]);

		max[0] = MAX(max[0], pixels[i][0]);
		max[1] = MAX(max[1], pixels[i][1]);
		max[2] = MAX(max[2], pixels[i][2]);
	}

	// single color?
	if(min[0] == max[0] && min[1] == max[1] && min[2] == max[2]) {

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

		float centroid[3] = { pixels[0][0], pixels[0][1], pixels[0][2] };
		for(int i = 1; i < 16; ++i) {
			centroid[0] += pixels[i][0];
			centroid[1] += pixels[i][1];
			centroid[2] += pixels[i][2];
		}
		centroid[0] /= 16;
		centroid[1] /= 16;
		centroid[2] /= 16;

		// move referential to centroid

		for(int i = 0; i < 16; ++i) {
			pixels[i][0] -= centroid[0];
			pixels[i][1] -= centroid[1];
			pixels[i][2] -= centroid[2];
		}

		// principal component analysis approximation: find best diagonal of the bounding box

		// calculate directions for all four diagonals

		float any_diagonal[3] = {
			max[0] - min[0],
			max[1] - min[1],
			max[2] - min[2]
		};
		float inv_diag_norm = 1 / sqrt(
			any_diagonal[0] * any_diagonal[0] +
			any_diagonal[1] * any_diagonal[1] +
			any_diagonal[2] * any_diagonal[2]);
		any_diagonal[0] *= inv_diag_norm;
		any_diagonal[1] *= inv_diag_norm;
		any_diagonal[2] *= inv_diag_norm;

		float diags[4][3] = {
			{ any_diagonal[0], any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], any_diagonal[1], -any_diagonal[2] },
			{ any_diagonal[0], -any_diagonal[1], any_diagonal[2] },
			{ any_diagonal[0], -any_diagonal[1], -any_diagonal[2] }
		};

		// for each diagonals calculate the projection of each colour

		float dot_products[4][16];
		float min_dot_product[4] = { 0, 0, 0, 0 };
		float max_dot_product[4] = { 0, 0, 0, 0 };

		float largest_dot_product_difference = 0;
		int best_diag_choice = 0;

		for(int diag_choice = 0; diag_choice < 4; ++diag_choice) {

			// project colors onto the diagonal

			for(int i = 0; i < 16; ++i) {
				dot_products[diag_choice][i] =
					pixels[i][0] * diags[diag_choice][0] +
					pixels[i][1] * diags[diag_choice][1] +
					pixels[i][2] * diags[diag_choice][2];
				min_dot_product[diag_choice] = MIN(min_dot_product[diag_choice], dot_products[diag_choice][i]);
				max_dot_product[diag_choice] = MAX(max_dot_product[diag_choice], dot_products[diag_choice][i]);
			}

			// is this diagonal more discriminating than the others?
			// (i.e. biggest difference between least and greatest dot products)

			float dot_product_difference = max_dot_product[diag_choice] - min_dot_product[diag_choice];
			if(dot_product_difference > largest_dot_product_difference) {
				best_diag_choice = diag_choice;
				largest_dot_product_difference = dot_product_difference;
			}
		}

		// map each color to the indice of the closest position

		unsigned char indices[16];
		float coeff = 3 / largest_dot_product_difference;
		for(int i = 0; i < 16; ++i) {
			indices[i] = ROUND_TO_BYTE((dot_products[best_diag_choice][i] - min_dot_product[best_diag_choice]) * coeff);
		}

		float clamp_min[3] = { -centroid[0], -centroid[1], -centroid[2] };
		float clamp_max[3] = { 255.0f - centroid[0], 255.0f - centroid[1], 255.0f - centroid[2] };

		float start_color[3], end_color[3];

		// refine using Least Squares
		for(int iteration = 0; iteration < 3; ++iteration) {
			// We have 16 equations like this:
			// alpha[i] * start + beta[i] * end = pixel[i]
			// with { alpha[i], beta[i] } equal to {0, 1}, {1/3, 2/3}, {2/3, 1/3} or {1, 0}.
			//
			// Least Squares solution:
			// ( start )   ( S{alpha[i]^2}       S{alpha[i]*beta[i]} )^-1   ( S{alpha[i]*pixel[i]} )
			// (  end  ) = ( S{alpha[i]*beta[i]} S{beta[i]^2}        )    x ( S{beta[i]*pixel[i]}  )

			float alpha = indices[0] / 3.0f;
			float S_alpha = alpha;
			float S_alpha_2 = alpha * alpha;
			float S_alpha_pixel[3] = {
				alpha * pixels[0][0],
				alpha * pixels[0][1],
				alpha * pixels[0][2]
			};
			for(int i = 1; i < 16; ++i) {
				alpha = indices[i] / 3.0f;
				
				S_alpha += alpha;
				S_alpha_2 += alpha * alpha;

				S_alpha_pixel[0] += alpha * pixels[i][0];
				S_alpha_pixel[1] += alpha * pixels[i][1];
				S_alpha_pixel[2] += alpha * pixels[i][2];
			}

			// calculations are simplified by the fact that beta[i] == (1 - alpha[i])
			float S_alpha_beta = S_alpha - S_alpha_2;
			float S_beta_2 = (16.0f - S_alpha) - S_alpha_beta;

			float factor = 1.0f / (S_alpha_2 * S_beta_2 - S_alpha_beta * S_alpha_beta);
			float start_factor = (S_beta_2 + S_alpha_beta) * factor;
			float end_factor = -(S_alpha_2 + S_alpha_beta) * factor;

			start_color[0] = start_factor * S_alpha_pixel[0];
			start_color[1] = start_factor * S_alpha_pixel[1];
			start_color[2] = start_factor * S_alpha_pixel[2];

			end_color[0] = end_factor * S_alpha_pixel[0];
			end_color[1] = end_factor * S_alpha_pixel[1];
			end_color[2] = end_factor * S_alpha_pixel[2];

			// clamp

			start_color[0] = MAX(clamp_min[0], MIN(clamp_max[0], start_color[0]));
			start_color[1] = MAX(clamp_min[1], MIN(clamp_max[1], start_color[1]));
			start_color[2] = MAX(clamp_min[2], MIN(clamp_max[2], start_color[2]));

			end_color[0] = MAX(clamp_min[0], MIN(clamp_max[0], end_color[0]));
			end_color[1] = MAX(clamp_min[1], MIN(clamp_max[1], end_color[1]));
			end_color[2] = MAX(clamp_min[2], MIN(clamp_max[2], end_color[2]));

			// recalculate indexes
			// map each color to the indice of the closest position

			float segment[3] = {
				start_color[0] - end_color[0],
				start_color[1] - end_color[1],
				start_color[2] - end_color[2]
			};
			float segment_norm_2 =
				segment[0] * segment[0] +
				segment[1] * segment[1] +
				segment[2] * segment[2];
			// coefficient used to pre-multiply dot product result == 3/(norm^2)
			float coeff = 3.0f / segment_norm_2;
			segment[0] *= coeff;
			segment[1] *= coeff;
			segment[2] *= coeff;

			for(int i = 0; i < 16; ++i) {
				// calculate dot product (pre-multiplied by 3/norm)
				float dot_product =
					(pixels[i][0] - end_color[0]) * segment[0] +
					(pixels[i][1] - end_color[1]) * segment[1] +
					(pixels[i][2] - end_color[2]) * segment[2];
				// round and clamp values
				indices[i] = ROUND_AND_CLAMP3(dot_product);
			}
		}

		unsigned int start_color_rounded[3];
		unsigned int end_color_rounded[3];

		start_color_rounded[0] = ROUND((centroid[0] + start_color[0]) * (31.0f / 255.0f));
		start_color_rounded[1] = ROUND((centroid[1] + start_color[1]) * (63.0f / 255.0f));
		start_color_rounded[2] = ROUND((centroid[2] + start_color[2]) * (31.0f / 255.0f));

		end_color_rounded[0] = ROUND((centroid[0] + end_color[0]) * (31.0f / 255.0f));
		end_color_rounded[1] = ROUND((centroid[1] + end_color[1]) * (63.0f / 255.0f));
		end_color_rounded[2] = ROUND((centroid[2] + end_color[2]) * (31.0f / 255.0f));

		// convert to R5G6B5 format

		unsigned short start = (unsigned short)(start_color_rounded[2] | (start_color_rounded[1] << 5) | (start_color_rounded[0] << 11));
		unsigned short end = (unsigned short)(end_color_rounded[2] | (end_color_rounded[1] << 5) | (end_color_rounded[0] << 11));

		// write block bits

		// write the endpoints		
		*((unsigned short*)block) = start;
		*((unsigned short*)(block + 2)) = end;

		// write the indices
		for( int i = 0; i < 4; ++i )
		{
			unsigned char* ind = indices + 4*i;
			block[4 + i] =
				DXT1_CODES_4[0*4 + ind[0]] |
				DXT1_CODES_4[1*4 + ind[1]] |
				DXT1_CODES_4[2*4 + ind[2]] |
				DXT1_CODES_4[3*4 + ind[3]];
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

static const unsigned int DXT5_CODES_8[8] = {
	0x00000001, 0x00000007, 0x00000006, 0x00000005,
	0x00000004, 0x00000003, 0x00000002, 0x00000000
};

static const unsigned int DXT5_CODES_6[6] = {
	0x00000000, 0x00000002, 0x00000003,
	0x00000004, 0x00000005, 0x00000001
};

void __fastcall encodeDxt5Block_float(
	const unsigned char * rgba,
	unsigned char * block)
{
	// read alpha, calculate bounds

	unsigned char min = 255;
	unsigned char max = 0;
	unsigned char min_non_0 = 255;
	unsigned char max_non_255 = 0;

	for(int i = 0; i < 16; ++i) {
		unsigned char a = rgba[4*i + 3];

		min = MIN(min, a);
		max = MAX(max, a);
		if(a != 0)
			min_non_0 = MIN(min_non_0, a);
		if(a != 255)
			max_non_255 = MAX(max_non_255, a);
	}

	// single alpha?
	if(min == max) {

		// single alpha
		// write block bits
		unsigned char single_alpha = rgba[3];

		block[1] = block[0] = single_alpha;
		block[3] = block[2] = (unsigned char) 0x00;
		*((unsigned int*)(block + 4)) = 0x00000000;

	} else {
		
		unsigned int codes[16];

		if(min == 0 && max == 255) {
			if(min_non_0 == 255) {
				min_non_0 = 0;
				max_non_255 = 255;
			}
			block[0] = min_non_0;
			block[1] = max_non_255;

			float coeff = (min_non_0 == max_non_255 ? 0.0f : 5.0f / (max_non_255 - min_non_0));
			for(int i = 0; i < 16; ++i) {
				unsigned char a = rgba[4*i + 3];
				codes[i] = (a == 0 ? 0x00000006 : (a == 255 ? 0x00000007 : DXT5_CODES_6[(int) ((a - min_non_0) * coeff + 0.5f)]));
			}
		} else {
			block[0] = max;
			block[1] = min;

			float coeff = 7.0f / (max - min);
			for(int i = 0; i < 16; ++i) {
				codes[i] = DXT5_CODES_8[(int) ((rgba[4*i + 3] - min) * coeff + 0.5f)];
			}
		}

		for(int i = 0; i < 2; ++i) {
			block[2 + i*3] = (unsigned char) (
				codes[0 + i*8] |
				(codes[1 + i*8] << 3) |
				((0x00000003 & codes[2 + i*8]) << 6));
			block[3 + i*3] = (unsigned char) (
				((0x00000004 & codes[2 + i*8]) >> 2) |
				(codes[3 + i*8] << 1) |
				(codes[4 + i*8] << 4) |
				((0x00000001 & codes[5 + i*8]) << 7));
			block[4 + i*3] = (unsigned char) (
				((0x00000006 & codes[5 + i*8]) >> 1) |
				(codes[6 + i*8] << 2) |
				(codes[7 + i*8] << 5));
		}
	}
}
