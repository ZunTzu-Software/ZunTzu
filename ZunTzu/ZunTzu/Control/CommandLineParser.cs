// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ZunTzu.Control.Dialogs;
using ZunTzu.Control.Messages;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization;
using ZunTzu.Networking;
using ZunTzu.Properties;
using ZunTzu.Visualization;

namespace ZunTzu.Control {

	/// <summary>Sub-component in charge of parsing commands entered using the prompter input TextBox.</summary>
	internal sealed class CommandLineParser {

		public CommandLineParser(Controller controller) {
			this.controller = controller;
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="commandText">Command text, stripped of the heading "cmd".</param>
		public void ParseCommand(string commandText) {
			Regex singleWordRegex = new Regex(@"^(?<1>\S*)\s*(?<2>.*)$", RegexOptions.Singleline);
			Match singleWordMatch = singleWordRegex.Match(commandText);
			if(singleWordMatch.Success) {
				string commandWord = singleWordMatch.Groups[1].Value.ToLower();
				if(commandWord == "") {
					// list all available commands
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.AvailableCommands);
					Regex commandNameRegex = new Regex(@"^parseCommand_(?<1>.*)$", RegexOptions.Singleline);
					foreach(MethodInfo methodInfo in typeof(CommandLineParser).GetMethods(
						BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.Instance |
						BindingFlags.InvokeMethod | BindingFlags.NonPublic)) {
						Match commandNameMatch = commandNameRegex.Match(methodInfo.Name);
						if(commandNameMatch.Success) {
							controller.View.Prompter.AddTextToHistory(0xFFFF0000, "         {0}", commandNameMatch.Groups[1].Value);
						}
					}
				} else {
					// run command
					string commandParameters = singleWordMatch.Groups[2].Value;

					try {
						typeof(CommandLineParser).InvokeMember(
							"parseCommand_" + commandWord,
							BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.Instance |
							BindingFlags.InvokeMethod | BindingFlags.NonPublic,
							null, this,
							new object[] { commandParameters });
					} catch(MissingMethodException) {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidCommand, commandWord);
					} catch(TargetInvocationException e) {
						Exception innerException = e.InnerException;
						string message = string.Format(Resources.CommandError, innerException.GetType().Name, innerException.Message);
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, message);
						controller.DialogState.Dialog = new MessageDialog(SystemIcons.Error, message);
						controller.State = controller.DialogState;
						controller.View.ShowDialog(controller.DialogState.Dialog);
					}
				}
			}
		}

		private Controller controller;

		// General commands ---------------------------------------------------

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_quit(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				controller.Quit();
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "Quit");
			}
		}

		// Network commands ---------------------------------------------------

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_host(string parameters) {
			IModel model = controller.Model;
			IView view = controller.View;
			Regex parametersRegex = new Regex(@"^(?<1>\d+)\s*(?<2>(?:copy)?)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(model.NetworkClient.Status != NetworkStatus.Disconnected) {
					view.Prompter.AddTextToHistory(0xFFFF0000, Resources.DisconnectFirst);
				} else {
					int port = int.Parse(parametersMatch.Groups[1].Value);
					bool copyToClipboard = (parametersMatch.Groups[2].Value == "copy");

					// prepare server process
					using(Process serverProcess = new Process()) {
						serverProcess.StartInfo.FileName = Assembly.GetExecutingAssembly().Location;
						serverProcess.StartInfo.Arguments = "-s " + port.ToString();
						serverProcess.StartInfo.UseShellExecute = false;
						serverProcess.StartInfo.RedirectStandardOutput = true;

						// launch server process
						serverProcess.Start();

						// wait for the server to start
						string serverOutput = serverProcess.StandardOutput.ReadLine();

						if(serverOutput.StartsWith("Server started")) {
							// connect to server
							model.IsHosting = true;
							model.StateChangeSequenceNumber = 0;
							model.NetworkClient.Connect("localhost", port);

							Regex publicAddressRegex = new Regex(@"^Server started (?<1>[0-4])/(?<2>[^/]+)/(?<3>[^/]+)", RegexOptions.Singleline);
							Match publicAddressMatch = publicAddressRegex.Match(serverOutput);
							if(publicAddressMatch.Success) {
								InternetConnectivity connectivity = (InternetConnectivity) Enum.Parse(typeof(InternetConnectivity), publicAddressMatch.Groups[1].Value);
								string publicIpAddress = publicAddressMatch.Groups[2].Value;
								string publicPort = publicAddressMatch.Groups[3].Value;

								if(connectivity != InternetConnectivity.Full) {
									FirewallDialog firewallDialog = null;
									switch(connectivity) {
										case InternetConnectivity.Unknown:
											firewallDialog = new FirewallDialog(controller, Connectivity.Limited);
											break;
										case InternetConnectivity.None:
											firewallDialog = new FirewallDialog(controller, Connectivity.NoInternet);
											break;
										case InternetConnectivity.NoEgress:
											firewallDialog = new FirewallDialog(controller, Connectivity.NoEgress);
											break;
										case InternetConnectivity.NoIngress:
											firewallDialog = new FirewallDialog(controller, Connectivity.NoIngress);
											break;
									}
									controller.DialogState.Dialog = firewallDialog;
									controller.State = controller.DialogState;
									controller.View.ShowDialog(firewallDialog);
								}

								if(connectivity != InternetConnectivity.None) {
									view.Prompter.AddTextToHistory(0xFFFF0000,
										(publicPort != "?" ? Resources.ServerListeningNatTraversalEnabled : Resources.ServerListeningNatTraversalDisabled),
										publicIpAddress,
										(publicPort != "?" ? publicPort : port.ToString()));

									if(copyToClipboard) {
										Clipboard.SetText(string.Format(Resources.ConnectionData, publicIpAddress, (publicPort != "?" ? publicPort : port.ToString())));
										view.Prompter.AddTextToHistory(0xFFFF0000, Resources.ConnectionDataCopiedToClipboard);
									}
								} else {
									view.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoInternetConnection);
								}
							} else {
								view.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoInternetConnection);
							}

							Settings.Default.HostPort = port;
						} else {
							// something went wrong
							if(serverOutput == "Invalid Device Address") {
								view.Prompter.AddTextToHistory(0xFFFF0000, Resources.CantStartServerInvalidDeviceAddress, port);

								FirewallDialog firewallDialog = new FirewallDialog(controller, Connectivity.PortInUse);
								controller.DialogState.Dialog = firewallDialog;
								controller.State = controller.DialogState;
								controller.View.ShowDialog(firewallDialog);
							} else {
								view.Prompter.AddTextToHistory(0xFFFF0000, Resources.CantStartServer, serverOutput);
							}
						}
					}
				}
			} else {
				view.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "host", "host <port_number> [copy]");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_hostremote(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>\S+)\s+(?<2>\d+)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(controller.Model.NetworkClient.Status != NetworkStatus.Disconnected) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.DisconnectFirst);
				} else {
					string remoteServer = parametersMatch.Groups[1].Value;
					int port = int.Parse(parametersMatch.Groups[2].Value);

					// connect to server
					controller.Model.IsHosting = true;
					controller.Model.StateChangeSequenceNumber = 0;
					controller.Model.NetworkClient.Connect(remoteServer, port);
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "hostremote", "hostremote <remote_server_address> <port_number>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_connect(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>\S+)\s+(?<2>\d+)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(controller.Model.NetworkClient.Status != NetworkStatus.Disconnected) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.DisconnectFirst);
				} else {
					string host = parametersMatch.Groups[1].Value;
					int port = int.Parse(parametersMatch.Groups[2].Value);
					controller.Model.IsHosting = false;
					controller.Model.NetworkClient.Connect(host, port);

					Settings.Default.ConnectHostNameOrAddress = host;
					Settings.Default.ConnectPort = port;
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "connect", "connect <host_address> <port_number>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_disconnect(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(controller.Model.NetworkClient.Status == NetworkStatus.Disconnected) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotConnected);
				} else {
					controller.Model.IsHosting = true;
					controller.Model.RemoveAllPlayers();
					controller.Model.NetworkClient.Disconnect();
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.YouAreDisconnected);

					controller.Model.AudioManager.PlayAudioFile("Disconnect.wma");
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "disconnect");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_netstats(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				foreach(string statistics in controller.Model.NetworkClient.Statistics)
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, statistics);
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "netstats");
			}
		}

		// File commands ---------------------------------------------------

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_open(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(!controller.Model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					string fileName = parametersMatch.Groups[1].Value;
					switch(Path.GetExtension(fileName).ToUpper()) {
						case ".ZTB": openGameBox(fileName); break;
						case ".ZTS": openGameOrScenario(fileName, true); break;
						default: openGameOrScenario(fileName, false); break;
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "open", "open <game_file_name>");
			}
		}

		private void openGameBox(string fileName) {
			IModel model = controller.Model;

			model.ClearTransientState();
			try {
				model.OpenGameBox(fileName);
				model.GameLibrary.AddReference(model.CurrentGameBox.Reference);
				model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);
			} catch {
				model.OpenGameBox(model.GameLibrary.DefaultGameBox);
				model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);
				throw;
			} finally {
				byte[] gameData;
				using(MemoryStream stream = new MemoryStream()) {
					using(DeflaterOutputStream compressionStream = new DeflaterOutputStream(stream)) {
						model.CurrentGameBox.CurrentGame.Save(compressionStream, false);
						compressionStream.Flush();
					}
					stream.Flush();
					gameData = stream.ToArray();
				}
				++model.StateChangeSequenceNumber;
				controller.NetworkClient.Send(new GameLoadedMessage(model.StateChangeSequenceNumber, gameData));
				controller.View.ResetGraphicsElements();
			}
		}

		private void openGameOrScenario(string fileName, bool isScenario) {
			IModel model = controller.Model;

			string gameBoxName;
			byte[] gameBoxHash;
			using(Stream stream = File.OpenRead(fileName)) {
				model.RetrieveGameBoxInfoFromGameData(stream, out gameBoxName, out gameBoxHash);
			}

			IGameBoxReference gameBoxReference = model.GameLibrary.FindGameBox(gameBoxHash);
			if(gameBoxReference == null) {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.UnableToOpenGame, gameBoxName);
			} else {
				model.ClearTransientState();
				model.OpenGameBox(gameBoxReference.FileName);
				try {
					using(Stream stream = File.OpenRead(fileName)) {
						if(isScenario)
							model.CurrentGameBox.OpenScenarioFromScenarioFile(fileName);
						else
							model.CurrentGameBox.OpenGame(fileName);
					}
				} catch {
					model.OpenGameBox(model.GameLibrary.DefaultGameBox);
					model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);
					throw;
				} finally {
					byte[] gameData;
					using(MemoryStream stream = new MemoryStream()) {
						using(DeflaterOutputStream compressionStream = new DeflaterOutputStream(stream)) {
							model.CurrentGameBox.CurrentGame.Save(compressionStream, false);
							compressionStream.Flush();
						}
						stream.Flush();
						gameData = stream.ToArray();
					}
					++model.StateChangeSequenceNumber;
					controller.NetworkClient.Send(new GameLoadedMessage(model.StateChangeSequenceNumber, gameData));
					controller.View.ResetGraphicsElements();
				}
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_openbox(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					string boxName = parametersMatch.Groups[1].Value;
					if(model.CurrentGameBox.Reference.Name.ToUpper() != boxName.ToUpper()) {
						IGameBoxReference gameBoxReference = model.GameLibrary.FindGameBox(boxName);
						if(gameBoxReference == null) {
							controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.GameBoxNotFound, boxName);
						} else {
							++model.StateChangeSequenceNumber;
							controller.NetworkClient.Send(new GameBoxHasChangedMessage(model.StateChangeSequenceNumber, gameBoxReference.Name, gameBoxReference.Hash));
						}
					} else {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.GameBoxAlreadyOpened, boxName);
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "openbox", "openbox <box_name>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_openscenario(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					model.ClearTransientState();
					string scenarioName = parametersMatch.Groups[1].Value;
					model.CurrentGameBox.OpenBuiltInScenario(scenarioName);
					byte[] gameData;
					using(MemoryStream stream = new MemoryStream()) {
						using(DeflaterOutputStream compressionStream = new DeflaterOutputStream(stream)) {
							model.CurrentGameBox.CurrentGame.Save(compressionStream, false);
							compressionStream.Flush();
						}
						stream.Flush();
						gameData = stream.ToArray();
					}
					++model.StateChangeSequenceNumber;
					controller.NetworkClient.Send(new GameLoadedMessage(model.StateChangeSequenceNumber, gameData));
					controller.View.ResetGraphicsElements();
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "openscenario", "openscenario <scenario_name>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_saveas(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					string fileName = parametersMatch.Groups[1].Value;
					if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox) {
						model.AnimationManager.LaunchAnimationSequence(new SaveGameAnimation(fileName));
						model.CurrentGameBox.CurrentGame.FileName = fileName;
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.GameSaved);
					} else {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoGameToSave);
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "saveas", "saveas <file_name>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_save(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox) {
						string fileName = model.CurrentGameBox.CurrentGame.FileName;
						if(fileName != null) {
							model.AnimationManager.LaunchAnimationSequence(new SaveGameAnimation(fileName));
							controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.GameSaved);
						} else {
							controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.UseSaveasCommand);
						}
					} else {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoGameToSave);
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "Save");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_saveasquit(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					string fileName = parametersMatch.Groups[1].Value;
					if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox) {
						model.AnimationManager.EndAllAnimations();
						string temporaryFileName = Path.GetTempFileName();
						using(Stream stream = File.Open(temporaryFileName, FileMode.Create, FileAccess.Write)) {
							model.CurrentGameBox.CurrentGame.Save(stream, true);
						}
						if(File.Exists(fileName))
							File.Delete(fileName);
						File.Move(temporaryFileName, fileName);
						model.CurrentGameBox.CurrentGame.FileName = fileName;
						controller.Quit();
					} else {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoGameToSave);
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "saveasquit", "saveasquit <file_name>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_savequit(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				IModel model = controller.Model;
				if(!model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox) {
						string fileName = model.CurrentGameBox.CurrentGame.FileName;
						if(fileName != null) {
							model.AnimationManager.EndAllAnimations();
							string temporaryFileName = Path.GetTempFileName();
							using(Stream stream = File.Open(temporaryFileName, FileMode.Create, FileAccess.Write)) {
								model.CurrentGameBox.CurrentGame.Save(stream, true);
							}
							if(File.Exists(fileName))
								File.Delete(fileName);
							File.Move(temporaryFileName, fileName);
							controller.Quit();
						} else {
							controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.UseSaveasquitCommand);
						}
					} else {
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoGameToSave);
					}
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "Savequit");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_restoreautosave(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				if(!controller.Model.IsHosting) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NotHosting);
				} else {
					string fileName = controller.AutosaveFileName;
					if(File.Exists(fileName))
						openGameOrScenario(fileName, true);
					else
						controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.AutosaveNotFound);
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "open", "open <game_file_name>");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_verify(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>.*\S)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				string fileName = parametersMatch.Groups[1].Value;
				foreach(string error in controller.Model.VerifyGameBox(fileName)) {
					controller.View.Prompter.AddTextToHistory(0xFFFF0000, error);
				}
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "verify", "verify <game_box_file_name>");
			}
		}

		// Other commands ---------------------------------------------------

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_videocapture(string parameters) {
			Regex parametersRegex = new Regex(@"^(?<1>on|off)\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				controller.VideoConferencingClient.CaptureEnabled = (parametersMatch.Groups[1].Value == "on");
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.InvalidParameters, "videocapture", "videocapture (on|off)");
			}
		}

		/// <summary>Parses and runs a command.</summary>
		/// <param name="parameters">Parameters for this command.</param>
		[ObfuscationAttribute(Exclude = true)]
		private void parseCommand_testaudio(string parameters) {
			Regex parametersRegex = new Regex(@"^\s*$", RegexOptions.Singleline);
			Match parametersMatch = parametersRegex.Match(parameters);
			if(parametersMatch.Success) {
				controller.Model.AudioManager.PlayAudioFile("Connect.wma");
			} else {
				controller.View.Prompter.AddTextToHistory(0xFFFF0000, Resources.NoParameterSupported, "testaudio");
			}
		}
	}
}
