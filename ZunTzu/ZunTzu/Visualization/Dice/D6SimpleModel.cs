// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.Visualization.Dice {

	/// <summary>A model of a six-sided die.</summary>
	public static class D6SimpleModel {

		/// <summary>Distance from the center of a die to the table surface.</summary>
		public static float Inradius = 1.0f;

		/// <summary>An array of vertice coordinates.</summary>
		/// <remarks>The values are: x, y, z, nx, ny, nz, u, v</remarks>
		public static float[,] Vertice = {
			{ 1f, 1f, -1f, 0f, 0f, -1f, 0.333333333333333f, 0f },
			{ -1f, 1f, -1f, 0f, 0f, -1f, 0f, 0f },
			{ -1f, -1f, -1f, 0f, 0f, -1f, 0f, 0.5f },
			{ 1f, -1f, -1f, 0f, 0f, -1f, 0.333333333333333f, 0.5f },

			{ -1f, 1f, 1f, 0f, 0f, 1f, 1f, 1f },
			{ 1f, 1f, 1f, 0f, 0f, 1f, 1f, 0.5f },
			{ 1f, -1f, 1f, 0f, 0f, 1f, 0.666666666666667f, 0.5f },
			{ -1f, -1f, 1f, 0f, 0f, 1f, 0.666666666666667f, 1f },

			{ 1f, 1f, 1f, 1f, 0f, 0f, 0.333333333333333f, 0f },
			{ 1f, 1f, -1f, 1f, 0f, 0f, 0.333333333333333f, 0.5f },
			{ 1f, -1f, -1f, 1f, 0f, 0f, 0.666666666666667f, 0.5f },
			{ 1f, -1f, 1f, 1f, 0f, 0f, 0.666666666666667f, 0f },

			{ -1f, 1f, -1f, -1f, 0f, 0f, 0.333333333f, 1f },
			{ -1f, 1f, 1f, -1f, 0f, 0f, 0.666666667f, 1f },
			{ -1f, -1f, 1f, -1f, 0f, 0f, 0.666666667f, 0.5f },
			{ -1f, -1f, -1f, -1f, 0f, 0f, 0.333333333f, 0.5f },

			{ 1f, 1f, 1f, 0f, 1f, 0f, 0f, 1f },
			{ -1f, 1f, 1f, 0f, 1f, 0f, 0.333333333333333f, 1f },
			{ -1f, 1f, -1f, 0f, 1f, 0f, 0.333333333333333f, 0.5f },
			{ 1f, 1f, -1f, 0f, 1f, 0f, 0f, 0.5f },

			{ 1f, -1f, -1f, 0f, -1f, 0f, 1f, 0.5f },
			{ -1f, -1f, -1f, 0f, -1f, 0f, 1f, 0f },
			{ -1f, -1f, 1f, 0f, -1f, 0f, 0.666666666666667f, 0f },
			{ 1f, -1f, 1f, 0f, -1f, 0f, 0.666666666666667f, 0.5f }
		};

		/// <summary>An array of triangles described as vertex indexes.</summary>
		public static Int16[,] Triangles = {
			{ 0, 1, 3 },
			{ 1, 2, 3 },
			{ 4, 5, 7 },
			{ 5, 6, 7 },
			{ 8, 9, 11 },
			{ 9, 10, 11 },
			{ 12, 13, 15 },
			{ 13, 14, 15 },
			{ 16, 17, 19 },
			{ 17, 18, 19 },
			{ 20, 21, 23 },
			{ 21, 22, 23 }
		};
	}

}
