// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using ZunTzu.FileSystem;
using ZunTzu.Properties;

namespace ZunTzu.Modelization {

	/// <summary>Properties for a set of dice.</summary>
	internal struct DiceHandProperties {
		/// <summary>Type of dice used.</summary>
		public DiceType DiceType;
		/// <summary>Dice count.</summary>
		public int Count { get { return Colors.Length; } }
		/// <summary>Color of each die.</summary>
		public uint[] Colors;
		/// <summary>Pips color of each die.</summary>
		public uint[] Pips;
		/// <summary>Name of the scanned image file for the texture of each die.</summary>
		public string[] TextureFileName;
	}

	/// <summary>Game box properties.</summary>
	/// <remarks>This is everything you can find in a game box except the rulebook: boards, dice, pieces.</remarks>
	internal sealed class GameBox : IGameBox {
		/// <summary>Reference information for this game box.</summary>
		public IGameBoxReference Reference { get { return reference; } }

		/// <summary>Name of the built-in scenario file to load at startup.</summary>
		public string StartupScenarioFileName { get { return startupScenarioFileName; } }

		/// <summary>Opens a scenario from the game box buit-in scenario list.</summary>
		/// <param name="fileName">Name of the scenario file.</param>
		public void OpenBuiltInScenario(string fileName) {
			IFile scenarioFile = archive.GetFile(fileName);
			using(Stream stream = scenarioFile.Open()) {
				currentGame = new Game(this, ScenarioType.BuiltIn, null, stream);
			}
		}

		/// <summary>Opens a scenario from an external scenario file.</summary>
		/// <param name="fileName">Name of the scenario file.</param>
		public void OpenScenarioFromScenarioFile(string fileName) {
			using(Stream stream = System.IO.File.OpenRead(fileName)) {
				currentGame = new Game(this, ScenarioType.FromScenarioFile, null, stream);
			}
		}

		/// <summary>Opens a previously saved game file.</summary>
		/// <param name="fileName">Name of the game file.</param>
		public void OpenGame(string fileName) {
			using(Stream stream = System.IO.File.OpenRead(fileName)) {
				currentGame = new Game(this, ScenarioType.FromGameFile, fileName, stream);
			}
		}

		/// <summary>Opens a game received over the network.</summary>
		/// <param name="inputStream">The stream from which to load the data.</param>
		public void OpenGame(Stream inputStream) {
			currentGame = new Game(this, ScenarioType.FromOverTheNetwork, null, inputStream);
		}

		/// <summary>Properties of the last opened game or scenario.</summary>
		public IGame CurrentGame { get { return currentGame; } }

		/// <summary>List of all build-in scenarios.</summary>
		public IEnumerable<IScenarioReference> BuiltInScenarios {
			get {
				foreach(string fileName in archive.Files) {
					if(fileName.ToUpper().EndsWith(".ZTS")) {
						yield return new ScenarioReference(archive, fileName);
					}
				}
			}
		}

		/// <summary>Dice sets used in this game.</summary>
		internal DiceHandProperties[] DiceHands { get { return diceHands; } }

		/// <summary>Description of all potentially available maps.</summary>
		internal MapProperties[] Maps { get { return maps; } }

		/// <summary>Description of all potentially available counter sheets.</summary>
		internal CounterSheetProperties[] CounterSheets { get { return counterSheets; } }

		/// <summary>Loads a game box.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		internal GameBox(string fileName) {
			using(SHA1 sha = new SHA1Managed()) {
				using(Stream stream = System.IO.File.OpenRead(fileName)) {
					reference = new GameBoxReference(fileName, sha.ComputeHash(stream));
				}
			}

			archive = new Archive(fileName);
			IFile gameBoxFile = archive.GetFile("game-box.xml");
			XmlDocument xml = new XmlDocument();
			using(Stream stream = gameBoxFile.Open()) {
				xml.Load(stream);
			}

			load(xml);
		}

		private enum ValidationState { ExpectingXml, ExpectingGameBox, ExpectingComponent, ExpectingDice, ExpectingSection, EndOfFile }

