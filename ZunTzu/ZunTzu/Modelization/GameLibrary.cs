// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Text;

namespace ZunTzu.Modelization {

	/// <summary>The repository for all game boxes.</summary>
	internal sealed class GameLibrary : IGameLibrary {
		/// <summary>All game boxes in the library, except the default one.</summary>
		public IEnumerable<IGameBoxReference> GameBoxes {
			get {
				foreach(List<GameBoxReference> referenceList in gameBoxes.Values)
					foreach(GameBoxReference reference in referenceList)
						yield return reference;
			}
		}

		/// <summary>The game box loaded at startup.</summary>
		public IGameBoxReference DefaultGameBox { get { return defaultGameBox; } }

		/// <summary>Looks for a game box file in the library.</summary>
		/// <param name="gameBoxName">Name of the game box.</param>
		/// <returns>A game box reference, or null if not found.</returns>
		public IGameBoxReference FindGameBox(string gameBoxName) {
			Debug.Assert(gameBoxName != null);
			string name = gameBoxName.ToUpper();
			if(name == defaultGameBox.Name) {
				return defaultGameBox;
			} else {
				List<GameBoxReference> referenceList;
				return (gameBoxes.TryGetValue(name, out referenceList) ? referenceList[0] : null);
			}
		}

		/// <summary>Looks for a game box file in the library.</summary>
		/// <param name="hash">Hash value of the game box.</param>
		/// <returns>A game box reference, or null if not found.</returns>
		public IGameBoxReference FindGameBox(byte[] hash) {
			Debug.Assert(hash != null);
			if(GameBoxReference.HashAreIdentical(hash, defaultGameBox.Hash)) {
				return defaultGameBox;
			} else {
				foreach(List<GameBoxReference> referenceList in gameBoxes.Values)
					foreach(GameBoxReference reference in referenceList)
						if(GameBoxReference.HashAreIdentical(hash, reference.Hash))
							return reference;
				return null;
			}
		}

		/// <summary>Adds a game box reference to this library.</summary>
		/// <param name="gameBoxReference">A game box reference.</param>
		public void AddReference(IGameBoxReference gameBoxReference) {
			Debug.Assert(gameBoxReference.Name != null);
			if(gameBoxReference != defaultGameBox) {
				string name = gameBoxReference.Name.ToUpper();
				List<GameBoxReference> referenceList;
				if(!gameBoxes.TryGetValue(name, out referenceList)) {
					referenceList = new List<GameBoxReference>(1);
					gameBoxes.Add(name, referenceList);
				} else {
					foreach(GameBoxReference reference in referenceList) {
						if(GameBoxReference.HashAreIdentical(reference.Hash, gameBoxReference.Hash)) {
							// update file path?
							if(reference.FileName != gameBoxReference.FileName) {
								removeReference(reference);
								break;
							} else {
								return;
							}
						}
					}
				}
				// remove other references pointing to the same file path (at most one normally)
				foreach(IGameBoxReference otherReference in GameBoxes) {
					if(otherReference.FileName == gameBoxReference.FileName) {
						removeReference(otherReference);
						break;
					}
				}
				referenceList.Add((GameBoxReference)gameBoxReference);
				updateLibraryFile();
			}
		}

		/// <summary>Adds a game box reference to this library.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		public void AddReference(string fileName) {
			GameBox gameBox = new GameBox(fileName);
			AddReference(gameBox.Reference);
		}

		/// <summary>Removes a game box from this library.</summary>
		/// <param name="gameBoxReference">A game box reference.</param>
		public void RemoveReference(IGameBoxReference gameBoxReference) {
			Debug.Assert(gameBoxReference.Name != null);
			if(gameBoxReference != defaultGameBox) {
				removeReference(gameBoxReference);
				updateLibraryFile();
			}
		}

