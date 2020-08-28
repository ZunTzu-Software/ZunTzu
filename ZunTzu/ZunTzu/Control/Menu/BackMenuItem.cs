// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class BackMenuItem : IMenuItem {

		public BackMenuItem(MenuItem[] parentMenuItems) {
			this.parentMenuItems = parentMenuItems;
		}

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.MenuItems = parentMenuItems;
		}

		private MenuItem[] parentMenuItems;
	}
}
