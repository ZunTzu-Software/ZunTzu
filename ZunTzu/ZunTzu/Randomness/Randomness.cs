// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Randomness {

	/// <summary>Simulator for the casting of a die.</summary>
	public interface IDieSimulator {
		/// <summary>Returns a random die result.</summary>
		/// <param name="faceCount">Number of faces of the die.</param>
		/// <returns>A random die result, in interval [0,faceCount-1].</returns>
		int GetDieResult(int faceCount);
		/// <summary>Returns a random permutation of count elements.</summary>
		/// <param name="count">Number of elements in the array to permute.</param>
		/// <returns>A random permutation.</returns>
		Permutation GetPermutation(int count);
		/// <summary>Registers a single measure from a physical source of randomness.</summary>
		/// <param name="measure">A single measure of random data.</param>
		void AddPhysicalMeasure(uint measure);
	}
}
