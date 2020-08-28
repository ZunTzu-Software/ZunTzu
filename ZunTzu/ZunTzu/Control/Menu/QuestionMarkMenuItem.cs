// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using ZunTzu.Control.Dialogs;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class QuestionMarkMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.MenuItems = new MenuItem[] {
				new ZunTzu.Visualization.BackMenuItem(new BackMenuItem(controller.View.Menu.MenuItems)),
				new MenuItem(Resources.MenuHelp, false, new DialogMenuItem(new HelpDialog())),
				new MenuItem(Resources.MenuAboutZunTzu, false, new DialogMenuItem(new AboutDialog()))
			};
		}
	}
}
