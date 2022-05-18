// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using ZunTzu.Control.Dialogs;
using ZunTzu.Networking;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class MultiplayerMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			controller.View.Menu.MenuItems = new MenuItem[] {
				new ZunTzu.Visualization.BackMenuItem(new BackMenuItem(controller.View.Menu.MenuItems)),
				new MenuItem(Resources.MenuHost, (controller.Model.NetworkClient.Status != NetworkStatus.Disconnected), new DialogMenuItem(new HostDialog(controller))),
				new MenuItem(Resources.MenuConnect, (controller.Model.NetworkClient.Status != NetworkStatus.Disconnected), new DialogMenuItem(new ConnectDialog(controller))),
				new MenuItem(Resources.MenuDisconnect, (controller.Model.NetworkClient.Status == NetworkStatus.Disconnected), new DisconnectMenuItem())
			};
		}
	}
}