		/// <summary>Constructor.</summary>
		internal GameLibrary() {
			if(File.Exists(libraryFileName)) {
				XmlDocument xml = new XmlDocument();
				using(Stream stream = File.OpenRead(libraryFileName)) {
					xml.Load(stream);
				}
				XmlElement rootNode = xml.DocumentElement;
				foreach(XmlElement gameBoxNode in rootNode.SelectNodes("game-box")) {
					if(gameBoxNode.SelectSingleNode("name") != null) {
						// old format
						XmlNode descriptionNode = gameBoxNode.SelectSingleNode("description");
						XmlNode copyrightNode = gameBoxNode.SelectSingleNode("copyright");
						XmlNode imageFileNode = gameBoxNode.SelectSingleNode("image-file");
						GameBoxReference reference = new GameBoxReference(
							gameBoxNode.SelectSingleNode("name").InnerText,
							(descriptionNode != null ? descriptionNode.InnerText : null),
							(copyrightNode != null ? copyrightNode.InnerText : null),
							gameBoxNode.SelectSingleNode("file").InnerText,
							Convert.FromBase64String(gameBoxNode.SelectSingleNode("hash").InnerText),
							null);
						string name = reference.Name.ToUpper();
						List<GameBoxReference> referenceList;
						if(!gameBoxes.TryGetValue(name, out referenceList)) {
							referenceList = new List<GameBoxReference>(1);
							gameBoxes.Add(name, referenceList);
						}
						referenceList.Add(reference);
					} else {
						// new format
						GameBoxReference reference = new GameBoxReference(
							gameBoxNode.GetAttribute("name"),
							gameBoxNode.GetAttribute("description"),
							gameBoxNode.GetAttribute("copyright"),
							gameBoxNode.GetAttribute("file"),
							Convert.FromBase64String(gameBoxNode.GetAttribute("hash")),
							(gameBoxNode.HasAttribute("icon") ? Convert.FromBase64String(gameBoxNode.GetAttribute("icon")) : null));
						string name = reference.Name.ToUpper();
						List<GameBoxReference> referenceList;
						if(!gameBoxes.TryGetValue(name, out referenceList)) {
							referenceList = new List<GameBoxReference>(1);
							gameBoxes.Add(name, referenceList);
						}
						referenceList.Add(reference);
					}
				}
			}
		}

		private static string libraryFileName {
			get {
				return Path.Combine(
					(ApplicationDeployment.IsNetworkDeployed ?
						ApplicationDeployment.CurrentDeployment.DataDirectory :
						System.Windows.Forms.Application.StartupPath),
					"library.xml");
			}
		}

		private void updateLibraryFile() {
			string temporaryFileName = Path.GetTempFileName();
			using(Stream stream = File.Open(temporaryFileName, FileMode.Create, FileAccess.Write)) {
				XmlTextWriter writer = null;
				try {
					writer = new XmlTextWriter(stream, Encoding.GetEncoding("windows-1252"));
					writer.Formatting = Formatting.Indented;
					writer.IndentChar = '	';
					writer.Indentation = 1;
					writer.WriteStartDocument(true);
					writer.WriteStartElement("library");
					writer.WriteAttributeString("version", "1.0");
					foreach(List<GameBoxReference> referenceList in gameBoxes.Values) {
						foreach(GameBoxReference reference in referenceList) {
							writer.WriteStartElement("game-box");
							writer.WriteAttributeString("name", reference.Name);
							if(reference.Description != null && reference.Description != "")
								writer.WriteAttributeString("description", reference.Description);
							if(reference.Copyright != null && reference.Copyright != "")
								writer.WriteAttributeString("copyright", reference.Copyright);
							writer.WriteAttributeString("file", reference.FileName);
							writer.WriteAttributeString("hash", Convert.ToBase64String(reference.Hash));
							if(reference.Icon != null)
								writer.WriteAttributeString("icon", Convert.ToBase64String(reference.Icon));
							writer.WriteEndElement();
						}
					}
					writer.WriteEndElement();
					writer.WriteEndDocument();
				} finally {
					if(writer != null)
						writer.Close();
				}
			}

			string outputFileName = libraryFileName;
			if(File.Exists(outputFileName))
				File.Delete(outputFileName);
			File.Move(temporaryFileName, outputFileName);
		}

		/// <summary>Removes a game box from this library without updating the file.</summary>
		/// <param name="gameBoxReference">A game box reference.</param>
		private void removeReference(IGameBoxReference gameBoxReference) {
			List<GameBoxReference> referenceList;
			if(gameBoxes.TryGetValue(gameBoxReference.Name.ToUpper(), out referenceList)) {
				for(int i = 0; i < referenceList.Count; ++i) {
					if(GameBoxReference.HashAreIdentical(referenceList[i].Hash, gameBoxReference.Hash)) {
						referenceList.RemoveAt(i);
						break;
					}
				}
			}
		}

		private SortedList<string, List<GameBoxReference>> gameBoxes = new SortedList<string, List<GameBoxReference>>();

		private readonly GameBoxReference defaultGameBox =
			new GameBoxReference("", null, null,
				Path.Combine(
					(ApplicationDeployment.IsNetworkDeployed ?
						ApplicationDeployment.CurrentDeployment.DataDirectory :
						System.Windows.Forms.Application.StartupPath),
					"default.ztb"),
				new byte[] { 112, 185, 152, 156, 251, 78, 49, 237, 154, 68, 67, 121, 11, 106, 45, 69, 136, 39, 33, 94 },
				null);
	}
}
