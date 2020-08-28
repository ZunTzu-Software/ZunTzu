// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.Visualization.Dice {

	/// <summary>A model of a twenty-sided die.</summary>
	public static class D20ComplexModel {

		/// <summary>Distance from the center of a die to the table surface.</summary>
		public static float Inradius = 1.34272005386689f;

		/// <summary>An array of vertice coordinates.</summary>
		/// <remarks>The values are: x, y, z, nx, ny, nz, u, v</remarks>
		public static float[,] Vertice = {
			{ 0f, -1.34206363626763f, -0.9247138328141f, 0f, -0.85065080835204f, -2.22703272882321f, 0f, 0.25f },
			{ 0.8f, -0.0476364452677142f, -1.41914102381402f, 0f, -0.85065080835204f, -2.22703272882321f, 0.333333333333333f, 0.25f },
			{ -0.8f, -0.0476364452677142f, -1.41914102381402f, 0f, -0.85065080835204f, -2.22703272882321f, 0.166666666666667f, 0f },

			{ -0.8f, -0.0476364452677142f, 1.41914102381402f, 0f, -0.85065080835204f, 2.22703272882321f, 0.5f, 0f },
			{ 0.8f, -0.0476364452677142f, 1.41914102381402f, 0f, -0.85065080835204f, 2.22703272882321f, 0.166666666666667f, 0f },
			{ 0f, -1.34206363626763f, 0.9247138328141f, 0f, -0.85065080835204f, 2.22703272882321f, 0.333333333333333f, 0.25f },

			{ -0.877077387546386f, 0.0770773875463857f, -1.3715045785463f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.333333333333333f, 0.25f },
			{ -0.0770773875463857f, 1.3715045785463f, -0.877077387546386f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.666666666666667f, 0.25f },
			{ -1.3715045785463f, 0.877077387546386f, -0.0770773875463857f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.5f, 0f },

			{ 0.9247138328141f, 0f, 1.34206363626763f, 2.22703272882321f, 0f, 0.85065080835204f, 0.833333333333333f, 0f },
			{ 1.41914102381402f, 0.8f, 0.0476364452677142f, 2.22703272882321f, 0f, 0.85065080835204f, 0.5f, 0f },
			{ 1.41914102381402f, -0.8f, 0.0476364452677142f, 2.22703272882321f, 0f, 0.85065080835204f, 0.666666666666667f, 0.25f },

			{ 1.34206363626763f, -0.9247138328141f, 0f, 0.85065080835204f, -2.22703272882321f, 0f, 0.666666666666667f, 0.25f },
			{ 0.0476364452677142f, -1.41914102381402f, -0.8f, 0.85065080835204f, -2.22703272882321f, 0f, 1f, 0.25f },
			{ 0.0476364452677142f, -1.41914102381402f, 0.8f, 0.85065080835204f, -2.22703272882321f, 0f, 0.833333333333333f, 0f },

			{ 1.34206363626763f, 0.9247138328141f, 0f, 0.85065080835204f, 2.22703272882321f, 0f, 0.333333333333333f, 0.25f },
			{ 0.0476364452677142f, 1.41914102381402f, 0.8f, 0.85065080835204f, 2.22703272882321f, 0f, 0f, 0.25f },
			{ 0.0476364452677142f, 1.41914102381402f, -0.8f, 0.85065080835204f, 2.22703272882321f, 0f, 0.166666666666667f, 0.5f },

			{ -0.0770773875463857f, -1.3715045785463f, -0.877077387546386f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.166666666666667f, 0.5f },
			{ -0.877077387546386f, -0.0770773875463857f, -1.3715045785463f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.5f, 0.5f },
			{ -1.3715045785463f, -0.877077387546386f, -0.0770773875463857f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.333333333333333f, 0.25f },

			{ -0.0770773875463857f, 1.3715045785463f, 0.877077387546386f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.666666666666667f, 0.25f },
			{ -0.877077387546386f, 0.0770773875463857f, 1.3715045785463f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.333333333333333f, 0.25f },
			{ -1.3715045785463f, 0.877077387546386f, 0.0770773875463857f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.5f, 0.5f },

			{ 0.0770773875463857f, 1.3715045785463f, -0.877077387546386f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.5f, 0.5f },
			{ 0.877077387546386f, 0.0770773875463857f, -1.3715045785463f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.833333333333333f, 0.5f },
			{ 1.3715045785463f, 0.877077387546386f, -0.0770773875463857f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.666666666666667f, 0.25f },

			{ -1.41914102381402f, 0.8f, 0.0476364452677142f, -2.22703272882321f, 0f, 0.85065080835204f, 1f, 0.25f },
			{ -0.9247138328141f, 0f, 1.34206363626763f, -2.22703272882321f, 0f, 0.85065080835204f, 0.666666666666667f, 0.25f },
			{ -1.41914102381402f, -0.8f, 0.0476364452677142f, -2.22703272882321f, 0f, 0.85065080835204f, 0.833333333333333f, 0.5f },

			{ 0.9247138328141f, 0f, -1.34206363626763f, 2.22703272882321f, 0f, -0.85065080835204f, 0f, 0.75f },
			{ 1.41914102381402f, -0.8f, -0.0476364452677142f, 2.22703272882321f, 0f, -0.85065080835204f, 0.333333333333333f, 0.75f },
			{ 1.41914102381402f, 0.8f, -0.0476364452677142f, 2.22703272882321f, 0f, -0.85065080835204f, 0.166666666666667f, 0.5f },

			{ -0.877077387546386f, -0.0770773875463857f, 1.3715045785463f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.5f, 0.5f },
			{ -0.0770773875463857f, -1.3715045785463f, 0.877077387546386f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.166666666666667f, 0.5f },
			{ -1.3715045785463f, -0.877077387546386f, 0.0770773875463857f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.333333333333333f, 0.75f },

			{ 0.877077387546386f, -0.0770773875463857f, -1.3715045785463f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.333333333333333f, 0.75f },
			{ 0.0770773875463857f, -1.3715045785463f, -0.877077387546386f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.666666666666667f, 0.75f },
			{ 1.3715045785463f, -0.877077387546386f, -0.0770773875463857f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.5f, 0.5f },

			{ 0.877077387546386f, 0.0770773875463857f, 1.3715045785463f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.833333333333333f, 0.5f },
			{ 0.0770773875463857f, 1.3715045785463f, 0.877077387546386f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.5f, 0.5f },
			{ 1.3715045785463f, 0.877077387546386f, 0.0770773875463857f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.666666666666667f, 0.75f },

			{ -0.0476364452677142f, -1.41914102381402f, -0.8f, -0.85065080835204f, -2.22703272882321f, 0f, 0.666666666666667f, 0.75f },
			{ -1.34206363626763f, -0.9247138328141f, 0f, -0.85065080835204f, -2.22703272882321f, 0f, 1f, 0.75f },
			{ -0.0476364452677142f, -1.41914102381402f, 0.8f, -0.85065080835204f, -2.22703272882321f, 0f, 0.833333333333333f, 0.5f },

			{ -0.0476364452677142f, 1.41914102381402f, 0.8f, -0.85065080835204f, 2.22703272882321f, 0f, 0.333333333333333f, 0.75f },
			{ -1.34206363626763f, 0.9247138328141f, 0f, -0.85065080835204f, 2.22703272882321f, 0f, 0f, 0.75f },
			{ -0.0476364452677142f, 1.41914102381402f, -0.8f, -0.85065080835204f, 2.22703272882321f, 0f, 0.166666666666667f, 1f },

			{ -1.41914102381402f, -0.8f, -0.0476364452677142f, -2.22703272882321f, 0f, -0.85065080835204f, 0.166666666666667f, 1f },
			{ -0.9247138328141f, 0f, -1.34206363626763f, -2.22703272882321f, 0f, -0.85065080835204f, 0.5f, 1f },
			{ -1.41914102381402f, 0.8f, -0.0476364452677142f, -2.22703272882321f, 0f, -0.85065080835204f, 0.333333333333333f, 0.75f },

			{ 0.0770773875463857f, -1.3715045785463f, 0.877077387546386f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.666666666666667f, 0.75f },
			{ 0.877077387546386f, -0.0770773875463857f, 1.3715045785463f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.333333333333333f, 0.75f },
			{ 1.3715045785463f, -0.877077387546386f, 0.0770773875463857f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.5f, 1f },

			{ -0.8f, 0.0476364452677142f, -1.41914102381402f, 0f, 0.85065080835204f, -2.22703272882321f, 0.5f, 1f },
			{ 0.8f, 0.0476364452677142f, -1.41914102381402f, 0f, 0.85065080835204f, -2.22703272882321f, 0.833333333333333f, 1f },
			{ 0f, 1.34206363626763f, -0.9247138328141f, 0f, 0.85065080835204f, -2.22703272882321f, 0.666666666666667f, 0.75f },

			{ -0.8f, 0.0476364452677142f, 1.41914102381402f, 0f, 0.85065080835204f, 2.22703272882321f, 1f, 0.75f },
			{ 0f, 1.34206363626763f, 0.9247138328141f, 0f, 0.85065080835204f, 2.22703272882321f, 0.666666666666667f, 0.75f },
			{ 0.8f, 0.0476364452677142f, 1.41914102381402f, 0f, 0.85065080835204f, 2.22703272882321f, 0.833333333333333f, 1f },

			//

			{ 0f, -1.34206363626763f, -0.9247138328141f, 0f, -0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },
			{ 0.8f, -0.0476364452677142f, -1.41914102381402f, 0f, -0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },
			{ -0.8f, -0.0476364452677142f, -1.41914102381402f, 0f, -0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },

			{ -0.8f, -0.0476364452677142f, 1.41914102381402f, 0f, -0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },
			{ 0.8f, -0.0476364452677142f, 1.41914102381402f, 0f, -0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },
			{ 0f, -1.34206363626763f, 0.9247138328141f, 0f, -0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },

			{ -0.877077387546386f, 0.0770773875463857f, -1.3715045785463f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ -0.0770773875463857f, 1.3715045785463f, -0.877077387546386f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ -1.3715045785463f, 0.877077387546386f, -0.0770773875463857f, -1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },

			{ 0.9247138328141f, 0f, 1.34206363626763f, 2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ 1.41914102381402f, 0.8f, 0.0476364452677142f, 2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ 1.41914102381402f, -0.8f, 0.0476364452677142f, 2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },

			{ 1.34206363626763f, -0.9247138328141f, 0f, 0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },
			{ 0.0476364452677142f, -1.41914102381402f, -0.8f, 0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },
			{ 0.0476364452677142f, -1.41914102381402f, 0.8f, 0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },

			{ 1.34206363626763f, 0.9247138328141f, 0f, 0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },
			{ 0.0476364452677142f, 1.41914102381402f, 0.8f, 0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },
			{ 0.0476364452677142f, 1.41914102381402f, -0.8f, 0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },

			{ -0.0770773875463857f, -1.3715045785463f, -0.877077387546386f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ -0.877077387546386f, -0.0770773875463857f, -1.3715045785463f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ -1.3715045785463f, -0.877077387546386f, -0.0770773875463857f, -1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },

			{ -0.0770773875463857f, 1.3715045785463f, 0.877077387546386f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ -0.877077387546386f, 0.0770773875463857f, 1.3715045785463f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ -1.3715045785463f, 0.877077387546386f, 0.0770773875463857f, -1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },

			{ 0.0770773875463857f, 1.3715045785463f, -0.877077387546386f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ 0.877077387546386f, 0.0770773875463857f, -1.3715045785463f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ 1.3715045785463f, 0.877077387546386f, -0.0770773875463857f, 1.37638192047117f, 1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },

			{ -1.41914102381402f, 0.8f, 0.0476364452677142f, -2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ -0.9247138328141f, 0f, 1.34206363626763f, -2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ -1.41914102381402f, -0.8f, 0.0476364452677142f, -2.22703272882321f, 0f, 0.85065080835204f, 0.1f, 0.5f },

			{ 0.9247138328141f, 0f, -1.34206363626763f, 2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ 1.41914102381402f, -0.8f, -0.0476364452677142f, 2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ 1.41914102381402f, 0.8f, -0.0476364452677142f, 2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },

			{ -0.877077387546386f, -0.0770773875463857f, 1.3715045785463f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ -0.0770773875463857f, -1.3715045785463f, 0.877077387546386f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ -1.3715045785463f, -0.877077387546386f, 0.0770773875463857f, -1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },

			{ 0.877077387546386f, -0.0770773875463857f, -1.3715045785463f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ 0.0770773875463857f, -1.3715045785463f, -0.877077387546386f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },
			{ 1.3715045785463f, -0.877077387546386f, -0.0770773875463857f, 1.37638192047117f, -1.37638192047117f, -1.37638192047117f, 0.1f, 0.5f },

			{ 0.877077387546386f, 0.0770773875463857f, 1.3715045785463f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ 0.0770773875463857f, 1.3715045785463f, 0.877077387546386f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ 1.3715045785463f, 0.877077387546386f, 0.0770773875463857f, 1.37638192047117f, 1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },

			{ -0.0476364452677142f, -1.41914102381402f, -0.8f, -0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },
			{ -1.34206363626763f, -0.9247138328141f, 0f, -0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },
			{ -0.0476364452677142f, -1.41914102381402f, 0.8f, -0.85065080835204f, -2.22703272882321f, 0f, 0.1f, 0.5f },

			{ -0.0476364452677142f, 1.41914102381402f, 0.8f, -0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },
			{ -1.34206363626763f, 0.9247138328141f, 0f, -0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },
			{ -0.0476364452677142f, 1.41914102381402f, -0.8f, -0.85065080835204f, 2.22703272882321f, 0f, 0.1f, 0.5f },

			{ -1.41914102381402f, -0.8f, -0.0476364452677142f, -2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ -0.9247138328141f, 0f, -1.34206363626763f, -2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ -1.41914102381402f, 0.8f, -0.0476364452677142f, -2.22703272882321f, 0f, -0.85065080835204f, 0.1f, 0.5f },

			{ 0.0770773875463857f, -1.3715045785463f, 0.877077387546386f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ 0.877077387546386f, -0.0770773875463857f, 1.3715045785463f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },
			{ 1.3715045785463f, -0.877077387546386f, 0.0770773875463857f, 1.37638192047117f, -1.37638192047117f, 1.37638192047117f, 0.1f, 0.5f },

			{ -0.8f, 0.0476364452677142f, -1.41914102381402f, 0f, 0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },
			{ 0.8f, 0.0476364452677142f, -1.41914102381402f, 0f, 0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },
			{ 0f, 1.34206363626763f, -0.9247138328141f, 0f, 0.85065080835204f, -2.22703272882321f, 0.1f, 0.5f },

			{ -0.8f, 0.0476364452677142f, 1.41914102381402f, 0f, 0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },
			{ 0f, 1.34206363626763f, 0.9247138328141f, 0f, 0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },
			{ 0.8f, 0.0476364452677142f, 1.41914102381402f, 0f, 0.85065080835204f, 2.22703272882321f, 0.1f, 0.5f },

			//

			{ 0f, -1.38638610390692f, -0.856833733745019f, 0f, -0.85065080835204f, -0.525731112119133f, 0.1f, 0.5f },
			{ 0.856833733745019f, 0f, -1.38638610390692f, 0.525731112119134f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ -0.856833733745019f, 0f, -1.38638610390692f, -0.525731112119134f, 0f, -0.85065080835204f, 0.1f, 0.5f },
			{ -0.856833733745019f, 0f, 1.38638610390692f, -0.525731112119134f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ 0f, -1.38638610390692f, 0.856833733745019f, 0f, -0.85065080835204f, 0.525731112119133f, 0.1f, 0.5f },
			{ 0.856833733745019f, 0f, 1.38638610390692f, 0.525731112119134f, 0f, 0.85065080835204f, 0.1f, 0.5f },
			{ -1.38638610390692f, 0.856833733745019f, 0f, -0.85065080835204f, 0.525731112119133f, 0f, 0.1f, 0.5f },
			{ 0f, 1.38638610390692f, -0.856833733745019f, 0f, 0.85065080835204f, -0.525731112119133f, 0.1f, 0.5f },
			{ 1.38638610390692f, -0.856833733745019f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0.1f, 0.5f },
			{ 1.38638610390692f, 0.856833733745019f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0.1f, 0.5f },
			{ 0f, 1.38638610390692f, 0.856833733745019f, 0f, 0.85065080835204f, 0.525731112119134f, 0.1f, 0.5f },
			{ -1.38638610390692f, -0.856833733745019f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0.1f, 0.5f }
		};

		/// <summary>An array of triangles described as vertex indexes.</summary>
		public static Int16[,] Triangles = {
			{ 0, 1, 2 },
			{ 3, 4, 5 },
			{ 6, 7, 8 },
			{ 9, 10, 11 },
			{ 12, 13, 14 },	// 5
			{ 15, 16, 17 },
			{ 18, 19, 20 },
			{ 21, 22, 23 },
			{ 24, 25, 26 },	// 9
			{ 27, 28, 29 },
			{ 30, 31, 32 },
			{ 33, 34, 35 },
			{ 36, 37, 38 }, // 13
			{ 39, 40, 41 },
			{ 42, 43, 44 },
			{ 45, 46, 47 },
			{ 48, 49, 50 }, // 17
			{ 51, 52, 53 },
			{ 54, 55, 56 },
			{ 57, 58, 59 },

			{ 60 + 18, 60 + 0, 60 + 19 },
			{ 60 + 19, 60 + 0, 60 + 2 },
			{ 60 + 1, 60 + 0, 60 + 37 },
			{ 60 + 37, 60 + 36, 60 + 1 },
			{ 60 + 1, 60 + 55, 60 + 2 },
			{ 60 + 2, 60 + 55, 60 + 54 },
			{ 60 + 33, 60 + 3, 60 + 34 },
			{ 60 + 34, 60 + 3, 60 + 5 },
			{ 60 + 51, 60 + 5, 60 + 52 },
			{ 60 + 52, 60 + 5, 60 + 4 },
			{ 60 + 59, 60 + 4, 60 + 57 },
			{ 60 + 57, 60 + 4, 60 + 3 },
			{ 60 + 7, 60 + 47, 60 + 8 },
			{ 60 + 8, 60 + 47, 60 + 46 },
			{ 60 + 8, 60 + 50, 60 + 6 },
			{ 60 + 6, 60 + 50, 60 + 49 },
			{ 60 + 6, 60 + 54, 60 + 7 },
			{ 60 + 7, 60 + 54, 60 + 56 },
			{ 60 + 10, 60 + 32, 60 + 11 },
			{ 60 + 11, 60 + 32, 60 + 31 },
			{ 60 + 9, 60 + 39, 60 + 10 },
			{ 60 + 10, 60 + 39, 60 + 41 },
			{ 60 + 11, 60 + 53, 60 + 9 },
			{ 60 + 9, 60 + 53, 60 + 52 },
			{ 60 + 12, 60 + 38, 60 + 13 },
			{ 60 + 13, 60 + 38, 60 + 37 },
			{ 60 + 13, 60 + 42, 60 + 14 },
			{ 60 + 14, 60 + 42, 60 + 44 },
			{ 60 + 14, 60 + 51, 60 + 12 },
			{ 60 + 12, 60 + 51, 60 + 53 },
			{ 60 + 17, 60 + 24, 60 + 15 },
			{ 60 + 15, 60 + 24, 60 + 26 },
			{ 60 + 15, 60 + 41, 60 + 16 },
			{ 60 + 16, 60 + 41, 60 + 40 },
			{ 60 + 16, 60 + 45, 60 + 17 },
			{ 60 + 17, 60 + 45, 60 + 47 },
			{ 60 + 20, 60 + 43, 60 + 18 },
			{ 60 + 18, 60 + 43, 60 + 42 },
			{ 60 + 19, 60 + 49, 60 + 20 },
			{ 60 + 20, 60 + 49, 60 + 48 },
			{ 60 + 22, 60 + 28, 60 + 23 },
			{ 60 + 23, 60 + 28, 60 + 27 },
			{ 60 + 23, 60 + 46, 60 + 21 },
			{ 60 + 21, 60 + 46, 60 + 45 },
			{ 60 + 21, 60 + 58, 60 + 22 },
			{ 60 + 22, 60 + 58, 60 + 57 },
			{ 60 + 25, 60 + 30, 60 + 26 },
			{ 60 + 26, 60 + 30, 60 + 32 },
			{ 60 + 24, 60 + 56, 60 + 25 },
			{ 60 + 25, 60 + 56, 60 + 55 },
			{ 60 + 28, 60 + 33, 60 + 29 },
			{ 60 + 29, 60 + 33, 60 + 35 },
			{ 60 + 29, 60 + 48, 60 + 27 },
			{ 60 + 27, 60 + 48, 60 + 50 },
			{ 60 + 30, 60 + 36, 60 + 31 },
			{ 60 + 31, 60 + 36, 60 + 38 },
			{ 60 + 34, 60 + 44, 60 + 35 },
			{ 60 + 35, 60 + 44, 60 + 43 },
			{ 60 + 39, 60 + 59, 60 + 40 },
			{ 60 + 40, 60 + 59, 60 + 58 },

			{ 60 + 60, 60 + 0, 60 + 18 }, { 60 + 60, 60 + 18, 60 + 42 }, { 60 + 60, 60 + 42, 60 + 13 }, { 60 + 60, 60 + 13, 60 + 37 }, { 60 + 60, 60 + 37, 60 + 0 },
			{ 60 + 61, 60 + 1, 60 + 36 }, { 60 + 61, 60 + 36, 60 + 30 }, { 60 + 61, 60 + 30, 60 + 25 }, { 60 + 61, 60 + 25, 60 + 55 }, { 60 + 61, 60 + 55, 60 + 1 },
			{ 60 + 62, 60 + 2, 60 + 54 }, { 60 + 62, 60 + 54, 60 + 6 }, { 60 + 62, 60 + 6, 60 + 49 }, { 60 + 62, 60 + 49, 60 + 19 }, { 60 + 62, 60 + 19, 60 + 2 },
			{ 60 + 63, 60 + 3, 60 + 33 }, { 60 + 63, 60 + 33, 60 + 28 }, { 60 + 63, 60 + 28, 60 + 22 }, { 60 + 63, 60 + 22, 60 + 57 }, { 60 + 63, 60 + 57, 60 + 3 },
			{ 60 + 64, 60 + 5, 60 + 51 }, { 60 + 64, 60 + 51, 60 + 14 }, { 60 + 64, 60 + 14, 60 + 44 }, { 60 + 64, 60 + 44, 60 + 34 }, { 60 + 64, 60 + 34, 60 + 5 },
			{ 60 + 65, 60 + 4, 60 + 59 }, { 60 + 65, 60 + 59, 60 + 39 }, { 60 + 65, 60 + 39, 60 + 9 }, { 60 + 65, 60 + 9, 60 + 52 }, { 60 + 65, 60 + 52, 60 + 4 },
			{ 60 + 66, 60 + 8, 60 + 46 }, { 60 + 66, 60 + 46, 60 + 23 }, { 60 + 66, 60 + 23, 60 + 27 }, { 60 + 66, 60 + 27, 60 + 50 }, { 60 + 66, 60 + 50, 60 + 8 },
			{ 60 + 67, 60 + 7, 60 + 56 }, { 60 + 67, 60 + 56, 60 + 24 }, { 60 + 67, 60 + 24, 60 + 17 }, { 60 + 67, 60 + 17, 60 + 47 }, { 60 + 67, 60 + 47, 60 + 7 },
			{ 60 + 68, 60 + 11, 60 + 31 }, { 60 + 68, 60 + 31, 60 + 38 }, { 60 + 68, 60 + 38, 60 + 12 }, { 60 + 68, 60 + 12, 60 + 53 }, { 60 + 68, 60 + 53, 60 + 11 },
			{ 60 + 69, 60 + 10, 60 + 41 }, { 60 + 69, 60 + 41, 60 + 15 }, { 60 + 69, 60 + 15, 60 + 26 }, { 60 + 69, 60 + 26, 60 + 32 }, { 60 + 69, 60 + 32, 60 + 10 },
			{ 60 + 70, 60 + 16, 60 + 40 }, { 60 + 70, 60 + 40, 60 + 58 }, { 60 + 70, 60 + 58, 60 + 21 }, { 60 + 70, 60 + 21, 60 + 45 }, { 60 + 70, 60 + 45, 60 + 16 },
			{ 60 + 71, 60 + 20, 60 + 48 }, { 60 + 71, 60 + 48, 60 + 29 }, { 60 + 71, 60 + 29, 60 + 35 }, { 60 + 71, 60 + 35, 60 + 43 }, { 60 + 71, 60 + 43, 60 + 20 }
		};
	}

}
