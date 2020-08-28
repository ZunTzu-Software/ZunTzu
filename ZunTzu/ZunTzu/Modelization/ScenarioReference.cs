// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ZunTzu.FileSystem;

namespace ZunTzu.Modelization {

	/// <summary>Reference to a built-in scenario.</summary>
	internal sealed class ScenarioReference : IScenarioReference {
		/// <summary>Constructor.</summary>
		internal ScenarioReference(IArchive archive, string fileName) {
			XmlDocument xml = new XmlDocument();
			using(Stream stream = archive.GetFile(fileName).Open()) {
				xml.Load(stream);
			}
			XmlElement gameNode = xml.DocumentElement;
			if(gameNode.SelectSingleNode("scenario") != null) {
				// old format
				XmlElement scenarioNode = (XmlElement) xml.SelectSingleNode("/game/scenario");
				this.name = scenarioNode.SelectSingleNode("name").InnerText;
				this.description = (scenarioNode.SelectSingleNode("description") != null ? scenarioNode.SelectSingleNode("description").InnerText : null);
				this.copyright = (scenarioNode.SelectSingleNode("copyright") != null ? scenarioNode.SelectSingleNode("copyright").InnerText : null);
			} else {
				// new format
				this.name = gameNode.GetAttribute("scenario-name");
				this.description = gameNode.GetAttribute("scenario-description");
				this.copyright = gameNode.GetAttribute("scenario-copyright");
			}
			this.fileName = fileName;
		}
		/// <summary>Name of this scenario.</summary>
		public string Name { get { return name; } }
		/// <summary>Description of this scenario.</summary>
		public string Description { get { return description; } }
		/// <summary>Copyright for this scenario.</summary>
		public string Copyright { get { return copyright; } }
		/// <summary>Name of the scenario file in the game box archive.</summary>
		public string FileName { get { return fileName; } }
		private readonly string name;
		private readonly string description;
		private readonly string copyright;
		private readonly string fileName;
	}
}
