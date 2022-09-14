// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ZunTzu.Graphics
{

    /// <summary>A single text fragment (a texture stripe), part of the text resource cached in a DXTextCache instance.</summary>
    struct DXCachedTextFragment {
		/// <summary>Texture used to render this text fragment.</summary>
		public D3DTexture Texture;
		/// <summary>Texture coordinates.</summary>
		public RectangleF TextureCoordinates;
	}

	/// <summary>Cache used to optimize text rendering.</summary>
	sealed class DXTextCache : IDisposable {

		/// <summary>Constructor.</summary>
		/// <param name="font">The font of all texts in this cache.</param>
		/// <remarks>You must use one DXTextCache instance per font used in the application.</remarks>
		public DXTextCache(Font font) {
			_font = font;

			// font specific text size
			_descentInPixels = (int) Math.Ceiling(font.FontFamily.GetCellDescent(font.Style) * font.Size / font.FontFamily.GetEmHeight(font.Style));
			_textHeight = font.Height - _descentInPixels + 2 + 1;
		}

		/// <summary>Font used by each text cached in this text cache.</summary>
		public Font Font { get { return _font; } }

		/// <summary>Height of each text cached in this text cache.</summary>
		public int TextHeight { get { return _textHeight; } }

		/// <summary>True if no text is cached in this text cache.</summary>
		public bool Empty { get { return _entries.Count == 0; } }

		/// <summary>Releases all unmanaged resources.</summary>
		public void Dispose() {
			foreach(TexturePoolEntry poolEntry in _texturePool)
				poolEntry.Texture.Dispose();
			_texturePool.Clear();
			_entries.Clear();
		}

		/// <summary>Mark all entries as part of mark-and-sweep garbage collection.</summary>
		/// <remarks>This method should be called at the begining of each frame.</remarks>
		public void MarkAllEntriesAsUnused() {
			foreach(TextCacheEntry textCacheEntry in _entries.Values)
				textCacheEntry.Unused = true;
		}

		/// <summary>Sweeps all marked entries as part of mark-and-sweep garbage collection.</summary>
		/// <remarks>This method should be called at the end of each frame.</remarks>
		public void RemoveUnusedEntries() {
			bool unusedEntriesFound;
			do {
				unusedEntriesFound = false;
				foreach(KeyValuePair<string, TextCacheEntry> dictionaryEntry in _entries) {
					TextCacheEntry entry = dictionaryEntry.Value;
					if(entry.Unused) {
						for(int i = 0; i < entry.TexturePoolEntries.Length; ++i) {
							TexturePoolEntry poolEntry = entry.TexturePoolEntries[i];
							poolEntry.StripeAllocated[entry.StripeIndexes[i]] = false;

							if(!poolEntry.Used) {
								poolEntry.Texture.Dispose();
								_texturePool.Remove(poolEntry);
							}
						}

						_entries.Remove(dictionaryEntry.Key);
						unusedEntriesFound = true;
						break;	// the iterator has been invalidated, so we need a new foreach loop
					}
				}
			} while(unusedEntriesFound);
		}

		/// <summary>Returns a cached text resource, or creates it if it doesn't exist.</summary>
		/// <param name="text">Text to render.</param>
		/// <returns>The cached text resource as an array of text fragments.</returns>
		public DXCachedTextFragment[] GetText(string text, out int textWidthInPixels) {
			TextCacheEntry entry = getOrCreateEntry(text);
			textWidthInPixels = entry.TextWidthInPixels;
			return entry.TextFragments;
		}

		/// <summary>Returns a cached text resource, or creates it if it doesn't exist.</summary>
		/// <param name="text">Text to render.</param>
		/// <returns>The cached text resource as an instance of TextCacheEntry.</returns>
		TextCacheEntry getOrCreateEntry(string text) {
			TextCacheEntry entry;
			if(!_entries.TryGetValue(text, out entry)) {
				// The text was not found in the cache -> create it
				SizeF textSize;
				using(Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb)) {
					using(System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap)) {
						textSize = graphics.MeasureString(text, _font);
					}
				}

				entry = new TextCacheEntry();
				_entries.Add(text, entry);
				entry.Unused = false;
				entry.TextWidthInPixels = 2 + (int) Math.Ceiling(textSize.Width);
				int fragmentsCount = (entry.TextWidthInPixels + 255) / 256;
				entry.TextFragments = new DXCachedTextFragment[fragmentsCount];
				entry.TexturePoolEntries = new TexturePoolEntry[fragmentsCount];
				entry.StripeIndexes = new int[fragmentsCount];

				for(int i = 0; i < fragmentsCount; ++i)
					allocateTextureFragment(out entry.TexturePoolEntries[i], out entry.StripeIndexes[i], out entry.TextFragments[i]);

				renderTextToTextures(entry, text);
			}
			entry.Unused = false;
			return entry;
		}

		/// <summary>Creates a new text resource.</summary>
		/// <param name="cacheEntry">An instance of TextCacheEntry.</param>
		/// <param name="text">Text.</param>
		/// <remarks>The text is white, surrounded by a one pixel wide black edge.</remarks>
		unsafe void renderTextToTextures(TextCacheEntry cacheEntry, string text) {
			using(Bitmap bitmap = new Bitmap(cacheEntry.TextWidthInPixels, _textHeight, PixelFormat.Format24bppRgb)) {
				using(System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap)) {
					graphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, bitmap.Width, _textHeight);
					graphics.DrawString(text, _font, new SolidBrush(Color.White), 1.0f, 2.0f - _descentInPixels);
				}

				BitmapData bitmapData = bitmap.LockBits(
					new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.ReadOnly,
					PixelFormat.Format24bppRgb);

				int width = bitmapData.Width;
				int stride = bitmapData.Stride;

				// build transparency mask (using a 3x3 filter grid)
				ushort[] transparencyMask = new ushort[(width + 2) * (_textHeight + 2)];
				fixed(ushort* mask2 = transparencyMask) {
					{
						ushort* firstLine = mask2;
						ushort* secondLine = firstLine + (width + 2);
						ushort* thirdLine = secondLine + (width + 2);

						byte* source = (byte*) bitmapData.Scan0;
						for(int y = 0; y < _textHeight; ++y) {
							for(int x = 0; x < width; ++x) {
								ushort blue = *source;
								*(firstLine+0) += blue; *(firstLine+1) += blue; *(firstLine+2) += blue;
								*(secondLine+0) += blue; *(secondLine+1) += blue; *(secondLine+2) += blue;
								*(thirdLine+0) += blue; *(thirdLine+1) += blue; *(thirdLine+2) = blue;	// *(thirdLine+2) == 0 -> no need to add
								++firstLine; ++secondLine; ++thirdLine;
								source += 3;
							}
							firstLine += 2; secondLine += 2; thirdLine += 2;
							source += stride - width * 3;
						}
					}

					// copy bitmap to textures
					int fragmentsCount = cacheEntry.TexturePoolEntries.Length; 
					for(int i = 0; i < fragmentsCount; ++i) {
						D3DTexture texture = cacheEntry.TexturePoolEntries[i].Texture;
						int stripeIndex = cacheEntry.StripeIndexes[i];

						int fragmentOrigin = i * 256;
						int fragmentWidth = (i < fragmentsCount - 1 ? 256 : width % 256);

						texture.Lock(out int texturePitch, out byte* textureBits);

						byte* source = (byte*) bitmapData.Scan0 + fragmentOrigin * 3;
						ushort* mask = mask2 + (width + 3) + fragmentOrigin;

						byte* dest = textureBits + (stripeIndex * _textHeight) * texturePitch;
						for(int y = 0; y < _textHeight; ++y) {
							for(int x = 0; x < fragmentWidth; ++x) {
								*(dest+0) = *(source+0);
								*(dest+1) = *(source+1);
								*(dest+2) = *(source+2);
								*(dest+3) = (*mask > 0xFF ? (byte)0xFF : (byte)*mask);
								dest += 4;
								source += 3;
								++mask;
							}
							for(int x = fragmentWidth; x < 256; ++x) {
								*(uint*)dest = 0x00000000;
								dest += 4;
							}
							dest += texturePitch - 256 * 4;
							source += stride - fragmentWidth * 3;
							mask += width + 2 - fragmentWidth;
						}

						texture.Unlock();
					}
				}
			}
		}

		/// <summary>Finds a free stripe on a texture to store a text fragment.</summary>
		/// <param name="texturePoolEntry">Texture pool entry that contains the allocated stripe.</param>
		/// <param name="stripeIndex">Index of the stripe, from top to bottom.</param>
		/// <param name="textFragment">New text fragment.</param>
		void allocateTextureFragment(out TexturePoolEntry texturePoolEntry, out int stripeIndex, out DXCachedTextFragment textFragment) {
			// look for a free stripe on an existing texture
			foreach(TexturePoolEntry poolEntry in _texturePool) {
				for(int i = 0; i < poolEntry.StripeAllocated.Length; ++i) {
					if(!poolEntry.StripeAllocated[i]) {
						poolEntry.StripeAllocated[i] = true;
						texturePoolEntry = poolEntry;
						stripeIndex = i;
						textFragment.Texture = poolEntry.Texture;
						textFragment.TextureCoordinates = new RectangleF(0.0f, (_textHeight * i) / 256.0f, 1.0f, _textHeight / 256.0f);
						return;
					}
				}
			}

			// no stripe available -> allocate a whole new texture
			TexturePoolEntry newPoolEntry = new TexturePoolEntry(_textHeight);
			_texturePool.Add(newPoolEntry);
			newPoolEntry.StripeAllocated[0] = true;
			texturePoolEntry = newPoolEntry;
			stripeIndex = 0;
			textFragment.Texture = newPoolEntry.Texture;
			textFragment.TextureCoordinates = new RectangleF(0.0f, 0.0f, 1.0f, _textHeight / 256.0f);
		}

		/// <summary>A text resource, as a set of text fragments.</summary>
		sealed class TextCacheEntry {
			public bool Unused;	// used in mark-and-sweep garbage collection
			public int TextWidthInPixels;
			public DXCachedTextFragment[] TextFragments;	// ordered set of texture stripes
			public TexturePoolEntry[] TexturePoolEntries;	// location of the stripes (for garbage collection purpose)
			public int[] StripeIndexes;	// location of the stripes (for garbage collection purpose)
		}

		/// <summary>Texture resource, combined with an allocation table.</summary>
		sealed class TexturePoolEntry {
			public TexturePoolEntry(int textHeight) {
				Texture = D3DTexture.Create(256, 256, D3DTextureFormat.A8R8G8B8);
				StripeAllocated = new bool[256 / textHeight];	// all false by default
			}
			public D3DTexture Texture;	// texture
			public bool[] StripeAllocated;	// allocation table
			public bool Used {
				get {
					for(int i = 0; i < StripeAllocated.Length; ++i) {
						if(StripeAllocated[i])
							return true;
					}
					return false;
				}
			}
		}

		readonly Font _font;
		readonly int _textHeight;
		readonly int _descentInPixels;
		IDictionary<string, TextCacheEntry> _entries = new Dictionary<string, TextCacheEntry>();
		List<TexturePoolEntry> _texturePool = new List<TexturePoolEntry>();
	}
}
