// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Windows.Forms;

//
//  +------------+     +---------------+     +---------+
//  | Networking |<----+ Modelization  |<----+         |
//  +------------+     +---------------+     |         |     
//                             ^             |         |
//                             |             | Control |
//                             |             |         |
//  +------------+     +-------+-------+     |         |     
//  |  Graphics  |<----+ Visualization |<----+         |
//  +------------+     +---------------+     +---------+
// 

namespace ZunTzu.Control {

	/// <summary>Summary description for IController.</summary>
	public interface IController {
		/// <summary>Processes mouse, keyboard and network messages.</summary>
		/// <param name="currentTimeInMicroseconds">Time of this frame.</param>
		void DoEvents(long currentTimeInMicroseconds);
		/// <summary>Parses and runs a command.</summary>
		/// <param name="command">The command to run, with no heading "cmd".</param>
		void ExecuteCommand(string command);
		/// <summary>True if ZunTzu exited abruptly leaving an autosave file.</summary>
		bool AutosaveAvailable { get; }
	}

	/// <summary>Standalone network server.</summary>
	public interface INetworkServer {
		/// <summary>Launches the server.</summary>
		void Run();
	}
}
