// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.Visualization.Dice {

	/// <summary>A model of a four-sided die.</summary>
	public static class D4SimpleModel {

		/// <summary>Distance from the center of a die to the table surface.</summary>
		public static float Inradius = 0.577350269189626f;

		/// <summary>An array of vertice coordinates.</summary>
		/// <remarks>The values are: x, y, z, nx, ny, nz, u, v</remarks>
		public static float[,] Vertice = {
			{ 1f, 1f, -1f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.333333333333333f, 0f },
			{ -1f, -1f, -1f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0.666666666666667f, 0.5f },
			{ -1f, 1f, 1f, -0.577350269189626f, 0.577350269189626f, -0.577350269189626f, 0f, 0.5f },

			{ 1f, 1f, -1f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.333333333333333f, 0f },
			{ 1f, -1f, 1f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 1f, 0f },
			{ -1f, -1f, -1f, 0.577350269189626f, -0.577350269189626f, -0.577350269189626f, 0.666666666666667f, 0.5f },

			{ 1f, 1f, -1f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.333333333333333f, 1f },
			{ -1f, 1f, 1f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 0.666666666666667f, 0.5f },
			{ 1f, -1f, 1f, 0.577350269189626f, 0.577350269189626f, 0.577350269189626f, 1f, 1f },

			{ 1f, -1f, 1f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.333333333333333f, 1f },
			{ -1f, 1f, 1f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0f, 0.5f },
			{ -1f, -1f, -1f, -0.577350269189626f, -0.577350269189626f, 0.577350269189626f, 0.666666666666667f, 0.5f }
		};

		/// <summary>An array of triangles described as vertex indexes.</summary>
		public static Int16[,] Triangles = {
			{ 0, 2, 1 },
			{ 3, 5, 4 },
			{ 6, 8, 7 },
			{ 9, 11, 10 }
		};
	}

}
