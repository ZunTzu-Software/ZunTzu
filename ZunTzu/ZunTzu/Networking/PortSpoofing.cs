// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ZunTzu.Networking {

	/// <summary>Networking from spoofed ports.</summary>
	internal static class PortSpoofing {

		/// <summary>The IP header of a UDP packet.</summary>
		[StructLayout(LayoutKind.Explicit)]
		private unsafe struct IPHeader {
			[FieldOffset(0)]
			public byte VersionAndInternetHeaderLength;
			[FieldOffset(1)]
			public byte TypeOfService;
			[FieldOffset(2)]
			public ushort TotalLength;
			[FieldOffset(4)]
			public ushort Identification;
			[FieldOffset(6)]
			public ushort Fragmentation;
			[FieldOffset(8)]
			public byte TimeToLive;
			[FieldOffset(9)]
			public byte Protocol;
			[FieldOffset(10)]
			public ushort HeaderChecksum;
			[FieldOffset(12)]
			public uint SourceAddress;
			[FieldOffset(16)]
			public uint DestinationAddress;

			[FieldOffset(0)]
			public fixed ushort Words[10];

			public IPHeader(IPAddress sourceAddress, IPAddress destinationAddress, int totalLength) {
				VersionAndInternetHeaderLength = (4 << 4) | 5;	// Internet Protocol
				TypeOfService = 0;
				TotalLength = (ushort) IPAddress.HostToNetworkOrder((short) totalLength);
				Identification = (ushort) IPAddress.HostToNetworkOrder((short) 1);
				Fragmentation = 0;
				TimeToLive = 255;
				Protocol = 17;	// UDP
				HeaderChecksum = 0;
				SourceAddress = (uint) sourceAddress.Address;
				DestinationAddress = (uint) destinationAddress.Address;
			}
		}

		/// <summary>The IP and UDP headers of a UDP packet.</summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct UDPHeader {
			public IPHeader IPHeader;
			public ushort SourcePort;
			public ushort DestinationPort;
			public ushort Length;
			public ushort Checksum;

			public UDPHeader(IPAddress sourceAddress, IPAddress destinationAddress, ushort sourcePort, ushort destinationPort, int dataLength) {
				unsafe {
					IPHeader = new IPHeader(sourceAddress, destinationAddress, dataLength + sizeof(UDPHeader));
				}
				SourcePort = (ushort) IPAddress.HostToNetworkOrder((short) sourcePort);
				DestinationPort = (ushort) IPAddress.HostToNetworkOrder((short) destinationPort);
				Length = (ushort) IPAddress.HostToNetworkOrder((short) (dataLength + 8));
				Checksum = 0;
			}
		}

		/// <summary>The pseudo-header of a UDP packet, used in computing the UDP checksum.</summary>
		[StructLayout(LayoutKind.Explicit)]
		private unsafe struct PseudoHeader {
			[FieldOffset(0)]
			public uint SourceAddress;
			[FieldOffset(4)]
			public uint DestinationAddress;
			[FieldOffset(8)]
			public byte Zeros;
			[FieldOffset(9)]
			public byte Protocol;
			[FieldOffset(10)]
			public ushort TotalLength;
			[FieldOffset(12)]
			public ushort SourcePort;
			[FieldOffset(14)]
			public ushort DestinationPort;
			[FieldOffset(16)]
			public ushort Length;
			[FieldOffset(18)]
			public ushort Checksum;

			[FieldOffset(0)]
			public fixed ushort Words[10];

			public PseudoHeader(UDPHeader header) {
				SourceAddress = header.IPHeader.SourceAddress;
				DestinationAddress = header.IPHeader.DestinationAddress;
				Zeros = 0;
				Protocol = header.IPHeader.Protocol;
				TotalLength = header.Length;
				SourcePort = header.SourcePort;
				DestinationPort = header.DestinationPort;
				Length = header.Length;
				Checksum = 0;
			}
		}

		/// <summary>Sends data as an UDP datagram from a spoofed port.</summary>
		/// <param name="spoofedEndPoint">The spoofed local endpoint.</param>
		/// <param name="remoteEndPoint">The remote endpoint.</param>
		/// <param name="data">An array that contains the data to send.</param>
		public static unsafe void Send(IPEndPoint spoofedEndPoint, IPEndPoint remoteEndPoint, byte[] data) {
			UDPHeader header = new UDPHeader(spoofedEndPoint.Address, remoteEndPoint.Address, (ushort) spoofedEndPoint.Port, (ushort) remoteEndPoint.Port, data.Length);

			// compute IP header checksum
			uint sum = 0;
			for(int i = 0; i < 10; ++i)
				sum += header.IPHeader.Words[i];
			while((sum >> 16) != 0)
				sum = (sum & 0xffff) + (sum >> 16);
			header.IPHeader.HeaderChecksum = (ushort) ~sum;

			// compute UDP checksum
			sum = 0;
			PseudoHeader pseudoHeader = new PseudoHeader(header);
			for(int i = 0; i < 10; ++i)
				sum += pseudoHeader.Words[i];
			fixed(byte* dataPtr = data) {
				for(int i = 0; i < data.Length / 2; ++i)
					sum += ((ushort*) dataPtr)[i];
			}
			if(data.Length % 2 == 1)
				sum += data[data.Length - 1];
			while((sum >> 16) != 0)
				sum = (sum & 0xffff) + (sum >> 16);
			header.Checksum = (ushort) ~sum;

			// copy header and data into datagram
			byte[] datagram = new byte[data.Length + sizeof(UDPHeader)];
			for(int i = 0; i < sizeof(UDPHeader); ++i)
				datagram[i] = ((byte*) &header)[i];
			for(int i = 0; i < data.Length; ++i)
				datagram[i + sizeof(UDPHeader)] = data[i];

			// send datagram
			using(Socket rawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Raw)) {
				rawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
				rawSocket.SendTo(datagram, remoteEndPoint);
			}
		}
	}
}
