// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Randomness {

	/// <summary>Stores results from a physical source of randomness, then applies a whitening algorithm.</summary>
	internal class EntropyPool {

		/// <summary>Constructor.</summary>
		/// <param name="initialBits">64 bits to initialize the entropy pool with.</param>
		public EntropyPool(UInt64 initialBits) {
			pool = initialBits;
		}

		/// <summary>Registers a single measure from a physical source of randomness.</summary>
		/// <param name="measure">A single measure of random data.</param>
		public void AddPhysicalMeasure(uint measure) {
			// hash measure
			uint hashedValue = 0U;
			while(measure != 0) {
				hashedValue ^= measure;
				measure >>= 1;
			}
			byte singleBit = (byte) (hashedValue & 1);

			// fix bias using von Neumann's method
			if(expectingSecondBit) {
				if(singleBit != firstBit)
					pool = (pool << 1) | singleBit;
			} else {
				firstBit = singleBit;
			}
			expectingSecondBit = !expectingSecondBit;
		}

		/// <summary>Retrieves random bits from the entropy pool.</summary>
		/// <param name="bitCount">The number of bits to retrieve.</param>
		/// <returns>The random bits, stored as the least significant bits of an unsigned 64 bits number.</returns>
		public UInt64 GetRandomBits(int bitCount) {
			UInt64 result = pool;
			// rotate bits
			pool = (pool >> bitCount) | (pool << (64 - bitCount));
			return result;
		}

		private UInt64 pool;
		private bool expectingSecondBit = false;
		private byte firstBit = 0;
	}
}
