// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.AudioVideo;
using ZunTzu.FileSystem;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Visualization {

	/// <summary>Sub-component in charge of the display of dice.</summary>
	internal sealed class DiceBag : ViewElement, IDiceBag {

		/// <summary>Constructor.</summary>
		public DiceBag(IModel model, View view) : base(view) {
			this.model = model;

			// precalculate each face orientation
			float pi = (float) Math.PI;
			float atanSqrtTwo = (float) Math.Atan(Math.Sqrt(2.0));
			float acosMinusOneThird = (float) Math.Acos(-1.0 / 3.0);
			float dihedralAngle = (float) Math.Acos(-Math.Sqrt(5.0) / 5.0);
			float halfPentagonAngle = (float) (Math.PI / 5.0);
			float piMinusDihedralAngle = (float) (Math.PI - Math.Acos(-Math.Sqrt(5) / 3.0));
			faceOrientations = new IRotation[6][] {
				new IRotation[4] {
					/*1*/ model.ComputeRotationYawPitchRoll(0.0f, -atanSqrtTwo, pi * 0.25f),
					/*2*/ model.ComputeRotationYawPitchRoll(0.0f, -atanSqrtTwo, pi * -0.75f),
					/*3*/ model.ComputeRotationYawPitchRoll(0.0f, pi - atanSqrtTwo, pi * 0.75f),
					/*4*/ model.ComputeRotationYawPitchRoll(0.0f, pi - atanSqrtTwo, pi * -0.25f)
				},
				new IRotation[6] {
					/*1*/ model.ComputeRotationYawPitchRoll(0.0f, 0.0f, 0.0f),
					/*2*/ model.ComputeRotationYawPitchRoll(pi * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * 0.5f)),
					/*3*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * -0.5f)),
					/*4*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi)),
					/*5*/ model.ComputeRotationYawPitchRoll(pi * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi)),
					/*6*/ model.ComputeRotationYawPitchRoll(pi, 0.0f, pi * 0.5f)
				},
				new IRotation[8] {
					/*1*/ model.ComputeRotationYawPitchRoll(pi * 0.25f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*2*/ model.ComputeRotationYawPitchRoll(pi * 0.25f, 0.0f, pi).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*3*/ model.ComputeRotationYawPitchRoll(pi * -0.25f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*4*/ model.ComputeRotationYawPitchRoll(pi * -0.25f, 0.0f, pi).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*5*/ model.ComputeRotationYawPitchRoll(pi * -0.75f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*6*/ model.ComputeRotationYawPitchRoll(pi * -0.75f, 0.0f, pi).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*7*/ model.ComputeRotationYawPitchRoll(pi * 0.75f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f)),
					/*8*/ model.ComputeRotationYawPitchRoll(pi * 0.75f, 0.0f, pi).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (acosMinusOneThird - pi) * 0.5f, 0.0f))
				},
				new IRotation[10] {
					/*1*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.75f, pi * 0.5f),
					/*2*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.25f, pi * 0.1f),
					/*3*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.75f, pi * -0.3f),
					/*4*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.25f, pi * -0.7f),
					/*5*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.75f, pi * -0.7f),
					/*6*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.25f, pi * -0.3f),
					/*7*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.75f, pi * 0.1f),
					/*8*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.25f, pi * 0.5f),
					/*9*/ model.ComputeRotationYawPitchRoll(0.0f, pi * -0.75f, pi * 0.9f),
					/*0*/ model.ComputeRotationYawPitchRoll(0.0f, pi * 0.25f, pi * 0.9f)
				},
				new IRotation[12] {
					/*1*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, halfPentagonAngle * -0.5f)),
					/*2*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, (float)Math.PI * 0.5f)),
					/*3*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle, halfPentagonAngle * 0.5f)),
					/*4*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle + (float)Math.PI, (float)Math.PI - halfPentagonAngle * 0.5f)),
					/*5*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle + (float)Math.PI, halfPentagonAngle * -1.5f)),
					/*6*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle + (float)Math.PI, halfPentagonAngle * -3.5f)),
					/*7*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle, halfPentagonAngle * 1.5f + (float)Math.PI)),
					/*8*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle, halfPentagonAngle * -1.5f)),
					/*9*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle, (float)Math.PI - halfPentagonAngle * 0.5f)),
					/*10*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle + (float)Math.PI, halfPentagonAngle * 0.5f)),
					/*11*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, dihedralAngle, (float)Math.PI * 0.5f)),
					/*12*/ model.ComputeRotationYawPitchRoll(((float)Math.PI - dihedralAngle) * -0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, (float)Math.PI, halfPentagonAngle * -0.5f))
				},
				new IRotation[20] {
					/*1*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, -pi / 3.0f)),
					/*2*/ model.ComputeRotationYawPitchRoll(0.0f, pi + piMinusDihedralAngle * -0.5f, 0.0f),
					/*3*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * -0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi / -3.0f)),
					/*4*/ model.ComputeRotationYawPitchRoll(pi * 0.5f + piMinusDihedralAngle * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * -5.0f / 6.0f)),
					/*5*/ model.ComputeRotationYawPitchRoll(piMinusDihedralAngle * 0.5f, pi * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * -5.0f / 6.0f)),
					/*6*/ model.ComputeRotationYawPitchRoll(piMinusDihedralAngle * 0.5f, pi * -0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * -5.0f / 6.0f)),
					/*7*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi * -2.0f / 3.0f)),
					/*8*/ model.ComputeRotationYawPitchRoll(0.0f, pi + piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi * -2.0f / 3.0f)),
					/*9*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * -0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi / 3.0f)),
					/*10*/ model.ComputeRotationYawPitchRoll(pi * -0.5f - piMinusDihedralAngle * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * 5.0f / 6.0f)),
					/*11*/ model.ComputeRotationYawPitchRoll(pi * 0.5f - piMinusDihedralAngle * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi / 6.0f)),
					/*12*/ model.ComputeRotationYawPitchRoll(0.0f, pi - piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi / -3.0f)),
					/*13*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi * 2.0f / 3.0f)),
					/*14*/ model.ComputeRotationYawPitchRoll(0.0f, pi + piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi * 2.0f / 3.0f)),
					/*15*/ model.ComputeRotationYawPitchRoll(piMinusDihedralAngle * -0.5f, pi * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * 5.0f / 6.0f)),
					/*16*/ model.ComputeRotationYawPitchRoll(piMinusDihedralAngle * -0.5f, pi * -0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi * 5.0f / 6.0f)),
					/*17*/ model.ComputeRotationYawPitchRoll(pi * -0.5f + piMinusDihedralAngle * 0.5f, 0.0f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, -pi / 6.0f)),
					/*18*/ model.ComputeRotationYawPitchRoll(0.0f, pi - piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, -piMinusDihedralAngle, pi / 3.0f)),
					/*19*/ model.ComputeRotationYawPitchRoll(0.0f, piMinusDihedralAngle * -0.5f, 0.0f),
					/*20*/ model.ComputeRotationYawPitchRoll(0.0f, pi + piMinusDihedralAngle * 0.5f, 0.0f).ComposeWith(model.ComputeRotationYawPitchRoll(0.0f, 0.0f, pi / 3.0f))
				}
			};
		}

		/// <summary>Called when the game display area is resized.</summary>
		public override void OnGameDisplayAreaResized() {
			area = view.DiceBagArea;

			// initialize casting area
			Rectangle gameDisplayArea = view.GameDisplayAreaInPixels;
			castingArea = new RectangleF(
				gameDisplayArea.X + gameDisplayArea.Width * 0.2f,
				gameDisplayArea.Y + gameDisplayArea.Height * 0.2f,
				gameDisplayArea.Width * 0.6f,
				gameDisplayArea.Height * 0.6f);

			// initialize position, orientation and size of each die
			float singleLineHeight = area.Width * 0.2f;
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			int lineIndex = 0;
			for(int diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex) {
				Die[] dice = diceHands[diceHandIndex].Dice;
				for(int dieIndex = 0; dieIndex < dice.Length; ++dieIndex) {
					dice[dieIndex].DockedPosition = new PointF(
						area.X + singleLineHeight * ((dieIndex % 5) + 0.5f),
						area.Y + singleLineHeight * (lineIndex + (dieIndex / 5) + 0.5f));
					dice[dieIndex].Position = dice[dieIndex].DockedPosition;
					dice[dieIndex].DockedSize = singleLineHeight * 0.5f * 0.7f;
					dice[dieIndex].Size = dice[dieIndex].DockedSize;
				}
				lineIndex += (dice.Length + 4) / 5;
			}
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			// recalculate area dimensions
			OnGameDisplayAreaResized();

			if(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox) {
				// load ZunTzu logo 3D model
				IFile diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.ZunTzuDiceTexture.png");
				logoMesh = graphics.LoadDieMesh(Dice.D6Model.Vertice, Dice.D6Model.Triangles, Dice.D6Model.Inradius, diceTextureFile, false);
			} else {
				logoMesh = null;

				// load 3D models of dice
				DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
				for(int i = 0; i < diceHands.Length; ++i) {
					DiceType diceType = diceHands[i].DiceType;
					Die[] dice = diceHands[i].Dice;

					for(int j = 0; j < dice.Length; ++j) {
						string textureFileName = dice[j].TextureFileName;

						// have we loaded a model for this die already?
						for(int k = 0; k <= i; ++k) {
							if(diceHands[k].DiceType == diceType) {
								Die[] diceAlreadyLoaded = diceHands[k].Dice;
								for(int l = 0; l < (k < i ? diceAlreadyLoaded.Length : j); ++l) {
									if(diceAlreadyLoaded[l].TextureFileName == textureFileName) {
										// already loaded --> reuse mesh
										dice[j].DieMesh = diceAlreadyLoaded[l].DieMesh;
										goto nextDie;
									}
								}
							}
						}
						// not loaded yet --> create new mesh
						IFile diceTextureFile = null;
						bool custom = (dice[j].TextureFileName != null);
						if(custom) {
							IArchive archive = new Archive(model.CurrentGameBox.Reference.FileName);
							diceTextureFile = archive.GetFile(dice[j].TextureFileName);
						}
						switch(diceType) {
							case DiceType.D4:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D4Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D4Model.Vertice, Dice.D4Model.Triangles, Dice.D4Model.Inradius, diceTextureFile, custom);
								break;

							case DiceType.D6:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D6Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D6Model.Vertice, Dice.D6Model.Triangles, Dice.D6Model.Inradius, diceTextureFile, custom);
								break;

							case DiceType.D8:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D8Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D8Model.Vertice, Dice.D8Model.Triangles, Dice.D8Model.Inradius, diceTextureFile, custom);
								break;

							case DiceType.D10:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D10Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D10Model.Vertice, Dice.D10Model.Triangles, Dice.D10Model.Inradius, diceTextureFile, custom);
								break;

							case DiceType.D12:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D12Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D12Model.Vertice, Dice.D12Model.Triangles, Dice.D12Model.Inradius, diceTextureFile, custom);
								break;

							case DiceType.D20:
								if(!custom)
									diceTextureFile = FileSystem.FileSystem.GetResource("ZunTzu.ResourceFiles.D20Texture.png");
								dice[j].DieMesh = graphics.LoadDieMesh(Dice.D20Model.Vertice, Dice.D20Model.Triangles, Dice.D20Model.Inradius, diceTextureFile, custom);
								break;
						}

					nextDie:
						;
					}
				}

				// initialize orientation of each die (if first time)
				for(int i = 0; i < diceHands.Length; ++i) {
					DiceType diceType = diceHands[i].DiceType;
					for(int j = 0; j < diceHands[i].Dice.Length; ++j) {
						if(diceHands[i].Dice[j].Orientation == null)
							diceHands[i].Dice[j].Orientation = faceOrientations[(int)diceType][faceOrientations[(int)diceType].Length - 1];
					}
				}

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
			}
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			PointF screenPosition = cursorLocation.ScreenPosition;
			if(Area.Contains(screenPosition)) {
				DiceBagCursorLocation location = cursorLocation as DiceBagCursorLocation;
				if(location == null) {
					location = new DiceBagCursorLocation();
					cursorLocation = location;
				}
				int diceHandIndex;
				int diceCount;
				getDiceAtPosition(screenPosition, out diceHandIndex, out diceCount);
				location.DiceHandIndex = diceHandIndex;
				location.DiceCount = diceCount;
				return true;
			} else {
				return false;
			}
		}

		/// <summary>Height of the player view in screen coordinates.</summary>
		public float GetHeight(float gameDisplayAreaHeightInPixels) {
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			if(diceHands.Length == 0) {
				return 0.0f;
			} else {
				float singleLineHeight = (0.02f * 1.6f) * gameDisplayAreaHeightInPixels;
				int lineCount = 0;
				for(int diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex)
					lineCount += (diceHands[diceHandIndex].Dice.Length + 4) / 5;
				return lineCount * singleLineHeight;
			}
		}

		/// <summary>Position and size of the stack inspector in screen coordinates.</summary>
		public RectangleF Area { get { return area; } }
		private RectangleF area;

		/// <summary>Index of the dice hand and number of dice at the given screen position.</summary>
		/// <param name="mouseScreenPosition">Position in screen coordinates.</param>
		/// <param name="dicehandIndex">Index of the dice hand at the given position.</param>
		/// <param name="diceCount">Number of dice that will be cast if the given position is clicked.</param>
		private void getDiceAtPosition(PointF mouseScreenPosition, out int diceHandIndex, out int diceCount) {
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			if(diceHands.Length == 0) {
				diceHandIndex = 0;
				diceCount = 0;
			} else {
				if(area.Contains(mouseScreenPosition)) {
					int lineCount = 0;
					for(int i = 0; i < diceHands.Length; ++i)
						lineCount += (diceHands[i].Dice.Length + 4) / 5;

					int lineIndex = (int) Math.Floor((mouseScreenPosition.Y - area.Y) * lineCount / area.Height);

					diceCount = 0;
					for(diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex) {
						int diceHandCount = diceHands[diceHandIndex].Dice.Length;
						if(lineIndex >= (diceHandCount + 4) / 5) {
							lineIndex -= (diceHandCount + 4) / 5;
							continue;
						} else {
							if(!diceHands[diceHandIndex].BeingCast) {
								diceCount = lineIndex * 5 + 1 + (int) Math.Floor((mouseScreenPosition.X - area.X) * 5.0f / area.Width);
								if(diceCount > diceHandCount)
									diceCount = 0;
							}
							break;
						}
					}
				} else {
					diceHandIndex = 0;
					diceCount = 0;
				}
			}
		}

		/// <summary>Cast a set of dice, all from the same dice hand.</summary>
		/// <param name="diceHandIndex">Index of the dice hand.</param>
		/// <param name="diceResults">Result of each of the dice cast.</param>
		public void CastDice(int diceHandIndex, int[] diceResults) {
			DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
			diceHands[diceHandIndex].DiceCountBeingCast = diceResults.Length;
			Die[] dice = diceHands[diceHandIndex].Dice;

			// TODO : sort dice according to result from highest to lowest.

			PointF[] finalPositions = new PointF[diceResults.Length];
			const int audioTracksMaxCount = 5;
			AudioTrack[] audioTracks = new AudioTrack[Math.Min(diceResults.Length, audioTracksMaxCount)];
			for(int i = 0; i < diceResults.Length; ++i) {
				float finalSize = dice[0].Size * 1.40f * 2.5f;

				PointF finalPosition;
				bool diceOverlap;
				do {
					finalPosition = new PointF(
						model.RandomNumberGenerator.GenerateSingle(castingArea.X, castingArea.Right),
						model.RandomNumberGenerator.GenerateSingle(castingArea.Y, castingArea.Bottom));
					diceOverlap = false;
					for(int j = 0; j < i; ++j) {						
						if((finalPosition.X - finalPositions[j].X) * (finalPosition.X - finalPositions[j].X) +
							(finalPosition.Y - finalPositions[j].Y) * (finalPosition.Y - finalPositions[j].Y) <
							6.0f * finalSize * finalSize)
						{
							diceOverlap = true;
							break;
						}
					}
				} while(diceOverlap);
				finalPositions[i] = finalPosition;

				if(i < audioTracksMaxCount) {
					bool audioTrackAlreadyUsed;
					do {
						audioTracks[i] = AudioTrack.Die0 + model.RandomNumberGenerator.GenerateInt32(0, 6);
						audioTrackAlreadyUsed = false;
						for(int j = 0; j < i; ++j) {
							if(audioTracks[j] == audioTracks[i]) {
								audioTrackAlreadyUsed = true;
								break;
							}
						}
					} while(audioTrackAlreadyUsed);
				}

				DiceType diceType = diceHands[diceHandIndex].DiceType;
				IRotation finalOrientation = faceOrientations[(int)diceType][diceResults[i] - 1].ComposeWith(
					model.ComputeRotationFromAxis(0.0f, 0.0f, 1.0f,
						model.RandomNumberGenerator.GenerateSingle((float)-Math.PI, (float)Math.PI)));

				model.AnimationManager.LaunchAnimationSequence(
					new CastDieAnimation(diceHandIndex, i, dice[i].DockedPosition, finalPosition, dice[i].Orientation, finalOrientation, dice[i].DockedSize, finalSize, (i < audioTracksMaxCount ? audioTracks[i] : AudioTrack.None)),
					new IdleAnimation(1000000L),
					new ResetDieAnimation(diceHandIndex, i, finalPosition, dice[i].DockedPosition, finalOrientation, faceOrientations[(int)diceType][diceResults[i] - 1], finalSize, dice[i].DockedSize));
			}
		}

		/// <summary>Displays the next frame.</summary>
		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			if(model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox) {
				// display "dice" logo
				IRotation rotation1 = model.ComputeRotationYawPitchRoll(
					0.750000000f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % (long)7000000) / 7000000.0f),
					0.521750554f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % (long)5000000) / 5000000.0f),
					0.339836909f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % (long)9000000) / 9000000.0f));

				IRotation rotation2 = model.ComputeRotationYawPitchRoll(
					0.750000000f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % 9000000) / 9000000.0f),
					2.356194490f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % 7000000) / 7000000.0f),
					3.141592654f + 0.1f * (float) Math.Cos((float) Math.PI * 2.0f * (currentTimeInMicroseconds % 5000000) / 5000000.0f));

				Rectangle gameDisplayArea = view.GameDisplayAreaInPixels;

				logoMesh.Render(
					new PointF(gameDisplayArea.X + gameDisplayArea.Width * 0.43f, gameDisplayArea.Y + gameDisplayArea.Height * 0.35f),
					gameDisplayArea.Height * 0.1f,
					rotation1.ToRotationMatrix(),
					(uint)Color.LightGreen.ToArgb(),
					0xff000000);

				logoMesh.Render(
					new PointF(gameDisplayArea.X + gameDisplayArea.Width * 0.57f, gameDisplayArea.Y + gameDisplayArea.Height * 0.45f),
					gameDisplayArea.Height * 0.1f,
					rotation2.ToRotationMatrix(),
					(uint)Color.Salmon.ToArgb(),
					0xff000000);

			} else {
				DiceHand[] diceHands = model.CurrentGameBox.CurrentGame.DiceHands;
				if(diceHands.Length > 0) {
					// check for mouse roll over
					int rolledOverDiceHandIndex = 0;
					int rolledOverDiceCount = 0;
					if(model.ThisPlayer.CursorLocation is IDiceBagCursorLocation) {
						IDiceBagCursorLocation diceBagMouseLocation = (IDiceBagCursorLocation)model.ThisPlayer.CursorLocation;
						rolledOverDiceHandIndex = diceBagMouseLocation.DiceHandIndex;
						rolledOverDiceCount = diceBagMouseLocation.DiceCount;
					}

					// render background
					//IImage image = graphics.CreateMonochromaticImage(0xFFFFFFFF);
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

					float singleLineHeight = area.Width * 0.2f;

					// idle dice
					for(int diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex) {
						DiceHand diceHand = diceHands[diceHandIndex];
						for(int dieIndex = 0; dieIndex < diceHand.Dice.Length; ++dieIndex) {
							if(!diceHand.BeingCast || dieIndex >= diceHand.DiceCountBeingCast) {
								Die die = diceHand.Dice[dieIndex];
								IDieMesh dieMesh = die.DieMesh;
								float size = die.Size * (!diceHand.BeingCast && (model.ThisPlayer.PieceBeingDragged == null && model.ThisPlayer.StackBeingDragged == null) && (rolledOverDiceHandIndex == diceHandIndex && rolledOverDiceCount > dieIndex) ? 1.40f : 1.0f);
								dieMesh.Render(
									die.Position,
									size,
									die.Orientation.ToRotationMatrix(),
									die.Color,
									die.Pips);
							}
						}
					}

					// shadows of dice being cast
					for(int diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex) {
						DiceHand diceHand = diceHands[diceHandIndex];
						for(int dieIndex = 0; dieIndex < diceHand.Dice.Length; ++dieIndex) {
							if(diceHand.BeingCast && dieIndex < diceHand.DiceCountBeingCast) {
								Die die = diceHand.Dice[dieIndex];
								IDieMesh dieMesh = die.DieMesh;
								float size = die.Size;
								dieMesh.RenderShadow(
									die.Position,
									size,
									die.Orientation.ToRotationMatrix(),
									0x40000000);
							}
						}
					}

					// dice being cast
					for(int diceHandIndex = 0; diceHandIndex < diceHands.Length; ++diceHandIndex) {
						DiceHand diceHand = diceHands[diceHandIndex];
						for(int dieIndex = 0; dieIndex < diceHand.Dice.Length; ++dieIndex) {
							if(diceHand.BeingCast && dieIndex < diceHand.DiceCountBeingCast) {
								Die die = diceHand.Dice[dieIndex];
								IDieMesh dieMesh = die.DieMesh;
								float size = die.Size;
								dieMesh.Render(
									die.Position,
									size,
									die.Orientation.ToRotationMatrix(),
									die.Color,
									die.Pips);
							}
						}
					}
				}
			}
		}

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class DiceBagCursorLocation : CursorLocation, IDiceBagCursorLocation {
			/// <summary>Index of the dice hand at the mouse cursor position.</summary>
			public int DiceHandIndex { get { return diceHandIndex; } set { diceHandIndex = value; } }
			/// <summary>Number of dice implied by the mouse cursor position.</summary>
			public int DiceCount { get { return diceCount; } set { diceCount = value; } }
			private int diceHandIndex;
			private int diceCount;
		}

		private IModel model;
		private RectangleF castingArea = RectangleF.Empty;
		private IImage[] frameImageElements = null;
		private IDieMesh logoMesh = null;

		/// <summary>Orientation for each of the faces of a die, for each type of die.</summary>
		private readonly IRotation[][] faceOrientations;
	}
}
