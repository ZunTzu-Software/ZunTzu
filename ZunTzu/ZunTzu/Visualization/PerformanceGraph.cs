// Copyright (c) 2022 ZunTzu Software and contributors

using ZunTzu.Graphics;
using System;
using System.Drawing;

namespace ZunTzu.Visualization {

	/// <summary>Displays a graph of the frame rate.</summary>
	internal sealed class PerformanceGraph {
		/*
		public void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			if(previousTime != 0L) {
				string frameRateText = (1000000.0/(double)(currentTimeInMicroseconds - previousTime)).ToString("F0") + " fps";
				graphics.DrawText(frameRateFont, 0xFFFFFFFF, new RectangleF(10.0f, 10.0f, 200.0f, 50.0f), frameRateText);
			}
			previousTime = currentTimeInMicroseconds;
		}

		private Font frameRateFont = new Font("Arial", 8, FontStyle.Bold);
		private long previousTime = 0L;
		*/

		/*
		public void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			if(previousTime != 0L) {
				frameRates[nextFrameIndex] = (float) (1000000.0 / (double)(currentTimeInMicroseconds - previousTime));
				nextFrameIndex = (nextFrameIndex + 1) % frameRates.Length;

				RectangleF area = new RectangleF(110.0f, 10.0f, 128.0f, 32.0f);

				// render background
				IImage image = graphics.CreateMonochromaticImage(0xFF7F0000);
				graphics.Transparency = 0.5f;
				image.Render(area);
				graphics.Transparency = 0.0f;

				IImage barImage = graphics.CreateMonochromaticImage(0xFF007F00);
				for(int i = 0; i < frameRates.Length; ++i) {
					float frameRate = frameRates[(nextFrameIndex + i) % frameRates.Length];

					// render bar
					barImage.Render(new RectangleF(
						area.X + i * (area.Width / frameRates.Length),
						area.Y + area.Height * (1.0f - frameRate / 120.0f),
						area.Width / frameRates.Length,
						area.Height * frameRate / 120.0f));
				}
			}
			previousTime = currentTimeInMicroseconds;
		}
		*/

		public void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			if(previousTime != 0L) {
				frameRates[nextFrameIndex] = (float) (1000000.0 / (double)(currentTimeInMicroseconds - previousTime));
				nextFrameIndex = (nextFrameIndex + 1) % frameRates.Length;

				RectangleF area = new RectangleF(110.0f, 10.0f, 128.0f, 32.0f);


				float minFrameRate = frameRates[0];
				float maxFrameRate = frameRates[0];
				float meanFrameRate = frameRates[0];
				for(int i = 1; i < frameRates.Length; ++i) {
					if(frameRates[i] < minFrameRate)
						minFrameRate = frameRates[i];
					else if(frameRates[i] > maxFrameRate)
						maxFrameRate = frameRates[i];
					meanFrameRate += frameRates[i];
				}
				meanFrameRate /= frameRates.Length;

				graphics.DrawText(font, 0xFFFFFFFF, area, StringAlignment.Near,
					((int)meanFrameRate).ToString("d3") + " (" + 
					((int)minFrameRate).ToString("d3") + "-" +
					((int)maxFrameRate).ToString("d3") + ")");
			}

			previousTime = currentTimeInMicroseconds;
		}

		private long previousTime = 0L;
		private float[] frameRates = new float[64];
		private int nextFrameIndex = 0;
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
	}
}