		/// <summary>Looks for errors in a game box.</summary>
		/// <param name="fileName">Game box filename.</param>
		/// <returns>A list of errors and warnings.</returns>
		/// <remarks>The first character of each error is the severity: E or W.</remarks>
		internal static IEnumerable<string> VerifyGameBox(string fileName) {
			List<string> errors = new List<string>();
			try {
				IArchive archive = new Archive(fileName);

				List<string> files = new List<string>(archive.Files);
				Dictionary<string, bool> referencedFiles = new Dictionary<string,bool>();
				string currentSheet = null;
				bool currentSheetHasBackImage = false;
				string currentSection = null;
				string currentSectionType = null;

				if(!files.Contains("game-box.xml")) {
					errors.Add("EArchive doesn't contain a \"game-box.xml\" file.");
				} else {
					// read game-box.xml
					IFile gameBoxFile = archive.GetFile("game-box.xml");

					XmlReaderSettings settings = new XmlReaderSettings();
					settings.IgnoreComments = true;
					//settings.IgnoreProcessingInstructions = true;
					settings.IgnoreWhitespace = true;

					using(Stream stream = gameBoxFile.Open()) {
						using(XmlTextReader xmlTextReader = new XmlTextReader(stream)) {
							using(XmlReader xml = XmlReader.Create(stream, settings)) {

								// read node game-box
								ValidationState state = ValidationState.ExpectingXml;
								xml.Read();
								while(state != ValidationState.EndOfFile && !xml.EOF) {
									switch(state) {
										case ValidationState.ExpectingXml:
											state = ValidationState.ExpectingGameBox;
											switch(xml.NodeType) {
												case XmlNodeType.XmlDeclaration:
													xml.Skip();
													break;

												default:
													errors.Add(string.Format("Egame-box.xml, line 1: must begin with a \"<?xml...\" directive."));
													break;
											}
											break;

										case ValidationState.ExpectingGameBox:
											switch(xml.NodeType) {
												case XmlNodeType.Element:
													if(xml.Name != "game-box") {
														goto default;
													} else {
														xml.Read();
														Dictionary<string, bool> attributes = new Dictionary<string, bool>();
														while(xml.NodeType == XmlNodeType.Attribute) {
															switch(xml.Name) {
																case "version":
																	if(xml.Value != "1.0")
																		errors.Add(string.Format("Egame-box.xml, line {0}: deprecated attribute \"version\" can't have a value different than \"1.0\".", xmlTextReader.LineNumber));
																	break;

																case "name":
																	if(xml.Value == "")
																		errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																	break;

																case "description":
																case "copyright":
																	break;

																case "icon":
																	string icon = xml.Value;
																	if(icon != "") {
																		if(!icon.EndsWith(".bmp"))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" should end with \"{2}\".", xmlTextReader.LineNumber, xml.Name, ".bmp"));
																		if(!files.Contains(icon))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" refers to a file that can't be found the game box archive.", xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[icon] = true;
																	}
																	break;

																case "startup-scenario":
																	string startupScenario = xml.Value;
																	if(startupScenario == "") {
																		errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																	} else {
																		if(!startupScenario.EndsWith(".zts"))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" should end with \"{2}\".", xmlTextReader.LineNumber, xml.Name, ".zts"));
																		if(!files.Contains(startupScenario))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" refers to a file that can't be found the game box archive.", xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[startupScenario] = true;
																	}
																	break;

																default:
																	errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
															xml.Read();
														}

														// check missing mandatory attributes
														foreach(string mandatoryAttribute in new string[] { "name", "description", "copyright", "startup-scenario" }) {
															if(!attributes.ContainsKey(mandatoryAttribute))
																errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" missing.", xmlTextReader.LineNumber, xml.Name));
														}

														state = ValidationState.ExpectingComponent;
													}
													break;

												default:
													errors.Add(string.Format("Egame-box.xml must begin with a \"<?xml...\" line followed by \"<game-box...\""));
													return errors;	// fatal error
											}
											break;

										case ValidationState.ExpectingComponent:
											switch(xml.NodeType) {
												case XmlNodeType.Element:
													Dictionary<string, bool> attributes = new Dictionary<string, bool>();
													switch(xml.Name) {
														case "dice-hand":
															xml.Read();
															while(xml.NodeType == XmlNodeType.Attribute) {
																switch(xml.Name) {
																	case "type":
																		List<string> eligibleValues = new List<string>(new string[] { "D4", "D6", "D8", "D10", "D12", "D20" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
																xml.Read();
															}
															state = ValidationState.ExpectingDice;
															break;

														case "map":
															xml.Read();
															while(xml.NodeType == XmlNodeType.Attribute) {
																switch(xml.Name) {
																	case "name":
																		if(xml.Value == "")
																			errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																		break;

																	case "image-file":
																		string imageFile = xml.Value;
																		if(imageFile == "") {
																			errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																		} else {
																			if(!imageFile.EndsWith(".jpg") && !imageFile.EndsWith(".png"))
																				errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" should end with \".jpg\" or \".png\".", xmlTextReader.LineNumber, xml.Name));
																			if(!files.Contains(imageFile))
																				errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" refers to a file that can't be found the game box archive.", xmlTextReader.LineNumber, xml.Name));
																			else
																				referencedFiles[imageFile] = true;
																		}
																		break;

																	case "resolution":
																		List<string> eligibleValues = new List<string>(new string[] { "150 dpi", "300 dpi", "600 dpi" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
																xml.Read();
															}

															// check missing mandatory attributes
															foreach(string mandatoryAttribute in new string[] { "name", "image-file" }) {
																if(!attributes.ContainsKey(mandatoryAttribute))
																	errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" missing.", xmlTextReader.LineNumber, xml.Name));
															}

															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(string.Format("Egame-box.xml, line {0}: expected \"</{1}>\".", xmlTextReader.LineNumber, "map"));
																return errors;	// fatal error
															} else if(xml.Name != "map") {
																errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, "map"));
																return errors;	// fatal error
															}
															break;

														case "counter-sheet":
														case "terrain-sheet":
															currentSheet = xml.Name;
															xml.Read();
															while(xml.NodeType == XmlNodeType.Attribute) {
																switch(xml.Name) {
																	case "name":
																		if(xml.Value == "")
																			errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																		break;

																	case "front-image-file":
																	case "back-image-file":
																	case "front-mask-file":
																	case "back-mask-file":
																		string imageFile = xml.Value;
																		if(imageFile == "") {
																			errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" present but empty.", xmlTextReader.LineNumber, xml.Name));
																		} else {
																			if(xml.Name.Contains("mask")) {
																				if(!imageFile.EndsWith(".jpg") && !imageFile.EndsWith(".png"))
																					errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" should end with \".jpg\" or \".png\".", xmlTextReader.LineNumber, xml.Name));
																			} else {
																				if(!imageFile.EndsWith(".png"))
																					errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" should end with \"{2}\".", xmlTextReader.LineNumber, xml.Name, ".png"));
																			}
																			if(!files.Contains(imageFile))
																				errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" refers to a file that can't be found the game box archive.", xmlTextReader.LineNumber, xml.Name));
																			else
																				referencedFiles[imageFile] = true;
																		}
																		break;

																	case "front-resolution":
																	case "back-resolution":
																		List<string> eligibleValues = new List<string>(new string[] { "150 dpi", "300 dpi", "600 dpi" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
																xml.Read();
															}

															// check missing mandatory attributes
															foreach(string mandatoryAttribute in new string[] { "name", "front-image-file" }) {
																if(!attributes.ContainsKey(mandatoryAttribute))
																	errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" missing.", xmlTextReader.LineNumber, mandatoryAttribute));
															}

															// check dependencies
															currentSheetHasBackImage = attributes.ContainsKey("back-image-file");
															if(!currentSheetHasBackImage) {
																foreach(string dependentAttribute in new string[] { "back-resolution", "back-mask-file" }) {
																	if(attributes.ContainsKey(dependentAttribute))
																		errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" is irrelevant without attribute \"{2}\".", xmlTextReader.LineNumber, dependentAttribute, "back-image-file"));
																}
															}

															state = ValidationState.ExpectingSection;
															break;

														default:
															errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported node \"<{1} \".", xmlTextReader.LineNumber, xml.Name));
															xml.Skip();
															break;
													}
													break;

												case XmlNodeType.EndElement:
													if(xml.Name == "game-box") {
														if(xml.Read())
															errors.Add(string.Format("Egame-box.xml, line {0}: found data after \"</game-box>\".", xmlTextReader.LineNumber));
														state = ValidationState.EndOfFile;
													} else {
														errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, "game-box"));
														return errors;	// fatal error
													}
													break;

												default:
													errors.Add(string.Format("Egame-box.xml, line {0}: expected \"<dice-hand \", \"<map \", \"<counter-sheet \", \"<terrain-sheet \" or \"</game-box>\".", xmlTextReader.LineNumber));
													return errors;	// fatal error
											}
											break;

										case ValidationState.ExpectingDice:
											switch(xml.NodeType) {
												case XmlNodeType.Element:
													Dictionary<string, bool> attributes = new Dictionary<string, bool>();
													switch(xml.Name) {
														case "dice":
															xml.Read();
															while(xml.NodeType == XmlNodeType.Attribute) {
																switch(xml.Name) {
																	case "count":
																		int count;
																		if(!int.TryParse(xml.Value, out count) || count <= 0)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	case "color":
																	case "pips":
																		Regex hexadecimalRegex = new Regex(@"^[0-9a-fA-F]{6}$", RegexOptions.Singleline);
																		Match hexadecimalMatch = hexadecimalRegex.Match(xml.Value);
																		if(!hexadecimalMatch.Success)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		if(attributes.ContainsKey("texture-file"))
																			errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" is incompatible with attribute \"{2}\".", xmlTextReader.LineNumber, xml.Name, "texture-file"));
																		break;

																	case "texture-file":
																		string textureFile = xml.Value;
																		if(!textureFile.EndsWith(".png"))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" should end with \"{2}\".", xmlTextReader.LineNumber, xml.Name, ".png"));
																		if(!files.Contains(textureFile))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" refers to a file that can't be found the game box archive.", xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[textureFile] = true;
																		foreach(string incompatibleAttribute in new string[] { "color", "pips" }) {
																			if(attributes.ContainsKey(incompatibleAttribute))
																				errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"{1}\" is incompatible with attribute \"{2}\".", xmlTextReader.LineNumber, xml.Name, incompatibleAttribute));
																		}
																		break;

																	default:
																		errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
																xml.Read();
															}

															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(string.Format("Egame-box.xml, line {0}: expected \"</{1}>\".", xmlTextReader.LineNumber, "dice"));
																return errors;	// fatal error
															} else if(xml.Name != "dice") {
																errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, "dice"));
																return errors;	// fatal error
															}
															xml.Read();
															break;

														default:
															errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported node \"<{1} \".", xmlTextReader.LineNumber, xml.Name));
															xml.Skip();
															break;
													}
													break;

												case XmlNodeType.EndElement:
													if(xml.Name == "dice-hand") {
														xml.Read();
														state = ValidationState.ExpectingComponent;
													} else {
														errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, "dice-hand"));
														return errors;	// fatal error
													}
													break;

												default:
													errors.Add(string.Format("Egame-box.xml, line {0}: expected \"<dice \" or \"</dice-hand>\".", xmlTextReader.LineNumber));
													return errors;	// fatal error
											}
											break;

										case ValidationState.ExpectingSection:
											switch(xml.NodeType) {
												case XmlNodeType.Element:
													Dictionary<string, bool> attributes = new Dictionary<string, bool>();
													switch(xml.Name) {
														case "counter-section":
														case "card-section":
														case "terrain-section":
															currentSection = xml.Name;
															currentSectionType = null;

															if(currentSheet == "terrain-sheet") {
																if(currentSection != "terrain-section")
																	errors.Add(string.Format("Egame-box.xml, line {0}: terrain sections are the only kind of section that can be used with terrain sheets.", xmlTextReader.LineNumber));
															} else {
																if(currentSection == "terrain-section")
																	errors.Add(string.Format("Egame-box.xml, line {0}: terrain sections can only be used with terrain sheets.", xmlTextReader.LineNumber));
															}

															xml.Read();
															while(xml.NodeType == XmlNodeType.Attribute) {
																switch(xml.Name) {
																	case "type":
																		List<string> eligibleValues;
																		switch(currentSection) {
																			case "terrain-section":
																				errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																				goto case "counter-section";

																			case "counter-section":
																				eligibleValues = new List<string>(new string[] { "FrontSideOnly", "BackSideOnly", "TwoSided" });
																				if(!eligibleValues.Contains(xml.Value))
																					errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else if(!currentSheetHasBackImage && xml.Value != "FrontSideOnly")
																					errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\" because no back image.", xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else
																					currentSectionType = xml.Value;
																				break;

																			case "card-section":
																				eligibleValues = new List<string>(new string[] { "CardFacesOnFront", "CardFacesOnBack", "CardFacesAndBackOnFront", "CardFacesOnBackBackOnOtherSide", "CardFacesOnFrontBackOnOtherSide", "CardFacesAndBackOnBack" });
																				if(!eligibleValues.Contains(xml.Value))
																					errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else if(!currentSheetHasBackImage && xml.Value != "CardFacesOnFront" && xml.Value != "CardFacesAndBackOnFront")
																					errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\" because no back image.", xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else
																					currentSectionType = xml.Value;
																				break;
																		}
																		break;

																	case "counter-type":
																		List<string> eligibleCTValues;
																		switch (currentSection) {
																			case "terrain-section":
																			case "card-section":
																				errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																				goto case "counter-section";

																			case "counter-section":
																				eligibleCTValues = new List<string>(new string[] { "Counter", "Block", "Concealed" });
																				if (!eligibleCTValues.Contains(xml.Value))
																					errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));																				
																				else
																					currentSectionType = xml.Value;
																				break;
																		}
																		break;

																	case "block-color":
																		Regex hexadecimalRegex = new Regex(@"^[0-9a-fA-F]{6}$", RegexOptions.Singleline);
																		Match hexadecimalMatch = hexadecimalRegex.Match(xml.Value);
																		if (!hexadecimalMatch.Success)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	case "rows":
																	case "columns":
																	case "supply":
																		int count;
																		if(!int.TryParse(xml.Value, out count) || count <= 0)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		if(xml.Name == "supply" && currentSection == "terrain-section")
																			errors.Add(string.Format("Wgame-box.xml, line {0}: attribute \"supply\" is irrelevant for a terrain section.", xmlTextReader.LineNumber));
																		break;

																	case "face-left":
																	case "face-right":
																	case "face-top":
																	case "face-bottom":
																	case "front-left":
																	case "front-right":
																	case "front-top":
																	case "front-bottom":
																	case "back-left":
																	case "back-right":
																	case "back-top":
																	case "back-bottom":
																		if(currentSection == "card-section" && xml.Name.StartsWith("front-"))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" is irrelevant for a card section.", xmlTextReader.LineNumber, xml.Name));
																		if(currentSection != "card-section" && xml.Name.StartsWith("face-"))
																			errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" is relevant for a card section only.", xmlTextReader.LineNumber, xml.Name));
																		float pixels;
																		if(!float.TryParse(xml.Value, out pixels))
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;
																	
																	case "shadow":
																		float length;
																		if(!float.TryParse(xml.Value, out length) || length < 0.0f)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;
																	
																	case "block-thickness":
																	case "block-sticker-reduction":
																		float lengthFrame;
																		if (!float.TryParse(xml.Value, out lengthFrame) || lengthFrame < 0.0f || lengthFrame > 100.0f)
																			errors.Add(string.Format("Egame-box.xml, line {0}: invalid value \"{1}\" for attribute \"{2}\".", xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(string.Format("Wgame-box.xml, line {0}: duplicate attribute \"{1}\".", xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
																xml.Read();
															}

															// check missing mandatory attributes
															string[] faceAttributes = { "face-top", "face-left", "face-bottom", "face-right" };
															string[] frontAttributes = { "front-top", "front-left", "front-bottom", "front-right" };
															string[] backAttributes = { "back-top", "back-left", "back-bottom", "back-right" };
															if(currentSection == "card-section") {
																foreach(string mandatoryAttribute in faceAttributes) {
																	if(!attributes.ContainsKey(mandatoryAttribute))
																		errors.Add(string.Format("Egame-box.xml, line {0}: mandatory attribute \"{1}\" missing.", xmlTextReader.LineNumber, mandatoryAttribute));
																}
																if(currentSectionType == null) {
																	foreach(string backAttribute in backAttributes) {
																		if(attributes.ContainsKey(backAttribute)) {
																			foreach(string mandatoryAttribute in backAttributes) {
																				if(!attributes.ContainsKey(mandatoryAttribute))
																					errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" is invalid without attribute \"{2}\".", xmlTextReader.LineNumber, backAttribute, mandatoryAttribute));
																			}
																			break;
																		}
																	}
																} else {
																	if(new List<string>(new string[] { "CardFacesAndBackOnFront", "CardFacesOnBackBackOnOtherSide", "CardFacesOnFrontBackOnOtherSide", "CardFacesAndBackOnBack" }).Contains(currentSectionType)) {
																		foreach(string mandatoryAttribute in backAttributes) {
																			if(!attributes.ContainsKey(mandatoryAttribute))
																				errors.Add(string.Format("Egame-box.xml, line {0}: section type is \"{1}\" so you must provide attribute \"{2}\".", xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																		}
																	} else {
																		foreach(string invalidAttribute in backAttributes) {
																			if(attributes.ContainsKey(invalidAttribute))
																				errors.Add(string.Format("Egame-box.xml, line {0}: section type is \"{1}\" so attribute \"{2}\" is invalid.", xmlTextReader.LineNumber, currentSectionType, invalidAttribute));
																		}
																	}
																}
															} else {
																if(new List<string>(new string[] { "FrontSideOnly", "TwoSided" }).Contains(currentSectionType)) {
																	foreach(string mandatoryAttribute in frontAttributes) {
																		if(!attributes.ContainsKey(mandatoryAttribute))
																			errors.Add(string.Format("Egame-box.xml, line {0}: section type is \"{1}\" so you must provide attribute \"{2}\".", xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																	}
																}
																if(new List<string>(new string[] { "BackSideOnly", "TwoSided" }).Contains(currentSectionType)) {
																	foreach(string mandatoryAttribute in backAttributes) {
																		if(!attributes.ContainsKey(mandatoryAttribute))
																			errors.Add(string.Format("Egame-box.xml, line {0}: section type is \"{1}\" so you must provide attribute \"{2}\".", xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																	}
																}
																if(currentSectionType == null) {
																	bool locationAttributeFound = false;
																	foreach(string frontAttribute in frontAttributes) {
																		if(attributes.ContainsKey(frontAttribute)) {
																			locationAttributeFound = true;
																			foreach(string mandatoryAttribute in frontAttributes) {
																				if(!attributes.ContainsKey(mandatoryAttribute))
																					errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" is invalid without attribute \"{2}\".", xmlTextReader.LineNumber, frontAttribute, mandatoryAttribute));
																			}
																			break;
																		}
																	}
																	foreach(string backAttribute in backAttributes) {
																		if(attributes.ContainsKey(backAttribute)) {
																			if(!currentSheetHasBackImage)
																				errors.Add(string.Format("Egame-box.xml, line {0}: invalid attribute \"{1}\" because no back image.", xmlTextReader.LineNumber, xml.Name));
																			locationAttributeFound = true;
																			foreach(string mandatoryAttribute in backAttributes) {
																				if(!attributes.ContainsKey(mandatoryAttribute))
																					errors.Add(string.Format("Egame-box.xml, line {0}: attribute \"{1}\" is invalid without attribute \"{2}\".", xmlTextReader.LineNumber, backAttribute, mandatoryAttribute));
																			}
																			break;
																		}
																	}
																	if(!locationAttributeFound)
																		errors.Add(string.Format("Egame-box.xml, line {0}: location attributes \"front-...\" and/or \"back-...\" missing.", xmlTextReader.LineNumber));
																}
															}

															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(string.Format("Egame-box.xml, line {0}: expected \"</{1}>\".", xmlTextReader.LineNumber, currentSection));
																return errors;	// fatal error
															} else if(xml.Name != currentSection) {
																errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, currentSection));
																return errors;	// fatal error
															}
															xml.Read();
															break;

														default:
															errors.Add(string.Format("Wgame-box.xml, line {0}: unsupported node \"<{1} \".", xmlTextReader.LineNumber, xml.Name));
															xml.Skip();
															break;
													}
													break;

												case XmlNodeType.EndElement:
													if(xml.Name == "dice-hand") {
														xml.Read();
														state = ValidationState.ExpectingComponent;
													} else {
														errors.Add(string.Format("Egame-box.xml, line {0}: found \"</{1}>\" but expected \"</{2}>\".", xmlTextReader.LineNumber, xml.Name, "dice-hand"));
														return errors;	// fatal error
													}
													break;

												default:
													errors.Add(string.Format("Egame-box.xml, line {0}: expected \"<dice \" or \"</dice-hand>\".", xmlTextReader.LineNumber));
													return errors;	// fatal error
											}
											break;
									}
								}
							}
						}
					}
				}
			} catch(Exception e) {
				errors.Add(string.Format("EError: {0}.", e.Message));
			}
			return errors;
		}

		/// <summary>Loads a game box.</summary>
		/// <param name="reference">Library reference of a game box.</param>
		internal GameBox(GameBoxReference reference) {
			using(SHA1 sha = new SHA1Managed()) {
				try {
					using(Stream stream = System.IO.File.OpenRead(reference.FileName)) {
						if(!GameBoxReference.HashAreIdentical(sha.ComputeHash(stream), reference.Hash))
							throw new ApplicationException("Game box file has been modified.");
					}
				} catch(System.IO.FileNotFoundException) {
					throw new System.IO.FileNotFoundException(string.Format(Resources.GameBoxFileMoved, reference.FileName));
				}
			}

			this.reference = reference;
			archive = new Archive(reference.FileName);
			IFile gameBoxFile = archive.GetFile("game-box.xml");
			XmlDocument xml = new XmlDocument();
			using(Stream stream = gameBoxFile.Open()) {
				xml.Load(stream);
			}

			load(xml);
		}

		/// <summary>Loads a game box from a XML document.</summary>
		/// <param name="xml">XML document.</param>
		private void load(XmlDocument xml) {
			XmlElement rootNode = (XmlElement) xml.SelectSingleNode("/game-box");
			string iconFileName;

			reference.Name = rootNode.GetAttribute("name");
			reference.Description = rootNode.GetAttribute("description");
			reference.Copyright = rootNode.GetAttribute("copyright");
			startupScenarioFileName = rootNode.GetAttribute("startup-scenario");

			iconFileName = (rootNode.HasAttribute("icon") ? rootNode.GetAttribute("icon") : "icon.bmp");

			XmlNodeList diceHandNodeList = rootNode.SelectNodes("dice-hand");
			diceHands = new DiceHandProperties[diceHandNodeList.Count];
			for(int i = 0; i < diceHandNodeList.Count; ++i) {
				XmlElement diceHandNode = (XmlElement) diceHandNodeList[i];
				diceHands[i].DiceType = (diceHandNode.HasAttribute("type") ? (DiceType)Enum.Parse(typeof(DiceType), diceHandNode.GetAttribute("type")) : DiceType.D6);

				XmlNodeList diceNodeList = diceHandNode.SelectNodes("dice");
				int diceCount = 0;
				for(int j = 0; j < diceNodeList.Count; ++j)
					diceCount += (((XmlElement) diceNodeList[j]).HasAttribute("count") ? XmlConvert.ToInt32(((XmlElement) diceNodeList[j]).GetAttribute("count")) : 1);
				diceHands[i].Colors = new uint[diceCount];
				diceHands[i].Pips = new uint[diceCount];
				diceHands[i].TextureFileName = new string[diceCount];

				int diceIndex = 0;
				for(int j = 0; j < diceNodeList.Count; ++j) {
					XmlElement diceNode = (XmlElement) diceNodeList[j];
					int count = (diceNode.HasAttribute("count") ? XmlConvert.ToInt32(diceNode.GetAttribute("count")) : 1);
					uint color = 0xff000000 | (diceNode.HasAttribute("color") ? UInt32.Parse(diceNode.GetAttribute("color"), System.Globalization.NumberStyles.AllowHexSpecifier) : 0xffffff);
					uint pips = 0xff000000 | (diceNode.HasAttribute("pips") ? UInt32.Parse(diceNode.GetAttribute("pips"), System.Globalization.NumberStyles.AllowHexSpecifier) : 0x000000);
					string textureFileName = (diceNode.HasAttribute("texture-file") ? diceNode.GetAttribute("texture-file") : null);
					for(int k = 0; k < count; ++k) {
						diceHands[i].Colors[diceIndex] = color;
						diceHands[i].Pips[diceIndex] = pips;
						diceHands[i].TextureFileName[diceIndex] = textureFileName;
						++diceIndex;
					}
				}
			}

			XmlNodeList mapNodeList = rootNode.SelectNodes("map");
			maps = new MapProperties[mapNodeList.Count];
			for(int i = 0; i < maps.Length; ++i) {
				XmlElement mapNode = (XmlElement) mapNodeList[i];
				maps[i] = new MapProperties();
				maps[i].Name = mapNode.GetAttribute("name");
				maps[i].ImageFileName = mapNode.GetAttribute("image-file");
				maps[i].ImageResolution = (mapNode.HasAttribute("resolution") ? parseImageResolution(mapNode.GetAttribute("resolution")) : ImageResolution.Dpi150);
			}

			XmlNodeList counterSheetNodeList = rootNode.SelectNodes("counter-sheet|terrain-sheet");
			counterSheets = new CounterSheetProperties[counterSheetNodeList.Count];
			for(int i = 0; i < counterSheets.Length; ++i) {
				XmlElement counterSheetNode = (XmlElement) counterSheetNodeList[i];
				counterSheets[i] = new CounterSheetProperties();
				counterSheets[i].Type = (counterSheetNode.Name == "terrain-sheet" ? CounterSheetType.Terrain : CounterSheetType.Piece);
				counterSheets[i].Name = counterSheetNode.GetAttribute("name");
				if(counterSheetNode.HasAttribute("front-image-file")) {
					counterSheets[i].FrontImageFileName = counterSheetNode.GetAttribute("front-image-file");
					counterSheets[i].FrontImageResolution = (counterSheetNode.HasAttribute("front-resolution") ? parseImageResolution(counterSheetNode.GetAttribute("front-resolution")) : ImageResolution.Dpi150);
				}
				if(counterSheetNode.HasAttribute("back-image-file")) {
					counterSheets[i].BackImageFileName = counterSheetNode.GetAttribute("back-image-file");
					counterSheets[i].BackImageResolution = (counterSheetNode.HasAttribute("back-resolution") ? parseImageResolution(counterSheetNode.GetAttribute("back-resolution")) : ImageResolution.Dpi150);
				}
				if(counterSheetNode.HasAttribute("front-mask-file")) {
					counterSheets[i].FrontMaskFileName = counterSheetNode.GetAttribute("front-mask-file");
				}
				if(counterSheetNode.HasAttribute("back-mask-file")) {
					counterSheets[i].BackMaskFileName = counterSheetNode.GetAttribute("back-mask-file");
				}
				XmlNodeList counterSectionNodeList = counterSheetNode.SelectNodes("counter-section|terrain-section");
				counterSheets[i].CounterSections = new CounterSectionProperties[counterSectionNodeList.Count];
				for(int j = 0; j < counterSheets[i].CounterSections.Length; ++j) {
					CounterSectionProperties properties = new CounterSectionProperties();
					XmlElement counterSectionNode = (XmlElement) counterSectionNodeList[j];
					properties.Type = (counterSectionNode.HasAttribute("type") ? (CounterSectionType) Enum.Parse(typeof(CounterSectionType), counterSectionNode.GetAttribute("type")) :
						(CounterSectionType) ((counterSectionNode.HasAttribute("front-left") ? 0x01 : 0x00) | (counterSectionNode.HasAttribute("back-left") ? 0x02 : 0x00)));
					properties.CounterType = counterSectionNode.HasAttribute("counter-type") ? (CounterType)Enum.Parse(typeof(CounterType), counterSectionNode.GetAttribute("counter-type")) : CounterType.Counter;
					if (properties.CounterType == CounterType.Block)
					{
						properties.BlockThickness = counterSectionNode.HasAttribute("block-thickness") ? float.Parse(counterSectionNode.GetAttribute("block-thickness")) : float.MinValue;
						properties.BlockColor = /*0xff000000 |*/ (counterSectionNode.HasAttribute("block-color") && properties.CounterType == CounterType.Block ? UInt32.Parse(counterSectionNode.GetAttribute("block-color"), System.Globalization.NumberStyles.AllowHexSpecifier) : 0xffffff);
						properties.BlockStickerReduction = counterSectionNode.HasAttribute("block-sticker-reduction") ? float.Parse(counterSectionNode.GetAttribute("block-sticker-reduction")) : 0.0f;
					}
                    else
                    {
						properties.BlockThickness = 0.0f;
						properties.BlockColor = 0xffffff;
						properties.BlockStickerReduction = 0.0f;
					}

					properties.Rows = (counterSectionNode.HasAttribute("rows") ? XmlConvert.ToInt32(counterSectionNode.GetAttribute("rows")) : 1);
					properties.Columns = (counterSectionNode.HasAttribute("columns") ? XmlConvert.ToInt32(counterSectionNode.GetAttribute("columns")) : 1);
					if(((int) properties.Type & 0x01) != 0) {
						properties.FrontImageLocation.X = XmlConvert.ToSingle(counterSectionNode.GetAttribute("front-left"));
						properties.FrontImageLocation.Y = XmlConvert.ToSingle(counterSectionNode.GetAttribute("front-top"));
						properties.FrontImageLocation.Width = XmlConvert.ToSingle(counterSectionNode.GetAttribute("front-right")) - properties.FrontImageLocation.X;
						properties.FrontImageLocation.Height = XmlConvert.ToSingle(counterSectionNode.GetAttribute("front-bottom")) - properties.FrontImageLocation.Y;
					}
					if(((int) properties.Type & 0x02) != 0) {
						properties.BackImageLocation.X = XmlConvert.ToSingle(counterSectionNode.GetAttribute("back-left"));
						properties.BackImageLocation.Y = XmlConvert.ToSingle(counterSectionNode.GetAttribute("back-top"));
						properties.BackImageLocation.Width = XmlConvert.ToSingle(counterSectionNode.GetAttribute("back-right")) - properties.BackImageLocation.X;
						properties.BackImageLocation.Height = XmlConvert.ToSingle(counterSectionNode.GetAttribute("back-bottom")) - properties.BackImageLocation.Y;
					}
					properties.ShadowLength = (counterSectionNode.HasAttribute("shadow") ? XmlConvert.ToSingle(counterSectionNode.GetAttribute("shadow")) : (counterSheets[i].Type == CounterSheetType.Terrain ? 0.0f : 20.0f));
					properties.Supply = (counterSectionNode.HasAttribute("supply") && counterSheets[i].Type != CounterSheetType.Terrain ? XmlConvert.ToInt32(counterSectionNode.GetAttribute("supply")) : 1);
					counterSheets[i].CounterSections[j] = properties;
				}
				XmlNodeList cardSectionNodeList = counterSheetNode.SelectNodes("card-section");
				counterSheets[i].CardSections = new CardSectionProperties[cardSectionNodeList.Count];
				for(int j = 0; j < counterSheets[i].CardSections.Length; ++j) {
					CardSectionProperties properties = new CardSectionProperties();
					XmlElement cardSectionNode = (XmlElement) cardSectionNodeList[j];
					properties.Type = (cardSectionNode.HasAttribute("type") ? (CounterSectionType) Enum.Parse(typeof(CounterSectionType), cardSectionNode.GetAttribute("type")) :
						cardSectionNode.HasAttribute("back-left") ? CounterSectionType.CardFacesAndBackOnFront : CounterSectionType.CardFacesOnFront);
					properties.Rows = (cardSectionNode.HasAttribute("rows") ? XmlConvert.ToInt32(cardSectionNode.GetAttribute("rows")) : 1);
					properties.Columns = (cardSectionNode.HasAttribute("columns") ? XmlConvert.ToInt32(cardSectionNode.GetAttribute("columns")) : 1);
					properties.FaceImageLocation.X = XmlConvert.ToSingle(cardSectionNode.GetAttribute("face-left"));
					properties.FaceImageLocation.Y = XmlConvert.ToSingle(cardSectionNode.GetAttribute("face-top"));
					properties.FaceImageLocation.Width = XmlConvert.ToSingle(cardSectionNode.GetAttribute("face-right")) - properties.FaceImageLocation.X;
					properties.FaceImageLocation.Height = XmlConvert.ToSingle(cardSectionNode.GetAttribute("face-bottom")) - properties.FaceImageLocation.Y;
					if(properties.Type >= CounterSectionType.CardFacesAndBackOnFront) {
						properties.BackImageLocation.X = XmlConvert.ToSingle(cardSectionNode.GetAttribute("back-left"));
						properties.BackImageLocation.Y = XmlConvert.ToSingle(cardSectionNode.GetAttribute("back-top"));
						properties.BackImageLocation.Width = XmlConvert.ToSingle(cardSectionNode.GetAttribute("back-right")) - properties.BackImageLocation.X;
						properties.BackImageLocation.Height = XmlConvert.ToSingle(cardSectionNode.GetAttribute("back-bottom")) - properties.BackImageLocation.Y;
					}
					properties.ShadowLength = (cardSectionNode.HasAttribute("shadow") ? XmlConvert.ToSingle(cardSectionNode.GetAttribute("shadow")) : (counterSheets[i].Type == CounterSheetType.Terrain ? 0.0f : 5.0f));
					properties.Supply = (cardSectionNode.HasAttribute("supply") && counterSheets[i].Type != CounterSheetType.Terrain ? XmlConvert.ToInt32(cardSectionNode.GetAttribute("supply")) : 1);
					counterSheets[i].CardSections[j] = properties;
				}
			}

			// icon
			if(iconFileName != "") {
				IFile iconFile = archive.GetFile(iconFileName);
				try {
					using(Stream stream = iconFile.Open()) {
						using(Bitmap iconImage = new Bitmap(stream)) {
							using(Bitmap scaledIconImage = new Bitmap(iconImage, new Size(48, 48))) {
								BitmapData bitmapData = scaledIconImage.LockBits(
									new Rectangle(0, 0, scaledIconImage.Width, scaledIconImage.Height),
									ImageLockMode.ReadOnly,
									PixelFormat.Format16bppRgb565);
								byte[] iconBytes = new byte[bitmapData.Stride * bitmapData.Height];
								unsafe {
									byte* ptr = (byte*) bitmapData.Scan0;
									for(int i = 0; i < iconBytes.Length; ++i)
										iconBytes[i] = *ptr++;
								}
								scaledIconImage.UnlockBits(bitmapData);
								reference.Icon = iconBytes;
							}
						}
					}
				} catch(ZunTzu.FileSystem.FileNotFoundException) {
					iconFileName = "";
				}
			}
		}

		private static ImageResolution parseImageResolution(string inputValue) {
			switch(inputValue) {
				case "600 dpi": return ImageResolution.Dpi600;
				case "300 dpi": return ImageResolution.Dpi300;
				default: return ImageResolution.Dpi150;
			}
		}

		private GameBoxReference reference;
		private string startupScenarioFileName;
		private DiceHandProperties[] diceHands;
		private IArchive archive;
		private MapProperties[] maps;
		private CounterSheetProperties[] counterSheets;
		private Game currentGame;
	}
}
