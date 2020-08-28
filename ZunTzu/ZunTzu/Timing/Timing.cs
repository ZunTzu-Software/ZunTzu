// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Timing {
	/// <summary>Clock with a microsecond accuracy.</summary>
	public interface IPrecisionTimer {
		/// <summary>Current time in microseconds ticks.</summary>
		long NowInMicroseconds { get; }
	}
}
