// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.IO;
using System.Reflection;

namespace ZunTzu.FileSystem {

	/// <summary>Resource file.</summary>
	internal sealed class Resource : IFile {

		/// <summary>Constuctor.</summary>
		/// <param name="resourceName">Resource name.</param>
		internal Resource(string resourceName) {
			this.resourceName = resourceName;
		}

		/// <summary>Size of this file in bytes.</summary>
		public int SizeInBytes {
			get {
				using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					return (int) stream.Length;
				}
			}
		}

		/// <summary>Opens this file for reading.</summary>
		/// <returns>An input stream.</returns>
		public Stream Open() {
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
		}

		/// <summary>Archive.</summary>
		IArchive IFile.Archive { get { return null; } }

		/// <summary>File name.</summary>
		string IFile.FileName { get { return null; } }

		private string resourceName;
	}
}
