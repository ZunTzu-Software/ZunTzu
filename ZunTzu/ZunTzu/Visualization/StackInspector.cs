// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Visualization {

	/// <summary>Sub-component in charge of the display of the content of the selected stack.</summary>
	/// <remarks>
	/// Piece position is in model coordinates. The Stack Inspector uses a scaling factor to
	/// display the pieces at the correct position.
	/// The scaling used for position is not necessarily identical to the scaling used for
	/// the size of the pieces themselves.
	/// The X position is normally between -1 (out of screen) and 0 (ideal positioning).
	/// </remarks>
	internal sealed class StackInspector : ViewElement, IStackInspector {

		/// <summary>Constructor.</summary>
		public StackInspector(IModel model, View view) : base(view) {
			this.model = model;
		}

		/// <summary>Position and size of the stack inspector in screen coordinates.</summary>
		public RectangleF Area {
			get {
				return view.StackInspectorArea;
			}
		}

		/// <summary>Scaling applied to the vertical positioning of the pieces.</summary>
		public float PositionScaling {
			get {
				if(model.CurrentSelection == null) {
					return 1.0f;
				} else {
					float pieceScaling = PieceScaling;

					IPiece[] selectedPieces = model.CurrentSelection.Stack.Pieces;
					float heightInModelCoordinates = 0.0f;
					for(int i = selectedPieces.Length - 1; i > 0; --i) {
						// to allow rotations, the room to allocate for each piece
						// must be at least equal to its diagonal
						heightInModelCoordinates += selectedPieces[i].Diagonal;
					}

					PointF[] piecePositions = model.StackInspectorPositions;
					for(int i = 0; i < piecePositions.Length; ++i)
						heightInModelCoordinates = Math.Max(heightInModelCoordinates, piecePositions[i].Y);

					IPiece firstPiece = selectedPieces[selectedPieces.Length - 1];
					IPiece lastPiece = selectedPieces[0];
					return Math.Min(pieceScaling, (Area.Height - (firstPiece.Diagonal + lastPiece.Diagonal) * 0.5f * pieceScaling) / heightInModelCoordinates);
				}
			}
		}

		/// <summary>Scaling applied to the piece themselves.</summary>
		public float PieceScaling {
			get {
				if(model.CurrentSelection == null) {
					return 1.0f;
				} else {
					float maxDiagonal = 0.0f;
					foreach(IPiece piece in model.CurrentSelection.Stack.Pieces)
						maxDiagonal = Math.Max(maxDiagonal, piece.Diagonal);
					return Area.Width / maxDiagonal;
				}
			}
		}

		/// <summary>Displays the next frame.</summary>
		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			ISelection selection = model.CurrentSelection;
			if(selection != null) {
				RectangleF area = view.StackInspectorArea;

				IStackInspectorCursorLocation cursorLocation = model.ThisPlayer.CursorLocation as IStackInspectorCursorLocation;

				// render background
				float scale = view.GameDisplayAreaInPixels.Height / 1200.0f;
				//image.Render(new RectangleF(area.X + 7.0f * scale, area.Y + 6.0f * scale, area.Width - 14.0f * scale, area.Height - 12.0f * scale), 0xb0f0c67d);
				frameImageElements[0].Render(new RectangleF(area.X, area.Y, 11.0f * scale, 10.0f * scale));
				frameImageElements[1].Render(new RectangleF(area.X + 11.0f * scale, area.Y, area.Width - 22.0f * scale, 10.0f * scale));
				frameImageElements[2].Render(new RectangleF(area.Right - 11.0f * scale, area.Y, 11.0f * scale, 10.0f * scale));
				frameImageElements[3].Render(new RectangleF(area.X, area.Y + 10.0f * scale, 11.0f * scale, area.Height - 20.0f * scale));
				frameImageElements[4].Render(new RectangleF(area.X + 11.0f * scale, area.Y + 10.0f * scale, area.Width - 22.0f * scale, area.Height - 20.0f * scale));
				frameImageElements[5].Render(new RectangleF(area.Right - 11.0f * scale, area.Y + 10.0f * scale, 11.0f * scale, area.Height - 20.0f * scale));
				frameImageElements[6].Render(new RectangleF(area.X, area.Bottom - 10.0f * scale, 11.0f * scale, 10.0f * scale));
				frameImageElements[7].Render(new RectangleF(area.X + 11.0f * scale, area.Bottom - 10.0f * scale, area.Width - 22.0f * scale, 10.0f * scale));
				frameImageElements[8].Render(new RectangleF(area.Right - 11.0f * scale, area.Bottom - 10.0f * scale, 11.0f * scale, 10.0f * scale));

				// render selected pieces
				float pieceScaling = PieceScaling;
				float positionScaling = PositionScaling;

				IPiece[] piecesInStack = selection.Stack.Pieces;
				PointF[] piecePositions = model.StackInspectorPositions;

				// computes modulation color for blinking
				uint blinkFactor = (uint) (128 * Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0)) + 128);
				uint blinkModulationColor = 0xFF000000 | blinkFactor << 16 | blinkFactor << 8 | blinkFactor;

				// render each selection hint (from back to front)
				float xOffset = area.X + area.Width * 0.5f;
				float yOffset = area.Y + piecesInStack[piecesInStack.Length - 1].Diagonal * 0.5f * pieceScaling;
				for(int i = 0; i < piecesInStack.Length; ++i) {
					if(piecePositions[i].X != 1.0f) {
						IPiece piece = piecesInStack[i];
						if(selection.Contains(piece)) {
							SizeF pieceSize = piece.Size;
							PointF position = new PointF(xOffset, yOffset + piecePositions[i].Y * positionScaling);
							float flipAngleCosinus = piece.FlipAngleCosinus;
							RectangleF localisation = new RectangleF(
								position.X - (pieceSize.Width * flipAngleCosinus * pieceScaling) * 0.5f - 2.0f,
								position.Y - (pieceSize.Height * pieceScaling) * 0.5f - 2.0f,
								pieceSize.Width * flipAngleCosinus * pieceScaling + 4.0f,
								pieceSize.Height * pieceScaling + 4.0f);
							piece.Graphics.RenderSilhouette(localisation, piece.RotationAngle, blinkModulationColor);
						}
					}
				}

				// render each piece in order (from back to front)
				for(int i = 0; i < piecesInStack.Length; ++i) {
					if(piecePositions[i].X != 1.0f) {
						IPiece piece = piecesInStack[i];

						SizeF pieceSize = piece.Size;
						PointF position = new PointF(xOffset, yOffset + piecePositions[i].Y * positionScaling);
						float flipAngleCosinus = piece.FlipAngleCosinus;
						RectangleF localisation = new RectangleF(
							position.X - (pieceSize.Width * flipAngleCosinus * pieceScaling) * 0.5f,
							position.Y - (pieceSize.Height * pieceScaling) * 0.5f,
							pieceSize.Width * flipAngleCosinus * pieceScaling,
							pieceSize.Height * pieceScaling);

						if (piece.IsBlock && piece.CounterSection.IsSingleSided && piece.Owner != Guid.Empty && piece.Owner != model.ThisPlayer.Guid)
							piece.Graphics.RenderBlockBlank(localisation, 0.0f, 0.0f, piece.RotationAngle, 0xff000000 | piece.BlockColor, 1.0f, true);
						else
							piece.Graphics.Render(localisation, piece.RotationAngle);
					}
				}

				// render insertion mark, if needed
				if(cursorLocation != null && (model.ThisPlayer.PieceBeingDragged != null || model.ThisPlayer.StackBeingDragged != null)) {
					int insertionIndex = cursorLocation.Index;

					// ideal arrangement of pieces in stack inspector (to avoid too much wobbling)
					float yPos;
					if(insertionIndex == 0) {
						float bottomPieceYPos = yOffset + piecePositions[0].Y * positionScaling;
						float bottomPieceDiagonal = piecesInStack[0].Diagonal;
						yPos = bottomPieceYPos + bottomPieceDiagonal * 0.5f * pieceScaling - 3.0f;
					} else if(insertionIndex == piecesInStack.Length) {
						yPos = area.Top + 3.0f;
					} else {
						float precedingPieceYPos = yOffset + piecePositions[insertionIndex - 1].Y * positionScaling;
						float nextPieceYPos = yOffset + piecePositions[insertionIndex].Y * positionScaling;
						float precedingPieceDiagonal = piecesInStack[insertionIndex - 1].Diagonal;
						float nextPieceDiagonal = piecesInStack[insertionIndex].Diagonal;

						yPos = nextPieceYPos + (precedingPieceYPos - nextPieceYPos) * precedingPieceDiagonal / (precedingPieceDiagonal + nextPieceDiagonal);
					}

					IImage image = graphics.MonochromaticImage;
					image.Render(new RectangleF(area.X + 2.0f, yPos - 1.0f, area.Width - 4.0f, 3.0f));
				}

				// icons
				closeIcon.Render(new RectangleF(area.Right - 11.0f * scale - 13.0f, area.Y + 10.0f * scale, 13.0f, 12.0f),
					(cursorLocation != null && cursorLocation.Icon == StackInspectorIcon.Close && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
				invertIcon.Render(new RectangleF(area.Left + 11.0f * scale, area.Y + 10.0f * scale, 11.0f, 12.0f),
					(selection.Stack.Pieces.Length < 2 ? 0xff7f7f7f : (cursorLocation != null && cursorLocation.Icon == StackInspectorIcon.Invert && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));
				shuffleIcon.Render(new RectangleF(area.Left + 2 * 11.0f * scale + 11.0f, area.Y + 10.0f * scale, 16.0f, 13.0f),
					(selection.Stack.Pieces.Length < 2 ? 0xff7f7f7f : (cursorLocation != null && cursorLocation.Icon == StackInspectorIcon.Shuffle && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));
				recycleIcon.Render(new RectangleF(area.Left + 3 * 11.0f * scale + 27.0f, area.Y + 10.0f * scale, 13.0f, 12.0f),
					(selection.Empty || selection.Stack.AttachedToCounterSection ? 0xff7f7f7f : (cursorLocation != null && cursorLocation.Icon == StackInspectorIcon.Recycle && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff)));

				// tool tips
				if(cursorLocation != null && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null) {
					if(cursorLocation.Icon == StackInspectorIcon.Close)
						graphics.DrawText(font, 0xff7fff7f,
							new RectangleF(area.Right - 11.0f * scale - 13.0f, area.Y + 10.0f * scale - 17.0f, 13.0f, 17.0f), StringAlignment.Near,
							Resources.ToolTipClose);
					else if(cursorLocation.Icon == StackInspectorIcon.Recycle)
						graphics.DrawText(font, (selection.Empty || selection.Stack.AttachedToCounterSection ? 0xff7f7f7f : 0xff7fff7f),
							new RectangleF(area.Left + 3 * 11.0f * scale + 27.0f, area.Y + 10.0f * scale - 17.0f, 13.0f, 17.0f), StringAlignment.Near,
							Resources.ToolTipRecycle);
					else if(cursorLocation.Icon == StackInspectorIcon.Shuffle)
						graphics.DrawText(font, (selection.Stack.Pieces.Length < 2 ? 0xff7f7f7f : 0xff7fff7f),
							new RectangleF(area.Left + 2 * 11.0f * scale + 11.0f, area.Y + 10.0f * scale - 17.0f, 13.0f, 17.0f), StringAlignment.Near,
							Resources.ToolTipShuffle);
					else if(cursorLocation.Icon == StackInspectorIcon.Invert)
						graphics.DrawText(font, (selection.Stack.Pieces.Length < 2 ? 0xff7f7f7f : 0xff7fff7f),
							new RectangleF(area.Left + 11.0f * scale, area.Y + 10.0f * scale - 17.0f, 13.0f, 17.0f), StringAlignment.Near,
							Resources.ToolTipInvert);
				}
			}
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			PointF screenPosition = cursorLocation.ScreenPosition;
			if(model.CurrentSelection != null && Area.Contains(screenPosition)) {
				StackInspectorCursorLocation location = cursorLocation as StackInspectorCursorLocation;
				if(location == null) {
					location = new StackInspectorCursorLocation();
					cursorLocation = location;
				}
				PointF anchorPosition;
				location.Piece = getPieceAtPosition(screenPosition, out anchorPosition);
				location.AnchorPosition = anchorPosition;
				location.Index = getIndexAtPosition(screenPosition);
				location.Icon = getIconAtPosition(screenPosition);
				return true;
			} else {
				return false;
			}
		}

		/// <summary>Returns the piece displayed at a given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <param name="anchorPosition">Point of the piece attached to the cursor hotspot.</param>
		/// <returns>Piece found at the given position, or null if none was found.</returns>
		private IPiece getPieceAtPosition(PointF mouseScreenPosition, out PointF anchorPosition) {
			ISelection selection = model.CurrentSelection;
			if(selection != null) {
				RectangleF area = view.StackInspectorArea;
				if(area.Contains(mouseScreenPosition)) {
					float pieceScaling = PieceScaling;
					float positionScaling = PositionScaling;

					IPiece[] piecesInStack = selection.Stack.Pieces;
					float inversePieceScaling = 1.0f / pieceScaling;
					PointF[] piecePositions = model.StackInspectorPositions;

					// each piece is tested in reverse order (from front to back)
					float xOffset = area.X + area.Width * 0.5f;
					float yOffset = area.Y + piecesInStack[piecesInStack.Length - 1].Diagonal * 0.5f * pieceScaling;
					for(int i = piecesInStack.Length - 1; i >= 0; --i) {
						IPiece piece = piecesInStack[i];

						SizeF size = piece.Size;
						PointF position = new PointF(xOffset, yOffset + piecePositions[i].Y * positionScaling);

						// we have to handle rotations of the piece
						// apply the inverse rotation to the mouse model position
						anchorPosition = new PointF(
							(mouseScreenPosition.X - position.X) * inversePieceScaling,
							(mouseScreenPosition.Y - position.Y) * inversePieceScaling);
						PointF transformedPosition;
						if(piece.RotationAngle == 0.0f) {
							transformedPosition = anchorPosition;
						} else {
							// rotation:
							// x <- x * cos - y * sin
							// y <- x * sin + y * cos
							float sin = (float) Math.Sin(piece.RotationAngle);
							float cos = (float) Math.Cos(piece.RotationAngle);

							transformedPosition = new PointF(
								anchorPosition.X * cos - anchorPosition.Y * sin,
								anchorPosition.X * sin + anchorPosition.Y * cos);
						}

						if(new RectangleF(
							-size.Width * 0.5f,
							-size.Height * 0.5f,
							size.Width,
							size.Height).Contains(transformedPosition))
						{
							// is the piece completely transparent at that location?
							uint color = piece.Graphics.GetColorAtPosition(transformedPosition);
							if((color & 0xFF000000) != 0x00000000) {
								// no it is not
								return piece;
							}
						}
					}
				}
			}
			anchorPosition = PointF.Empty;
			return null;
		}

		/// <summary>Returns the index from bottom to top in the stack at the given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>An value indicating the position in the stack from 0 (bottom) to the number of pieces in the stack (top).</returns>
		private int getIndexAtPosition(PointF mouseScreenPosition) {
			ISelection selection = model.CurrentSelection;
			if(selection != null) {
				RectangleF area = view.StackInspectorArea;
				if(area.Contains(mouseScreenPosition)) {
					float pieceScaling = PieceScaling;
					float positionScaling = PositionScaling;

					IPiece[] piecesInStack = selection.Stack.Pieces;

					// ideal arrangement of pieces in stack inspector (to avoid too much wobbling)
					float yPos = area.Y + piecesInStack[piecesInStack.Length - 1].Diagonal * 0.5f * pieceScaling;
					for(int i = piecesInStack.Length - 1; i >= 0; --i) {
						if(mouseScreenPosition.Y < yPos)
							return i + 1;
						yPos += piecesInStack[i].Diagonal * positionScaling;
					}
				}
			}
			return 0;
		}

		/// <summary>Returns the icon at the given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>A value indicating the icon.</returns>
		private StackInspectorIcon getIconAtPosition(PointF mouseScreenPosition) {
			ISelection selection = model.CurrentSelection;
			if(selection != null) {
				RectangleF area = view.StackInspectorArea;
				if(area.Contains(mouseScreenPosition)) {
					float scale = view.GameDisplayAreaInPixels.Height / 1200.0f;

					// Close
					RectangleF closeIconLocation = new RectangleF(area.Right - 11.0f * scale - 13.0f, area.Y + 10.0f * scale, 13.0f, 12.0f);
					if(closeIconLocation.Contains(mouseScreenPosition))
						return StackInspectorIcon.Close;

					// Recycle
					RectangleF recycleIconLocation = new RectangleF(area.Left + 3 * 11.0f * scale + 27.0f, area.Y + 10.0f * scale, 13.0f, 12.0f);
					if(recycleIconLocation.Contains(mouseScreenPosition))
						return StackInspectorIcon.Recycle;

					// Shuffle
					RectangleF shuffleIconLocation = new RectangleF(area.Left + 2 * 11.0f * scale + 11.0f, area.Y + 10.0f * scale, 16.0f, 13.0f);
					if(shuffleIconLocation.Contains(mouseScreenPosition))
						return StackInspectorIcon.Shuffle;

					// Reverse order
					RectangleF invertIconLocation = new RectangleF(area.Left + 11.0f * scale, area.Y + 10.0f * scale, 11.0f, 12.0f);
					if(invertIconLocation.Contains(mouseScreenPosition))
						return StackInspectorIcon.Invert;
				}
			}
			return StackInspectorIcon.None;
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			//rotationMarkImage = iconsTileSet.ExtractImage(new RectangleF(13.0f * 4, 0.0f * 4, 16.0f * 4, 21.0f * 4));
			frameImageElements = new IImage[9] {
				iconsTileSet.ExtractImage(new RectangleF(231.0f * 4, 172.0f * 4, 11.0f * 4, 10.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(241.0f * 4, 172.0f * 4, 1.0f * 4, 10.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(242.0f * 4, 172.0f * 4, 11.0f * 4, 10.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(231.0f * 4, 182.0f * 4, 11.0f * 4, 38.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(241.0f * 4, 182.0f * 4, 1.0f * 4, 38.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(242.0f * 4, 182.0f * 4, 11.0f * 4, 38.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(231.0f * 4, 220.0f * 4, 11.0f * 4, 10.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(241.0f * 4, 220.0f * 4, 1.0f * 4, 10.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(242.0f * 4, 220.0f * 4, 11.0f * 4, 10.0f * 4))
			};
			closeIcon = iconsTileSet.ExtractImage(new RectangleF(231.0f * 4, 2.0f * 4, 13.0f * 4, 12.0f * 4));
			recycleIcon = iconsTileSet.ExtractImage(new RectangleF(231.0f * 4, 15.0f * 4, 13.0f * 4, 12.0f * 4));
			shuffleIcon = iconsTileSet.ExtractImage(new RectangleF(233.0f * 4, 28.0f * 4, 16.0f * 4, 13.0f * 4));
			invertIcon = iconsTileSet.ExtractImage(new RectangleF(209.0f * 4, 16.0f * 4, 11.0f * 4, 12.0f * 4));
		}

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class StackInspectorCursorLocation : CursorLocation, IStackInspectorCursorLocation {
			/// <summary>The piece displayed at the mouse cursor position.</summary>
			public IPiece Piece { get { return piece; } set { piece = value; } }
			/// <summary>The point of the piece under the cursor hotspot.</summary>
			public PointF AnchorPosition { get { return anchorPosition; } set { anchorPosition = value; } }
			/// <summary>The index from bottom to top in the stack at the mouse cursor position.</summary>
			public int Index { get { return index; } set { index = value; } }
			/// <summary>The icon displayed at the mouse cursor position.</summary>
			public StackInspectorIcon Icon { get { return icon; } set { icon = value; } }
			private IPiece piece;
			private PointF anchorPosition;
			private int index;
			private StackInspectorIcon icon;
		}

		private IModel model;
		/// <summary>Mark used to indicate piece orientation while rotating.</summary>
		//private IImage rotationMarkImage = null;
		private IImage[] frameImageElements = null;
		private IImage closeIcon = null;
		private IImage recycleIcon = null;
		private IImage shuffleIcon = null;
		private IImage invertIcon = null;
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
	}
}
