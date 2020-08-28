// Copyright (c) 2020 ZunTzu Software and contributors

using System;
//using System.Collections;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {

	/// <summary>Stack of pieces.</summary>
	/// <remarks>
	/// A player selects and moves stacks of pieces.
	/// A Piece that stands alone is actually part of a stack which contains only that piece.
	/// </remarks>
	internal sealed class Stack : IStack {
		public SizeF IndentationBetweenPieces {
			get {
				return (pieces[0] is ICard ? new SizeF(5.0f, 5.0f) : new SizeF(40.0f, 40.0f));
			}
		}

		/// <summary>Unique identifier for this stack.</summary>
		public int Id { get { return pieces[0].Id; } }
		/// <summary>Indicates if the stack is for a piece still attached to the counter sheet.</summary>
		public bool AttachedToCounterSection {
			get { return attachedToCounterSection; }
			set {
				if(value != attachedToCounterSection) {
					attachedToCounterSection = value;
					if(attachedToCounterSection)
						foreach(Piece piece in pieces)
							piece.RotationAngle = 0.0f;
					boundingBoxIsObsolete = true;
				}
			}
		}
		private bool attachedToCounterSection = false;

		/// <summary>Board on which this stack stays.</summary>
		IBoard IStack.Board { get { return Board; } }
		public Board Board {
			get { return board; }
			set { board = value; }
		}
		private Board board = null;

		/// <summary>Position relative to the board.</summary>
		public PointF Position {
			get {
				if(attachedToCounterSection) {
					return pieces[0].Position;
				} else {
					return position;
				}
			}
			set {
				if(value != position) {
					position = value;
					boundingBoxIsObsolete = true;
				}
			}
		}
		private PointF position = new PointF(0.0f, 0.0f);

		/// <summary>List of pieces in this stack.</summary>
		/// <remarks>The pieces are sorted back to front.</remarks>
		public IPiece[] Pieces { get { return pieces; } }
		private Piece[] pieces = null;

		/// <summary>Rearrange pieces inside that stack.</summary>
		/// <param name="newArrangement">New ordering for the pieces in this stack.</param>
		internal void RearrangePieces(IPiece[] newArrangement) {
			pieces = new Piece[newArrangement.Length];
			newArrangement.CopyTo(pieces, 0);
			boundingBoxIsObsolete = true;
		}
		/// <summary>Gets the selection encompassing all the pieces of this stack.</summary>
		/// <returns>Selection object.</returns>
		public ISelection Select() {
			return new Selection(this);
		}
		/// <summary>Adds the selected pieces at a given position in this stack.</summary>
		/// <param name="selectedStack">Stack currently containing the selected pieces.</param>
		/// <param name="selectedPieces">Pieces to add.</param>
		/// <param name="toIndex">New index of the bottom-most piece to add.</param>
		internal void MergeToPosition(IStack selectedStack, IPiece[] selectedPieces, int toIndex) {
			Debug.Assert(selectedStack != this && selectedPieces.Length > 0 && toIndex <= pieces.Length);

			Piece[] updatedList = new Piece[pieces.Length + selectedPieces.Length];
			Array.Copy(pieces, updatedList, toIndex);
			selectedPieces.CopyTo(updatedList, toIndex);
			Array.Copy(pieces, toIndex, updatedList, toIndex + selectedPieces.Length, pieces.Length - toIndex);
			pieces = updatedList;

			foreach(Piece piece in selectedPieces)
				piece.Stack = this;

			boundingBoxIsObsolete = true;
		}
		/// <summary>Splits the pieces of this stack between two stacks.</summary>
		/// <param name="selectedPieces">Pieces that will form the new stack.</param>
		/// <param name="newStack">The newly created stack.</param>
		internal void Split(IPiece[] selectedPieces, IStack newStack) {
			Debug.Assert(newStack != this && selectedPieces.Length > 0);

			Piece[] updatedList = new Piece[pieces.Length - selectedPieces.Length];
			int updatedListIndex = 0;
			foreach(Piece piece in pieces) {
				// is it contained in selectedPieces?
				bool found = false;
				foreach(IPiece selectedPiece in selectedPieces) {
					if(selectedPiece == piece) {
						found = true;
						break;
					}
				}
				if(!found)
					updatedList[updatedListIndex++] = piece;
			}
			pieces = updatedList;
			boundingBoxIsObsolete = true;

			Piece[] newList = new Piece[selectedPieces.Length];
			for(int i = 0; i < newList.Length; ++i)
				newList[i] = (Piece) selectedPieces[i];

			Stack stack = (Stack) newStack;
			stack.pieces = newList;
			foreach(Piece piece in newList)
				piece.Stack = stack;
			stack.boundingBoxIsObsolete = true;
		}

		/// <summary>Stack constructor.</summary>
		/// <remarks>The stack is not fully created.</remarks>
		public Stack() {}
		/// <summary>Stack constructor.</summary>
		/// <param name="piece">Piece in this stack at construction time.</param>
		/// <remarks>The stack is created attached to the counter section.</remarks>
		public Stack(Piece piece) {
			pieces = new Piece[1] { piece };
			piece.Stack = this;
			attachedToCounterSection = true;
		}
		/// <summary>private Stack constructor.</summary>
		/// <param name="pieces">Pieces in this stack at construction time.</param>
		/// <remarks>The stack is created attached to the counter section.</remarks>
		//private Stack(Piece[] pieces) {
		//	this.pieces = pieces;
		//	foreach(Piece piece in pieces)
		//		piece.Stack = this;
		//}

		/// <summary>Smallest rectangular area which contains this stack.</summary>
		public RectangleF BoundingBox {
			get {
				if(boundingBoxIsObsolete)
					updateBoundingBox();
				return boundingBox;
			}
		}
		/// <summary>Smallest rectangular area which contains this stack.</summary>
		/// <remarks>This data is precalculated. You must call <c>InvalidateBoundingBox</c> if the value is obsolete.</remarks>
		private RectangleF boundingBox = RectangleF.Empty;

		/// <summary>Triggers the recomputation of the bounding box.</summary>
		/// <remarks>
		/// This method is typically called after a piece has been rotated or flipped.
		/// </remarks>
		public void InvalidateBoundingBox() {
			boundingBoxIsObsolete = true;
		}
		private bool boundingBoxIsObsolete = true;

		/// <summary>Recomputes the precalculated bounding box of this stack.</summary>
		/// <remarks>
		/// This method is called each time the bounding box becomes obsolete, i.e. when the stacks moves or when the piece list changes.
		/// This method doesn't need to be called if the stack is still attached to the counter section.
		/// </remarks>
		private void updateBoundingBox() {
			if(pieces == null) {
				// bounding box of an empty stack is a zero-sized rectangle
				boundingBox = RectangleF.Empty;
			} else {
				if(attachedToCounterSection) {
					// bounding box of the stack is bounding box of the only piece in the stack
					Piece piece = pieces[0];
					piece.BoundingBox = piece.BoundingBoxWhenAttached;
					piece.Position = piece.PositionWhenAttached;
					boundingBox = piece.BoundingBox;

				} else {
					// bounding box of the stack is union of bounding box of each piece

					boundingBox = new RectangleF(position, new SizeF(0.0f, 0.0f));
					PointF lowerLeftCorner = new PointF(0.0f, 0.0f);
					SizeF indentationBetweenPieces = IndentationBetweenPieces;
					float unfoldedIndentationCoefficient = (!unfolded || !(pieces[0] is ICard) ? 0.6f : 0.6f * (pieces.Length > 1 ? (float) Math.Pow(0.65f, Math.Log(pieces.Length - 1, 2.0)) : 1.0f));

					for(int i = 0; i < pieces.Length; ++i) {
						Piece piece = pieces[i];
						SizeF size = piece.Size;

						if(i == 0) {
							lowerLeftCorner = new PointF(
								position.X - size.Width * 0.5f,
								position.Y + size.Height * 0.5f);
						} else if(!unfolded) {
							lowerLeftCorner.X += indentationBetweenPieces.Width;
							lowerLeftCorner.Y -= indentationBetweenPieces.Height;
						} else {
							SizeF sizeOfpieceBelow = pieces[i - 1].Size;
							if(pieces[i - 1].RolledOver) {
								lowerLeftCorner.X += sizeOfpieceBelow.Width * 0.6f;
								lowerLeftCorner.Y -= (!(pieces[0] is ICard) ? sizeOfpieceBelow.Height * 0.6f : indentationBetweenPieces.Height);
							} else {
								lowerLeftCorner.X += sizeOfpieceBelow.Width * unfoldedIndentationCoefficient;
								lowerLeftCorner.Y -= (!(pieces[0] is ICard) ? sizeOfpieceBelow.Height * unfoldedIndentationCoefficient : indentationBetweenPieces.Height);
							}
						}

						piece.Position = new PointF(
							lowerLeftCorner.X + size.Width * 0.5f,
							lowerLeftCorner.Y - size.Height * 0.5f);

						// compute bounding box for this piece

						PointF rotatedCoord0;
						PointF rotatedCoord1;
						if(piece.RotationAngle != 0.0f) {
							// rotation:
							// x <- x * cos - y * sin
							// y <- x * sin + y * cos
							float sin = (float) Math.Sin(-piece.RotationAngle);
							float cos = (float) Math.Cos(-piece.RotationAngle);

							rotatedCoord0 = new PointF(
								(-size.Width * 0.5f) * cos - (size.Height * 0.5f) * sin,
								(-size.Width * 0.5f) * sin + (size.Height * 0.5f) * cos);

							rotatedCoord1 = new PointF(
								(size.Width * 0.5f) * cos - (size.Height * 0.5f) * sin,
								(size.Width * 0.5f) * sin + (size.Height * 0.5f) * cos);
						} else {
							rotatedCoord0 = new PointF(
								-size.Width * 0.5f,
								-size.Height * 0.5f);

							rotatedCoord1 = new PointF(
								size.Width * 0.5f,
								-size.Height * 0.5f);
						}

						float xMax = Math.Max(Math.Abs(rotatedCoord0.X), Math.Abs(rotatedCoord1.X));
						float yMax = Math.Max(Math.Abs(rotatedCoord0.Y), Math.Abs(rotatedCoord1.Y));

						piece.BoundingBox = new RectangleF(
							piece.Position.X - xMax,
							piece.Position.Y - yMax,
							xMax * 2.0f + piece.CounterSection.ShadowLength,
							yMax * 2.0f + piece.CounterSection.ShadowLength);

						boundingBox = RectangleF.Union(boundingBox, piece.BoundingBox);
					}
				}
			}
			boundingBoxIsObsolete = false;
		}

		/// <summary>Indicates if a mouse cursor is over the stack.</summary>
		public bool Unfolded {
			get { return unfolded; }
			set {
				if(value != unfolded) {
					unfolded = value;
					if(pieces.Length > 1)
						boundingBoxIsObsolete = true;
				}
			}
		}
		private bool unfolded = false;
	}
}
