// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Properties;

namespace ZunTzu.Visualization {

	/// <summary>Component in charge of the menu.</summary>
	internal sealed class Menu : ViewElement, IMenu {

		public Menu(IModel model, View view) : base(view) {
			this.model = model;
		}

		/// <summary>False if the menu is hidden.</summary>
		public bool IsVisible { get { return isVisible; } set { isVisible = value; } }

		/// <summary>Items currently displayed.</summary>
		public MenuItem[] MenuItems { get { return menuItems; } set { menuItems = value; } }

		/// <summary>Retrieves the menu item at the given position.</summary>
		/// <param name="position">A position in screen coordinates.</param>
		/// <returns>A menu item, or null if none is found.</returns>
		private MenuItem getItemAtPosition(PointF position) {
			RectangleF area = view.MenuArea;
			if(area.Contains(position)) {
				if(isVisible) {
					RectangleF buttonLocation = new RectangleF(area.X, area.Y, area.Width, 28.0f);
					foreach(MenuItem item in menuItems) {
						if(buttonLocation.Contains(position)) {
							RectangleF leftExtremityBoundingBox = new RectangleF(buttonLocation.X, buttonLocation.Y, 18.0f, 28.0f);
							RectangleF rightExtremityBoundingBox = new RectangleF(buttonLocation.Right - 18.0f, buttonLocation.Y, 18.0f, 28.0f);
							RectangleF middleBoundingBox = new RectangleF(buttonLocation.X + 18.0f, buttonLocation.Y, buttonLocation.Width - (2 * 18.0f), 28.0f);
							if(middleBoundingBox.Contains(position)) {
								return item;
							} else if(leftExtremityBoundingBox.Contains(position)) {
								uint color = buttonImageElements[0].GetColorAtPosition(
									new PointF((position.X - leftExtremityBoundingBox.X - 9.0f) * 4.0f,
									(position.Y - leftExtremityBoundingBox.Y - 14.0f) * 4.0f));
								return ((color & 0xFF000000) != 0x00000000 ? item : null);
							} else if(rightExtremityBoundingBox.Contains(position)) {
								uint color = buttonImageElements[1].GetColorAtPosition(
									new PointF((position.X - rightExtremityBoundingBox.X - 9.0f) * 4.0f,
									(position.Y - rightExtremityBoundingBox.Y - 14.0f) * 4.0f));
								return ((color & 0xFF000000) != 0x00000000 ? item : null);
							}
						}
						buttonLocation.Y += 32.0f;
					}
				} else {
					RectangleF iconBoundingBox = new RectangleF(area.Right - 18.0f, area.Y + 7.0f, 13.0f, 11.0f);
					if(iconBoundingBox.Contains(position)) {
						uint color = buttonImageElements[4].GetColorAtPosition(
							new PointF((position.X - iconBoundingBox.X - 6.5f) * 4.0f,
							(position.Y - iconBoundingBox.Y - 5.5f) * 4.0f));
						return ((color & 0xFF000000) != 0x00000000 ? showMenuSwitch : null);
					}
				}
			}
			return null;
		}

