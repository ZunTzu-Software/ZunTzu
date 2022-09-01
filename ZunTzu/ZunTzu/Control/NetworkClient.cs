// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using ZunTzu.Control.Messages;
using ZunTzu.Networking;

namespace ZunTzu.Control {

	/// <summary>Summary description for NetworkController.</summary>
	internal sealed class NetworkClient {

		public NetworkClient(IClient client) {
			this.client = client;
		}

		/// <summary>Send a message to other players.</summary>
		/// <param name="message">Message to send.</param>
		public void Send(Message message) {
			client.Send(message.Serialize());
		}

		/// <summary>Send a message to a single player.</summary>
		/// <param name="message">Message to send.</param>
		public void Send(UInt64 recipientId, Message message) {
			client.Send(recipientId, message.Serialize());
		}

		public IEnumerable<Message> RetrieveNetworkMessages() {
			foreach(NetworkMessage message in client.RetrieveNetworkMessages())
				yield return Message.CreateInstance(message);
		}

		private IClient client;
	}
}
