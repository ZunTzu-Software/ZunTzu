// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>BeginRotationMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class BeginRotationMessage : Message {

		public BeginRotationMessage() {}

		public override NetworkMessageType Type { get { return NetworkMessageType.BeginRotation; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {}

		public sealed override void Handle(Controller controller) {
			controller.NetworkClient.Send(senderId, new ContinueRotationMessage());
		}
	}
}
