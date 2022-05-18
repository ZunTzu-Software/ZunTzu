// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZunTzu.FileSystem {

	/// <summary>ZunTzu archive file (game box, scenario, anything...).</summary>
	internal sealed class Archive : IArchive {

		/// <summary>Constructor.</summary>
		/// <param name="archiveFileName">File name.</param>
		public Archive(string archiveFileName) {
			this.archiveFileName = archiveFileName;
		}

		/// <summary>Retrieves a single entry in this archive.</summary>
		/// <param name="fileName">File name.</param>
		/// <returns>A file.</returns>
		public IFile GetFile(string fileName) {
			return new File(this, fileName);
		}

		/// <summary>List of all the files in this archive.</summary>
		public IEnumerable<string> Files {
			get {
				List<string> files = new List<string>();
				ZipInputStream stream = null;
				try {
					stream = new ZipInputStream(Open());
					ZipEntry entry;
					while((entry = stream.GetNextEntry()) != null)
						files.Add(entry.Name);
				} catch(Exception) {
					if(stream != null)
						stream.Close();
					throw;
				}
				if(stream != null)
					stream.Close();
				return files;
			}
		}

		/// <summary>Opens this archive for reading.</summary>
		/// <returns>An input stream.</returns>
		internal Stream Open() {
			return System.IO.File.OpenRead(archiveFileName);
		}

		/// <summary>File name.</summary>
		public string FileName { get { return archiveFileName; } }

		private string archiveFileName;
	}
}
