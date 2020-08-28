// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.FileSystem;
using System.IO;

namespace ZunTzu.Modelization {

	/// <summary>Game library reference to a game box.</summary>
	internal sealed class GameBoxReference : IGameBoxReference {
		/// <summary>Constructor.</summary>
		internal GameBoxReference(string archivefileName, byte[] hash)
		: this(null, null, null, archivefileName, hash, null) {}

		/// <summary>Constructor.</summary>
		internal GameBoxReference(string name, string description, string copyright, string archivefileName, byte[] hash, byte[] icon) {
			Debug.Assert(hash != null);
			this.name = name;
			this.description = description;
			this.copyright = copyright;
			this.archivefileName = archivefileName;
			this.hash = hash;
			this.icon = icon;
		}

		/// <summary>Name of this game box.</summary>
		public string Name { get { return name; } set { name = value; } }

		/// <summary>Description of this game box.</summary>
		public string Description { get { return description; } set { description = value; } }

		/// <summary>Copyright for this game box maps, pieces and rules.</summary>
		public string Copyright { get { return copyright; } set { copyright = value; } }

		/// <summary>Name of the file from which this game box was loaded.</summary>
		public string FileName { get { return archivefileName; } }

		/// <summary>Icon of this box.</summary>
		public byte [] Icon { get { return icon; } set { icon = value; } }

		/// <summary>SHA1 hash value for this game box file.</summary>
		public byte[] Hash { get { return hash; } }

		/// <summary>Compares two hash values.</summary>
		/// <param name="hash1">First hash value.</param>
		/// <param name="hash2">Second hash value.</param>
		/// <returns>True if the hash values are identical.</returns>
		internal static bool HashAreIdentical(byte[] hash1, byte[] hash2) {
			Debug.Assert(hash1 != null && hash2 != null);
			if(hash1.Length != hash2.Length) {
				return false;
			} else {
				for(int i = 0; i < hash1.Length; ++i)
					if(hash1[i] != hash2[i])
						return false;
				return true;
			}
		}

		private string name = null;
		private string description = null;
		private string copyright = null;
		private readonly string archivefileName;
		private readonly byte[] hash;
		private byte[] icon = null;
	}
}
