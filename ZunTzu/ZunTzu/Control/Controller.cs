// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ZunTzu.AudioVideo;
using ZunTzu.Control.Dialogs;
using ZunTzu.Control.Messages;
using ZunTzu.Control.States;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Networking;
using ZunTzu.Properties;
using ZunTzu.Randomness;
using ZunTzu.Visualization;

namespace ZunTzu.Control {

	/// <summary>
	/// Summary description for Controller.
	/// </summary>
	public sealed class Controller : IController {

		public Controller(IModel model, Form mainForm, IView view) {
			this.mainForm = mainForm;
			this.model = model;
			this.view = view;

			commandLineParser = new CommandLineParser(this);
			networkClient = new NetworkClient(model.NetworkClient);
			videoConferencingClient = new VideoConferencingClient(this);
			dieSimulator = new DieSimulator();

			DraggingPieceState = new DraggingPieceState(this);
			DraggingStackState = new DraggingStackState(this);
			IdleState = new IdleState(this);
			MeasuringState = new MeasuringState(this);
			MovingState = new MovingState(this);
			ScrollingState = new ScrollingState(this);
			SelectingPieceState = new SelectingPieceState(this);
			SelectingStackState = new SelectingStackState(this);
			DialogState = new DialogState(this);
			ResizingHandState = new ResizingHandState(this);
			DraggingHandPieceState = new DraggingHandPieceState(this);

			state = IdleState;

			view.Menu.ShowMenuSwitch = new ZunTzu.Visualization.MenuItem(null, false, new Menu.ShowMenuSwitchMenuItem());

			mainForm.Closing += new CancelEventHandler(onMainFormClosing);
			mainForm.KeyPress += new KeyPressEventHandler(onKeyPress);
			mainForm.KeyDown += new KeyEventHandler(onKeyDown);
			mainForm.KeyUp += new KeyEventHandler(onKeyUp);
			mainForm.MouseDown += new MouseEventHandler(onMouseDown);
			mainForm.MouseMove += new MouseEventHandler(onMouseMove);
			mainForm.MouseUp += new MouseEventHandler(onMouseUp);
			mainForm.MouseDoubleClick += new MouseEventHandler(onMouseDoubleClick);
			mainForm.MouseWheel += new MouseEventHandler(onMouseWheel);

			//view.Tabs.BoardSelected += new BoardSelectedHandler(onBoardSelected);
			view.Prompter.TextEntered += new TextEnteredHandler(onTextEntered);
		}

