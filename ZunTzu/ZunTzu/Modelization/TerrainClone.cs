// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>An instance of terrain cloned from a terrain prototype.</summary>
	/// <remarks>
	/// A terrain clone is created based on a terrain prototype.
	/// It is never attached.
	/// </remarks>
	internal sealed class TerrainClone : Piece, ITerrainClone {

		/// <summary>Prototype of this piece.</summary>
		public ITerrainPrototype Prototype { get { return prototype; } }
		private readonly TerrainPrototype prototype;

		/// <summary>Visible side.</summary>
		/// <remarks>This value is not used if the piece is still attached to the counter section.</remarks>
		public override Side Side {
			get { return side; }
			set {
				if(value != side) {
					Debug.Assert(CounterSection.Type == CounterSectionType.TwoSided);
					side = value;
					stack.InvalidateBoundingBox();
				}
			}
		}
		private Side side;

		/// <summary>Piece constructor.</summary>
		public TerrainClone(TerrainPrototype prototype) {
			this.prototype = prototype;
			side = (CounterSection.Type == CounterSectionType.BackSideOnly ? Side.Back : Side.Front);
			stack.AttachedToCounterSection = false;
		}

		/// <summary>Piece constructor.</summary>
		public TerrainClone(TerrainClone prototype) {
			this.prototype = (TerrainPrototype) prototype.Prototype;
			rotationAngle = prototype.rotationAngle;
			side = prototype.Side;
			stack.AttachedToCounterSection = false;
		}

		/// <summary>Counter section from which this piece is cut.</summary>
		public override ICounterSection CounterSection { get { return prototype.CounterSection; } }

		/// <summary>Unique identifier for this piece.</summary>
		public override int Id { get { return prototype.Id; } }

		/// <summary>Row of this piece in the counter section.</summary>
		public override int Row { get { return prototype.Row; } }

		/// <summary>Column of this piece in the counter section.</summary>
		public override int Column { get { return prototype.Column; } }

		/// <summary>Texture sets used to display this piece.</summary>
		public override IImage FrontGraphics {
			get { return prototype.FrontGraphics; }
			set { throw new InvalidOperationException(); }
		}
		public override IImage BackGraphics {
			get { return prototype.BackGraphics; }
			set { throw new InvalidOperationException(); }
		}

		/// <summary>Bounding box of this piece relative to the board when attached to the counter section.</summary>
		public override RectangleF BoundingBoxWhenAttached {
			get { return prototype.BoundingBoxWhenAttached; }
		}

		/// <summary>Position of the center of this piece relative to the board when attached to the counter section.</summary>
		public override PointF PositionWhenAttached {
			get { return prototype.PositionWhenAttached; }
		}
	}
}