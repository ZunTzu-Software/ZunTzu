/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

// this is to fix linker errors for things like IID_IDirectSound3DBuffer
// important: disable precompiled header for this file!!

#define INITGUID
#include <DSound.h>
