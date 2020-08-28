// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Graphics;

namespace ZunTzu.Visualization {

	/// <summary>Sub-component in charge of the display of chat, and text notifications.</summary>
	internal sealed class Prompter : ViewElement, IPrompter {

		public Prompter(View view, Form mainForm) : base(view) {
			this.mainForm = mainForm;

			textBox = new TextBox();
			OnGameDisplayAreaResized();
			textBox.Visible = false;

			mainForm.Controls.Add(textBox);

			textBox.KeyPress += new KeyPressEventHandler(onKeyPress);
		}

		/// <summary>Inserts lines of text at the bottom of the history list.</summary>
		/// <param name="color">Color of the text.</param>
		/// <param name="text">Text to display.</param>
		public void AddTextToHistory(uint color, string text) {
			TextLine line = new TextLine();
			line.Time = 0;
			line.Color = color;
			line.Text = text;

			newTextLines.Enqueue(line);
		}

		/// <summary>Inserts a line of text at the bottom of the history list.</summary>
		/// <param name="color">Color of the text.</param>
		/// <param name="format">The format string.</param>
		/// <param name="args">An array of objects to write using format.</param>
		public void AddTextToHistory(uint color, string format, params object[] args) {
			AddTextToHistory(color, string.Format(format, args));
		}

		/// <summary>Display the input TextBox and sets the text currently displayed in the input TextBox.</summary>
		/// <param name="textToDisplay">Text to display in the input TextBox.</param>
		public void ShowInputBox(string textToDisplay) {
			textBox.Text = textToDisplay;
			textBox.Visible = true;
			textBox.Focus();
			textBox.Select(textBox.TextLength, 0);
		}

		/// <summary>Aborts input and hide the input TextBox.</summary>
		public void HideInputBox() {
			textBox.Visible = false;
			textBox.Text = string.Empty;
		}

		/// <summary>Indicates whether the input TextBox is visible.</summary>
		public bool InputBoxVisible { get { return textBox.Visible; } }

		private const long durationBeforeDecay = 10000000;
		private const long durationBeforeDeletion = 20000000;
		/// <summary>Displays the next frame.</summary>
		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			// remove obsolete text lines
			while(textLines.Count > 0 &&
				textLines.Peek().Time < (currentTimeInMicroseconds - durationBeforeDeletion))
			{
				textLines.Dequeue();
			}

			// add new text lines
			foreach(TextLine line in newTextLines) {
				TextLine timestampedLine = line;
				timestampedLine.Time = currentTimeInMicroseconds;
				textLines.Enqueue(timestampedLine);
			}
			newTextLines.Clear();

			RectangleF area = view.PrompterArea;

			// render text lines from bottom (newest ones) to top (oldest ones)
			int i = 0;
			foreach(TextLine line in textLines) {
				// the older it is, the more transparent
				uint color = line.Color;
				if(currentTimeInMicroseconds - durationBeforeDecay >= line.Time) {
					float transparency = (float) (currentTimeInMicroseconds - durationBeforeDecay - line.Time) / (float) (durationBeforeDeletion - durationBeforeDecay);
					color &= 0x00ffffff | ((uint) (255.0f * (1.0f - transparency)) << 24);
				}

				graphics.DrawText(font, color,
					new RectangleF(area.X, area.Bottom + (i - textLines.Count - 1) * font.Height, area.Width, font.Height),
					StringAlignment.Near,
					line.Text);

				++i;
			}
		}

		/// <summary>Called when the game display area is resized.</summary>
		public override void OnGameDisplayAreaResized() {
			RectangleF area = view.PrompterArea;
			textBox.Location = new Point((int)area.X, (int)area.Bottom - font.Height);
			textBox.Size = new Size((int)area.Width, font.Height);
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			//mainForm.Controls.Remove(textBox);
			OnGameDisplayAreaResized();
			textBox.Visible = false;
			//mainForm.Controls.Add(textBox);
		}

		/// <summary>Occurs when a user has pressed ENTER in the input TextBox.</summary>
		public event TextEnteredHandler TextEntered;

		private void onKeyPress(object o, KeyPressEventArgs e) {
			if(textBox.Visible) {
				int keyChar = (int)(byte)e.KeyChar;
				if(keyChar == 13) {
					// ENTER was pressed
					textBox.Visible = false;
					TextEntered(textBox.Text);
					textBox.Text = string.Empty;
					e.Handled = true;
				} else if(keyChar == 27) {
					// ESCAPE was pressed
					textBox.Visible = false;
					textBox.Text = string.Empty;
					e.Handled = true;
				}
			}
		}

		private Form mainForm;
		private TextBox textBox;
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);

		private struct TextLine {
			public long Time;
			public uint Color;
			public string Text;
		}
		private Queue<TextLine> textLines = new Queue<TextLine>();
		private Queue<TextLine> newTextLines = new Queue<TextLine>();
	}
}
