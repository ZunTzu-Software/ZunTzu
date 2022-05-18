// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;
using ZunTzu.Randomness;

namespace ZunTzu.Control.Messages {

	/// <summary>ShuffleResultMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ShuffleResultMessage : StateChangeMessage {

		internal ShuffleResultMessage() { }

		public ShuffleResultMessage(int stateChangeSequenceNumber, Permutation permutation) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			permutationBytes = permutation.ToBytes();
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ShuffleResult; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref permutationBytes);
		}

		public sealed override void Handle(Controller controller) {
			IModel model = controller.Model;
			model.StateChangeSequenceNumber = stateChangeSequenceNumber;

			Permutation permutation = Permutation.FromBytes(permutationBytes);
			model.CommandManager.ExecuteCommandSequence(new ShuffleCommand(model, permutation));
		}

		private byte[] permutationBytes;
	}
}
