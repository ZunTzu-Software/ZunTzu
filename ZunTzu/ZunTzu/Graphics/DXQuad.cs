// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;

namespace ZunTzu.Graphics {

	public sealed class DXQuad {
		public Texture Texture;
		public uint ModulationColor;
		public PointF Coord0;
		public PointF Coord1;
		public PointF Coord2;
		public PointF Coord3;
		public RectangleF TextureCoordinates;
	}
}
