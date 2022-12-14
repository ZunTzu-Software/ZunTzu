// Copyright (c) 2022 ZunTzu Software and contributors

using System;
//using System.Diagnostics;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>Game piece.</summary>
	/// <remarks>
	/// A piece is cut from a counter section.
	/// It is always part of a stack.
	/// </remarks>
	internal abstract class Piece : IPiece
	{

		/// <summary>Stack that contains this piece.</summary>
		/// <remarks>A piece is always part of a stack.</remarks>
		IStack IPiece.Stack { get { return Stack; } }
		public Stack Stack
		{
			get { return stack; }
			set { stack = value; }
		}
		protected Stack stack;

		/// <summary>Rotation angle in Radians.</summary>
		/// <remarks>
		/// The value for an upside up position is zero.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		public float RotationAngle
		{
			get { return rotationAngle; }
			set
			{
				if (value != rotationAngle)
				{
					rotationAngle = value;
					stack.InvalidateBoundingBox();
				}
			}
		}
		protected float rotationAngle = 0.0f;

		/// <summary>Cosinus value of the flip angle.</summary>
		/// <remarks>
		/// The value for a piece at rest (i.e. not flipping) is 1.0f.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		public float FlipAngleCosinus
		{
			get { return (stack.AttachedToCounterSection ? 1.0f : flipAngleCosinus); }
			set { flipAngleCosinus = value; }
		}
		private float flipAngleCosinus = 1.0f;

		/// <summary>Visible side.</summary>
		/// <remarks>This value is not used if the piece is still attached to the counter section.</remarks>
		public abstract Side Side { get; set; }

		/// <summary>Gets the selection encompassing this piece only.</summary>
		/// <returns>Selection object.</returns>
		public ISelection Select()
		{
			return new Selection(this);
		}

		/// <summary>Piece constructor.</summary>
		public Piece()
		{
			// a stack is immediately created for this piece
			stack = new Stack(this);
		}

		/// <summary>Counter section from which this piece is cut.</summary>
		public abstract ICounterSection CounterSection { get; }

		/// <summary>Unique identifier for this piece.</summary>
		public abstract int Id { get; }

		/// <summary>Row of this piece in the counter section.</summary>
		public abstract int Row { get; }

		/// <summary>Column of this piece in the counter section.</summary>
		public abstract int Column { get; }

		/// <summary>Size of this piece (recto and verso).</summary>
		public SizeF Size { get { return (Side == Side.Front ? CounterSection.PieceFrontSize : CounterSection.PieceBackSize); } }
		public float Diagonal { get { return (Side == Side.Front ? CounterSection.PieceFrontDiagonal : CounterSection.PieceBackDiagonal); } }

		//public float BlockThickness { get { return (CounterSection.BlockThickness == float.MinValue ? Diagonal * 0.99f / 4 for dpi150 and /2 for dpi300 : (Diagonal * CounterSection.BlockThickness/ 100.0f)/4 or 2 for dpis 250 and 300); } }
		//TODO: It's not clear what related measure is the BlockThickness (used in the RenderBlock method - View.cs)
		public float BlockThickness { get { return (CounterSection.BlockThickness == float.MinValue ? 100.0f : CounterSection.BlockThickness); } }

		public uint BlockColor { get { return CounterSection.BlockColor; } }

		/// <summary>Texture sets used to display this piece.</summary>
		public abstract IImage FrontGraphics { get; set; }
		public abstract IImage BackGraphics { get; set; }
		public IImage Graphics
		{
			get { return (Side == Side.Front ? FrontGraphics : BackGraphics); }
		}

		/// <summary>Smallest rectangular area which contains this piece.</summary>
		RectangleF IPiece.BoundingBox { get { return boundingBox; } }
		public RectangleF BoundingBox
		{
			get { return boundingBox; }
			set { boundingBox = value; }
		}
		/// <summary>Smallest rectangular area which contains this piece.</summary>
		/// <remarks>This data is precalculated. You must call <c>Stack.InvalidateBoundingBox</c> if the value is obsolete.</remarks>
		private RectangleF boundingBox;

		/// <summary>Position of the center of this piece relative to the board.</summary>
		PointF IPiece.Position { get { return position; } }
		public PointF Position
		{
			get { return position; }
			set { position = value; }
		}
		/// <summary>Position of the center of this piece relative to the board.</summary>
		/// <remarks>This data is precalculated. You must call <c>Stack.InvalidateBoundingBox</c> if the value is obsolete.</remarks>
		private PointF position;

		/// <summary>Bounding box of this piece relative to the board when attached to the counter section.</summary>
		public abstract RectangleF BoundingBoxWhenAttached { get; }

		/// <summary>Position of the center of this piece relative to the board when attached to the counter section.</summary>
		public abstract PointF PositionWhenAttached { get; }

		/// <summary>Position of this piece in its stack relative to the other pieces.</summary>
		public int IndexInStackFromBottomToTop
		{
			get
			{
				IPiece[] pieces = stack.Pieces;
				for (int i = 0; i < pieces.Length; ++i)
					if (pieces[i] == this)
						return i;
				throw new ApplicationException("Piece not in stack.");
			}
		}

		/// <summary>Indicates if a player as moved his mouse cursor over this card.</summary>
		public bool RolledOver
		{
			get { return rolledOver; }
			set
			{
				if (value != rolledOver)
				{
					rolledOver = value;
					stack.InvalidateBoundingBox();
				}
			}
		}
		private bool rolledOver = false;

		/// <summary>The only player who can see this piece, or Guid.Empty.</summary>
		public Guid Owner { get { return owner; } set { owner = value; } }
		private Guid owner = Guid.Empty;

		public bool IsBlock
		{
			get
			{
				if (this.CounterSection.CounterType == CounterType.Block)
					return true;
				else
					return false;
			}
		}

		public float BlockAddedFrame
        {
			get
			{
				if (this.CounterSection.CounterType == CounterType.Block)
					return this.CounterSection.BlockAddedFrame / 100.0f;
				else
					return 0.0f;
			}
		}
	}
}