// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ZunTzu.Modelization {

	/// <summary>Counter section, part of a counter sheet.</summary>
	/// <remarks>
	/// A counter section is basically a rectangular grid that marks the piece boundaries.
	/// All the pieces of the same section have the same size.
	/// </remarks>
	internal sealed class CounterSection : ICounterSection {
		/// <summary>Indicates if the pieces are single-sided or double-sided.</summary>
		public CounterSectionType Type { get { return type; } }
		private readonly CounterSectionType type;
		/// <summary>Indicates if the pieces are counters, blocks, or concealed.</summary>
		public CounterType CounterType { get { return counterType; } }
		private readonly CounterType counterType;
		/// <summary>Location of this grid on the counter sheet scanned image (recto).</summary>
		public RectangleF FrontImageLocation { get { return frontImageLocation; } }
		private readonly RectangleF frontImageLocation;
		/// <summary>Location of this grid on the counter sheet scanned image (verso).</summary>
		public RectangleF BackImageLocation { get { return backImageLocation; } }
		private readonly RectangleF backImageLocation;
		/// <summary>Size of the pieces of this counter section (recto side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		public SizeF PieceFrontSize { get { return pieceFrontSize; } }
		private readonly SizeF pieceFrontSize;
		/// <summary>Size of the pieces of this counter section (verso side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		public SizeF PieceBackSize { get { return pieceBackSize; } }
		private readonly SizeF pieceBackSize;
		/// <summary>Diagonal length of the pieces of this counter section (recto side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		public float PieceFrontDiagonal { get { return pieceFrontDiagonal; } }
		private readonly float pieceFrontDiagonal;
		/// <summary>Diagonal length of the pieces of this counter section (verso side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		public float PieceBackDiagonal { get { return pieceBackDiagonal; } }
		private readonly float pieceBackDiagonal;
		/// <summary>Offset of the shadow of the pieces of this counter section.</summary>
		public float ShadowLength { get { return shadowLength; } }
		private readonly float shadowLength;
		/// <summary>Number of copies of each piece.</summary>
		public int Supply { get { return supply; } }
		private readonly int supply;
		/// <summary>Thickness of the block, in %, if it's a block.</summary>
		public float BlockThickness { get { return blockThickness; } }
		private readonly float blockThickness;
		/// <summary>Percentage (0-100) of how much do you want to reduce the sticker size to create a frame around it (centered).</summary>
		public float BlockStickerReduction { get { return blockStickerReduction; } }
		private readonly float blockStickerReduction; 
		/// <summary>Color of the block, if it's a block.</summary>
		public uint BlockColor { get { return blockColor; } }
		private readonly uint blockColor;
		/// <summary>List of all pieces cut from this counter section.</summary>
		public IPiece[] Pieces { get { return pieces; } }
		private readonly Piece[] pieces;

		/// <summary>Counter sheet this section is part of.</summary>
		public ICounterSheet CounterSheet { get{ return counterSheet; } }
		private readonly CounterSheet counterSheet;

		/// <summary>CounterSection constructor.</summary>
		public CounterSection(CounterSheet counterSheet, CounterSectionProperties properties, List<Piece> pieceList) {
			this.counterSheet = counterSheet;
			type = properties.Type;
			counterType = properties.CounterType;
			blockThickness = properties.BlockThickness;
			blockStickerReduction = properties.BlockStickerReduction;
			blockColor = properties.BlockColor;
			frontImageLocation = properties.FrontImageLocation;
			for(int i = (int)counterSheet.Properties.FrontImageResolution; i > 0; --i)
				frontImageLocation = new RectangleF(frontImageLocation.X * 2, frontImageLocation.Y * 2, frontImageLocation.Width * 2, frontImageLocation.Height * 2);
			backImageLocation = properties.BackImageLocation;
			for(int i = (int)counterSheet.Properties.BackImageResolution; i > 0; --i)
				backImageLocation = new RectangleF(backImageLocation.X * 2, backImageLocation.Y * 2, backImageLocation.Width * 2, backImageLocation.Height * 2);

			// create pieces for that section
			pieceFrontSize = new SizeF(
				frontImageLocation.Width / properties.Columns,
				frontImageLocation.Height / properties.Rows);
			pieceBackSize = new SizeF(
				backImageLocation.Width / properties.Columns,
				backImageLocation.Height / properties.Rows);
			pieceFrontDiagonal = (float) Math.Sqrt(pieceFrontSize.Width * pieceFrontSize.Width + pieceFrontSize.Height * pieceFrontSize.Height);
			pieceBackDiagonal = (float) Math.Sqrt(pieceBackSize.Width * pieceBackSize.Width + pieceBackSize.Height * pieceBackSize.Height);
			shadowLength = properties.ShadowLength;
			supply = properties.Supply;
			pieces = new Piece[properties.Rows * properties.Columns * properties.Supply];
			for(int row = 0; row < properties.Rows; ++row) {
				for(int col = 0; col < properties.Columns; ++col) {
					for(int copy = 0; copy < properties.Supply; ++copy) {
						Piece piece = (counterSheet.Properties.Type == CounterSheetType.Terrain ?
							(Piece) new TerrainPrototype(pieceList.Count, this, row, col) :
							(Piece) new Counter(pieceList.Count, this, row, col));
						pieces[(row * properties.Columns + col) * properties.Supply + copy] = piece;
						pieceList.Add(piece);
					}
				}
			}
		}

		/// <summary>CounterSection constructor.</summary>
		public CounterSection(CounterSheet counterSheet, CardSectionProperties properties, List<Piece> pieceList) {
			this.counterSheet = counterSheet;
			type = properties.Type;
			frontImageLocation = properties.FaceImageLocation;
			for(int i = (int) (HasCardFaceOnFront ? counterSheet.Properties.FrontImageResolution : counterSheet.Properties.BackImageResolution); i > 0; --i)
				frontImageLocation = new RectangleF(frontImageLocation.X * 2, frontImageLocation.Y * 2, frontImageLocation.Width * 2, frontImageLocation.Height * 2);
			backImageLocation = properties.BackImageLocation;
			for(int i = (int) (HasCardBackOnFront ? counterSheet.Properties.FrontImageResolution : counterSheet.Properties.BackImageResolution); i > 0; --i)
				backImageLocation = new RectangleF(backImageLocation.X * 2, backImageLocation.Y * 2, backImageLocation.Width * 2, backImageLocation.Height * 2);

			// create pieces for that section
			pieceFrontSize = new SizeF(
				frontImageLocation.Width / properties.Columns,
				frontImageLocation.Height / properties.Rows);
			pieceBackSize = backImageLocation.Size;
			pieceFrontDiagonal = (float) Math.Sqrt(pieceFrontSize.Width * pieceFrontSize.Width + pieceFrontSize.Height * pieceFrontSize.Height);
			pieceBackDiagonal = (float) Math.Sqrt(pieceBackSize.Width * pieceBackSize.Width + pieceBackSize.Height * pieceBackSize.Height);
			shadowLength = properties.ShadowLength;
			supply = properties.Supply;
			pieces = new Piece[properties.Rows * properties.Columns * properties.Supply];
			for(int row = 0; row < properties.Rows; ++row) {
				for(int col = 0; col < properties.Columns; ++col) {
					for(int copy = 0; copy < properties.Supply; ++copy) {
						Piece piece = new Card(pieceList.Count, this, row, col);
						pieces[(row * properties.Columns + col) * properties.Supply + copy] = piece;
						pieceList.Add(piece);
					}
				}
			}
		}

		// Tests about the counter section type

		public bool ContainsCounters { get { return type < CounterSectionType.CardFacesOnFront; } }
		public bool IsSingleSided { get { return type != CounterSectionType.TwoSided && type < CounterSectionType.CardFacesAndBackOnFront; } }
		public bool HasCardFaceOnFront { get { return !ContainsCounters && (((int) type & 1) == 0); } }
		public bool HasCardBackOnFront { get { return type == CounterSectionType.CardFacesAndBackOnFront || type == CounterSectionType.CardFacesOnBackBackOnOtherSide; } }

		//public bool ContainsCards { get { return !ContainsCounters; } }
		//public bool ContainsSingleSidedCards { get { return ContainsCards && type < CounterSectionType.CardFacesAndBackOnFront; } }
		//public bool HasCardFaceOnBack { get { return ContainsCards && (((int) type & 1) == 1); } }
		//public bool HasCardBackOnBack { get { return type == CounterSectionType.CardFacesAndBackOnBack || type == CounterSectionType.CardFacesOnFrontBackOnOtherSide; } }
	}
}
