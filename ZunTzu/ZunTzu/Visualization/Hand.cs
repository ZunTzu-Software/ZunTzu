// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Visualization {

	/// <summary>Sub-component in charge of the display of the content of the player's hand.</summary>
	internal sealed class Hand : ViewElement, IHand {

		/// <summary>Constructor.</summary>
		public Hand(IModel model, View view)
		: base(view)
		{
			this.model = model;
		}

		/// <summary>Displays the next frame.</summary>
		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayerHand playerHand = game.GetPlayerHand(model.ThisPlayer.Guid);
			if(isVisible && playerHand != null) {

				// animate hand folding/unfolding
				if(state == HandState.Unfolding) {
					float unfoldingSpeed = unfoldedHeight / 300000.0f;
					currentHeight += (int) (unfoldingSpeed * (currentTimeInMicroseconds - previousRenderTime));
					if(currentHeight >= unfoldedHeight) {
						currentHeight = unfoldedHeight;
						state = HandState.Unfolded;
					}
				} else if(state == HandState.Folding && timeBeforeFolding < currentTimeInMicroseconds) {
					if(timeBeforeFolding == 0) {
						timeBeforeFolding = currentTimeInMicroseconds + 1000000;
					} else {
						float foldingSpeed = unfoldedHeight / 300000.0f;
						currentHeight -= (int) (foldingSpeed * (currentTimeInMicroseconds - previousRenderTime));
						if(currentHeight <= 0) {
							currentHeight = 0;
							state = HandState.Folded;
						}
					}
				}

				if(state != HandState.Folded && currentHeight > 0) {
					RectangleF area = view.HandArea;

					HandCursorLocation cursorLocation = model.ThisPlayer.CursorLocation as HandCursorLocation;

					// render background
					frameImageElements[0].Render(new RectangleF(area.X, area.Y, area.Width, 6.0f));
					if(area.Height > 6.0f)
						frameImageElements[1].Render(new RectangleF(area.X, area.Y + 6.0f, area.Width, area.Height - 6.0f));

					// render pieces
					if(playerHand.Count > 0) {
						IPiece[] pieces = playerHand.Pieces;

						focusPiece = (cursorLocation != null ? cursorLocation.FocusPiece : null);

						int handCount = 0;
						float totalWidth = 0;
						float minWidth = float.MaxValue;
						for(int i = 0; i < pieces.Length; ++i) {
							IPiece piece = pieces[i];
							if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
								++handCount;
								float width = piece.Size.Width;
								totalWidth += width;
								if(width < minWidth)
									minWidth = width;
							}
						}
						// maxScaling is such that offset == 2.0f - minWidth * pieceScaling
						// => 2.0f - minWidth * pieceScaling == (area.Width - 4.0f * (pieces.Length + 1) - totalWidth * pieceScaling) / (pieces.Length - 1)
						// => 2.0f * (pieces.Length - 1) - minWidth * pieceScaling * (pieces.Length - 1) == area.Width - 4.0f * (pieces.Length + 1) - totalWidth * pieceScaling
						// => (totalWidth - minWidth * (pieces.Length - 1)) * pieceScaling == area.Width - 4.0f * (pieces.Length + 1) - 2.0f * (pieces.Length - 1) 
						// => pieceScaling == (area.Width - 4.0f * (pieces.Length + 1) - 2.0f * (pieces.Length - 1)) / (totalWidth - minWidth * (pieces.Length - 1)) 
						float maxScaling = Math.Min(1.0f,
							(handCount < 2 ?
								(area.Width - 4.0f * (handCount + 1)) / totalWidth :
								(area.Width - 6.0f * handCount - 2.0f) / (totalWidth - minWidth * (handCount - 1))));
						pieceScaling = Math.Max(0.0625f, Math.Min(maxScaling, pieceScaling));
						float offset = (handCount < 2 ? 0.0f :
							Math.Min(0.0f, (area.Width - 4.0f * (handCount + 1) - totalWidth * pieceScaling) / (handCount - 1)));

						// render each piece in order (from back to front)
						{
							float xPos = area.X + 4.0f;
							float yPos = area.Y + 10.0f;
							for(int i = 0; i < pieces.Length; ++i) {
								IPiece piece = pieces[i];
								if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
									SizeF size = piece.Size;
									SizeF scaledSize = new SizeF(size.Width * pieceScaling, size.Height * pieceScaling);
									PointF position = new PointF(xPos + scaledSize.Width * 0.5f, yPos + scaledSize.Height * 0.5f);
									float flipAngleCosinus = piece.FlipAngleCosinus;
									RectangleF localisation = new RectangleF(
										position.X - scaledSize.Width * 0.5f * flipAngleCosinus,
										position.Y - scaledSize.Height * 0.5f,
										scaledSize.Width * flipAngleCosinus,
										scaledSize.Height);

									// render a roll-over hint
									if(cursorLocation != null && piece == cursorLocation.Piece) {
										// cards pop up when rolled over
										if(piece is ICard) {
											float overflow = localisation.Bottom - area.Bottom;
											if(overflow > 0.0f)
												localisation.Y -= overflow;
										}

										// computes modulation color for blinking
										uint blinkFactor = (uint) (128 * Math.Sin((double) (currentTimeInMicroseconds % (long) 400000) * (Math.PI / 200000.0)) + 128);
										uint blinkModulationColor = 0xFF000000 | blinkFactor << 16 | blinkFactor << 8 | blinkFactor;
										RectangleF hintLocalisation = new RectangleF(
											localisation.X - 2.0f,
											localisation.Y - 2.0f,
											localisation.Width + 4.0f,
											localisation.Height + 4.0f);
										piece.Graphics.RenderSilhouette(hintLocalisation, piece.RotationAngle, blinkModulationColor);
									}
									piece.Graphics.Render(localisation, piece.RotationAngle);
									xPos += scaledSize.Width + 4.0f + (piece != focusPiece ? offset : 0.0f);
								}
							}
						}

						// render focus piece visible through the other pieces
						if(focusPiece != null) {
							float xPos = area.X + 4.0f;
							float yPos = area.Y + 10.0f;
							for(int i = 0; i < pieces.Length; ++i) {
								IPiece piece = pieces[i];
								if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
									SizeF size = piece.Size;
									SizeF scaledSize = new SizeF(size.Width * pieceScaling, size.Height * pieceScaling);
									if(piece == focusPiece) {
										PointF position = new PointF(xPos + scaledSize.Width * 0.5f, yPos + scaledSize.Height * 0.5f);
										float flipAngleCosinus = piece.FlipAngleCosinus;
										RectangleF localisation = new RectangleF(
											position.X - scaledSize.Width * 0.5f * flipAngleCosinus,
											position.Y - scaledSize.Height * 0.5f,
											scaledSize.Width * flipAngleCosinus,
											scaledSize.Height);

										// cards pop up when rolled over
										if(cursorLocation != null && piece == cursorLocation.Piece && piece is ICard) {
											float overflow = localisation.Bottom - area.Bottom;
											if(overflow > 0.0f)
												localisation.Y -= overflow;
										}

										piece.Graphics.Render(localisation, piece.RotationAngle, 0x3fffffff);	// 25% transparent
										break;
									}
									xPos += scaledSize.Width + 4.0f + offset;
								}
							}
						}

						// render insertion mark, if needed
						if(cursorLocation != null && (model.ThisPlayer.PieceBeingDragged != null || model.ThisPlayer.StackBeingDragged != null)) {
							int insertionIndex = cursorLocation.Index;
							float xPos = area.X + 4.0f;
							for(int i = 0; i < Math.Min(insertionIndex, pieces.Length); ++i) {
								IPiece piece = pieces[i];
								if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
									xPos += piece.Size.Width * pieceScaling + 4.0f + (piece != focusPiece ? offset : 0.0f);
								}
							}
							IImage image = graphics.MonochromaticImage;
							image.Render(new RectangleF(xPos - 4.0f, area.Y + 8.0f, 3.0f, area.Height - 10.0f));
						}
					}

					// icons
					if(state == HandState.Unfolded) {
						if(isPinned)
							unpinIcon.Render(new RectangleF(area.Right - 12.0f, area.Y + 6.0f, 10.0f, 10.0f),
								(cursorLocation != null && cursorLocation.Icon == HandIcon.Pin && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));
						else
							pinIcon.Render(new RectangleF(area.Right - 24.0f, area.Y + 6.0f, 22.0f, 10.0f),
								(cursorLocation != null && cursorLocation.Icon == HandIcon.Pin && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null ? 0xff7fff7f : 0xffffffff));

						// tool tips
						if(cursorLocation != null && model.ThisPlayer.StackBeingDragged == null && model.ThisPlayer.PieceBeingDragged == null) {
							if(cursorLocation.Icon == HandIcon.Pin)
								graphics.DrawText(font, 0xff7fff7f,
									new RectangleF(area.Right - 33.0f, area.Y + 5.0f - 17.0f, 31.0f, 17.0f), StringAlignment.Far,
									(isPinned ? Resources.ToolTipUnpin : Resources.ToolTipPin));
						}
					}
				}
			}
			previousRenderTime = currentTimeInMicroseconds;
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			if(isVisible && state != HandState.Folded && currentHeight > 0) {
				PointF screenPosition = cursorLocation.ScreenPosition;
				RectangleF area = view.HandArea;
				if(area.Contains(screenPosition) && model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid) != null) {
					HandCursorLocation location = cursorLocation as HandCursorLocation;
					if(location == null) {
						location = new HandCursorLocation();
						cursorLocation = location;
					}
					PointF anchorPosition;
					location.Piece = getPieceAtPosition(screenPosition, out anchorPosition);
					location.AnchorPosition = anchorPosition;
					location.Index = getIndexAtPosition(screenPosition);
					location.FocusPiece = getFocusPieceAtPosition(screenPosition);
					location.Icon = getIconAtPosition(screenPosition);
					if(location.Icon != HandIcon.None)
						location.Piece = null;
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns the piece displayed at a given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <param name="anchorPosition">Point of the piece attached to the cursor hotspot.</param>
		/// <returns>Piece found at the given position, or null if none was found.</returns>
		private IPiece getPieceAtPosition(PointF mouseScreenPosition, out PointF anchorPosition) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayerHand playerHand = game.GetPlayerHand(model.ThisPlayer.Guid);
			anchorPosition = PointF.Empty;
			if(playerHand.Count == 0) {
				return null;
			} else {
				RectangleF area = view.HandArea;
				IPiece[] pieces = playerHand.Pieces;

				// trigger bounding box calculation
				RectangleF box = pieces[0].Stack.BoundingBox;

				int handCount = 0;
				float totalWidth = 0;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						totalWidth += piece.Size.Width;
						++handCount;
					}
				}
				float offset = (handCount < 2 ? 0.0f :
					Math.Min(0.0f, (area.Width - 4.0f * (handCount + 1) - totalWidth * pieceScaling) / (handCount - 1)));
				float inversePieceScaling = 1.0f / pieceScaling;

				// each piece is tested in reverse order (from front to back)
				float xPos = area.X + 4.0f;
				float yPos = area.Y + 10.0f;
				IPiece pieceAtPosition = null;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						SizeF size = piece.Size;
						RectangleF boundingBox = piece.BoundingBox;
						float shadowLength = piece.CounterSection.ShadowLength;
						RectangleF actualBoundingBox = new RectangleF(xPos, yPos, (boundingBox.Width - shadowLength) * pieceScaling, (boundingBox.Height - shadowLength) * pieceScaling);
						if(actualBoundingBox.Contains(mouseScreenPosition)) {
							SizeF scaledSize = new SizeF(size.Width * pieceScaling, size.Height * pieceScaling);
							PointF position = new PointF(xPos + scaledSize.Width * 0.5f, yPos + scaledSize.Height * 0.5f);

							// we have to handle rotations of the piece
							// apply the inverse rotation to the mouse model position
							PointF pieceAnchorPosition = new PointF(
								(mouseScreenPosition.X - position.X) * inversePieceScaling,
								(mouseScreenPosition.Y - position.Y) * inversePieceScaling);
							PointF transformedPosition;
							if(piece.RotationAngle == 0.0f) {
								transformedPosition = pieceAnchorPosition;
							} else {
								// rotation:
								// x <- x * cos - y * sin
								// y <- x * sin + y * cos
								float sin = (float) Math.Sin(piece.RotationAngle);
								float cos = (float) Math.Cos(piece.RotationAngle);

								transformedPosition = new PointF(
									pieceAnchorPosition.X * cos - pieceAnchorPosition.Y * sin,
									pieceAnchorPosition.X * sin + pieceAnchorPosition.Y * cos);
							}

							if(new RectangleF(
								-size.Width * 0.5f,
								-size.Height * 0.5f,
								size.Width,
								size.Height).Contains(transformedPosition)) {
								// is the piece completely transparent at that location?
								uint color = piece.Graphics.GetColorAtPosition(transformedPosition);
								if((color & 0xFF000000) != 0x00000000) {
									// no it is not
									pieceAtPosition = piece;
									// cards pop up when rolled over
									if(piece is ICard) {
										float bottom = position.Y + scaledSize.Height * 0.5f;
										float overflow = bottom - area.Bottom;
										if(overflow > 0.0f)
											pieceAnchorPosition.Y += overflow * inversePieceScaling;
									}
									anchorPosition = pieceAnchorPosition;
								}
							}
						}

						xPos += size.Width * pieceScaling + 4.0f + (piece != focusPiece ? offset : 0.0f);
					}
				}
				return pieceAtPosition;
			}
		}

		/// <summary>Returns the index from left to right in the hand at the given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>An value indicating the position in the stack from 0 (leftmost) to the number of pieces in the stack (rightmost).</returns>
		private int getIndexAtPosition(PointF mouseScreenPosition) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayerHand playerHand = game.GetPlayerHand(model.ThisPlayer.Guid);
			if(playerHand.Count > 0) {
				RectangleF area = view.HandArea;
				IPiece[] pieces = playerHand.Pieces;

				int handCount = 0;
				float totalWidth = 0;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						totalWidth += piece.Size.Width;
						++handCount;
					}
				}
				float offset = (handCount < 2 ? 0.0f :
					Math.Min(0.0f, (area.Width - 4.0f * (handCount + 1) - totalWidth * pieceScaling) / (handCount - 1)));

				float xPos = area.X + 4.0f;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						float pieceWidth = piece.Size.Width * pieceScaling;
						if(piece == focusPiece) {
							if(mouseScreenPosition.X < xPos + pieceWidth)
								return (mouseScreenPosition.X < xPos + pieceWidth * 0.5f ? i : i + 1);
							xPos += pieceWidth + 4.0f;
						} else {
							if(mouseScreenPosition.X < xPos + pieceWidth + offset)
								return (mouseScreenPosition.X < xPos + pieceWidth * 0.5f ? i : i + 1);
							xPos += pieceWidth + 4.0f + offset;
						}
					}
				}
				return pieces.Length;
			}
			return 0;
		}

		/// <summary>Returns the piece that has the focus.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>Piece found at the given position, or null if none was found.</returns>
		private IPiece getFocusPieceAtPosition(PointF mouseScreenPosition) {
			IGame game = model.CurrentGameBox.CurrentGame;
			IPlayerHand playerHand = game.GetPlayerHand(model.ThisPlayer.Guid);
			if(playerHand.Count > 0) {
				RectangleF area = view.HandArea;
				IPiece[] pieces = playerHand.Pieces;

				int handCount = 0;
				float totalWidth = 0;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						totalWidth += piece.Size.Width;
						++handCount;
					}
				}
				float offset = (handCount < 2 ? 0.0f :
					Math.Min(0.0f, (area.Width - 4.0f * (handCount + 1) - totalWidth * pieceScaling) / (handCount - 1)));

				float xPos = area.X + 4.0f;
				for(int i = 0; i < pieces.Length; ++i) {
					IPiece piece = pieces[i];
					if((game.Mode == Mode.Terrain) == (piece is ITerrain)) {
						float pieceWidth = piece.Size.Width * pieceScaling;
						if(piece == focusPiece) {
							if(mouseScreenPosition.X < xPos + pieceWidth)
								return piece;
							xPos += pieceWidth + 4.0f;
						} else {
							if(mouseScreenPosition.X < xPos + pieceWidth + offset)
								return piece;
							xPos += pieceWidth + 4.0f + offset;
						}
					}
				}
			}
			return null;
		}

		/// <summary>Returns the icon at the given position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <returns>A value indicating the icon.</returns>
		private HandIcon getIconAtPosition(PointF mouseScreenPosition) {
			if(state == HandState.Unfolded) {
				RectangleF area = view.HandArea;
				if(area.Contains(mouseScreenPosition)) {
					// Pin
					RectangleF pinIconLocation = (isPinned ?
						new RectangleF(area.Right - 12.0f, area.Y + 6.0f, 10.0f, 10.0f) :
						new RectangleF(area.Right - 24.0f, area.Y + 6.0f, 22.0f, 10.0f));
					if(pinIconLocation.Contains(mouseScreenPosition))
						return HandIcon.Pin;

					// Resize
					RectangleF resizeIconLocation = new RectangleF(area.X, area.Y, area.Width, 6.0f);
					if(resizeIconLocation.Contains(mouseScreenPosition))
						return HandIcon.Resize;
				}
			}
			return HandIcon.None;
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			//rotationMarkImage = iconsTileSet.ExtractImage(new RectangleF(13.0f * 4, 0.0f * 4, 16.0f * 4, 21.0f * 4));
			frameImageElements = new IImage[2] {
				iconsTileSet.ExtractImage(new RectangleF(205.0f * 4, 2.0f * 4, 1.0f * 4, 6.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(205.0f * 4, 8.0f * 4, 1.0f * 4, 1.0f * 4))
			};
			pinIcon = iconsTileSet.ExtractImage(new RectangleF(20.0f * 4, 76.0f * 4, 22.0f * 4, 10.0f * 4));
			unpinIcon = iconsTileSet.ExtractImage(new RectangleF(44.0f * 4, 76.0f * 4, 10.0f * 4, 10.0f * 4));
		}

		/// <summary>Scaling applied to the pieces.</summary>
		public float PieceScaling { get { return pieceScaling; } set { pieceScaling = value; } }

		/// <summary>Height of the hand in screen pixels when fully unfolded.</summary>
		public int UnfoldedHeight {
			get { return unfoldedHeight; }
			set {
				unfoldedHeight = Math.Min(Math.Max(32, value), view.GameDisplayAreaInPixels.Height - 50);
				if(state == HandState.Unfolded)
					currentHeight = unfoldedHeight;
			}
		}

		/// <summary>True if the hand is locked in the unfolded position.</summary>
		public bool IsPinned { get { return isPinned; } set { isPinned = value; } }

		/// <summary>Folding state of the hand.</summary>
		public HandState State { get { return state; } set { state = value; } }

		/// <summary>Height of the hand in screen coordinates.</summary>
		public float Height { get { return currentHeight; } }

		/// <summary>True if the hand is not hidden.</summary>
		public bool IsVisible {
			get { return isVisible; }
			set {
				if(isVisible != value) {
					isVisible = value;
					if(isVisible) {
						if(!isPinned) {
							state = HandState.Unfolding;
							currentHeight = 0;
						}
					} else {
						if(!isPinned) {
							state = HandState.Folded;
							currentHeight = 0;
						}
					}
				}
			}
		}

		/// <summary>Makes the hand fold after a wait time.</summary>
		public void Fold() {
			if(isVisible && !isPinned && (state == HandState.Unfolded || state == HandState.Unfolding) &&
				model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid) != null)
			{
				state = HandState.Folding;
				timeBeforeFolding = 0;
			}
		}

		/// <summary>Makes the hand unfold immediately.</summary>
		public void Unfold() {
			if(isVisible && !isPinned && (state == HandState.Folded || state == HandState.Folding) &&
				model.CurrentGameBox.CurrentGame.GetPlayerHand(model.ThisPlayer.Guid) != null)
			{
				state = HandState.Unfolding;
			}
		}

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class HandCursorLocation : CursorLocation, IHandCursorLocation {
			/// <summary>The piece displayed at the mouse cursor position.</summary>
			public IPiece Piece { get { return piece; } set { piece = value; } }
			/// <summary>The point of the piece under the cursor hotspot.</summary>
			public PointF AnchorPosition { get { return anchorPosition; } set { anchorPosition = value; } }
			/// <summary>The index from left to right in the hand at the mouse cursor position.</summary>
			public int Index { get { return index; } set { index = value; } }
			/// <summary>The icon displayed at the mouse cursor position.</summary>
			public HandIcon Icon { get { return icon; } set { icon = value; } }
			/// <summary>The piece displayed at the mouse cursor position.</summary>
			public IPiece FocusPiece { get { return focusPiece; } set { focusPiece = value; } }
			private IPiece piece;
			private PointF anchorPosition;
			private int index;
			private HandIcon icon;
			private IPiece focusPiece;
		}

		private IModel model;
		private IImage[] frameImageElements = null;
		private IImage pinIcon = null;
		private IImage unpinIcon = null;
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
		private int unfoldedHeight = 100;
		private int currentHeight = 0;
		private bool isPinned = false;
		private float pieceScaling = 0.25f;
		private long timeBeforeFolding = 0;
		private long previousRenderTime = 0;
		private HandState state = HandState.Folded;
		private bool isVisible = false;
		private IPiece focusPiece = null;
	}
}
