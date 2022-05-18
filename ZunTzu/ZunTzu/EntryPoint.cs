// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.Win32;
using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using ZunTzu.AudioVideo;
using ZunTzu.Control;
using ZunTzu.Graphics;
using ZunTzu.Modelization;
using ZunTzu.Networking;
using ZunTzu.Properties;
using ZunTzu.Timing;
using ZunTzu.Visualization;

namespace ZunTzu {

	/// <summary>The main entry point for the application.</summary>
	public sealed class EntryPoint {

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		static void Main(string[] args) {

			// required by DirectPlay Voice
			System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

			// depending on the command line arguments, we launch a client or a server
			if(args.Length == 2 && args[0] == "-s") {
				// launch standalone server
				int port = int.Parse(args[1]);
				using(IServer server = new DXServer()) {
					server.Start(port);
				}

			} else {
				// configure ZunTzu to handle .ztb, .ztg and .zts files
				installFileAssociations();

				// parse command line parameters or URL parameters
				string fileToOpen = parseParameters(args);

				// launch client
				System.Windows.Forms.Application.EnableVisualStyles();
#if !DEBUG
				try {
#endif
					// retrieve user settings
					DisplayProperties displayProperties;
					displayProperties.TextureQuality = (TextureQualityType) Settings.Default.DisplayTextureFormat;
					displayProperties.MapsAndCountersDetailLevel = (DetailLevelType) Settings.Default.DisplayMapAndCounterDetail;
					displayProperties.WaitForVerticalBlank = Settings.Default.DisplayWaitForVerticalBlank;
					displayProperties.PreferredFullscreenMode = Settings.Default.DisplayPreferedFullscreenMode;
					displayProperties.DiceModelsDetailLevel = (ModelDetailType) Settings.Default.DisplayDiceModelComplexity;
					displayProperties.GameAspectRatio = (Settings.Default.DisplayWidescreen ? AspectRatioType.SixteenToTen : AspectRatioType.FourToThree);

					AudioProperties audioProperties;
					audioProperties.MuteSoundEffects = Settings.Default.AudioDisableSoundEffects;

					Size windowSize = Settings.Default.DisplayWindowSize;
					System.Windows.Forms.FormWindowState windowState = (Settings.Default.DisplayMaximizeWindow ? System.Windows.Forms.FormWindowState.Maximized : System.Windows.Forms.FormWindowState.Normal);

					string playerFirstName = Settings.Default.PlayerFirstName;
					string playerLastName = Settings.Default.PlayerLastName;
					Guid playerGuid = Settings.Default.PlayerId;
					if(playerGuid == Guid.Empty) {
						// assign a unique persistent id to this player
						do {
							playerGuid = Guid.NewGuid();
						} while(playerGuid == Guid.Empty);
						Settings.Default.PlayerId = playerGuid;
						Settings.Default.Save();
					}

					if(Settings.Default.Language != "")
						Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Settings.Default.Language);

					// initialize mainForm, model, view, controller
					using(MainForm mainForm = new MainForm(windowSize, windowState)) {
						IModel model = new Model(mainForm, audioProperties, playerFirstName, playerLastName, playerGuid);
					IView view = new View(model, mainForm, displayProperties);
						IController controller = new Controller(model, mainForm, view);
						IPrecisionTimer precisionTimer = new PrecisionTimer();

						model.OpenGameBox(model.GameLibrary.DefaultGameBox);
						model.CurrentGameBox.OpenBuiltInScenario(model.CurrentGameBox.StartupScenarioFileName);

						view.ResetGraphicsElements();

						if(controller.AutosaveAvailable &&
							System.Windows.Forms.MessageBox.Show(
								Resources.AutosaveDialogText,
								Resources.AutosaveDialogCaption,
								System.Windows.Forms.MessageBoxButtons.YesNoCancel,
								System.Windows.Forms.MessageBoxIcon.Question,
								System.Windows.Forms.MessageBoxDefaultButton.Button1)
							== System.Windows.Forms.DialogResult.Yes)
						{
							mainForm.Show();
							controller.ExecuteCommand("restoreautosave");
						} else {
							mainForm.Show();

							if(fileToOpen != null)
								controller.ExecuteCommand("open " + fileToOpen);
							else
								view.Prompter.AddTextToHistory(0xFF00FFFF, Resources.Welcome);
						}

						// While the form is still valid, render and process messages
						long currentTimeInMicroseconds = precisionTimer.NowInMicroseconds;
						while(mainForm.Created) {
							view.Render(currentTimeInMicroseconds);
							currentTimeInMicroseconds = precisionTimer.NowInMicroseconds;
							model.AnimationManager.Animate(currentTimeInMicroseconds);
							controller.DoEvents(currentTimeInMicroseconds);
						}
					}
#if !DEBUG
				} catch(Exception e) {
					// display error message, invite to send a report
					try {
						string version = "unknown";
						if(ApplicationDeployment.IsNetworkDeployed)
							version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
						string reportContent = e.ToString();

						// Direct3D drivers?
						if(reportContent.Contains("-2146232832") && reportContent.Contains("Microsoft.DirectX.Direct3D.Manager.GetDeviceCaps")) {
							System.Windows.Forms.MessageBox.Show(
								"Graphics card doesn't meet ZunTzu's minimum requirements. Are the drivers installed?",
								"Can't start ZunTzu",
								System.Windows.Forms.MessageBoxButtons.OK,
								System.Windows.Forms.MessageBoxIcon.Error);
						} else {
							ErrorForm errorForm = new ErrorForm(
								"version " + version + "\r\n" +
								"os " + Environment.OSVersion.VersionString + "\r\n" +
								"framework " + Environment.Version.ToString() + "\r\n" +
								"cpu count " + Environment.ProcessorCount.ToString() + "\r\n" +
								reportContent);
							errorForm.ShowDialog();
							if(errorForm.DialogResult == System.Windows.Forms.DialogResult.OK) {
								// send an error report to www.zuntzu.com
								HttpWebRequest request = (HttpWebRequest) WebRequest.Create(@"http://www.zuntzu.com/forum/reportcrash.php");
								request.UserAgent = "ZunTzu";
								request.Method = "POST";
								request.ContentType = "application/x-www-form-urlencoded";
								Encoding encoding = Encoding.GetEncoding(1252);
								byte[] reportData = encoding.GetBytes("message=" +
									HttpUtility.UrlEncode(
										"version " + version + "\r\n" +
										"os " + Environment.OSVersion.VersionString + "\r\n" +
										"framework " + Environment.Version.ToString() + "\r\n" +
										"cpu count " + Environment.ProcessorCount.ToString() + "\r\n" +
										"[code]" + reportContent + "[/code]",
										encoding));
								request.ContentLength = reportData.Length;
								using(Stream requestStream = request.GetRequestStream()) {
									requestStream.Write(reportData, 0, reportData.Length);
								}
								request.Timeout = 20000;
								WebResponse response = request.GetResponse();
							}
						}
					} catch(Exception) { }
				}
#endif
			}
		}

