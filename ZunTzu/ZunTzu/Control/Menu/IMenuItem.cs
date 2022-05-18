// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Control.Menu {

	/// <summary>Abstract base class for all menu items.</summary>
	public interface IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		void Select(Controller controller);
	}
}
