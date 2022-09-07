// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Windows.Forms;
using ZunTzu.Graphics;
using ZunTzu.Modelization;

//
//  +------------+     +---------------+     +---------+
//  | Networking |<----+ Modelization  |<----+         |
//  +------------+     +---------------+     |         |     
//                             ^             |         |
//                             |             | Control |
//                             |             |         |
//  +------------+     +-------+-------+     |         |     
//  |  Graphics  |<----+ Visualization |<----+         |
//  +------------+     +---------------+     +---------+
// 

namespace ZunTzu.Visualization {

	public enum ModelDetailType { Complex, Simple };

	/// <summary>Properties of the display.</summary>
	public struct DisplayProperties {
		/// <summary>Indicates if the display adapter should wait for the vertical blank between two frames.</summary>
		public bool WaitForVerticalBlank;
		/// <summary>Indicates the preferred mode when operating in fullscreen</summary>
		/// <remarks>If 0, the current desktop mode will be used.</remarks>
		public int PreferredFullscreenMode;
		/// <summary>Game aspect ratio.</summary>
		/// <remarks>If the screen physical aspect ratio is different than the game aspect ratio, black bands will appear.</remarks>
		public AspectRatioType GameAspectRatio;
	}

	/// <summary>Component in charge of displaying everything.</summary>
	public interface IView {
		/// <summary>Display a dialog box.</summary>
		/// <param name="dialog">Dialog box.</param>
		void ShowDialog(Form dialog);
		/// <summary>User preferences for the display.</summary>
		/// <remarks>Setting the properties will deallocate all resources.</remarks>
		DisplayProperties DisplayProperties { get; set; }
		/// <summary>All eligible fullscreen modes for this display adapter.</summary>
		IFullscreenMode[] EligibleFullscreenModes { get; }
		/// <summary>Toggles between fullscreen and windowed mode.</summary>
		bool Fullscreen { get; set; }
		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		void ResetGraphicsElements();
		/// <summary>Displays the next frame.</summary>
		/// <param name="currentTimeInMicroseconds">Time of this frame.</param>
		void Render(long currentTimeInMicroseconds);
		/// <summary>Determine mouse locations.</summary>
		void UpdateMouseLocations();
		/// <summary>Converts coordinates from model referential to screen referential.</summary>
		/// <param name="modelCoordinates">Location in model coordinates.</param>
		/// <returns>Same location in screen coordinates.</returns>
		/// <remarks>
		/// Screen referential is the referential of the mouse cursor and the display pixels.
		/// Model referential is the referential of the map textures.
		/// </remarks>
		PointF ConvertModelToScreenCoordinates(PointF modelCoordinates);
		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <param name="screenCoordinates">Size in screen coordinates.</param>
		/// <returns>Same size in model coordinates.</returns>
		SizeF ConvertModelToScreenCoordinates(SizeF modelCoordinates);
		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <param name="screenCoordinates">Rectangular area in screen coordinates.</param>
		/// <returns>Same rectangular area in model coordinates.</returns>
		RectangleF ConvertModelToScreenCoordinates(RectangleF modelCoordinates);
		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <param name="screenCoordinates">Location in screen coordinates.</param>
		/// <returns>Same location in model coordinates.</returns>
		PointF ConvertScreenToModelCoordinates(PointF screenCoordinates);
		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <param name="screenCoordinates">Size in screen coordinates.</param>
		/// <returns>Same size in model coordinates.</returns>
		SizeF ConvertScreenToModelCoordinates(SizeF screenCoordinates);
		/// <summary>Converts coordinates from screen referential to model referential.</summary>
		/// <param name="screenCoordinates">Rectangular area in screen coordinates.</param>
		/// <returns>Same rectangular area in model coordinates.</returns>
		RectangleF ConvertScreenToModelCoordinates(RectangleF screenCoordinates);
		/// <summary>Display area in pixels.</summary>
		/// <remarks>The display area is smaller than the screen if black bars are displayed.</remarks>
		Rectangle GameDisplayAreaInPixels { get; }

		/// <summary>Sub-component in charge of the tabs used to change the visible board.</summary>
		ITabs Tabs { get; }
		/// <summary>Sub-component in charge of the display of the content of the selected stack.</summary>
		IStackInspector StackInspector { get; }
		/// <summary>Sub-component in charge of the display of chat, and text notifications.</summary>
		IPrompter Prompter { get; }
		/// <summary>Sub-component in charge of the display of dice.</summary>
		IDiceBag DiceBag { get; }
		/// <summary>Sub-component in charge of the display of the player names.</summary>
		IPlayerView PlayerView { get; }
		/// <summary>Sub-component in charge of the menu.</summary>
		IMenu Menu { get; }
		/// <summary>Sub-component in charge of the player's hand.</summary>
		IHand Hand { get; }

