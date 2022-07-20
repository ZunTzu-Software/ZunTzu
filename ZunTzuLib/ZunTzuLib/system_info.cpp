/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"

unsigned char CPUCount(unsigned int * logicalCpuCount, unsigned int * coreCount, unsigned int * physicalCpuCount);

extern "C" int __cdecl GetProcessorCoreCount()
{
#ifdef USE_SSE2
	unsigned int logicalCpuCount = 0;
	unsigned int coreCount = 0;
	unsigned int physicalCpuCount = 0;
	unsigned char StatusFlag = CPUCount(&logicalCpuCount, &coreCount, &physicalCpuCount);
	if(8 == StatusFlag) {
		// User Configuration Error: Not all logical processors in the system are enabled
		// while running this process.
		return 1;
	} else {
		return (coreCount < 1 || coreCount > 8 ? 1 : coreCount);
	}
#else
	return 1;
#endif
}
