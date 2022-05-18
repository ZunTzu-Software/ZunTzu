// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using ZunTzu.FileSystem;
using ZunTzu.Properties;

/*
Error messages:

GameBoxError00	Error: {0}.
GameBoxError01	Archive doesn't contain a "game-box.xml" file.
GameBoxError02	game-box.xml doesn't begin with a "<?xml " line followed by "<game-box ".
GameBoxError03	, line {0}: attributes "{1}" and "{2}" are incompatible.
GameBoxError04	, line {0}: attribute "{1}" is deprecated.
GameBoxError05	, line {0}: attribute "{1}" is invalid without attribute "{2}".
GameBoxError06	, line {0}: attribute "{1}" is irrelevant for a card section.
GameBoxError07	, line {0}: attribute "{1}" is irrelevant for a counter or terrain section.
GameBoxError08	, line {0}: attribute "{1}" refers to a file that can't be found in the game box archive.
GameBoxError09	, line {0}: attribute "{1}" should end with "{2}".
GameBoxError10	, line {0}: attribute "{1}" should end with ".jpg" or ".png".
GameBoxError11	, line {0}: attribute "{1}" is irrelevant for a terrain section.
GameBoxError12	, line {0}: board "{1}" is defined twice.
GameBoxError13	, line {0}: attribute "{1}" is defined twice.
GameBoxError14	, line {0}: expected end-tag "</{1}>".
GameBoxError15	, line {0}: expected "<dice " or "</dice-hand>".
GameBoxError16	, line {0}: expected "<dice-hand ", "<map ", "<counter-sheet ", "<terrain-sheet " or "</game-box>".
GameBoxError17	, line {0}: expected section definition or "</{1}>".
GameBoxError18	, line {0}: found "</{1}>" but expected "</{2}>".
GameBoxError19	, line {0}: found data after final end-tag "</{1}>".
GameBoxError20	, line {0}: invalid attribute "{1}" because no back image.
GameBoxError21	, line {0}: invalid value "{1}" for attribute "{2}" because no back image.
GameBoxError22	, line {0}: location attributes "front-..." or "back-..." missing.
GameBoxError23	, line {0}: mandatory attribute "{1}" missing.
GameBoxError24	, line {0}: mandatory attribute "{1}" present but empty.
GameBoxError25	, line {0}: section type is "{1}" so attribute "{2}" is invalid.
GameBoxError26	, line {0}: section type is "{1}" so attribute "{2}" is required.
GameBoxError27	, line {0}: invalid section type for this sheet.
GameBoxError28	, line {0}: unsupported attribute "{1}".
GameBoxError29	, line {0}: unsupported tag "<{1} ".
GameBoxError30	: file not referenced in game-box.xml.
GameBoxError31	: scenario file doesn't begin with a "<?xml " line followed by "<game ".
GameBoxError32	, line {0}: expected "<layout ", "<hand " or "</game>".
GameBoxError33	, line {0}: expected counter definition or "</{1}>".
GameBoxError34	, line {0}: expected stack definition or "</{1}>".
GameBoxError35	, line {0}: visible board defined twice.
GameBoxError36	, line {0}: attribute "board" refers to an undefined board "{1}".
GameBoxError37	, line {0}: board "{1}" wrongly assumed to be double-sided.
GameBoxError38	, line {0}: stack with no counter.
GameBoxError39	, line {0}: board defined as private.
GameBoxError40	, line {0}: non-empty private hand content.
GameBoxError41	: not a valid image file.
GameBoxError42	, line {0}: mask size doesn't match image size.
GameBoxError43	, line {0}: invalid value "{1}" for attribute "{2}".
GameBoxError44	, line {0}: mandatory attribute "board" or "tab" missing.

*/

namespace ZunTzu.Modelization {

	public static class GameBoxVerifier {

		public static string ErrorCode = "E";
		public static string WarningCode = "W";
		public static string DeprecatedCode = "D";

		private enum GameBoxValidationState { ExpectingXml, ExpectingGameBox, ExpectingComponent, ExpectingDice, ExpectingSection, EndOfFile }

