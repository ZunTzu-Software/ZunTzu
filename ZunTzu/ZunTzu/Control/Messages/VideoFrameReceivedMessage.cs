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
		internal VideoFrameReceivedMessage() { }

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.VideoFrameReceived; } }

		/// <summary>Deserializes the data for this message.</summary>
		/// <param name="serializedMessage">The serialized data of the message.</param>
		internal override void Deserialize(byte[] serializedMessage) {
			frameBuffer = serializedMessage;
		}

		public sealed override void Handle(Controller controller) {
			if(frameBuffer != null) {
				IPlayer sender = controller.Model.GetPlayer(senderId);
				if(sender != null)
					controller.View.PlayerView.UpdateVideoFrame(senderId, frameBuffer);
			}
		}

		private byte[] frameBuffer;
	}
}
