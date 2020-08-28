// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using ZunTzu.Graphics;
using ZunTzu.Modelization;

namespace ZunTzu.Visualization {

	/// <summary></summary>
	internal sealed class PlayerView : ViewElement, IPlayerView {

		private const float marginCoeff = 0.0f;
		private const float singleLineHeightCoeff = 76.0f / 1200.0f;

		/// <summary>Constructor.</summary>
		public PlayerView(IModel model, View view) : base(view) {
			this.model = model;

			// substitution font
			if(font.FontFamily.Name == "Microsoft Sans Serif")
				font = new Font("Impact", 15.0f, 0, GraphicsUnit.Pixel);
		}

		/// <summary>Height of the player view in screen coordinates.</summary>
		public float GetHeight(float gameDisplayAreaHeightInPixels) {
			if(model.PlayerCount == 0) {
				return 0.0f;
			} else {
				float margin = marginCoeff * gameDisplayAreaHeightInPixels;
				float singleLineHeight = singleLineHeightCoeff * gameDisplayAreaHeightInPixels;
				return model.PlayerCount * singleLineHeight + 2.0f * margin;
			}
		}

		/// <summary>Position and size of the player view in screen coordinates.</summary>
		public RectangleF Area { get { return view.PlayerViewArea; } }

		/// <summary>Displays the next frame.</summary>
		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			if(model.PlayerCount > 0) {
				RectangleF area = view.PlayerViewArea;
				float margin = marginCoeff * view.GameDisplayAreaInPixels.Height;
				float singleLineHeight = singleLineHeightCoeff * view.GameDisplayAreaInPixels.Height;
				IImage background = graphics.MonochromaticImage;

				float scale = view.GameDisplayAreaInPixels.Height / 1200.0f;

				// render player list
				area.Y += margin;
				foreach(IPlayer player in model.Players) {
					// render colored background
					RectangleF backgroundArea = new RectangleF(area.X + scale * 68.0f, area.Y + scale * 15.0f, scale * 120.0f, scale * 46.0f);
					background.Render(backgroundArea, player.Color);

					// render player picture
					RectangleF playerIconArea = new RectangleF(area.X + scale * 6.0f, area.Y + scale * 6.0f, scale * 64.0f, scale * 64.0f);
					if(player.VideoAssetIndex == -1)
						playerIcon.Render(playerIconArea);
					else
						playerVideoFrames[player.VideoAssetIndex].Render(playerIconArea);

					// render frame
					RectangleF frameArea = new RectangleF(area.X, area.Y, scale * 195.0f, scale * 76.0f);
					frameIcon.Render(frameArea);

					// render name
					RectangleF firstNameArea = new RectangleF(area.X + scale * 76.0f, area.Y + scale * 17.0f, scale * 110.0f, scale * 25.0f);
					graphics.DrawTextToFit(font, 0xFFFFFFFF, firstNameArea, StringAlignment.Center, player.FirstName);
					RectangleF lastNameArea = new RectangleF(area.X + scale * 76.0f, area.Y + scale * 39.0f, scale * 110.0f, scale * 20.0f);
					graphics.DrawTextToFit(font, 0xFFFFFFFF, lastNameArea, StringAlignment.Center, player.LastName);

					if(player == model.ThisPlayer) {
						background.Render(new RectangleF(area.X + scale * 193.0f, area.Y + scale * 14.0f, scale * 7.0f, scale * 46.0f), 0xFF000000);
						background.Render(new RectangleF(area.X + scale * 193.0f, area.Y + scale * (60.0f - model.NetworkClient.VoiceInputLevel * (46.0f / 99.0f)), scale * 7.0f, scale * model.NetworkClient.VoiceInputLevel * (46.0f / 99.0f)),
							(model.NetworkClient.IsRecording ? 0xFF00FF00 : 0xFFFFFFFF));
						microphoneLevelIcon.Render(new RectangleF(area.X + scale * 187.0f, area.Y + scale * 8.0f, scale * 19.0f, scale * 59.0f));
						microphoneThresholdIcon.Render(new RectangleF(area.X + scale * 198.0f, area.Y + scale * (54.5f - model.NetworkClient.VoiceActivationThresholdLevel * (46.0f / 99.0f)), scale * 9.0f, scale * 11.0f));
					} else if(player.VoicePlaybackInProgress) {
						loudspeakerIcon.Render(new RectangleF(area.X + scale * 60.0f, area.Y + scale * 37.0f - 7.5f, 10.0f, 15.0f), player.Color);
						float animationProgress = (currentTimeInMicroseconds % (long) 300000) * 0.00000333333f;
						float wavesPositionX = 6.0f + animationProgress * 6.0f;
						float wavesHalfHeight = 1.0f + wavesPositionX * 0.8f;
						soundWavesIcon.Render(new RectangleF(area.X + scale * 60.0f + wavesPositionX, area.Y + scale * 37.0f - wavesHalfHeight, wavesHalfHeight * (10.0f / 7.5f), wavesHalfHeight * 2.0f), player.Color);
					}

					// render hand count
					IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(player.Guid);
					if(playerHand != null) {
						RectangleF handArea = new RectangleF(area.X - 7.0f, area.Y + scale * (76.0f * 0.5f) - 9.0f, 17.0f, 18.0f);
						handIcon.Render(handArea, player.Color);
						if(playerHand.Count > 0) {
							handArea.Offset(2.0f, 3.0f);
							graphics.DrawText(handCountFont, 0xFFFFFFFF, handArea, StringAlignment.Center, playerHand.Count.ToString());
						}
					}

					area.Y += singleLineHeight;
				}
			}
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			loudspeakerIcon = iconsTileSet.ExtractImage(new RectangleF(208.0f * 4, 1.0f * 4, 10.0f * 4, 15.0f * 4));
			soundWavesIcon = iconsTileSet.ExtractImage(new RectangleF(218.0f * 4, 1.0f * 4, 10.0f * 4, 15.0f * 4));
			frameIcon = iconsTileSet.ExtractImage(new RectangleF(1.0f * 4, 163.0f * 4, 195.0f * 4, 76.0f * 4));
			playerIcon = iconsTileSet.ExtractImage(new RectangleF(1.0f * 4, 98.0f * 4, 64.0f * 4, 64.0f * 4));
			microphoneLevelIcon = iconsTileSet.ExtractImage(new RectangleF(198.0f * 4, 171.0f * 4, 19.0f * 4, 59.0f * 4));
			microphoneThresholdIcon = iconsTileSet.ExtractImage(new RectangleF(221.0f * 4, 208.0f * 4, 9.0f * 4, 11.0f * 4));
			handIcon = iconsTileSet.ExtractImage(new RectangleF(19.0f * 4, 55.0f * 4, 17.0f * 4, 18.0f * 4));

