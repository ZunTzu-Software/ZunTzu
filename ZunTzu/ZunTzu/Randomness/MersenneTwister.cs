// Copyright (c) 2020 ZunTzu Software and contributors

using System;

/* 
   A C-program for MT19937, with initialization improved 2002/1/26.
   Coded by Takuji Nishimura and Makoto Matsumoto.

   Before using, initialize the state by using init_genrand(seed)  
   or init_by_array(init_key, key_length).

   Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
   All rights reserved.                          

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

     1. Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.

     2. Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.

     3. The names of its contributors may not be used to endorse or promote 
        products derived from this software without specific prior written 
        permission.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


   Any feedback is very welcome.
   http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html
   email: m-mat @ math.sci.hiroshima-u.ac.jp (remove space)
*/

/*
   Recoding and conversion to C# Copyright (C) 2006, ZunTzu Software, All rights reserved.
*/

namespace ZunTzu.Randomness {

	/// <summary>Pseudorandom number generator.</summary>
	/// <remarks>
	/// The Mersenne twister is a pseudorandom number generator developed in 1997 by Makoto Matsumoto and
	/// Takuji Nishimura that is based on a matrix linear recurrence over a finite binary field .
	/// It provides for fast generation of very high quality pseudorandom numbers, having been designed
	/// specifically to rectify many of the flaws found in older algorithms.
	/// Its name derives from the fact that period length is chosen to be a Mersenne prime.
	/// There are at least two common variants of the algorithm, differing only in the size of the Mersenne
	/// primes used. This version is the Mersenne Twister MT19937, the newer and more commonly used one,
	/// with 32-bit word length.
	/// </remarks>
	internal class MersenneTwister {

		// Period parameters
		private const uint N = 624U;
		private const uint M = 397U;
		private const uint MATRIX_A = 0x9908b0dfU;   // constant vector a
		private const uint UPPER_MASK = 0x80000000U; // most significant w-r bits
		private const uint LOWER_MASK = 0x7fffffffU; // least significant r bits

		private uint[] mt = new uint[N]; // the array for the state vector
		private uint mti;	// current value index

		/// <summary>Initializes the generator from a seed.</summary>
		/// <param name="seed">Seed</param>
		public MersenneTwister(uint seed) {
			mt[0] = seed;
			for(mti = 1; mti < N; mti++) {
				mt[mti] = (1812433253U * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + mti);
				// See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
				// In the previous versions, MSBs of the seed affect only MSBs of the array mt[].
				// 2002/01/09 modified by Makoto Matsumoto
			}
		}

		/// <summary>Generates a pseudorandom number on [0,0xffffffff]-interval.</summary>
		/// <returns>A pseudorandom number.</returns>
		public uint GetRandomUInt32() {
			if(mti >= N) {
				// Generate an array of N untempered numbers
				uint kk;
				for(kk = 0; kk < N - M; kk++)
					mt[kk] = mt[kk + M] ^ (((mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK)) >> 1) ^ ((uint) -(mt[kk + 1] & 0x1U) & MATRIX_A);
				for(; kk < N - 1; kk++)
					mt[kk] = mt[kk + M - N] ^ (((mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK)) >> 1) ^ ((uint) -(mt[kk + 1] & 0x1U) & MATRIX_A);
				mt[N - 1] = mt[M - 1] ^ (((mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK)) >> 1) ^ ((uint) -(mt[0] & 0x1U) & MATRIX_A);
				mti = 0;
			}

			// Extract a tempered pseudorandom number based on the mti-th value
			uint y = mt[mti++];
			y ^= (y >> 11);
			y ^= (y << 7) & 0x9d2c5680U;
			y ^= (y << 15) & 0xefc60000U;
			y ^= (y >> 18);
			return y;
		}

		/// <summary>Generates a pseudorandom number on [0,1)-real-interval.</summary>
		/// <returns>A pseudorandom number.</returns>
		/// <remarks>Due to Isaku Wada, 2002/01/09</remarks>
		public double GetRandomDouble() {
			return GetRandomUInt32() * (1.0 / 4294967296.0); // divided by 2^32
		}
	}
}
