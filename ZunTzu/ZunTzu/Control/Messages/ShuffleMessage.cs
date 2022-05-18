// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;
using ZunTzu.Randomness;

namespace ZunTzu.Control.Messages {

	/// <summary>ShuffleMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ShuffleMessage : StateChangeRequestMessage {

		internal ShuffleMessage() { }

		public ShuffleMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public sealed override NetworkMessageType Type { get { return NetworkMessageType.Shuffle; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			ISelection selection = model.CurrentSelection;
			if(model.IsHosting && selection != null && selection.Stack.Pieces.Length > 1) {
				Permutation permutation = controller.DieSimulator.GetPermutation(selection.Stack.Pieces.Length);
				++model.StateChangeSequenceNumber;
				controller.NetworkClient.Send(new ShuffleResultMessage(model.StateChangeSequenceNumber, permutation));
			}
		}
	}
}
