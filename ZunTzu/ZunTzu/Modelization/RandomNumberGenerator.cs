// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization {

	/// <summary>A simple random number generator.</summary>
	internal sealed class RandomNumberGenerator : IRandomNumberGenerator {

		/// <summary>Generates a random integer number.</summary>
		/// <param name="lowerBound">Minimum eligible value.</param>
		/// <param name="upperBound">Maximum eligible value.</param>
		/// <returns>A random integer number.</returns>
		public int GenerateInt32(int lowerBound, int upperBound) {
			return (int) Math.Floor(lowerBound + random.NextDouble() * (upperBound + 1 - lowerBound));
		}

		/// <summary>Generates a random float number.</summary>
		/// <param name="lowerBound">Minimum eligible value (inclusive).</param>
		/// <param name="upperBound">Maximum eligible value (exclusive).</param>
		/// <returns>A random float number.</returns>
		public float GenerateSingle(float lowerBound, float upperBound) {
			return (float)(random.NextDouble() * (upperBound - lowerBound) + lowerBound);
		}

		private Random random = new Random();
	}
}
