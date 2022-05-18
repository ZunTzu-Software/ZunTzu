// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>Counter sheet.</summary>
	/// <remarks>
	/// A counter sheet is a rectangular piece of cardboard from which a pieces are cut from.
	/// Both sides of a counter sheet are used, allowing for double-sided pieces.
	/// For convenience, a counter sheet also acts as a regular board.
	/// </remarks>
	internal sealed class CounterSheet : Board, ICounterSheet {
		/// <summary>Static properties of this counter sheet.</summary>
		public CounterSheetProperties Properties { get { return properties; } }
		private readonly CounterSheetProperties properties;
		/// <summary>Side currently visible.</summary>
		public Side Side {
			get { return side; }
			set {
				if(value != side) {
					side = value;
					foreach(Stack stack in this.Stacks)
						stack.InvalidateBoundingBox();
				}
			}
		}
		private Side side = Side.Front;
		/// <summary>Texture sets used to display the recto side of this sheet.</summary>
		public ITileSet FrontGraphics {
			get { return frontGraphics; }
			set { frontGraphics = value; }
		}
		private ITileSet frontGraphics = null;
		/// <summary>Texture sets used to display the verso side of this sheet.</summary>
		public ITileSet BackGraphics {
			get { return backGraphics; }
			set { backGraphics = value; }
		}
		private ITileSet backGraphics = null;
		/// <summary>List of all the counter sections "precut" in this sheet.</summary>
		public ICounterSection[] CounterSections { get { return counterSections; } }
		private readonly CounterSection[] counterSections;

		public CounterSheet(int id, CounterSheetProperties properties, List<Piece> pieceList) : base(id) {
			this.properties = properties;
			base.Name = properties.Name;
			counterSections = new CounterSection[properties.CounterSections.Length + properties.CardSections.Length];
			for(int i = 0; i < properties.CounterSections.Length; ++i)
				counterSections[i] = new CounterSection(this, properties.CounterSections[i], pieceList);
			for(int i = 0; i < properties.CardSections.Length; ++i)
				counterSections[properties.CounterSections.Length + i] = new CounterSection(this, properties.CardSections[i], pieceList);
		}

		/// <summary>Total area of this board.</summary>
		/// <remarks>Area outside of this area will be displayed in black.</remarks>
		public override RectangleF TotalArea {
			get {
				return new RectangleF(new PointF(0.0f, 0.0f),
					(side == Side.Front ? frontGraphics : backGraphics).Size);
			}
		}
	}
}