		/// <summary>Mouse cursor used when scrolling.</summary>
		Cursor HandCursor { get; }
		/// <summary>Mouse cursor used when scrolling.</summary>
		Cursor FistCursor { get; }
		/// <summary>Mouse cursor used when zooming.</summary>
		Cursor ZoomCursor { get; }
		/// <summary>Mouse cursor used when pointing.</summary>
		Cursor FingerCursor { get; }
		/// <summary>Mouse cursor used when dragging.</summary>
		Cursor FingerAddCursor { get; }
		/// <summary>Mouse cursor used when dragging.</summary>
		Cursor FingerRemoveCursor { get; }
		/// <summary>Mouse cursor used when hovering above dice.</summary>
		Cursor FingerDoubleCursor { get; }
	}

	/// <summary>Sub-component in charge of the display of the player names.</summary>
	public interface IPlayerView {
		/// <summary>Displays a new video frame from a player's webcam.</summary>
		/// <param name="playerId">The player's id.</param>
		/// <param name="videoFrame">A 64x64 frame buffer in R8G8B8 format.</param>
		void UpdateVideoFrame(UInt64 playerId, byte[] videoFrame);
	}

	/// <summary>Sub-component in charge of the display of the content of the selected stack.</summary>
	public interface IStackInspector {
		/// <summary>Scaling applied to the pieces.</summary>
		float PieceScaling { get; }
	}

	public delegate void TextEnteredHandler(string text);

	/// <summary>Sub-component in charge of the display of chat, and text notifications.</summary>
	public interface IPrompter {
		/// <summary>Inserts a line of text at the bottom of the history list.</summary>
		/// <param name="color">Color of the text.</param>
		/// <param name="text">Text to display.</param>
		void AddTextToHistory(uint color, string text);
		/// <summary>Inserts a line of text at the bottom of the history list.</summary>
		/// <param name="color">Color of the text.</param>
		/// <param name="format">The format string.</param>
		/// <param name="args">An array of objects to write using format.</param>
		void AddTextToHistory(uint color, string format, params object[] args);
		/// <summary>Display the input TextBox and sets the text currently displayed in the input TextBox.</summary>
		/// <param name="textToDisplay">Text to display in the input TextBox.</param>
		void ShowInputBox(string textToDisplay);
		/// <summary>Aborts input and hide the input TextBox.</summary>
		void HideInputBox();
		/// <summary>Indicates whether the input TextBox is visible.</summary>
		bool InputBoxVisible { get; }
		/// <summary>Occurs when a user has pressed ENTER in the input TextBox.</summary>
		event TextEnteredHandler TextEntered;
	}

	/// <summary>Sub-component in charge of the display of dice.</summary>
	public interface IDiceBag {
		/// <summary>Cast a set of dice, all from the same dice hand.</summary>
		/// <param name="diceHandIndex">Index of the dice hand.</param>
		/// <param name="diceResults">Result of each of the dice cast.</param>
		void CastDice(int diceHandIndex, int[] diceResults);
	}

	/// <summary>Sub-component in charge of the tabs used to change the visible board.</summary>
	public interface ITabs {
		/// <summary>Scroll to the first tab position.</summary>
		void ShowFirstTab();
		/// <summary>Scroll to the previous tab position.</summary>
		void ShowPreviousTab();
		/// <summary>Scroll to the next tab position.</summary>
		void ShowNextTab();
		/// <summary>Scroll to the last tab position.</summary>
		void ShowLastTab();
	}

	/// <summary>An item of the menu.</summary>
	public class MenuItem {
		/// <summary>Constructor.</summary>
		public MenuItem(string text, bool isDisabled, object userData) {
			Text = text;
			IsDisabled = isDisabled;
			UserData = userData;
		}
		public string Text;
		public bool IsDisabled;
		public bool IsSubMenu;
		public object UserData;
	}

	/// <summary>An item of the menu which leads to a sub-menu.</summary>
	public sealed class SubMenuItem : MenuItem {
		/// <summary>Constructor.</summary>
		public SubMenuItem(string text, bool isDisabled, object userData) : base(text, isDisabled, userData) {}
	}