		/// <summary>Looks for errors in a game box.</summary>
		/// <param name="fileName">Game box filename.</param>
		/// <returns>A list of errors and warnings.</returns>
		/// <remarks>The first character of each error is the severity: E (error), W (warning), D (deprecated).</remarks>
		public static IEnumerable<string> VerifyGameBox(string fileName) {
			List<string> errors = new List<string>();
			try {
				IArchive archive = new Archive(fileName);

				Dictionary<string, bool> boardDoubleSidedness = new Dictionary<string, bool>();
				List<string> files = new List<string>(archive.Files);
				Dictionary<string, bool> referencedFiles = new Dictionary<string, bool>();
				string gameBoxName = null;
				string currentSheet = null;
				string currentSheetName = null;
				bool currentSheetHasBackImage = false;
				string currentSection = null;
				string currentSectionType = null;

				if(!files.Contains("game-box.xml")) {
					errors.Add(ErrorCode + Resources.GameBoxError01);
				} else {
					// read game-box.xml
					IFile gameBoxFile = archive.GetFile("game-box.xml");

					using(Stream stream = gameBoxFile.Open()) {
						using(XmlTextReader xmlTextReader = new XmlTextReader(stream)) {
							xmlTextReader.EntityHandling = EntityHandling.ExpandEntities;
							xmlTextReader.ProhibitDtd = true;
							xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
							XmlReader xml = xmlTextReader;

							GameBoxValidationState state = GameBoxValidationState.ExpectingXml;
							xml.Read();
							while(state != GameBoxValidationState.EndOfFile && !xml.EOF) {
								if(xml.NodeType == XmlNodeType.Comment) {
									xml.Skip();
									continue;
								}
								if(xml.NodeType != XmlNodeType.Element &&
									xml.NodeType != XmlNodeType.EndElement &&
									xml.NodeType != XmlNodeType.XmlDeclaration)
								{
									errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.NodeType.ToString()));
									xml.Skip();
									continue;
								}

								switch(state) {
									case GameBoxValidationState.ExpectingXml:
										state = GameBoxValidationState.ExpectingGameBox;
										switch(xml.NodeType) {
											case XmlNodeType.XmlDeclaration:
												xml.Skip();
												break;

											default:
												errors.Add(ErrorCode + Resources.GameBoxError02);
												break;
										}
										break;

									case GameBoxValidationState.ExpectingGameBox:
										switch(xml.NodeType) {
											case XmlNodeType.Element:
												if(xml.Name != "game-box") {
													goto default;
												} else {
													Dictionary<string, bool> attributes = new Dictionary<string, bool>();
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "version":
																	errors.Add(DeprecatedCode + "game-box.xml" + string.Format(Resources.GameBoxError04, xmlTextReader.LineNumber, xml.Name));
																	break;

																case "name":
																	gameBoxName = xml.Value;
																	if(gameBoxName == "")
																		errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																	break;

																case "description":
																case "copyright":
																	break;

																case "icon":
																	string icon = xml.Value;
																	if(icon != "") {
																		if(!icon.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError09, xmlTextReader.LineNumber, xml.Name, ".bmp"));
																		else if(icon == "icon.bmp")
																			errors.Add(DeprecatedCode + "game-box.xml" + string.Format(Resources.GameBoxError04, xmlTextReader.LineNumber, xml.Name));

																		if(!files.Contains(icon))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError08, xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[icon] = true;
																	}
																	break;

																case "startup-scenario":
																	string startupScenario = xml.Value;
																	if(startupScenario == "") {
																		errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																	} else {
																		if(!startupScenario.EndsWith(".zts", StringComparison.OrdinalIgnoreCase))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError09, xmlTextReader.LineNumber, xml.Name, ".zts"));
																		if(!files.Contains(startupScenario))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError08, xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[startupScenario] = true;
																	}
																	break;

																default:
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													if(files.Contains("icon.bmp") && !attributes.ContainsKey("icon"))
														referencedFiles["icon.bmp"] = true;

													// check missing mandatory attributes
													foreach(string mandatoryAttribute in new string[] { "name", "description", "copyright", "startup-scenario" }) {
														if(!attributes.ContainsKey(mandatoryAttribute))
															errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
													}

													if(xml.IsEmptyElement) {
														state = GameBoxValidationState.EndOfFile;
														if(xml.Read())
															errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError19, xmlTextReader.LineNumber, "game-box"));
													} else {
														state = GameBoxValidationState.ExpectingComponent;
														xml.Read();
													}
												}
												break;

											default:
												errors.Add(ErrorCode + Resources.GameBoxError02);
												return errors;	// fatal error
										}
										break;

