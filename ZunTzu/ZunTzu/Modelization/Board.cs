// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using ZunTzu.Graphics;

namespace ZunTzu.Modelization {
	/// <summary>Board.</summary>
	/// <remarks>This is the base class for maps and counter sheets</remarks>
	internal abstract class Board : IBoard {
		/// <summary>Name of this board.</summary>
		public string Name { get { return name; } set { name = value; } }
		/// <summary>Puts a stack on this board, in front of all others.</summary>
		/// <param name="stack">Stack to insert.</param>
		internal void MoveStackToFront(IStack stack) {
			MoveStackToZOrder(stack, (stack.Board == this ? Stacks.Count - 1 : Stacks.Count));
		}
		/// <summary>Puts a stack on this board, behind all others.</summary>
		/// <param name="stack">Stack to insert.</param>
		internal void MoveStackToBack(IStack stack) {
			MoveStackToZOrder(stack, 0);
		}
		/// <summary>Puts a stack on this board, at the given order relative to the other stacks.</summary>
		/// <param name="stack">Stack to insert.</param>
		/// <remarks>zOrder is zero for the most backward stack.</remarks>
		internal void MoveStackToZOrder(IStack stack, int zOrder) {
			if(stack.Board != this || zOrder != GetZOrder(stack)) {
				if(stack.Board != null) {
					((Board)stack.Board).RemoveStack(stack);
				}
				Stacks.Insert(zOrder, (Stack)stack);
				((Stack)stack).Board = this;
			}
		}
		/// <summary>Removes a stack from this board.</summary>
		/// <param name="stack">Stack to remove.</param>
		internal void RemoveStack(IStack stack) {
			Stacks.Remove((Stack)stack);
			((Stack)stack).Board = null;
		}
		/// <summary>Returns the z-order of this stack, i.e. how many stacks are behind it.</summary>
		/// <param name="stack">Stack.</param>
		public int GetZOrder(IStack stack) {
			return Stacks.IndexOf((Stack)stack);
		}

		/// <summary>Returns all the unfolded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from top to bottom.</remarks>
		public void FillListWithUnfoldedStacksWithinAreaFrontToBack(List<IStack> list, RectangleF area) {
			list.Clear();
			for(int i = Stacks.Count - 1; i >= 0; --i) {
				Stack stack = Stacks[i];
				if(stack.Unfolded && area.IntersectsWith(stack.BoundingBox))
					list.Add(stack);
			}
		}

		/// <summary>Returns all the unfolded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from bottom to top.</remarks>
		public void FillListWithUnfoldedStacksWithinAreaBackToFront(List<IStack> list, RectangleF area) {
			list.Clear();
			for(int i = 0; i < Stacks.Count; ++i) {
				Stack stack = Stacks[i];
				if(stack.Unfolded && area.IntersectsWith(stack.BoundingBox))
					list.Add(stack);
			}
		}

		/// <summary>Returns all the folded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from bottom to top.</remarks>
		public void FillListWithFoldedStacksWithinAreaBackToFront(List<IStack> list, RectangleF area) {
			list.Clear();
			for(int i = 0; i < Stacks.Count; ++i) {
				Stack stack = Stacks[i];
				if(!stack.Unfolded && area.IntersectsWith(stack.BoundingBox))
					list.Add(stack);
			}
		}

