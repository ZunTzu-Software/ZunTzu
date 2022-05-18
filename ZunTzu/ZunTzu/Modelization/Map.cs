// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>Map.</summary>
	/// <remarks>
	/// A map is a rectangular piece of printed paper or cardboard on which pieces will be moved.
	/// Only one side of a map is used (the printed side).
	/// </remarks>
	internal sealed class Map : Board, IMap {
		/// <summary>Static properties of this map.</summary>
		public MapProperties Properties { get { return properties; } }
		private readonly MapProperties properties;

		/// <summary>Texture sets used to display this map.</summary>
		public ITileSet BackgroundGraphics {
			get { return backgroundGraphics; }
			set { backgroundGraphics = value; }
		}
		private ITileSet backgroundGraphics = null;

		/// <summary>Map constructor.</summary>
		public Map(int id, MapProperties properties) : base(id) {
			this.properties = properties;
			base.Name = (properties != null ? properties.Name : "");
		}

		/// <summary>Total area of this board.</summary>
		/// <remarks>Area outside of this area will be displayed in black.</remarks>
		public override RectangleF TotalArea {
			get { return new RectangleF(new PointF(0.0f, 0.0f),  backgroundGraphics.Size); }
		}
	}
}
