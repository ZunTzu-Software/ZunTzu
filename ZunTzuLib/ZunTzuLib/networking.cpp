/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "raknet/RakPeerInterface.h"

using namespace RakNet;

extern "C" void* __cdecl CreatePeer()
{
	RakPeerInterface* peer = RakPeerInterface::GetInstance();
	return peer;
}

extern "C" void __cdecl FreePeer(void* client_or_server)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	RakPeerInterface::DestroyInstance(peer);
}

extern "C" int __cdecl StartupClient(void* client, unsigned short port)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	SocketDescriptor sock{ port, "" };
	StartupResult result = peer->Startup(1, &sock, 1);
	return result;
}

extern "C" int __cdecl StartupServer(void* server, unsigned short port)
{
	auto peer = static_cast<RakPeerInterface*>(server);

	const unsigned short max_clients = 32;
	peer->SetMaximumIncomingConnections(max_clients);

	SocketDescriptor sock{ port, "" };
	StartupResult result = peer->Startup(max_clients, &sock, 1);
	return result;
}

extern "C" void __cdecl Shutdown(void* client_or_server)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	unsigned int wait_for_pending_messages = 10;
	peer->Shutdown(wait_for_pending_messages);
}

extern "C" int __cdecl Connect(void* client, const char* host, unsigned short remotePort)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	ConnectionAttemptResult result = peer->Connect(host, remotePort, nullptr, 0);
	return result;
}

extern "C" uint32_t __cdecl Send(void* client, const char* data, int length, int priority, int reliability, char orderingChannel, AddressOrGUID systemIdentifier, bool broadcast)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	uint32_t send_receipt = peer->Send(data, length, static_cast<PacketPriority>(priority), static_cast<PacketReliability>(reliability), orderingChannel, systemIdentifier, broadcast);
	return send_receipt;
}

extern "C" void* __cdecl Receive(void* client_or_server)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	auto packet = peer->Receive();
	return packet;
}

extern "C" void __cdecl GetPacketData(void* packet, const unsigned char** data, unsigned int* length, const void** sender)
{
	auto pkt = static_cast<Packet*>(packet);
	*data = pkt->data;
	*length = pkt->length;
	*sender = &pkt->guid;
}

extern "C" void __cdecl DeallocatePacket(void* client_or_server, void* packet)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	auto pkt = static_cast<Packet*>(packet);
	peer->DeallocatePacket(pkt);
}

extern "C" void __cdecl Statistics(void* client)
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

