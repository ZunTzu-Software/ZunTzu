// Copyright (c) 2022 ZunTzu Software and contributors

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using ZunTzu.Timing;

namespace ZunTzu.FileSystem {

	/// <summary>Single entry in a ZunTzu archive.</summary>
	internal sealed class File : IFile {

		/// <summary>Constructor.</summary>
		/// <param name="archive">Archive.</param>
		/// <param name="fileName">File name.</param>
		internal File(Archive archive, string fileName) {
			this.archive = archive;
			this.fileName = fileName;
		}

		/// <summary>Size of this file in bytes.</summary>
		/// <exception cref="FileNotFoundException">The file does not exist in the archive.</exception>
		public int SizeInBytes {
			get {
				using(ZipInputStream stream = new ZipInputStream(archive.Open())) {
					ZipEntry entry;
					while((entry = stream.GetNextEntry()) != null) {
						if(entry.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)) {
							return (int)entry.Size;
						}
					}
				}
				throw new FileNotFoundException(string.Format("File \"{0}\" not found in archive.", fileName));
			}
		}

		/// <summary>Opens this file for reading.</summary>
		/// <returns>An input stream.</returns>
		/// <exception cref="FileNotFoundException">The file does not exist in the archive.</exception>
		public Stream Open() {
			if(fileName.EndsWith(".encrypted", StringComparison.InvariantCultureIgnoreCase)) {
				return openEncryptedFile();
			} else {
				ZipInputStream stream = null;
				try {
					stream = new ZipInputStream(archive.Open());
					ZipEntry entry;
					while((entry = stream.GetNextEntry()) != null) {
						if(entry.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)) {
							return stream;
						}
					}
				} catch(Exception) {
					if(stream != null)
						stream.Close();
					throw;
				}
				if(stream != null)
					stream.Close();
				throw new FileNotFoundException(string.Format("File \"{0}\" not found in archive.", fileName));
			}
		}

		private Stream openEncryptedFile() {
			throw new FileNotFoundException(string.Format("Encryption is not supported in the open source version."));
		}

		/// <summary>Archive.</summary>
		public IArchive Archive { get { return archive; } }

		/// <summary>File name.</summary>
		public string FileName { get { return fileName; } }

		private Archive archive;
		private string fileName;
	}
}
