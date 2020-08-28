// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class DisconnectMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.IsVisible = false;
			controller.ExecuteCommand("disconnect");
		}
	}
}
