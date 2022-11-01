// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Networking;
using ZunTzu.Properties;

namespace ZunTzu.Control.Messages {

	/// <summary>VideoFrameReceivedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VideoFrameReceivedMessage : SystemMessage {
		internal VideoFrameReceivedMessage(byte[] frameBuffer)
		{
			Debug.Assert(frameBuffer.Length == 10 + 64 * 64 * 3); // message ID, message type, sender, pixels
			_frameBuffer = frameBuffer;
			_senderId =
				((UInt64)frameBuffer[2] << 0) |
				((UInt64)frameBuffer[3] << 8) |
				((UInt64)frameBuffer[4] << 16) |
				((UInt64)frameBuffer[5] << 24) |
				((UInt64)frameBuffer[6] << 32) |
				((UInt64)frameBuffer[7] << 40) |
				((UInt64)frameBuffer[8] << 48) |
				((UInt64)frameBuffer[9] << 56);
		}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VideoFrameReceived; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer)
		{
			throw new InvalidOperationException(); // this message is never serialized or deserialized (use the constructor instead) 
		}

		public sealed override void Handle(Controller controller) {
			if(_frameBuffer != null) {
				IPlayer sender = controller.Model.GetPlayer(_senderId);
				if(sender != null)
					controller.View.PlayerView.UpdateVideoFrame(_senderId, _frameBuffer);
			}
		}

		byte[] _frameBuffer;
		UInt64 _senderId = 0;
	}
}
