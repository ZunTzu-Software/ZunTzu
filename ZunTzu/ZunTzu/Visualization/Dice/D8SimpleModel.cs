// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.Visualization.Dice {

	/// <summary>A model of a eight-sided die.</summary>
	public static class D8SimpleModel {

		/// <summary>Distance from the center of a die to the table surface.</summary>
		public static float Inradius = 0.750555349946514f;

		/// <summary>An array of vertice coordinates.</summary>
		/// <remarks>The values are: x, y, z, nx, ny, nz, u, v</remarks>
		public static float[,] Vertice = {
			{ 0f, 0f, -1.3f, 0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0f, 0.333333333333333f },
			{ 1.3f, 0f, 0f, 0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.5f, 0.333333333333333f },
			{ 0f, 1.3f, 0f, 0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.25f, 0f },

			{ 0f, 0f, -1.3f, -0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.75f, 0f },
			{ -1.3f, 0f, 0f, -0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.25f, 0f },
			{ 0f, -1.3f, 0f, -0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.5f, 0.333333333333333f },

			{ 0f, 0f, -1.3f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 1f, 0.333333333333333f },
			{ 0f, 1.3f, 0f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.75f, 0f },
			{ -1.3f, 0f, 0f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.5f, 0.333333333333333f },

			{ 0f, 0f, -1.3f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0f, 0.333333333333333f },
			{ 0f, -1.3f, 0f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.25f, 0.666666666666667f },
			{ 1.3f, 0f, 0f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.5f, 0.333333333333333f },

			{ 0f, 0f, 1.3f, -0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.25f, 0.666666666666667f },
			{ -1.3f, 0f, 0f, -0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.75f, 0.666666666666667f },
			{ 0f, 1.3f, 0f, -0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.5f, 0.333333333333333f },

			{ 0f, 0f, 1.3f, 0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 1f, 0.333333333333333f },
			{ 1.3f, 0f, 0f, 0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.5f, 0.333333333333333f },
			{ 0f, -1.3f, 0f, 0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.75f, 0.666666666666667f },

			{ 0f, 0f, 1.3f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.5f, 1f },
			{ 0f, 1.3f, 0f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.25f, 0.666666666666667f },
			{ 1.3f, 0f, 0f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0f, 1f },

			{ 0f, 0f, 1.3f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.25f, 0.666666666666667f },
			{ 0f, -1.3f, 0f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.5f, 1f },
			{ -1.3f, 0f, 0f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.75f, 0.666666666666667f }
		};

		/// <summary>An array of triangles described as vertex indexes.</summary>
		public static Int16[,] Triangles = {
			{ 0, 1, 2 },
			{ 3, 4, 5 },
			{ 6, 7, 8 },
			{ 9, 10, 11 },
			{ 12, 13, 14 },
			{ 15, 16, 17 },
			{ 18, 19, 20 },
			{ 21, 22, 23 }
		};
	}

}