		/// <summary>Returns the top-most piece (but not terrain) at a given position on this board.</summary>
		/// <param name="position">Position in local coordinates.</param>
		/// <returns>A piece or null.</returns>
		public IPiece GetPieceAtPosition(PointF position) {
			// for each layer (bottom layer is for unpunched pieces, next layer is for punched cards, top layer is for punched counters)
			for(int layer = 2; layer >= 0; --layer) {
				for(int pass = 0; pass < 2; ++pass) {
					for(int i = Stacks.Count - 1; i >= 0; --i) {
						Stack stack = Stacks[i];
						IPiece[] pieces = stack.Pieces;
						if(stack.Unfolded == (pass == 0) &&
							!(pieces[0] is ITerrain) &&
							((layer == 0 && stack.AttachedToCounterSection) ||
							(layer == 1 && !stack.AttachedToCounterSection && pieces[0] is ICard) ||
							(layer == 2 && !stack.AttachedToCounterSection && pieces[0] is ICounter)) &&
							stack.BoundingBox.Contains(position))
						{
							// ignore attached pieces on the opposite side
							ICounterSection counterSection = pieces[0].CounterSection;
							CounterSectionType type = counterSection.Type;
							if(!stack.AttachedToCounterSection ||
								(counterSection.ContainsCounters && ((int) type & ((int) ((ICounterSheet) this).Side + 1)) != 0) ||
								(!counterSection.ContainsCounters && counterSection.HasCardFaceOnFront == (((ICounterSheet) this).Side == Side.Front)))
							{
								for(int j = pieces.Length - 1; j >= 0; --j) {
									IPiece piece = pieces[j];
									if(piece.BoundingBox.Contains(position)) {
										// we have to compute the model coordinates of the position
										// relative to the counter sheet

										// we have to handle rotations of the piece
										// apply the inverse rotation to the position
										PointF piecePosition = piece.Position;
										PointF transformedPosition = new PointF(
											position.X - piecePosition.X,
											position.Y - piecePosition.Y);

										if(piece.RotationAngle != 0.0f) {
											// rotation:
											// x <- x * cos - y * sin
											// y <- x * sin + y * cos
											float sin = (float) Math.Sin(piece.RotationAngle);
											float cos = (float) Math.Cos(piece.RotationAngle);

											transformedPosition = new PointF(
												transformedPosition.X * cos - transformedPosition.Y * sin,
												transformedPosition.X * sin + transformedPosition.Y * cos);
										}

										SizeF size = piece.Size;
										if(new RectangleF(
											-size.Width * 0.5f,
											-size.Height * 0.5f,
											size.Width,
											size.Height).Contains(transformedPosition)) {
											// is the piece completely transparent at that location?
											if(piece.Graphics != null) {
												uint color = piece.Graphics.GetColorAtPosition(transformedPosition);
												if((color & 0xFF000000) != 0x00000000) {
													// no it is not
													if(!stack.Unfolded && piece is ICard)
														return pieces[pieces.Length - 1];
													else
														return piece;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}

		/// <summary>Returns the top-most terrain at a given position on this board.</summary>
		/// <param name="position">Position in local coordinates.</param>
		/// <returns>A piece or null.</returns>
		public ITerrain GetTerrainAtPosition(PointF position) {
			// for each layer (bottom layer is for unpunched pieces, top layer is for punched counters)
			for(int layer = 1; layer >= 0; --layer) {
				for(int pass = 0; pass < 2; ++pass) {
					for(int i = Stacks.Count - 1; i >= 0; --i) {
						Stack stack = Stacks[i];
						ITerrain piece = stack.Pieces[0] as ITerrain;
						if(piece != null &&
							stack.Unfolded == (pass == 0) &&
							((layer == 0 && stack.AttachedToCounterSection) ||
							(layer == 1 && !stack.AttachedToCounterSection)) &&
							stack.BoundingBox.Contains(position))
						{
							if(piece.BoundingBox.Contains(position)) {
								// we have to compute the model coordinates of the position
								// relative to the counter sheet

								// we have to handle rotations of the piece
								// apply the inverse rotation to the position
								PointF piecePosition = piece.Position;
								PointF transformedPosition = new PointF(
									position.X - piecePosition.X,
									position.Y - piecePosition.Y);

								if(piece.RotationAngle != 0.0f) {
									// rotation:
									// x <- x * cos - y * sin
									// y <- x * sin + y * cos
									float sin = (float) Math.Sin(piece.RotationAngle);
									float cos = (float) Math.Cos(piece.RotationAngle);

									transformedPosition = new PointF(
										transformedPosition.X * cos - transformedPosition.Y * sin,
										transformedPosition.X * sin + transformedPosition.Y * cos);
								}

								SizeF size = piece.Size;
								if(new RectangleF(
									-size.Width * 0.5f,
									-size.Height * 0.5f,
									size.Width,
									size.Height).Contains(transformedPosition)) {
									// is the piece completely transparent at that location?
									if(piece.Graphics != null) {
										uint color = piece.Graphics.GetColorAtPosition(transformedPosition);
										if((color & 0xFF000000) != 0x00000000) {
											// no it is not
											return piece;
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}

		/// <summary>Id of this board in the current scenario.</summary>
		public int Id { get { return id; } }
		/// <summary>Visible area of this board.</summary>
		/// <remarks>Zooming and scrolling affect the visible area of the visible board.</remarks>
		public RectangleF VisibleArea {
			get {
				return visibleArea;
			}
			set { visibleArea = value; }
		}
		internal RectangleF visibleArea = RectangleF.Empty;
		/// <summary>List of all the piece stacks on this board.</summary>
		/// <remarks>The stacks are sorted back to front.</remarks>
		internal List<Stack> Stacks = new List<Stack>();

		/// <summary>Board constructor.</summary>
		protected Board(int id) {
			this.id = id;
		}

		/// <summary>Total area of this board.</summary>
		/// <remarks>Area outside of this area will be displayed in black.</remarks>
		public abstract RectangleF TotalArea { get; }

		/// <summary>Returns the stack at the given z-order.</summary>
		/// <param name="zOrder">Z-order of this stack, i.e. how many stacks are behind it.</param>
		/// <returns>A stack or null if not found.</returns>
		public IStack GetStackFromZOrder(int zOrder) {
			return (zOrder >= 0 && zOrder < Stacks.Count ? Stacks[zOrder] : null);
		}

		/// <summary>The only player who can see this board, or Guid.Empty.</summary>
		public Guid Owner { get { return owner; } set { owner = value; } }

		private string name = null;
		private readonly int id;
		private Guid owner = Guid.Empty;
	}
}
