// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;

namespace ZunTzu.Randomness {

	/// <summary>Simulator for the casting of a die.</summary>
	public class DieSimulator : IDieSimulator {

		/// <summary>Constructor.</summary>
		public DieSimulator() {
			// initialise the Mersenne Twister and the entropy pool with pseudo-random data
			Random random = new Random();
			byte[] bytes = new byte[12];
			random.NextBytes(bytes);

			entropyPool = new EntropyPool(
				((UInt64) bytes[0] << 0) |
				((UInt64) bytes[1] << 8) |
				((UInt64) bytes[2] << 16) |
				((UInt64) bytes[3] << 24) |
				((UInt64) bytes[4] << 32) |
				((UInt64) bytes[5] << 40) |
				((UInt64) bytes[6] << 48) |
				((UInt64) bytes[7] << 56));

			mersenneTwister = new MersenneTwister(
				(((UInt32) bytes[8] << 0) | 0x1U) |
				((UInt32) bytes[9] << 8) |
				((UInt32) bytes[10] << 16) |
				((UInt32) bytes[11] << 24));
		}

		/// <summary>Returns a random die result.</summary>
		/// <param name="faceCount">Number of faces of the die.</param>
		/// <returns>A random die result, in interval [0,faceCount-1].</returns>
		public int GetDieResult(int faceCount) {
			Debug.Assert(faceCount > 1);

			uint usedBits = (uint) (faceCount - 1);
			int usedBitsCount = 0;
			for(uint shiftedBits = usedBits; shiftedBits != 0U; shiftedBits >>= 1) {
				++usedBitsCount;
				usedBits |= shiftedBits;
			}

			uint combinedResult;
			do {
				uint pseudorandomBits = mersenneTwister.GetRandomUInt32() >> (32 - usedBitsCount);
				uint entropyBits = (uint) entropyPool.GetRandomBits(usedBitsCount) & usedBits;
				combinedResult = pseudorandomBits ^ entropyBits;
			} while(combinedResult >= faceCount);
			return (int) combinedResult + 1;
		}

		/// <summary>Returns a random permutation of count elements.</summary>
		/// <param name="count">Number of elements in the array to permute.</param>
		/// <returns>A random permutation.</returns>
		public Permutation GetPermutation(int count) {
			int[] permutedIndexes = new int[count];
			unsafe {
				bool* alreadyUsed = stackalloc bool[count];
				for(int i = 0; i < count; ++i)
					alreadyUsed[i] = false;
				for(int i = 0; i < count - 1; ++i) {
					int randomNumber = GetDieResult(count - i) - 1;
					int permutedIndex = 0;
					while(true) {
						while(alreadyUsed[permutedIndex])
							++permutedIndex;
						if(randomNumber == 0)
							break;
						--randomNumber;
						++permutedIndex;
					}
					permutedIndexes[i] = permutedIndex;
					alreadyUsed[permutedIndex] = true;
				}
				for(int i = 0; i < count; ++i) {
					if(!alreadyUsed[i]) {
						permutedIndexes[count - 1] = i;
						break;
					}
				}
			}
			return new Permutation(permutedIndexes);
		}

		/// <summary>Registers a single measure from a physical source of randomness.</summary>
		/// <param name="measure">A single measure of random data.</param>
		public void AddPhysicalMeasure(uint measure) {
			entropyPool.AddPhysicalMeasure(measure);
		}

		/// <summary>For test purpose only.</summary>
		public UInt64 EntropyPool { get { return entropyPool.GetRandomBits(0); } }

		private MersenneTwister mersenneTwister;
		private EntropyPool entropyPool;
	}
}