	/// <summary>An item of the menu which leads to the parent menu.</summary>
	public sealed class BackMenuItem : MenuItem {
		/// <summary>Constructor.</summary>
		public BackMenuItem(object userData) : base("", false, userData) {}
	}

	/// <summary>Component in charge of the menu.</summary>
	public interface IMenu {
		/// <summary>False if the menu is hidden.</summary>
		bool IsVisible { get; set; }
		/// <summary>Items currently displayed.</summary>
		MenuItem[] MenuItems { get; set; }
		/// <summary>Icon used to make the menu visible.</summary>
		MenuItem ShowMenuSwitch { get; set; }
	}

	/// <summary>Component in charge of the player's hand.</summary>
	public enum HandState { Folded, Unfolding, Unfolded, Folding }
	public interface IHand {
		/// <summary>Scaling applied to the pieces.</summary>
		float PieceScaling { get; set; }
		/// <summary>Height of the hand in screen pixels when fully unfolded.</summary>
		int UnfoldedHeight { get; set; }
		/// <summary>True if the hand is locked in the unfolded position.</summary>
		bool IsPinned { get; set; }
		/// <summary>Folding state of the hand.</summary>
		HandState State { get; }
		/// <summary>True if the hand is not hidden.</summary>
		bool IsVisible { get; set; }
		/// <summary>Makes the hand fold after a wait time.</summary>
		void Fold();
		/// <summary>Makes the hand unfold immediately.</summary>
		void Unfold();
	}

	/// <summary>Current mouse cursor location, providing it is out of the main frame boundaries.</summary>
	public interface IOutOfFrameCursorLocation : ICursorLocation {}

	/// <summary>Current mouse cursor location, providing it is above the visible board.</summary>
	public interface IBoardCursorLocation : ICursorLocation {
		/// <summary>The top-most piece at a mouse cursor position on the visible board.</summary>
		IPiece Piece { get; }
	}

	/// <summary>Current mouse cursor location, providing it is above the player view.</summary>
	public interface IPlayerViewCursorLocation : ICursorLocation {}

	/// <summary>Current mouse cursor location, providing it is above the stack inspector.</summary>
	public enum StackInspectorIcon { None, Close, Recycle, Shuffle, Invert }
	public interface IStackInspectorCursorLocation : ICursorLocation {
		/// <summary>The piece displayed at the mouse cursor position.</summary>
		IPiece Piece { get; }
		/// <summary>The point of the piece under the cursor hotspot.</summary>
		PointF AnchorPosition { get; }
		/// <summary>The index from bottom to top in the stack at the mouse cursor position.</summary>
		int Index { get; }
		/// <summary>The icon displayed at the mouse cursor position.</summary>
		StackInspectorIcon Icon { get; }
	}

	/// <summary>Current mouse cursor location, providing it is above the dice bag.</summary>
	public interface IDiceBagCursorLocation : ICursorLocation {
		/// <summary>Index of the dice hand at the mouse cursor position.</summary>
		int DiceHandIndex { get; }
		/// <summary>Number of dice implied by the mouse cursor position.</summary>
		int DiceCount { get; }
	}

	/// <summary>Current mouse cursor location, providing it is above the tabs.</summary>
	public enum TabsIcon { None, Undo, Redo, Hand, TerrainMode, Stacking, HideReveal, FirstTab, PreviousTab, NextTab, LastTab }
	public interface ITabsCursorLocation : ICursorLocation {
		/// <summary>The board for the tab at the mouse cursor position.</summary>
		IBoard Tab { get; }
		/// <summary>The icon displayed at the mouse cursor position.</summary>
		TabsIcon Icon { get; }
	}

	/// <summary>Current mouse cursor location, providing it is above the menu.</summary>
	public interface IMenuCursorLocation : ICursorLocation {
		/// <summary>The menu item at the mouse cursor position.</summary>
		MenuItem Item { get; }
	}

	/// <summary>Current mouse cursor location, providing it is above the player's hand.</summary>
	public enum HandIcon { None, Pin, Resize }
	public interface IHandCursorLocation : ICursorLocation {
		/// <summary>The piece displayed at the mouse cursor position.</summary>
		IPiece Piece { get; }
		/// <summary>The point of the piece under the cursor hotspot.</summary>
		PointF AnchorPosition { get; }
		/// <summary>The index from left to right in the hand at the mouse cursor position.</summary>
		int Index { get; }
		/// <summary>The icon displayed at the mouse cursor position.</summary>
		HandIcon Icon { get; }
	}
}
