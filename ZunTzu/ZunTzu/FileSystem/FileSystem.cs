// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.IO;

//
//  +------------+     +---------------+
//  | FileSystem |<----+ Modelization  |
//  +------------+     +---------------+
//        ^
//        |
//        |
//  +-----+------+
//  |  Graphics  |
//  +------------+
// 

namespace ZunTzu.FileSystem {

	/// <summary>The exception that is thrown when an attempt to access a file that does not exist in an archive fails.</summary>
	public sealed class FileNotFoundException : ApplicationException {
		/// <summary>Initializes a new instance of the FileNotFoundException class.</summary>
		/// <param name="message">A String that describes the error.</param>
		public FileNotFoundException(string message) : base(message) {}
	}

	/// <summary>ZunTzu archive file (game box, scenario, anything...).</summary>
	public interface IArchive {
		/// <summary>Retrieves a single entry in this archive.</summary>
		/// <param name="fileName">File name.</param>
		/// <returns>A file.</returns>
		IFile GetFile(string fileName);
		/// <summary>List of all the files in this archive.</summary>
		IEnumerable<string> Files { get; }
		/// <summary>File name.</summary>
		string FileName { get; }
	}

	/// <summary>Single entry in a ZunTzu archive.</summary>
	public interface IFile {
		/// <summary>Size of this file in bytes.</summary>
		/// <exception cref="FileNotFoundException">The file does not exist in the archive.</exception>
		int SizeInBytes { get; }
		/// <summary>Opens this file for reading.</summary>
		/// <returns>An input stream.</returns>
		/// <exception cref="FileNotFoundException">The file does not exist in the archive.</exception>
		Stream Open();
		/// <summary>Archive.</summary>
		IArchive Archive { get; }
		/// <summary>File name.</summary>
		string FileName { get; }
	}

	/// <summary>Static file system functions.</summary>
	public abstract class FileSystem {
		/// <summary>Retrieves a single resource.</summary>
		/// <param name="resourceName">Resource name.</param>
		/// <returns>A file.</returns>
		public static IFile GetResource(string resourceName) {
			return new Resource(resourceName);
		}
	}
}
