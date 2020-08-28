// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace ZunTzu.Visualization.Dice {

	/// <summary>A model of a twelve-sided die.</summary>
	public static class D12ComplexModel {

		/// <summary>Distance from the center of a die to the table surface.</summary>
		public static float Inradius = 0.846055964155777f;

		/// <summary>An array of vertice coordinates.</summary>
		/// <remarks>The values are: x, y, z, nx, ny, nz, u, v</remarks>
		public static float[,] Vertice = {
			{ -0.722573111211913f, 0.67f, -0.755065080835204f, -0.525731112119134f, 0f, -0.85065080835204f, 0.145898033750315f, 0f },
			{ -0.0525731112119134f, 0.41408277246243f, -1.16914785329763f, -0.525731112119134f, 0f, -0.85065080835204f, 0.291796067500631f, 0.112942001776332f },
			{ -0.0525731112119134f, -0.41408277246243f, -1.16914785329763f, -0.525731112119134f, 0f, -0.85065080835204f, 0.23606797749979f, 0.295685999407889f },
			{ -0.722573111211913f, -0.67f, -0.755065080835204f, -0.525731112119134f, 0f, -0.85065080835204f, 0.0557280900008412f, 0.295685999407889f },
			{ -1.13665588367434f, 0f, -0.499147853297634f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0.112942001776332f },

			{ 1.13665588367434f, 0f, -0.499147853297634f, 0.525731112119134f, 0f, -0.85065080835204f, 0.381966011250105f, 0.408628001184222f },
			{ 0.722573111211913f, -0.67f, -0.755065080835204f, 0.525731112119134f, 0f, -0.85065080835204f, 0.23606797749979f, 0.295685999407889f },
			{ 0.0525731112119134f, -0.41408277246243f, -1.16914785329763f, 0.525731112119134f, 0f, -0.85065080835204f, 0.291796067500631f, 0.112942001776332f },
			{ 0.0525731112119134f, 0.41408277246243f, -1.16914785329763f, 0.525731112119134f, 0f, -0.85065080835204f, 0.472135954999579f, 0.112942001776332f },
			{ 0.722573111211913f, 0.67f, -0.755065080835204f, 0.525731112119134f, 0f, -0.85065080835204f, 0.527864045000421f, 0.295685999407889f },

			{ -0.41408277246243f, -1.16914785329763f, 0.0525731112119134f, 0f, -0.85065080835204f, 0.525731112119134f, 0.618033988749895f, 0f },
			{ 0.41408277246243f, -1.16914785329763f, 0.0525731112119134f, 0f, -0.85065080835204f, 0.525731112119134f, 0.76393202250021f, 0.112942001776332f },
			{ 0.67f, -0.755065080835204f, 0.722573111211913f, 0f, -0.85065080835204f, 0.525731112119134f, 0.708203932499369f, 0.295685999407889f },
			{ 0f, -0.499147853297634f, 1.13665588367434f, 0f, -0.85065080835204f, 0.525731112119134f, 0.527864045000421f, 0.295685999407889f },
			{ -0.67f, -0.755065080835204f, 0.722573111211913f, 0f, -0.85065080835204f, 0.525731112119134f, 0.472135954999579f, 0.112942001776332f },

			{ 0.41408277246243f, -1.16914785329763f, -0.0525731112119134f, 0f, -0.85065080835204f, -0.525731112119134f, 0.854101966249684f, 0.408628001184222f },
			{ -0.41408277246243f, -1.16914785329763f, -0.0525731112119134f, 0f, -0.85065080835204f, -0.525731112119134f, 0.708203932499369f, 0.295685999407889f },
			{ -0.67f, -0.755065080835204f, -0.722573111211913f, 0f, -0.85065080835204f, -0.525731112119134f, 0.76393202250021f, 0.112942001776332f },
			{ 0f, -0.499147853297634f, -1.13665588367434f, 0f, -0.85065080835204f, -0.525731112119134f, 0.944271909999159f, 0.112942001776332f },
			{ 0.67f, -0.755065080835204f, -0.722573111211913f, 0f, -0.85065080835204f, -0.525731112119134f, 1f, 0.295685999407889f },

			{ -0.755065080835204f, 0.722573111211913f, 0.67f, -0.85065080835204f, 0.525731112119134f, 0f, 0.145898033750315f, 0.295685999407889f },
			{ -0.499147853297634f, 1.13665588367434f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0.291796067500631f, 0.408628001184222f },
			{ -0.755065080835204f, 0.722573111211913f, -0.67f, -0.85065080835204f, 0.525731112119134f, 0f, 0.23606797749979f, 0.591371998815778f },
			{ -1.16914785329763f, 0.0525731112119134f, -0.41408277246243f, -0.85065080835204f, 0.525731112119134f, 0f, 0.0557280900008412f, 0.591371998815778f },
			{ -1.16914785329763f, 0.0525731112119134f, 0.41408277246243f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0.408628001184222f },

			{ -0.755065080835204f, -0.722573111211913f, 0.67f, -0.85065080835204f, -0.525731112119134f, 0f, 0.381966011250105f, 0.704314000592111f },
			{ -1.16914785329763f, -0.0525731112119134f, 0.41408277246243f, -0.85065080835204f, -0.525731112119134f, 0f, 0.23606797749979f, 0.591371998815778f },
			{ -1.16914785329763f, -0.0525731112119134f, -0.41408277246243f, -0.85065080835204f, -0.525731112119134f, 0f, 0.291796067500631f, 0.408628001184222f },
			{ -0.755065080835204f, -0.722573111211913f, -0.67f, -0.85065080835204f, -0.525731112119134f, 0f, 0.472135954999579f, 0.408628001184222f },
			{ -0.499147853297634f, -1.13665588367434f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0.527864045000421f, 0.591371998815778f },

			{ 0.755065080835204f, 0.722573111211913f, -0.67f, 0.85065080835204f, 0.525731112119134f, 0f, 0.618033988749895f, 0.295685999407889f },
			{ 0.499147853297634f, 1.13665588367434f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0.76393202250021f, 0.408628001184222f },
			{ 0.755065080835204f, 0.722573111211913f, 0.67f, 0.85065080835204f, 0.525731112119134f, 0f, 0.708203932499369f, 0.591371998815778f },
			{ 1.16914785329763f, 0.0525731112119134f, 0.41408277246243f, 0.85065080835204f, 0.525731112119134f, 0f, 0.527864045000421f, 0.591371998815778f },
			{ 1.16914785329763f, 0.0525731112119134f, -0.41408277246243f, 0.85065080835204f, 0.525731112119134f, 0f, 0.472135954999579f, 0.408628001184222f },

			{ 0.755065080835204f, -0.722573111211913f, -0.67f, 0.85065080835204f, -0.525731112119134f, 0f, 0.854101966249684f, 0.704314000592111f },
			{ 1.16914785329763f, -0.0525731112119134f, -0.41408277246243f, 0.85065080835204f, -0.525731112119134f, 0f, 0.708203932499369f, 0.591371998815778f },
			{ 1.16914785329763f, -0.0525731112119134f, 0.41408277246243f, 0.85065080835204f, -0.525731112119134f, 0f, 0.76393202250021f, 0.408628001184222f },
			{ 0.755065080835204f, -0.722573111211913f, 0.67f, 0.85065080835204f, -0.525731112119134f, 0f, 0.944271909999159f, 0.408628001184222f },
			{ 0.499147853297634f, -1.13665588367434f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 1f, 0.591371998815778f },

			{ -0.41408277246243f, 1.16914785329763f, 0.0525731112119134f, 0f, 0.85065080835204f, 0.525731112119134f, 0.145898033750315f, 0.591371998815778f },
			{ -0.67f, 0.755065080835204f, 0.722573111211913f, 0f, 0.85065080835204f, 0.525731112119134f, 0.291796067500631f, 0.704314000592111f },
			{ 0f, 0.499147853297634f, 1.13665588367434f, 0f, 0.85065080835204f, 0.525731112119134f, 0.23606797749979f, 0.887057998223668f },
			{ 0.67f, 0.755065080835204f, 0.722573111211913f, 0f, 0.85065080835204f, 0.525731112119134f, 0.0557280900008412f, 0.887057998223668f },
			{ 0.41408277246243f, 1.16914785329763f, 0.0525731112119134f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0.704314000592111f },

			{ 0.41408277246243f, 1.16914785329763f, -0.0525731112119134f, 0f, 0.85065080835204f, -0.525731112119134f, 0.381966011250105f, 1f },
			{ 0.67f, 0.755065080835204f, -0.722573111211913f, 0f, 0.85065080835204f, -0.525731112119134f, 0.23606797749979f, 0.887057998223668f },
			{ 0f, 0.499147853297634f, -1.13665588367434f, 0f, 0.85065080835204f, -0.525731112119134f, 0.291796067500631f, 0.704314000592111f },
			{ -0.67f, 0.755065080835204f, -0.722573111211913f, 0f, 0.85065080835204f, -0.525731112119134f, 0.472135954999579f, 0.704314000592111f },
			{ -0.41408277246243f, 1.16914785329763f, -0.0525731112119134f, 0f, 0.85065080835204f, -0.525731112119134f, 0.527864045000421f, 0.887057998223668f },

			{ -1.13665588367434f, 0f, 0.499147853297634f, -0.525731112119134f, 0f, 0.85065080835204f, 0.618033988749895f, 0.591371998815778f },
			{ -0.722573111211913f, -0.67f, 0.755065080835204f, -0.525731112119134f, 0f, 0.85065080835204f, 0.76393202250021f, 0.704314000592111f },
			{ -0.0525731112119134f, -0.41408277246243f, 1.16914785329763f, -0.525731112119134f, 0f, 0.85065080835204f, 0.708203932499369f, 0.887057998223668f },
			{ -0.0525731112119134f, 0.41408277246243f, 1.16914785329763f, -0.525731112119134f, 0f, 0.85065080835204f, 0.527864045000421f, 0.887057998223668f },
			{ -0.722573111211913f, 0.67f, 0.755065080835204f, -0.525731112119134f, 0f, 0.85065080835204f, 0.472135954999579f, 0.704314000592111f },

			{ 0.722573111211913f, -0.67f, 0.755065080835204f, 0.525731112119134f, 0f, 0.85065080835204f, 0.854101966249684f, 1f },
			{ 1.13665588367434f, 0f, 0.499147853297634f, 0.525731112119134f, 0f, 0.85065080835204f, 0.708203932499369f, 0.887057998223668f },
			{ 0.722573111211913f, 0.67f, 0.755065080835204f, 0.525731112119134f, 0f, 0.85065080835204f, 0.76393202250021f, 0.704314000592111f },
			{ 0.0525731112119134f, 0.41408277246243f, 1.16914785329763f, 0.525731112119134f, 0f, 0.85065080835204f, 0.944271909999159f, 0.704314000592111f },
			{ 0.0525731112119134f, -0.41408277246243f, 1.16914785329763f, 0.525731112119134f, 0f, 0.85065080835204f, 1f, 0.887057998223668f },


			{ -0.722573111211913f, 0.67f, -0.755065080835204f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ -0.0525731112119134f, 0.41408277246243f, -1.16914785329763f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ -0.0525731112119134f, -0.41408277246243f, -1.16914785329763f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ -0.722573111211913f, -0.67f, -0.755065080835204f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ -1.13665588367434f, 0f, -0.499147853297634f, -0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },

			{ 1.13665588367434f, 0f, -0.499147853297634f, 0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ 0.722573111211913f, -0.67f, -0.755065080835204f, 0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ 0.0525731112119134f, -0.41408277246243f, -1.16914785329763f, 0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ 0.0525731112119134f, 0.41408277246243f, -1.16914785329763f, 0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },
			{ 0.722573111211913f, 0.67f, -0.755065080835204f, 0.525731112119134f, 0f, -0.85065080835204f, 0f, 0f },

			{ -0.41408277246243f, -1.16914785329763f, 0.0525731112119134f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0.41408277246243f, -1.16914785329763f, 0.0525731112119134f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0.67f, -0.755065080835204f, 0.722573111211913f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0f, -0.499147853297634f, 1.13665588367434f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ -0.67f, -0.755065080835204f, 0.722573111211913f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f },

			{ 0.41408277246243f, -1.16914785329763f, -0.0525731112119134f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ -0.41408277246243f, -1.16914785329763f, -0.0525731112119134f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ -0.67f, -0.755065080835204f, -0.722573111211913f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ 0f, -0.499147853297634f, -1.13665588367434f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ 0.67f, -0.755065080835204f, -0.722573111211913f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f },

			{ -0.755065080835204f, 0.722573111211913f, 0.67f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ -0.499147853297634f, 1.13665588367434f, 0f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ -0.755065080835204f, 0.722573111211913f, -0.67f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ -1.16914785329763f, 0.0525731112119134f, -0.41408277246243f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ -1.16914785329763f, 0.0525731112119134f, 0.41408277246243f, -0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },

			{ -0.755065080835204f, -0.722573111211913f, 0.67f, -0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ -1.16914785329763f, -0.0525731112119134f, 0.41408277246243f, -0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ -1.16914785329763f, -0.0525731112119134f, -0.41408277246243f, -0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ -0.755065080835204f, -0.722573111211913f, -0.67f, -0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ -0.499147853297634f, -1.13665588367434f, 0f, -0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },

			{ 0.755065080835204f, 0.722573111211913f, -0.67f, 0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ 0.499147853297634f, 1.13665588367434f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ 0.755065080835204f, 0.722573111211913f, 0.67f, 0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ 1.16914785329763f, 0.0525731112119134f, 0.41408277246243f, 0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },
			{ 1.16914785329763f, 0.0525731112119134f, -0.41408277246243f, 0.85065080835204f, 0.525731112119134f, 0f, 0f, 0f },

			{ 0.755065080835204f, -0.722573111211913f, -0.67f, 0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ 1.16914785329763f, -0.0525731112119134f, -0.41408277246243f, 0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ 1.16914785329763f, -0.0525731112119134f, 0.41408277246243f, 0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ 0.755065080835204f, -0.722573111211913f, 0.67f, 0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },
			{ 0.499147853297634f, -1.13665588367434f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f, 0f },

			{ -0.41408277246243f, 1.16914785329763f, 0.0525731112119134f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ -0.67f, 0.755065080835204f, 0.722573111211913f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0f, 0.499147853297634f, 1.13665588367434f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0.67f, 0.755065080835204f, 0.722573111211913f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f },
			{ 0.41408277246243f, 1.16914785329763f, 0.0525731112119134f, 0f, 0.85065080835204f, 0.525731112119134f, 0f, 0f },

			{ 0.41408277246243f, 1.16914785329763f, -0.0525731112119134f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ 0.67f, 0.755065080835204f, -0.722573111211913f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ 0f, 0.499147853297634f, -1.13665588367434f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ -0.67f, 0.755065080835204f, -0.722573111211913f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f },
			{ -0.41408277246243f, 1.16914785329763f, -0.0525731112119134f, 0f, 0.85065080835204f, -0.525731112119134f, 0f, 0f },

			{ -1.13665588367434f, 0f, 0.499147853297634f, -0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ -0.722573111211913f, -0.67f, 0.755065080835204f, -0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ -0.0525731112119134f, -0.41408277246243f, 1.16914785329763f, -0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ -0.0525731112119134f, 0.41408277246243f, 1.16914785329763f, -0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ -0.722573111211913f, 0.67f, 0.755065080835204f, -0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },

			{ 0.722573111211913f, -0.67f, 0.755065080835204f, 0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ 1.13665588367434f, 0f, 0.499147853297634f, 0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ 0.722573111211913f, 0.67f, 0.755065080835204f, 0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ 0.0525731112119134f, 0.41408277246243f, 1.16914785329763f, 0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f },
			{ 0.0525731112119134f, -0.41408277246243f, 1.16914785329763f, 0.525731112119134f, 0f, 0.85065080835204f, 0f, 0f }
		};

		/// <summary>An array of triangles described as vertex indexes.</summary>
		public static Int16[,] Triangles = {
			{ 0, 4, 3 },
			{ 0, 3, 2 },
			{ 0, 2, 1 },

			{ 5, 9, 8 },
			{ 5, 8, 7 },
			{ 5, 7, 6 },

			{ 10, 14, 13 },
			{ 10, 13, 12 },
			{ 10, 12, 11 },

			{ 15, 19, 18 },
			{ 15, 18, 17 },
			{ 15, 17, 16 },

			{ 20, 24, 23 },
			{ 20, 23, 22 },
			{ 20, 22, 21 },

			{ 25, 29, 28 },
			{ 25, 28, 27 },
			{ 25, 27, 26 },

			{ 30, 34, 33 },
			{ 30, 33, 32 },
			{ 30, 32, 31 },

			{ 35, 39, 38 },
			{ 35, 38, 37 },
			{ 35, 37, 36 },

			{ 40, 44, 43 },
			{ 40, 43, 42 },
			{ 40, 42, 41 },

			{ 45, 49, 48 },
			{ 45, 48, 47 },
			{ 45, 47, 46 },

			{ 50, 54, 53 },
			{ 50, 53, 52 },
			{ 50, 52, 51 },

			{ 55, 59, 58 },
			{ 55, 58, 57 },
			{ 55, 57, 56 },


			{ 60, 61, 108 },
			{ 61, 62, 68 },
			{ 62, 63, 78 },
			{ 63, 64, 88 },
			{ 64, 60, 83 },

			{ 65, 66, 96 },
			{ 66, 67, 79 },
			{ 67, 68, 62 },
			{ 68, 69, 107 },
			{ 69, 65, 90 },

			{ 70, 71, 76 },
			{ 71, 72, 99 },
			{ 72, 73, 115 },
			{ 73, 74, 112 },
			{ 74, 70, 85 },

			{ 75, 76, 71 },
			{ 76, 77, 89 },
			{ 77, 78, 63 },
			{ 78, 79, 67 },
			{ 79, 75, 95 },

			{ 80, 81, 101 },
			{ 81, 82, 109 },
			{ 82, 83, 60 },
			{ 83, 84, 87 },
			{ 84, 80, 110 },

			{ 85, 86, 111 },
			{ 86, 87, 84 },
			{ 87, 88, 64 },
			{ 88, 89, 77 },
			{ 89, 85, 70 },

			{ 90, 91, 106 },
			{ 91, 92, 104 },
			{ 92, 93, 117 },
			{ 93, 94, 97 },
			{ 94, 90, 65 },

			{ 95, 96, 66 },
			{ 96, 97, 94 },
			{ 97, 98, 116 },
			{ 98, 99, 72 },
			{ 99, 95, 75 },

			{ 100, 101, 81 },
			{ 101, 102, 114 },
			{ 102, 103, 118 },
			{ 103, 104, 92 },
			{ 104, 100, 105 },

			{ 105, 106, 91 },
			{ 106, 107, 69 },
			{ 107, 108, 61 },
			{ 108, 109, 82 },
			{ 109, 105, 100 },

			{ 110, 111, 86 },
			{ 111, 112, 74 },
			{ 112, 113, 119 },
			{ 113, 114, 102 },
			{ 114, 110, 80 },

			{ 115, 116, 98 },
			{ 116, 117, 93 },
			{ 117, 118, 103 },
			{ 118, 119, 113 },
			{ 119, 115, 73 },

			{ 62, 78, 67 },
			{ 63, 88, 77 },
			{ 60, 108, 82 },
			{ 64, 83, 87 },
			{ 61, 68, 107 },
			{ 65, 96, 94 },
			{ 66, 79, 95 },
			{ 69, 90, 106 },
			{ 71, 99, 75 },
			{ 70, 76, 89 },
			{ 72, 115, 98 },
			{ 74, 85, 111 },
			{ 73, 112, 119 },
			{ 84, 110, 86 },
			{ 81, 109, 100 },
			{ 80, 101, 114 },
			{ 92, 117, 103 },
			{ 91, 104, 105 },
			{ 93, 97, 116 },
			{ 102, 118, 113 }
		};
	}
}