		private static void installFileAssociations() {
			try {
				string applicationExecutablePath = "\"" + Assembly.GetExecutingAssembly().Location + "\"";
				string openCommand = (ApplicationDeployment.IsNetworkDeployed ?
					"explorer \"" + ApplicationDeployment.CurrentDeployment.UpdateLocation + "?file=%1#ZunTzu.application, Culture=en, PublicKeyToken=bbfc02ea80687e07, processorArchitecture=msil\"" :
					applicationExecutablePath + " \"%1\"");
				RegistryKey key;

				Registry.ClassesRoot.CreateSubKey(".ztb").SetValue("", "ZunTzu.Box.1");
				key = Registry.ClassesRoot.CreateSubKey("ZunTzu.Box.1");
				key.SetValue("", "ZunTzu Game Box");
				key.SetValue("FriendlyTypeName", "@" + applicationExecutablePath + ",1");
				key.CreateSubKey("CurVer").SetValue("", "ZunTzu.Box.1");
				key.CreateSubKey("DefaultIcon").SetValue("", applicationExecutablePath + ",2");
				key.CreateSubKey("Shell\\Open\\Command").SetValue("", openCommand);

				Registry.ClassesRoot.CreateSubKey(".ztg").SetValue("", "ZunTzu.Game.1");
				key = Registry.ClassesRoot.CreateSubKey("ZunTzu.Game.1");
				key.SetValue("", "ZunTzu Game");
				key.SetValue("FriendlyTypeName", "@" + applicationExecutablePath + ",2");
				key.CreateSubKey("CurVer").SetValue("", "ZunTzu.Game.1");
				key.CreateSubKey("DefaultIcon").SetValue("", applicationExecutablePath + ",1");
				key.CreateSubKey("Shell\\Open\\Command").SetValue("", openCommand);

				Registry.ClassesRoot.CreateSubKey(".zts").SetValue("", "ZunTzu.Scenario.1");
				key = Registry.ClassesRoot.CreateSubKey("ZunTzu.Scenario.1");
				key.SetValue("", "ZunTzu Scenario");
				key.SetValue("FriendlyTypeName", "@" + applicationExecutablePath + ",3");
				key.CreateSubKey("CurVer").SetValue("", "ZunTzu.Scenario.1");
				key.CreateSubKey("DefaultIcon").SetValue("", applicationExecutablePath + ",1");
				key.CreateSubKey("Shell\\Open\\Command").SetValue("", openCommand);
			} catch {}
		}

		private static string parseParameters(string[] args) {
			if(ApplicationDeployment.IsNetworkDeployed) {
				string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
				if(activationData != null) {
					Uri url = new Uri(activationData[0]);
					Regex fileRegex = new Regex(@"^\?file=(?<1>[^&]*)$", RegexOptions.Singleline);
					Match fileMatch = fileRegex.Match(Uri.UnescapeDataString(url.Query));
					if(fileMatch.Success)
						return fileMatch.Groups[1].Value;
				}
			} else {
				if(args.Length == 1)
					return args[0];
			}
			return null;
		}
	}
}
