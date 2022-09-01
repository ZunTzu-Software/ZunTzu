/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include "ZunTzuLib.h"
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
	SocketDescriptor sock{ port, nullptr };
	StartupResult result = peer->Startup(1, &sock, 1);
	return result;
}

extern "C" int __cdecl StartupServer(void* server, unsigned short port)
{
	auto peer = static_cast<RakPeerInterface*>(server);

	const unsigned short max_clients = 32;

	SocketDescriptor sock{ port, nullptr };
	StartupResult result = peer->Startup(max_clients, &sock, 1);

	if (result == RAKNET_STARTED) {
		peer->SetMaximumIncomingConnections(max_clients);
	}

	return result;
}

extern "C" void __cdecl Shutdown(void* client_or_server)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	unsigned int block_duration = 300;
	peer->Shutdown(block_duration);
}

extern "C" int __cdecl Connect(void* client, const char* host, unsigned short remote_port)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	ConnectionAttemptResult result = peer->Connect(host, remote_port, nullptr, 0);
	return result;
}

extern "C" int __cdecl Send(void* client, const char* data, int length, int priority, int reliability, int ordering_channel, void* recipient, bool broadcast)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	auto recipient_address_or_guid = static_cast<AddressOrGUID*>(recipient);
	uint32_t send_receipt = peer->Send(data, length, static_cast<PacketPriority>(priority), static_cast<PacketReliability>(reliability), (char)ordering_channel, *recipient_address_or_guid, broadcast);
	return send_receipt;
}

extern "C" void* __cdecl Receive(void* client_or_server)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	auto packet = peer->Receive();
	return packet;
}

extern "C" void __cdecl GetPacketData(void* packet, const char** data, int* length, const void** sender)
{
	auto pkt = static_cast<Packet*>(packet);
	*data = reinterpret_cast<char*>(pkt->data);
	*length = pkt->length;
	*sender = static_cast<const void*>(&pkt->guid);
}

extern "C" void __cdecl DeallocatePacket(void* client_or_server, void* packet)
{
	auto peer = static_cast<RakPeerInterface*>(client_or_server);
	auto pkt = static_cast<Packet*>(packet);
	peer->DeallocatePacket(pkt);
}

extern "C" unsigned long long __cdecl GetGuid(void* client)
{
	auto peer = static_cast<RakPeerInterface*>(client);
	return peer->GetMyGUID().g;
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
