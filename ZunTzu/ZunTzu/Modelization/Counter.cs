// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>Game piece.</summary>
	/// <remarks>
	/// A piece is cut from a counter section.
	/// It is always part of a stack.
	/// </remarks>
	internal sealed class Counter : Piece, ICounter {

		/// <summary>Visible side.</summary>
		/// <remarks>This value is not used if the piece is still attached to the counter section.</remarks>
		public override Side Side {
			get { return (stack.AttachedToCounterSection ? counterSection.CounterSheet.Side : side); }
			set {
				if(!stack.AttachedToCounterSection && value != side) {
					Debug.Assert(counterSection.Type == CounterSectionType.TwoSided);
					side = value;
					stack.InvalidateBoundingBox();
				}
			}
		}
		private Side side;

		/// <summary>Piece constructor.</summary>
		public Counter(int id, CounterSection counterSection, int row, int column) {
			this.id = id;
			this.counterSection = counterSection;
			this.row = row;
			this.column = column;
			side = (counterSection.Type == CounterSectionType.BackSideOnly ? Side.Back : Side.Front);
		}

		/// <summary>Counter section from which this piece is cut.</summary>
		public override ICounterSection CounterSection { get { return counterSection; } }
		private readonly CounterSection counterSection;

		/// <summary>Unique identifier for this piece.</summary>
		public override int Id { get { return id; } }
		private readonly int id;
		/// <summary>Row of this piece in the counter section.</summary>
		public override int Row { get { return row; } }
		private readonly int row;
		/// <summary>Column of this piece in the counter section.</summary>
		public override int Column { get { return column; } }
		private readonly int column;

		/// <summary>Texture sets used to display this piece.</summary>
		public override IImage FrontGraphics {
			get { return frontGraphics; }
			set { frontGraphics = value; }
		}
		private IImage frontGraphics = null;
		public override IImage BackGraphics {
			get { return backGraphics; }
			set { backGraphics = value; }
		}
		private IImage backGraphics = null;

		/// <summary>Bounding box of this piece relative to the board when attached to the counter section.</summary>
		public override RectangleF BoundingBoxWhenAttached {
			get {
				if(counterSection.CounterSheet.Side == Side.Front) {
					SizeF pieceSize = counterSection.PieceFrontSize;
					RectangleF counterSectionImageLocation = counterSection.FrontImageLocation;
					return new RectangleF(
						counterSectionImageLocation.X + column * pieceSize.Width,
						counterSectionImageLocation.Y + row * pieceSize.Height,
						pieceSize.Width,
						pieceSize.Height);
				} else {
					SizeF pieceSize = counterSection.PieceBackSize;
					RectangleF counterSectionImageLocation = counterSection.BackImageLocation;
					return new RectangleF(
						counterSectionImageLocation.Right - (column + 1) * pieceSize.Width,
						counterSectionImageLocation.Y + row * pieceSize.Height,
						pieceSize.Width,
						pieceSize.Height);
				}
			}
		}

		/// <summary>Position of the center of this piece relative to the board when attached to the counter section.</summary>
		public override PointF PositionWhenAttached {
			get {
				if((counterSection.CounterSheet.Side == Side.Front && counterSection.Type != CounterSectionType.BackSideOnly) || counterSection.Type == CounterSectionType.FrontSideOnly) {
					SizeF pieceSize = counterSection.PieceFrontSize;
					RectangleF counterSectionImageLocation = counterSection.FrontImageLocation;
					return new PointF(
						counterSectionImageLocation.X + (column + 0.5f) * pieceSize.Width,
						counterSectionImageLocation.Y + (row + 0.5f) * pieceSize.Height);
				} else {
					SizeF pieceSize = counterSection.PieceBackSize;
					RectangleF counterSectionImageLocation = counterSection.BackImageLocation;
					return new PointF(
						counterSectionImageLocation.Right - (column + 0.5f) * pieceSize.Width,
						counterSectionImageLocation.Y + (row + 0.5f) * pieceSize.Height);
				}
			}
		}
	}
}