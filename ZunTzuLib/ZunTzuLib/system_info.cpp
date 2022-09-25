/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include <thread>
#include "ZunTzuLib.h"

extern "C" int __cdecl GetProcessorCoreCount()
{
	return (int)std::thread::hardware_concurrency();
}
