// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Runtime.InteropServices;

namespace ZunTzu.Timing {
	/// <summary>Clock with a microsecond accuracy.</summary>
	public sealed class PrecisionTimer : IPrecisionTimer {
		/// <summary>Constructor.</summary>
		public PrecisionTimer() {
			if(!QueryPerformanceFrequency(out highPerformanceTimerFrequency))
				throw new ApplicationException("The installed hardware does not support a high-resolution performance counter.");
			if(!QueryPerformanceCounter(out referenceTicks))
				throw new ApplicationException("Failed to query the high-resolution performance counter.");
		}

		/// <summary>Current time in microseconds ticks.</summary>
		public long NowInMicroseconds {
			get {
				ulong ticks;
				if(!QueryPerformanceCounter(out ticks))
					throw new ApplicationException("Failed to query the high-resolution performance counter.");

				return (long)(((ticks - referenceTicks) * (ulong)1000000) / highPerformanceTimerFrequency);
			}
		}

		/// <summary>Retrieves the current value of the high-resolution performance counter.</summary>
		/// <param name="lpPerformanceCount">Pointer to a variable that receives the current performance-counter value, in counts.</param>
		/// <returns>True if the function succeeds.</returns>
		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(out ulong lpPerformanceCount);

		/// <summary>Retrieves the frequency of the high-resolution performance counter.</summary>
		/// <param name="lpFrequency">Pointer to a variable that receives the current performance-counter frequency, in counts per second.</param>
		/// <returns>True if the installed hardware supports a high-resolution performance counter.</returns>
		/// <remarks>The QueryPerformanceFrequency function retrieves the frequency of the high-resolution performance counter, if one exists. The frequency cannot change while the system is running.</remarks>
		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out ulong lpFrequency);

		private ulong referenceTicks;
		private ulong highPerformanceTimerFrequency;
	}
}
