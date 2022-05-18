// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using ZunTzu.Control.Dialogs;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class SettingsMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.MenuItems = new MenuItem[] {
				new ZunTzu.Visualization.BackMenuItem(new BackMenuItem(controller.View.Menu.MenuItems)),
				new MenuItem(Resources.MenuPlayer, false, new DialogMenuItem(new PlayerDialog(controller))),
				new MenuItem(Resources.MenuDisplay, false, new DialogMenuItem(new DisplayDialog(controller))),
				new MenuItem(Resources.MenuVoice, false, new DialogMenuItem(new VoiceDialog(controller))),
				new MenuItem(Resources.MenuVideo, false, new DialogMenuItem(new VideoDialog(controller)))
			};
		}
	}
}