		/// <summary>Reloads all graphics resource.</summary>
		/// <remarks>Called for instance after a game box has just been loaded.</remarks>
		public override void ResetGraphicsElements(IGraphics graphics, ITileSet iconsTileSet) {
			buttonImageElements = new IImage[5] {
				iconsTileSet.ExtractImage(new RectangleF(32.0f * 4, 25.0f * 4, 18.0f * 4, 28.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(50.0f * 4, 25.0f * 4, 18.0f * 4, 28.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(49.0f * 4, 25.0f * 4, 1.0f * 4, 28.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(59.0f * 4, 1.0f * 4, 13.0f * 4, 14.0f * 4)),
				iconsTileSet.ExtractImage(new RectangleF(1.0f * 4, 48.0f * 4, 13.0f * 4, 11.0f * 4))
			};
		}

		public override void Render(IGraphics graphics, long currentTimeInMicroseconds) {
			MenuItem itemAtMousePosition = null;
			if(model.ThisPlayer.CursorLocation is IMenuCursorLocation)
				itemAtMousePosition = ((IMenuCursorLocation) model.ThisPlayer.CursorLocation).Item;
			RectangleF area = view.MenuArea;
			if(isVisible) {
				RectangleF buttonLocation = new RectangleF(area.X, area.Y, area.Width, 28.0f);
				foreach(MenuItem item in menuItems) {
					uint modulationColor = (item == itemAtMousePosition ? 0xFF7FFF7F : 0xFFFFFFFF);
					buttonImageElements[0].Render(
						new RectangleF(buttonLocation.X, buttonLocation.Y, 18.0f, 28.0f),
						(item.IsDisabled ? 0xFFFFFFFF : modulationColor));
					buttonImageElements[1].Render(
						new RectangleF(buttonLocation.Right - 18.0f, buttonLocation.Y, 18.0f, 28.0f),
						(item.IsDisabled ? 0xFFFFFFFF : modulationColor));
					buttonImageElements[2].Render(
						new RectangleF(buttonLocation.X + 18.0f, buttonLocation.Y, buttonLocation.Width - (2 * 18.0f), 28.0f),
						(item.IsDisabled ? 0xFFFFFFFF : modulationColor));
					if(item is SubMenuItem)
						buttonImageElements[3].Render(
							new RectangleF(buttonLocation.Right - 18.0f, buttonLocation.Y + 7.0f, 13.0f, 14.0f),
							(item.IsDisabled ? 0xFF7F7F7F : modulationColor));
					else if(item is BackMenuItem)
						buttonImageElements[3].Render(
							new RectangleF(buttonLocation.Left + 5.0f, buttonLocation.Y + 7.0f, 13.0f, 14.0f),
							(float) Math.PI, (item.IsDisabled ? 0xFF7F7F7F : modulationColor));
					graphics.DrawText(font, (item.IsDisabled ? 0xFF7F7F7F : modulationColor),
						new RectangleF(buttonLocation.X, buttonLocation.Y + 7.0f, buttonLocation.Width, buttonLocation.Height),
						StringAlignment.Center, item.Text);

					buttonLocation.Y += 32.0f;
				}
			} else {
				uint modulationColor = (showMenuSwitch == itemAtMousePosition ? 0xFF7FFF7F : 0xFFFFFFFF);
				buttonImageElements[4].Render(new RectangleF(area.Right - 18.0f, area.Y + 7.0f, 13.0f, 11.0f), modulationColor);

				// tool tip
				if(showMenuSwitch == itemAtMousePosition) {
					graphics.DrawText(font, modulationColor,
						new RectangleF(area.Right - 18.0f - 13.0f, area.Y + 7.0f, 13.0f, 17.0f), StringAlignment.Far,
						Resources.ToolTipShowMenu);
				}
			}
		}

		/// <summary>Updates the mouse cursor location if it is over this view element.</summary>
		/// <param name="cursorLocation">The current mouse cursor position in screen and model coordinates.</param>
		/// <returns>False if it is not over this view element.</returns>
		public override bool ContainsCursorLocation(ref ICursorLocation cursorLocation) {
			PointF screenPosition = cursorLocation.ScreenPosition;
			MenuItem menuItemSelected = getItemAtPosition(screenPosition);
			if(menuItemSelected != null) {
				MenuCursorLocation location = cursorLocation as MenuCursorLocation;
				if(location == null) {
					location = new MenuCursorLocation();
					cursorLocation = location;
				}
				location.Item = menuItemSelected;
				return true;
			} else {
				return false;
			}
		}

		/// <summary>Icon used to make the menu visible.</summary>
		public MenuItem ShowMenuSwitch { get { return showMenuSwitch; } set { showMenuSwitch = value; } }

		/// <summary>Current mouse cursor location, providing it is above this panel.</summary>
		private class MenuCursorLocation : CursorLocation, IMenuCursorLocation {
			/// <summary>The menu item at the mouse cursor position.</summary>
			public MenuItem Item { get { return item; } set { item = value; } }
			private MenuItem item;
		}

		private IModel model;
		private MenuItem[] menuItems = new MenuItem[0];
		private MenuItem showMenuSwitch = null;
		private bool isVisible = false;
		private Font font = new Font("Arial", 14.0f, FontStyle.Bold, GraphicsUnit.Pixel);
		/// <summary>Button used to render the menus.</summary>
		private IImage[] buttonImageElements = null;
	}
}
