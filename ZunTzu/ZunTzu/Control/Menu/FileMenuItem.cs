// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using ZunTzu.Control.Dialogs;
using ZunTzu.Modelization;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Menu {

	/// <summary>A menu item.</summary>
	public sealed class FileMenuItem : IMenuItem {

		/// <summary>Called when the user clicks on this menu item.</summary>
		/// <param name="controller">A controller instance.</param>
		public void Select(Controller controller) {
			IModel model = controller.Model;
			controller.View.Menu.MenuItems = new MenuItem[] {
				new ZunTzu.Visualization.BackMenuItem(new BackMenuItem(controller.View.Menu.MenuItems)),
				new MenuItem(Resources.MenuOpenFile, !model.IsHosting, new OpenFileMenuItem()),
				new MenuItem(Resources.MenuLibrary, false, new DialogMenuItem(new LibraryDialog(controller))),
				new MenuItem(Resources.MenuOpenScenario, (model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || !model.IsHosting), new DialogMenuItem(new ScenarioDialog(controller))),
				new MenuItem(Resources.MenuSaveAs, (model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || !model.IsHosting), new SaveAsMenuItem()),
				new MenuItem(Resources.MenuSave, (model.CurrentGameBox.Reference == model.GameLibrary.DefaultGameBox || model.CurrentGameBox.CurrentGame.FileName == null || !model.IsHosting), new SaveMenuItem()),
				new MenuItem(Resources.MenuQuit, false, new DialogMenuItem(new QuitDialog(controller)))
			};
		}
	}
}
