// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Randomness {

	/// <summary>A permutation of an array of elements of type T.</summary>
	public struct Permutation {

		/// <summary>Applies this permutation to an array.</summary>
		/// <typeparam name="T">The type of the elements in the array.</typeparam>
		/// <param name="array">The array to permutate.</param>
		/// <returns>The permuted array.</returns>
		public T[] Apply<T>(T[] array) {
			if(array.Length != permutedIndexes.Length)
				throw new ArgumentException();
			T[] permutedArray = new T[array.Length];
			for(int i = 0; i < array.Length; ++i)
				permutedArray[permutedIndexes[i]] = array[i];
			return permutedArray;
		}

		/// <summary>The inverse of this permutation.</summary>
		public Permutation Inverse {
			get {
				int[] inversePermutedIndexes = new int[permutedIndexes.Length];
				for(int i = 0; i < permutedIndexes.Length; ++i)
					inversePermutedIndexes[permutedIndexes[i]] = i;
				return new Permutation(inversePermutedIndexes);
			}
		}

		/// <summary>Returns a representation of this permutation as an array of bytes.</summary>
		/// <returns>An array of bytes.</returns>
		public byte[] ToBytes() {
			byte[] result = new byte[permutedIndexes.Length * 4];
			unsafe {
				fixed(byte* ptr2 = result) {
					int* ptr = (int*) ptr2;
					for(int i = 0; i < permutedIndexes.Length; ++i)
						*ptr++ = permutedIndexes[i];
				}
			}
			return result;
		}

		/// <summary>Returns the permutation corresponding to an array of bytes.</summary>
		/// <param name="bytes">An array of bytes representing a permutation.</param>
		/// <returns>A permutation.</returns>
		public static Permutation FromBytes(byte[] bytes) {
			int[] permutedIndexes = new int[bytes.Length / 4];
			unsafe {
				fixed(byte* ptr2 = bytes) {
					int* ptr = (int*) ptr2;
					for(int i = 0; i < permutedIndexes.Length; ++i)
						permutedIndexes[i] = *ptr++;
				}
			}
			return new Permutation(permutedIndexes);
		}

		/// <summary>Constructor.</summary>
		/// <param name="permutedIndexes">For each element, the index of the element in the permuted array.</param>
		public Permutation(int[] permutedIndexes) {
			// check it's a valid permutation
			unsafe {
				bool* found = stackalloc bool[permutedIndexes.Length];
				for(int i = 0; i < permutedIndexes.Length; ++i)
					found[i] = false;
				for(int i = 0; i < permutedIndexes.Length; ++i)
					if(found[permutedIndexes[i]])
						throw new ArgumentException();
					else
						found[permutedIndexes[i]] = true;
			}
			this.permutedIndexes = permutedIndexes;
		}

		private int[] permutedIndexes;
	}
}
