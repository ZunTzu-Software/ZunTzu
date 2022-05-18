// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using ZunTzu.Control;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class ShowMenuSwitchMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.IsVisible = true;
			controller.View.Menu.MenuItems = new MenuItem[] {
				new SubMenuItem(Resources.MenuFile, false, new FileMenuItem()),
				new SubMenuItem(Resources.MenuMultiplayer, false, new MultiplayerMenuItem()),
				new SubMenuItem(Resources.MenuSettings, false, new SettingsMenuItem()),
				new SubMenuItem(Resources.MenuQuestionMark, false, new QuestionMarkMenuItem())
			};
		}
	}
}