			videoTexture = graphics.CreateVideoTexture(new Size(256, 256));
			for(int y = 0; y < 4; ++y)
				for(int x = 0; x < 4; ++x)
					playerVideoFrames[y * 4 + x] = videoTexture.ExtractImage(new RectangleF(x * 64.0f, y * 64.0f, 64.0f, 64.0f));
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			// TODO: mouse location over player view
			/*
			PointF screenPosition = cursorLocation.ScreenPosition;
			if(Area.Contains(screenPosition)) {
				PlayerViewCursorLocation location = cursorLocation as PlayerViewCursorLocation;
				if(location == null) {
					location = new PlayerViewCursorLocation();
					cursorLocation = location;
				}
				return true;
			} else {
				return false;
			}
			*/
			return false;
		}

		/// <summary>Displays a new video frame from a player's webcam.</summary>
		/// <param name="playerId">The player's id.</param>
		/// <param name="videoFrame">A 64x64 frame buffer in R8G8B8 format.</param>
		public unsafe void UpdateVideoFrame(int playerId, byte[] videoFrame) {
			// crop in a circular shape using alpha transparency
			byte* alphaFrame = stackalloc byte[64 * 64 * 4];
			fixed(byte* videoFramePtr = videoFrame) {
				byte* source = videoFramePtr;
				byte* dest = alphaFrame;
				for(int y = 0; y < 64; ++y) {
					for(int x = 0; x < 64; ++x, source += 3, dest += 4) {
						*(dest + 0) = *(source + 0);
						*(dest + 1) = *(source + 1);
						*(dest + 2) = *(source + 2);
						// every pixel outside a 31.5 radius circle must be transparent
						*(dest + 3) = (byte) (x * (x - 63) + y * (y - 63) + 993 > 0 ? 0x00 : 0xff);
					}
				}
			}

			IPlayer player = model.GetPlayer(playerId);
			if(player != null) {
				if(player.VideoAssetIndex == -1) {
					// allocate a video texture asset
					for(int i = 0; i < 16; ++i) {
						bool indexAlreadyInUse = false;
						foreach(Player otherPlayer in model.Players) {
							if(otherPlayer.VideoAssetIndex == i) {
								indexAlreadyInUse = true;
								break;
							}
						}
						if(!indexAlreadyInUse) {
							player.VideoAssetIndex = i;
							videoTexture.Update(new Rectangle((i % 4) * 64, (i / 4) * 64, 64, 64), (IntPtr) alphaFrame);
						}
					}
				} else {
					videoTexture.Update(new Rectangle((player.VideoAssetIndex % 4) * 64, (player.VideoAssetIndex / 4) * 64, 64, 64), (IntPtr) alphaFrame);
				}
			}
		}

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class PlayerViewCursorLocation : CursorLocation, IPlayerViewCursorLocation { }

		private IModel model;
		private Font font = new Font("Copperplate Gothic Bold" , 15.0f , 0, GraphicsUnit.Pixel);
		private IImage loudspeakerIcon = null;
		private IImage soundWavesIcon = null;
		private IImage frameIcon = null;
		private IImage playerIcon = null;
		private IImage microphoneLevelIcon = null;
		private IImage microphoneThresholdIcon = null;
		private IImage handIcon = null;
		private readonly Font handCountFont = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);

		private IVideoTexture videoTexture = null;
		private IImage[] playerVideoFrames = new IImage[16];
	}
}
