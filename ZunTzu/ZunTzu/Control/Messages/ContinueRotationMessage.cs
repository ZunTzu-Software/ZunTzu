// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>ContinueRotationMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ContinueRotationMessage : Message {

		internal ContinueRotationMessage() {}

		public override NetworkMessageType Type { get { return NetworkMessageType.ContinueRotation; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {}

		public sealed override void Handle(Controller controller) {
			controller.IdleState.ContinueRotation();
		}
	}
}