									case GameBoxValidationState.ExpectingComponent:
										switch(xml.NodeType) {
											case XmlNodeType.Element:
												Dictionary<string, bool> attributes = new Dictionary<string, bool>();
												switch(xml.Name) {
													case "dice-hand":
														if(xml.HasAttributes) {
															while(xml.MoveToNextAttribute()) {
																switch(xml.Name) {
																	case "type":
																		List<string> eligibleValues = new List<string>(new string[] { "D4", "D6", "D8", "D10", "D12", "D20" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
															}
															xml.MoveToElement();
														}
														if(!xml.IsEmptyElement)
															state = GameBoxValidationState.ExpectingDice;
														xml.Read();
														break;

													case "map":
														if(xml.HasAttributes) {
															while(xml.MoveToNextAttribute()) {
																switch(xml.Name) {
																	case "name":
																		currentSheetName = xml.Value;
																		if(currentSheetName == "")
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																		else if(boardDoubleSidedness.ContainsKey(currentSheetName))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError12, xmlTextReader.LineNumber, currentSheetName));
																		else
																			boardDoubleSidedness.Add(currentSheetName, false);
																		break;

																	case "image-file":
																		string imageFile = xml.Value;
																		if(imageFile == "") {
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																		} else {
																			if(!imageFile.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
																				!imageFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
																				!imageFile.EndsWith(".jpg.encrypted", StringComparison.OrdinalIgnoreCase) &&
																				!imageFile.EndsWith(".png.encrypted", StringComparison.OrdinalIgnoreCase))
																				errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError10, xmlTextReader.LineNumber, xml.Name));
																			if(!files.Contains(imageFile))
																				errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError08, xmlTextReader.LineNumber, xml.Name));
																			else
																				referencedFiles[imageFile] = true;
																		}
																		break;

																	case "resolution":
																		List<string> eligibleValues = new List<string>(new string[] { "150 dpi", "300 dpi", "600 dpi" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
															}
															xml.MoveToElement();
														}

														// check missing mandatory attributes
														foreach(string mandatoryAttribute in new string[] { "name", "image-file" }) {
															if(!attributes.ContainsKey(mandatoryAttribute))
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
														}

														if(xml.IsEmptyElement) {
															xml.Read();
														} else {
															xml.Read();
															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError14, xmlTextReader.LineNumber, "map"));
																return errors;	// fatal error
															} else if(xml.Name != "map") {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "map"));
																return errors;	// fatal error
															}
														}
														break;

													case "counter-sheet":
													case "terrain-sheet":
														currentSheet = xml.Name;
														if(xml.HasAttributes) {
															while(xml.MoveToNextAttribute()) {
																switch(xml.Name) {
																	case "name":
																		currentSheetName = xml.Value;
																		if(currentSheetName == "")
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																		else if(boardDoubleSidedness.ContainsKey(currentSheetName))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError12, xmlTextReader.LineNumber, currentSheetName));
																		else
																			boardDoubleSidedness.Add(currentSheetName, false);
																		break;

																	case "front-image-file":
																	case "back-image-file":
																	case "front-mask-file":
																	case "back-mask-file":
																		string imageFile = xml.Value;
																		if(imageFile == "") {
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																		} else {
																			if(xml.Name.Contains("mask")) {
																				if(!imageFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
																					!imageFile.EndsWith(".png.encrypted", StringComparison.OrdinalIgnoreCase))
																					errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError09, xmlTextReader.LineNumber, xml.Name, ".png"));
																			} else {
																				if(!imageFile.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
																					!imageFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
																					!imageFile.EndsWith(".jpg.encrypted", StringComparison.OrdinalIgnoreCase) &&
																					!imageFile.EndsWith(".png.encrypted", StringComparison.OrdinalIgnoreCase))
																					errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError10, xmlTextReader.LineNumber, xml.Name));
																			}
																			if(!files.Contains(imageFile))
																				errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError08, xmlTextReader.LineNumber, xml.Name));
																			else
																				referencedFiles[imageFile] = true;
																		}
																		break;

																	case "front-resolution":
																	case "back-resolution":
																		List<string> eligibleValues = new List<string>(new string[] { "150 dpi", "300 dpi", "600 dpi" });
																		if(!eligibleValues.Contains(xml.Value))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
															}
															xml.MoveToElement();
														}

														// check missing mandatory attributes
														foreach(string mandatoryAttribute in new string[] { "name", "front-image-file" }) {
															if(!attributes.ContainsKey(mandatoryAttribute))
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
														}

														// check dependencies
														currentSheetHasBackImage = attributes.ContainsKey("back-image-file");
														if(!currentSheetHasBackImage) {
															foreach(string dependentAttribute in new string[] { "back-resolution", "back-mask-file" }) {
																if(attributes.ContainsKey(dependentAttribute))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError05, xmlTextReader.LineNumber, dependentAttribute, "back-image-file"));
															}
														} else {
															if(boardDoubleSidedness.ContainsKey(currentSheetName))
																boardDoubleSidedness[currentSheetName] = true;
														}

														if(xml.IsEmptyElement) {
															xml.Read();
														} else {
															xml.Read();
															state = GameBoxValidationState.ExpectingSection;
														}
														break;

													default:
														errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.Name));
														xml.Skip();
														break;
												}
												break;

											case XmlNodeType.EndElement:
												if(xml.Name == "game-box") {
													if(xml.Read())
														errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError19, xmlTextReader.LineNumber, "game-box"));
													state = GameBoxValidationState.EndOfFile;
												} else {
													errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "game-box"));
													return errors;	// fatal error
												}
												break;

											default:
												errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError16, xmlTextReader.LineNumber));
												return errors;	// fatal error
										}
										break;

									case GameBoxValidationState.ExpectingDice:
										switch(xml.NodeType) {
											case XmlNodeType.Element:
												Dictionary<string, bool> attributes = new Dictionary<string, bool>();
												switch(xml.Name) {
													case "dice":
														if(xml.HasAttributes) {
															while(xml.MoveToNextAttribute()) {
																switch(xml.Name) {
																	case "count":
																		int count;
																		if(!int.TryParse(xml.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out count) || count <= 0)
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	case "color":
																	case "pips":
																		Regex hexadecimalRegex = new Regex(@"^[0-9a-fA-F]{6}$", RegexOptions.Singleline);
																		Match hexadecimalMatch = hexadecimalRegex.Match(xml.Value);
																		if(!hexadecimalMatch.Success)
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		if(attributes.ContainsKey("texture-file"))
																			errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError03, xmlTextReader.LineNumber, xml.Name, "texture-file"));
																		break;

																	case "texture-file":
																		string textureFile = xml.Value;
																		if(!textureFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError09, xmlTextReader.LineNumber, xml.Name, ".png"));
																		if(!files.Contains(textureFile))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError08, xmlTextReader.LineNumber, xml.Name));
																		else
																			referencedFiles[textureFile] = true;
																		foreach(string incompatibleAttribute in new string[] { "color", "pips" }) {
																			if(attributes.ContainsKey(incompatibleAttribute))
																				errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError03, xmlTextReader.LineNumber, xml.Name, incompatibleAttribute));
																		}
																		break;

																	default:
																		errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
															}
															xml.MoveToElement();
														}

														if(xml.IsEmptyElement) {
															xml.Read();
														} else {
															xml.Read();
															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError14, xmlTextReader.LineNumber, "dice"));
																return errors;	// fatal error
															} else if(xml.Name != "dice") {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "dice"));
																return errors;	// fatal error
															}
														}
														break;

													default:
														errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.Name));
														xml.Skip();
														break;
												}
												break;

											case XmlNodeType.EndElement:
												if(xml.Name == "dice-hand") {
													xml.Read();
													state = GameBoxValidationState.ExpectingComponent;
												} else {
													errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "dice-hand"));
													return errors;	// fatal error
												}
												break;

											default:
												errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError15, xmlTextReader.LineNumber));
												return errors;	// fatal error
										}
										break;

									case GameBoxValidationState.ExpectingSection:
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
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError27, xmlTextReader.LineNumber));
														} else {
															if(currentSection == "terrain-section")
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError27, xmlTextReader.LineNumber));
														}

														if(xml.HasAttributes) {
															while(xml.MoveToNextAttribute()) {
																switch(xml.Name) {
																	case "type":
																		List<string> eligibleValues;
																		switch(currentSection) {
																			case "terrain-section":
																				errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																				goto case "counter-section";

																			case "counter-section":
																				eligibleValues = new List<string>(new string[] { "FrontSideOnly", "BackSideOnly", "TwoSided" });
																				if(!eligibleValues.Contains(xml.Value))
																					errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else if(!currentSheetHasBackImage && xml.Value != "FrontSideOnly")
																					errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError21, xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else
																					currentSectionType = xml.Value;
																				break;

																			case "card-section":
																				eligibleValues = new List<string>(new string[] { "CardFacesOnFront", "CardFacesOnBack", "CardFacesAndBackOnFront", "CardFacesOnBackBackOnOtherSide", "CardFacesOnFrontBackOnOtherSide", "CardFacesAndBackOnBack" });
																				if(!eligibleValues.Contains(xml.Value))
																					errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else if(!currentSheetHasBackImage && xml.Value != "CardFacesOnFront" && xml.Value != "CardFacesAndBackOnFront")
																					errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError21, xmlTextReader.LineNumber, xml.Value, xml.Name));
																				else
																					currentSectionType = xml.Value;
																				break;
																		}
																		break;

																	case "rows":
																	case "columns":
																	case "supply":
																		int count;
																		if(!int.TryParse(xml.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out count) || count <= 0)
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		if(xml.Name == "supply" && currentSection == "terrain-section")
																			errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError11, xmlTextReader.LineNumber, "supply"));
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
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError06, xmlTextReader.LineNumber, xml.Name));
																		if(currentSection != "card-section" && xml.Name.StartsWith("face-"))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError07, xmlTextReader.LineNumber, xml.Name));
																		float pixels;
																		if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out pixels))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	case "shadow":
																		float length;
																		if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out length) || length < 0.0f)
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																		break;

																	default:
																		errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																		break;
																}
																if(attributes.ContainsKey(xml.Name))
																	errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
																else
																	attributes.Add(xml.Name, true);
															}
															xml.MoveToElement();
														}

														// check missing mandatory attributes
														string[] faceAttributes = { "face-top", "face-left", "face-bottom", "face-right" };
														string[] frontAttributes = { "front-top", "front-left", "front-bottom", "front-right" };
														string[] backAttributes = { "back-top", "back-left", "back-bottom", "back-right" };
														if(currentSection == "card-section") {
															foreach(string mandatoryAttribute in faceAttributes) {
																if(!attributes.ContainsKey(mandatoryAttribute))
																	errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
															}
															if(currentSectionType == null) {
																foreach(string backAttribute in backAttributes) {
																	if(attributes.ContainsKey(backAttribute)) {
																		foreach(string mandatoryAttribute in backAttributes) {
																			if(!attributes.ContainsKey(mandatoryAttribute))
																				errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError05, xmlTextReader.LineNumber, backAttribute, mandatoryAttribute));
																		}
																		break;
																	}
																}
															} else {
																if(new List<string>(new string[] { "CardFacesAndBackOnFront", "CardFacesOnBackBackOnOtherSide", "CardFacesOnFrontBackOnOtherSide", "CardFacesAndBackOnBack" }).Contains(currentSectionType)) {
																	foreach(string mandatoryAttribute in backAttributes) {
																		if(!attributes.ContainsKey(mandatoryAttribute))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError26, xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																	}
																} else {
																	foreach(string invalidAttribute in backAttributes) {
																		if(attributes.ContainsKey(invalidAttribute))
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError25, xmlTextReader.LineNumber, currentSectionType, invalidAttribute));
																	}
																}
															}
														} else {
															if(new List<string>(new string[] { "FrontSideOnly", "TwoSided" }).Contains(currentSectionType)) {
																foreach(string mandatoryAttribute in frontAttributes) {
																	if(!attributes.ContainsKey(mandatoryAttribute))
																		errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError26, xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																}
															}
															if(new List<string>(new string[] { "BackSideOnly", "TwoSided" }).Contains(currentSectionType)) {
																foreach(string mandatoryAttribute in backAttributes) {
																	if(!attributes.ContainsKey(mandatoryAttribute))
																		errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError26, xmlTextReader.LineNumber, currentSectionType, mandatoryAttribute));
																}
															}
															if(currentSectionType == null) {
																bool locationAttributeFound = false;
																foreach(string frontAttribute in frontAttributes) {
																	if(attributes.ContainsKey(frontAttribute)) {
																		locationAttributeFound = true;
																		foreach(string mandatoryAttribute in frontAttributes) {
																			if(!attributes.ContainsKey(mandatoryAttribute))
																				errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError05, xmlTextReader.LineNumber, frontAttribute, mandatoryAttribute));
																		}
																		break;
																	}
																}
																foreach(string backAttribute in backAttributes) {
																	if(attributes.ContainsKey(backAttribute)) {
																		if(!currentSheetHasBackImage)
																			errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError20, xmlTextReader.LineNumber, xml.Name));
																		locationAttributeFound = true;
																		foreach(string mandatoryAttribute in backAttributes) {
																			if(!attributes.ContainsKey(mandatoryAttribute))
																				errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError05, xmlTextReader.LineNumber, backAttribute, mandatoryAttribute));
																		}
																		break;
																	}
																}
																if(!locationAttributeFound)
																	errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError22, xmlTextReader.LineNumber));
															}
														}

														if(xml.IsEmptyElement) {
															xml.Read();
														} else {
															xml.Read();
															if(xml.NodeType != XmlNodeType.EndElement) {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError14, xmlTextReader.LineNumber, currentSection));
																return errors;	// fatal error
															} else if(xml.Name != currentSection) {
																errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, currentSection));
																return errors;	// fatal error
															}
														}
														break;

													default:
														errors.Add(WarningCode + "game-box.xml" + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.Name));
														xml.Skip();
														break;
												}
												break;

											case XmlNodeType.EndElement:
												if(xml.Name == currentSheet) {
													xml.Read();
													state = GameBoxValidationState.ExpectingComponent;
												} else {
													errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, currentSheet));
													return errors;	// fatal error
												}
												break;

											default:
												errors.Add(ErrorCode + "game-box.xml" + string.Format(Resources.GameBoxError17, xmlTextReader.LineNumber, currentSheet));
												return errors;	// fatal error
										}
										break;
								}
							}
						}
					}

					// validate other files
					foreach(string filename in files) {
						if(filename.EndsWith(".zts", StringComparison.OrdinalIgnoreCase)) {
							// scenario file
							errors.AddRange(verifyBuiltInScenario(archive, filename, gameBoxName, boardDoubleSidedness));
						} else if(filename != "game-box.xml") {
							if(!referencedFiles.ContainsKey(filename))
								errors.Add(WarningCode + filename + Resources.GameBoxError30);

							if(filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) {
								// check JPEG signature
							} else if(filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
								// check PNG signature
							}
						}
					}
				}
			} catch(Exception e) {
				errors.Add(ErrorCode + string.Format(Resources.GameBoxError00, e.Message));
			}
			return errors;
		}

		private enum GameValidationState { ExpectingXml, ExpectingGame, ExpectingLayout, ExpectingStack, ExpectingCounter, EndOfFile }

		/// <summary>Looks for errors in a built-in scenario.</summary>
		/// <param name="archive">Game box archive.</param>
		/// <param name="scenarioFileName">Scenario filename.</param>
		/// <returns>A list of errors and warnings.</returns>
		/// <remarks>The first character of each error is the severity: E (error), W (warning), D (deprecated).</remarks>
		private static IEnumerable<string> verifyBuiltInScenario(IArchive archive, string scenarioFileName, string gameBoxName, Dictionary<string, bool> boardDoubleSidedness) {
			List<string> errors = new List<string>();
			try {
				// read scenario file as XML
				IFile scenarioFile = archive.GetFile(scenarioFileName);

				string currentBoardName = null;
				int visibleBoardCount = 0;
				string currentContainer = null;

				using(Stream stream = scenarioFile.Open()) {
					using(XmlTextReader xmlTextReader = new XmlTextReader(stream)) {
						xmlTextReader.EntityHandling = EntityHandling.ExpandEntities;
						xmlTextReader.ProhibitDtd = true;
						xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
						XmlReader xml = xmlTextReader;

						GameValidationState state = GameValidationState.ExpectingXml;
						xml.Read();
						while(state != GameValidationState.EndOfFile && !xml.EOF) {
							if(xml.NodeType == XmlNodeType.Comment) {
								xml.Skip();
								continue;
							}
							if(xml.NodeType != XmlNodeType.Element &&
								xml.NodeType != XmlNodeType.EndElement &&
								xml.NodeType != XmlNodeType.XmlDeclaration)
							{
								errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.NodeType.ToString()));
								xml.Skip();
								continue;
							}

							switch(state) {
								case GameValidationState.ExpectingXml:
									state = GameValidationState.ExpectingGame;
									switch(xml.NodeType) {
										case XmlNodeType.XmlDeclaration:
											xml.Skip();
											break;

										default:
											errors.Add(ErrorCode + scenarioFileName + Resources.GameBoxError31);
											break;
									}
									break;

								case GameValidationState.ExpectingGame:
									switch(xml.NodeType) {
										case XmlNodeType.Element:
											if(xml.Name != "game") {
												goto default;
											} else {
												Dictionary<string, bool> attributes = new Dictionary<string, bool>();
												if(xml.HasAttributes) {
													while(xml.MoveToNextAttribute()) {
														switch(xml.Name) {
															case "game-box":
																if(xml.Value != gameBoxName)
																	errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																break;

															case "scenario-name":
																if(xml.Value == "")
																	errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError24, xmlTextReader.LineNumber, xml.Name));
																break;

															case "scenario-description":
															case "scenario-copyright":
															case "hash":
																break;

															case "mode":
															case "stacking":
																List<string> eligibleValues = new List<string>(
																	(xml.Name == "mode" ?
																		new string[] { "Default", "Terrain" } :
																		new string[] { "enabled", "disabled" }));
																if(!eligibleValues.Contains(xml.Value))
																	errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																break;

															default:
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																break;
														}
														if(attributes.ContainsKey(xml.Name))
															errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
														else
															attributes.Add(xml.Name, true);
													}
													xml.MoveToElement();
												}

												// check missing mandatory attributes
												foreach(string mandatoryAttribute in new string[] { "game-box", "scenario-name" }) {
													if(!attributes.ContainsKey(mandatoryAttribute))
														errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
												}

												if(xml.IsEmptyElement) {
													state = GameValidationState.EndOfFile;
													if(xml.Read())
														errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError19, xmlTextReader.LineNumber, "game"));
												} else {
													state = GameValidationState.ExpectingLayout;
													xml.Read();
												}
											}
											break;

										default:
											errors.Add(ErrorCode + scenarioFileName + Resources.GameBoxError31);
											return errors;	// fatal error
									}
									break;

								case GameValidationState.ExpectingLayout:
									switch(xml.NodeType) {
										case XmlNodeType.Element:
											Dictionary<string, bool> attributes = new Dictionary<string, bool>();
											switch(xml.Name) {
												case "layout":
													currentBoardName = null;
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "board":
																	currentBoardName = xml.Value;
																	if(!boardDoubleSidedness.ContainsKey(currentBoardName))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError36, xmlTextReader.LineNumber, currentBoardName));
																	break;

																case "tab":
																	break;

																case "left":
																case "top":
																case "right":
																case "bottom":
																	float pixels;
																	if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out pixels))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "side":
																	if(xml.Value == "Back" && currentBoardName != null) {
																		bool doubleSided;
																		if(boardDoubleSidedness.TryGetValue(currentBoardName, out doubleSided) && !doubleSided)
																			errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError37, xmlTextReader.LineNumber, currentBoardName));
																	}
																	List<string> eligibleValues = new List<string>(new string[] { "Front", "Back" });
																	if(!eligibleValues.Contains(xml.Value))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "visible":
																	if(xml.Value == "true") {
																		++visibleBoardCount;
																		if(visibleBoardCount > 1)
																			errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError35, xmlTextReader.LineNumber));
																	} else if(xml.Value != "false") {
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	}
																	break;

																case "owner":
																	errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError39, xmlTextReader.LineNumber));
																	try {
																		Guid owner = new Guid(xml.Value);
																	} catch(Exception) {
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	}
																	break;

																default:
																	errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													// check missing mandatory attributes
													if(!attributes.ContainsKey("tab") && !attributes.ContainsKey("board"))
														errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError44, xmlTextReader.LineNumber));

													if(!xml.IsEmptyElement)
														state = GameValidationState.ExpectingStack;
													xml.Read();
													break;

												case "hand":
													currentContainer = "hand";
													errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError40, xmlTextReader.LineNumber));
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "owner":
																	try {
																		Guid owner = new Guid(xml.Value);
																	} catch(Exception) {
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	}
																	break;

																default:
																	errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													// check missing mandatory attributes
													foreach(string mandatoryAttribute in new string[] { "owner" }) {
														if(!attributes.ContainsKey(mandatoryAttribute))
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
													}

													if(!xml.IsEmptyElement)
														state = GameValidationState.ExpectingCounter;
													xml.Read();
													break;
											}
											break;

										case XmlNodeType.EndElement:
											if(xml.Name == "game") {
												if(xml.Read())
													errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError19, xmlTextReader.LineNumber, "game"));
												state = GameValidationState.EndOfFile;
											} else {
												errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "game"));
												return errors;	// fatal error
											}
											break;

										default:
											errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError32, xmlTextReader.LineNumber));
											return errors;	// fatal error
									}
									break;

								case GameValidationState.ExpectingStack:
									switch(xml.NodeType) {
										case XmlNodeType.Element:
											Dictionary<string, bool> attributes = new Dictionary<string, bool>();
											switch(xml.Name) {
												case "stack":
													currentContainer = "stack";
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "x":
																case "y":
																	float pixels;
																	if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out pixels))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																default:
																	errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													// check missing mandatory attributes
													foreach(string mandatoryAttribute in new string[] { "x", "y" }) {
														if(!attributes.ContainsKey(mandatoryAttribute))
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
													}

													if(xml.IsEmptyElement) {
														errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError38, xmlTextReader.LineNumber));
														xml.Read();
													} else {
														xml.Read();
														state = GameValidationState.ExpectingCounter;
													}
													break;

												case "counter":
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "id":
																	int id;
																	if(!int.TryParse(xml.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out id))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "x":
																case "y":
																	float pixels;
																	if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out pixels))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "rot":
																	float degrees;
																	if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out degrees))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "side":
																	List<string> eligibleValues = new List<string>(new string[] { "Front", "Back" });
																	if(!eligibleValues.Contains(xml.Value))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																default:
																	errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													// check missing mandatory attributes
													foreach(string mandatoryAttribute in new string[] { "id", "x", "y" }) {
														if(!attributes.ContainsKey(mandatoryAttribute))
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
													}

													if(xml.IsEmptyElement) {
														xml.Read();
													} else {
														xml.Read();
														if(xml.NodeType != XmlNodeType.EndElement) {
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError14, xmlTextReader.LineNumber, "counter"));
															return errors;	// fatal error
														} else if(xml.Name != "counter") {
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "counter"));
															return errors;	// fatal error
														}
													}
													break;

												default:
													errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.Name));
													xml.Skip();
													break;
											}
											break;

										case XmlNodeType.EndElement:
											if(xml.Name == "layout") {
												xml.Read();
												state = GameValidationState.ExpectingLayout;
											} else {
												errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "layout"));
												return errors;	// fatal error
											}
											break;

										default:
											errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError34, xmlTextReader.LineNumber, "layout"));
											return errors;	// fatal error
									}
									break;

								case GameValidationState.ExpectingCounter:
									switch(xml.NodeType) {
										case XmlNodeType.Element:
											Dictionary<string, bool> attributes = new Dictionary<string, bool>();
											switch(xml.Name) {
												case "counter":
													if(xml.HasAttributes) {
														while(xml.MoveToNextAttribute()) {
															switch(xml.Name) {
																case "id":
																	int id;
																	if(!int.TryParse(xml.Value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out id))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "rot":
																	float degrees;
																	if(!float.TryParse(xml.Value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out degrees))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																case "side":
																	List<string> eligibleValues = new List<string>(new string[] { "Front", "Back" });
																	if(!eligibleValues.Contains(xml.Value))
																		errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError43, xmlTextReader.LineNumber, xml.Value, xml.Name));
																	break;

																default:
																	errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError28, xmlTextReader.LineNumber, xml.Name));
																	break;
															}
															if(attributes.ContainsKey(xml.Name))
																errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError13, xmlTextReader.LineNumber, xml.Name));
															else
																attributes.Add(xml.Name, true);
														}
														xml.MoveToElement();
													}

													// check missing mandatory attributes
													foreach(string mandatoryAttribute in new string[] { "id" }) {
														if(!attributes.ContainsKey(mandatoryAttribute))
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError23, xmlTextReader.LineNumber, mandatoryAttribute));
													}

													if(xml.IsEmptyElement) {
														xml.Read();
													} else {
														xml.Read();
														if(xml.NodeType != XmlNodeType.EndElement) {
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError14, xmlTextReader.LineNumber, "counter"));
															return errors;	// fatal error
														} else if(xml.Name != "counter") {
															errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, "counter"));
															return errors;	// fatal error
														}
													}
													break;

												default:
													errors.Add(WarningCode + scenarioFileName + string.Format(Resources.GameBoxError29, xmlTextReader.LineNumber, xml.Name));
													xml.Skip();
													break;
											}
											break;

										case XmlNodeType.EndElement:
											if(xml.Name == currentContainer) {
												xml.Read();
												state = (currentContainer == "stack" ? GameValidationState.ExpectingStack : GameValidationState.ExpectingLayout);
											} else {
												errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError18, xmlTextReader.LineNumber, xml.Name, currentContainer));
												return errors;	// fatal error
											}
											break;

										default:
											errors.Add(ErrorCode + scenarioFileName + string.Format(Resources.GameBoxError33, xmlTextReader.LineNumber, currentContainer));
											return errors;	// fatal error
									}
									break;
							}
						}
					}
				}
			} catch(Exception e) {
				errors.Add(ErrorCode + string.Format(Resources.GameBoxError00, e.Message));
			}
			return errors;
		}
	}
}
