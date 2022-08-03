/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "raknet\RakPeerInterface.h"

using namespace RakNet;

extern "C" void* __cdecl CreateClient()
{
	return RakPeerInterface::GetInstance();
}

extern "C" void __cdecl FreeClient(void* client)
{
	auto peer_interface = static_cast<RakPeerInterface*>(client);
	RakPeerInterface::DestroyInstance(peer_interface);
}

extern "C" void __cdecl Connect(void* client)
{
}

extern "C" void __cdecl Send(void* client)
{
}

extern "C" void __cdecl Statistics(void* client)
{
}

extern "C" void * __cdecl CreateServer()
{
	return nullptr;
}

extern "C" void __cdecl FreeServer(void* server)
{
}

extern "C" void __cdecl Host(void* server)
{
}

extern "C" void __cdecl SendToOne(void* server, int player_id)
{
}

extern "C" void __cdecl SendToAllOthers(void* server, int player_id)
{
}

extern "C" void __cdecl SendToAll(void* server)
{
}

