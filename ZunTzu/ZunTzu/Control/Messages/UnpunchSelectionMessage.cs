// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>UnpunchSelectionMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class UnpunchSelectionMessage : StateChangeRequestMessage {

		internal UnpunchSelectionMessage() {}

		public UnpunchSelectionMessage(int stateChangeSequenceNumber) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.UnpunchSelection; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			ISelection selection = model.CurrentSelection;
			if(selection != null && !selection.Empty) {
				IStack stack = selection.Stack;
				CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
				if(stack.Pieces.Length == selection.Pieces.Length) {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new UnpunchSelectionCommand(model, stack));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new UnpunchSubSelectionCommand(model, selection));
				}
			}
		}
	}
}
