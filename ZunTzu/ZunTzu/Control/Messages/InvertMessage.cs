// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>InvertMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class InvertMessage : StateChangeRequestMessage {

		internal InvertMessage() { }

		public InvertMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.Invert; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			model.CommandManager.ExecuteCommandSequence(new InvertCommand(model));
		}
	}
}