		/// <summary>Processes mouse, keyboard and network messages.</summary>
		/// <param name="currentTimeInMicroseconds">Time of this frame.</param>
		public void DoEvents(long currentTimeInMicroseconds) {
			// process mouse and keyboard events
			System.Windows.Forms.Application.DoEvents();

			// add a physical measure based on the mouse cursor positions to the entropy pool
			int physicalMeasure = 0;
			foreach(IPlayer player in model.Players) {
				Point position = player.CursorLocation.ScreenPosition;
				physicalMeasure += position.X + position.Y;
			}
			dieSimulator.AddPhysicalMeasure((uint) physicalMeasure);

			// process network messages
			foreach(Message message in networkClient.RetrieveNetworkMessages()) {
				message.Handle(this);
			}

			// autosave every minute
			if(model.IsHosting &&
				model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox &&
				currentTimeInMicroseconds - LastAutosaveTimeInMicroseconds > 60000000L)
			{
				model.AnimationManager.LaunchAnimationSequence(new SaveGameAnimation(AutosaveFileName));
				LastAutosaveTimeInMicroseconds = currentTimeInMicroseconds;
			}

			view.UpdateMouseLocations();

			// cursor visibility
			ICursorLocation cursorLocation = model.ThisPlayer.CursorLocation;
			if(cursorLocation is IOutOfFrameCursorLocation ||
				cursorLocation is IMenuCursorLocation ||
				cursorLocation is IHandCursorLocation ||
				state == DialogState)
			{
				if(model.ThisPlayer.IsCursorVisible) {
					model.ThisPlayer.IsCursorVisible = false;
					networkClient.Send(new HideCursorMessage());
				}
			} else {
				if(!model.ThisPlayer.IsCursorVisible) {
					model.ThisPlayer.IsCursorVisible = true;
					networkClient.Send(new ShowCursorMessage());
				}
			}

			// hand folding/unfolding
			switch(handFoldingActivationState) {
				case HandFoldingActivationState.OutOfActivationArea:
					if(cursorLocation is ITabsCursorLocation || cursorLocation is IHandCursorLocation) {
						if(isZooming || state == ScrollingState) {
							handFoldingActivationState = HandFoldingActivationState.InActivationAreaScrolling;
						} else {
							view.Hand.Unfold();
							handFoldingActivationState = HandFoldingActivationState.InActivationArea;
						}
					}
					break;
				case HandFoldingActivationState.InActivationArea:
					if(state != ResizingHandState && !(cursorLocation is ITabsCursorLocation || cursorLocation is IHandCursorLocation || cursorLocation is IOutOfFrameCursorLocation)) {
						view.Hand.Fold();
						handFoldingActivationState = HandFoldingActivationState.OutOfActivationArea;
					}
					break;
				case HandFoldingActivationState.InActivationAreaScrolling:
					if(!(cursorLocation is ITabsCursorLocation || cursorLocation is IHandCursorLocation || cursorLocation is IOutOfFrameCursorLocation)) {
						handFoldingActivationState = HandFoldingActivationState.OutOfActivationArea;
					}
					break;
			}

			if(!isZooming && !(model.ThisPlayer.CursorLocation is IOutOfFrameCursorLocation))
				state.UpdateCursor(mainForm, view);
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="command">The command to run, with no heading "cmd".</param>
		public void ExecuteCommand(string command) {
			commandLineParser.ParseCommand(command);
		}

		/// <summary>True if ZunTzu exited abruptly leaving an autosave file.</summary>
		public bool AutosaveAvailable {
			get {
				return File.Exists(AutosaveFileName);
			}
		}

		internal string AutosaveFileName {
			get {
				return Path.Combine(
					(ApplicationDeployment.IsNetworkDeployed ?
						ApplicationDeployment.CurrentDeployment.DataDirectory :
						System.Windows.Forms.Application.StartupPath),
					"autosave.ztg");
			}
		}

		/// <summary>Closes ZunTzu.</summary>
		internal void Quit() {
			if(model.NetworkClient.Status != NetworkStatus.Disconnected)
				model.NetworkClient.Disconnect();

			// save user settings
			DisplayProperties displayProperties = view.DisplayProperties;
			Settings.Default.DisplayTextureFormat = (int) displayProperties.TextureQuality;
			Settings.Default.DisplayMapAndCounterDetail = (int) displayProperties.MapsAndCountersDetailLevel;
			Settings.Default.DisplayWaitForVerticalBlank = displayProperties.WaitForVerticalBlank;
			Settings.Default.DisplayPreferedFullscreenMode = displayProperties.PreferredFullscreenMode;
			Settings.Default.DisplayDiceModelComplexity = (int) displayProperties.DiceModelsDetailLevel;
			Settings.Default.DisplayWidescreen = (displayProperties.GameAspectRatio == ZunTzu.Graphics.AspectRatioType.SixteenToTen);

			AudioProperties audioProperties = model.AudioManager.AudioProperties;
			Settings.Default.AudioDisableSoundEffects = audioProperties.MuteSoundEffects;

			Settings.Default.DisplayWindowSize = mainForm.ClientSize;
			Settings.Default.DisplayMaximizeWindow = (mainForm.WindowState == FormWindowState.Maximized);

			Settings.Default.PlayerFirstName = model.ThisPlayer.FirstName;
			Settings.Default.PlayerLastName = model.ThisPlayer.LastName;

			Settings.Default.Save();

			// delete autosave file
			string fileName = AutosaveFileName;
			if(File.Exists(fileName))
				File.Delete(fileName);

			mainForm.Closing -= new CancelEventHandler(onMainFormClosing);
			mainForm.Close();
		}

		internal unsafe void FlashWindow() {
			if(Form.ActiveForm == null) {
				FLASHWINFO info = new FLASHWINFO();
				info.cbSize = (uint) sizeof(FLASHWINFO);
				info.hwnd = mainForm.Handle;
				info.dwFlags = FlashWindowFlags.FLASHW_ALL | FlashWindowFlags.FLASHW_TIMERNOFG;
				info.uCount = 3; // uint.MaxValue;
				info.dwTimeout = 0;
				FlashWindowEx(ref info);
			}
		}

		internal long LastAutosaveTimeInMicroseconds = 0;
		internal Form MainForm { get { return mainForm; } }
		internal IModel Model { get { return model; } }
		internal IView View { get { return view; } }
		internal NetworkClient NetworkClient { get { return networkClient; } }
		internal VideoConferencingClient VideoConferencingClient { get { return videoConferencingClient; } }
		internal IDieSimulator DieSimulator { get { return dieSimulator; } }
		internal State State {
			get { return state; }
			set {
				//view.Prompter.AddTextToHistory(0xFFFFFFFF, value.GetType().ToString());
				state = value;
				if(state != IdleState)
					view.Menu.IsVisible = false;
				if(!isZooming) {
					//mainForm.Cursor = state.Cursor;
					mainForm.Capture = state.MouseCaptured;
				}
			}
		}
		internal DraggingPieceState DraggingPieceState;
		internal DraggingStackState DraggingStackState;
		internal IdleState IdleState;
		internal MeasuringState MeasuringState;
		internal MovingState MovingState;
		internal ScrollingState ScrollingState;
		internal SelectingPieceState SelectingPieceState;
		internal SelectingStackState SelectingStackState;
		internal DialogState DialogState;
		internal ResizingHandState ResizingHandState;
		internal DraggingHandPieceState DraggingHandPieceState;

		private void onMainFormClosing(object sender, CancelEventArgs e) {
			// cancel closing, show quit dialog box
			if(state == DialogState && DialogState.Dialog != null)
				DialogState.Dialog.Close();
			e.Cancel = true;
			DialogState.Dialog = new QuitDialog(this);
			State = DialogState;
			view.Menu.IsVisible = false;
			view.ShowDialog(DialogState.Dialog);
		}

		private void onKeyPress(object o, KeyPressEventArgs e) {
			if(state != DialogState) {
				int keyChar = (int)(byte)e.KeyChar;
				if(keyChar == 27) {
					// Esc was pressed
					State.HandleEscapeKeyPress();
					e.Handled = true;
				} else if(keyChar == 19) {
					// Ctrl+S was pressed
					ExecuteCommand("save");
					e.Handled = true;
				} else if(keyChar == 25) {
					// Ctrl+Y was pressed
					if(model.CommandManager.CanRedo)
						networkClient.Send(new RedoMessage(model.StateChangeSequenceNumber));
					e.Handled = true;
				} else if(keyChar == 26) {
					// Ctrl+Z was pressed
					if(model.CommandManager.CanUndo)
						networkClient.Send(new UndoMessage(model.StateChangeSequenceNumber));
					e.Handled = true;
				} else if(keyChar == 32 && !view.Prompter.InputBoxVisible) {
					// Space was pressed
					State.HandleGrabKeyPress();
					e.Handled = true;
				} else {
					// Another key was pressed -> display command prompt
					if(!view.Prompter.InputBoxVisible) {
						view.Prompter.ShowInputBox(e.KeyChar.ToString());
						e.Handled = true;
					}
				}
			}
		}

		private void onKeyDown(object o, KeyEventArgs e) {
			if(state != DialogState) {
				if(e.Control && !model.ThisPlayer.DeckAutoInspect) {
					model.ThisPlayer.DeckAutoInspect = true;
					networkClient.Send(new EnableDeckAutoInspectMessage());
				}
				if(e.KeyData == Keys.Delete) {
					// Del was pressed
					State.HandleDeleteKeyPress();
					e.Handled = true;
				} else if(e.KeyCode == Keys.Enter && e.Alt) {
					// Alt+Enter was pressed
					e.Handled = true;
					view.Fullscreen = !view.Fullscreen;
				} else if(e.KeyCode == Keys.Tab && e.Control) {
					// Ctrl+Tab or Shift+Ctrl+Tab was pressed
					e.Handled = true;
					IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
					IBoard[] boards = model.CurrentGameBox.CurrentGame.Boards;
					for(int i = 0; i < boards.Length; ++i) {
						if(boards[i] == visibleBoard) {
							int selectedBoardIndex = (i + (e.Shift ? boards.Length - 1 : 1)) % boards.Length;
							if(model.CurrentGameBox.CurrentGame.Mode != Mode.Terrain) {
								while(true) {
									ICounterSheet selectedCounterSheet = boards[selectedBoardIndex] as ICounterSheet;
									if(selectedCounterSheet == null || selectedCounterSheet.Properties.Type != CounterSheetType.Terrain)
										break;
									selectedBoardIndex = (selectedBoardIndex + (e.Shift ? boards.Length - 1 : 1)) % boards.Length;
								}
							}
							networkClient.Send(new VisibleBoardChangedMessage(model.StateChangeSequenceNumber, boards[selectedBoardIndex].Id));
							break;
						}
					}
				} else if(e.KeyData == Keys.Up || e.KeyData == Keys.Down) {
					if(state != DialogState) {
						State.HandleMouseWheel(e.KeyData == Keys.Up ? 120 : -120);
						e.Handled = true;
					}
				}
			}
		}

		private void onKeyUp(object o, KeyEventArgs e) {
			if(state != DialogState) {
				if(!e.Control && model.ThisPlayer.DeckAutoInspect) {
					model.ThisPlayer.DeckAutoInspect = false;
					networkClient.Send(new DisableDeckAutoInspectMessage());
				}
			}
		}

		private void onMouseDown(object o, MouseEventArgs e) {
			if(state != DialogState && view.GameDisplayAreaInPixels.Contains(e.X, e.Y)) {
				if(e.Button == MouseButtons.Right) {
					if(State == ScrollingState) {
						State = MeasuringState;
						model.IsMeasuring = true;
						model.RulerStartPosition = model.ThisPlayer.CursorLocation.ModelPosition;
						model.RulerEndPosition = model.RulerStartPosition;
					} else {
						ICursorLocation location = model.ThisPlayer.CursorLocation;
						isZoomingInHand = location is IHandCursorLocation;
						zoomInvariantScreenPosition = location.ScreenPosition;
						isZooming = true;
						mainForm.Cursor = view.ZoomCursor;
						mainForm.Capture = true;
					}
				} else if(e.Button == MouseButtons.Left) {
					if(isZooming) {
						isZooming = false;
						State = MeasuringState;
						model.IsMeasuring = true;
						model.RulerStartPosition = model.ThisPlayer.CursorLocation.ModelPosition;
						model.RulerEndPosition = model.RulerStartPosition;
					} else {
						State.HandleLeftMouseButtonDown();
					}
				}
			}
		}

		private void onMouseUp(object o, MouseEventArgs e) {
			if(state != DialogState) {
				if(e.Button == MouseButtons.Right) {
					if(State == MeasuringState) {
						State = IdleState;
						model.IsMeasuring = false;
					} else {
						isZooming = false;
						mainForm.Capture = State.MouseCaptured;
					}
				} else if(e.Button == MouseButtons.Left) {
					State.HandleLeftMouseButtonUp();
				}
			}
		}

		private void onMouseDoubleClick(object sender, MouseEventArgs e) {
			if(state != DialogState) {
				if(e.Button == MouseButtons.Right) {
					State.HandleRightMouseDoubleClick();
				} else if(e.Button == MouseButtons.Left) {
					State.HandleLeftMouseDoubleClick();
				}
			}
		}

		private void onMouseMove(object o, MouseEventArgs e) {
			if(e.Location != previousMouseScreenPosition) {
				if(isZooming) {
					float scaling = (float) Math.Pow(1.01, e.Y - previousMouseScreenPosition.Y);
					if(isZoomingInHand) {
						view.Hand.PieceScaling = view.Hand.PieceScaling / scaling;
					} else {
						IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
						RectangleF visibleArea = visibleBoard.VisibleArea;
						Rectangle gameDisplayArea = view.GameDisplayAreaInPixels;

						visibleArea.X += (zoomInvariantScreenPosition.X - gameDisplayArea.X) * visibleArea.Width / gameDisplayArea.Width * (1 - scaling);
						visibleArea.Y += (zoomInvariantScreenPosition.Y - gameDisplayArea.Y) * visibleArea.Height / gameDisplayArea.Height * (1 - scaling);
						visibleArea.Width *= scaling;
						visibleArea.Height = gameDisplayArea.Height * visibleArea.Width / gameDisplayArea.Width;
						visibleBoard.VisibleArea = visibleArea;

						PointF mouseModelPosition = view.ConvertScreenToModelCoordinates(e.Location);
						networkClient.Send(new VisibleAreaChangedMessage(mouseModelPosition, visibleBoard.Id, visibleArea));
					}
				} else {
					State.HandleMouseMove(previousMouseScreenPosition, e.Location);
				}
				previousMouseScreenPosition = e.Location;
			}
		}

		private void onMouseWheel(object o, MouseEventArgs e) {
			if(state != DialogState) {
				State.HandleMouseWheel(e.Delta);
			}
		}

		private void onTextEntered(string text) {
			// determine if it is a chat message or a command
			Regex commandRegex = new Regex(@"^\s*cmd\s*(?<1>.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			Match commandMatch = commandRegex.Match(text);
			if(commandMatch.Success) {
				// parse command
				commandLineParser.ParseCommand(commandMatch.Groups[1].Value);
			} else {
				// display chat message
				networkClient.Send(new ChatMessage(text));
			}
		}

		private enum HandFoldingActivationState { OutOfActivationArea, InActivationArea, InActivationAreaScrolling };

		[DllImport("user32.dll")]
		private static extern int FlashWindowEx(ref FLASHWINFO pwfi);
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct FLASHWINFO {
			public uint cbSize;
			public IntPtr hwnd;
			public FlashWindowFlags dwFlags;
			public uint uCount;
			public uint dwTimeout;
		}
		[Flags]
		private enum FlashWindowFlags : uint {
			FLASHW_ALL = 0x00000003,	// Flash both the window caption and taskbar button. This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.			
			FLASHW_CAPTION = 0x00000001,	// Flash the window caption.
			FLASHW_STOP = 0,	// Stop flashing. The system restores the window to its original state.
			FLASHW_TIMER = 0x00000004,	// Flash continuously, until the FLASHW_STOP flag is set.
			FLASHW_TIMERNOFG = 0x0000000C,	// Flash continuously until the window comes to the foreground.
			FLASHW_TRAY = 0x00000002	// Flash the taskbar button.
		}

		private Form mainForm;
		private IModel model;
		private IView view;
		private NetworkClient networkClient;
		private VideoConferencingClient videoConferencingClient;
		private CommandLineParser commandLineParser;
		private IDieSimulator dieSimulator;
		private State state;
		private bool isZooming = false;
		private bool isZoomingInHand = false;
		private Point previousMouseScreenPosition;
		private Point zoomInvariantScreenPosition;
		private HandFoldingActivationState handFoldingActivationState = HandFoldingActivationState.OutOfActivationArea;
	}
}
