// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>GrabStackMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GrabStackMessage : StateChangeRequestMessage {

		internal GrabStackMessage() { }

		public GrabStackMessage(int stateChangeSequenceNumber, int stackBeingGrabbedId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.stackBeingGrabbedId = stackBeingGrabbedId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GrabStack; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref stackBeingGrabbedId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty) {
				IPiece pieceBeingGrabbed = model.CurrentGameBox.CurrentGame.GetPieceById(stackBeingGrabbedId);
				IStack stackBeingGrabbed = pieceBeingGrabbed.Stack;
				CommandContext context = new CommandContext(stackBeingGrabbed.Board, stackBeingGrabbed.BoundingBox);
				if(stackBeingGrabbed.AttachedToCounterSection) {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new GrabAttachedStackCommand(model, sender.Guid, stackBeingGrabbed));
				} else {
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						(pieceBeingGrabbed == stackBeingGrabbed.Pieces[0] ?
							(ICommand) new GrabStackCommand(model, sender.Guid, stackBeingGrabbed) :
							(ICommand) new GrabTopOfStackCommand(model, sender.Guid, pieceBeingGrabbed)));
				}
			}
		}

		private int stackBeingGrabbedId;
	}
}
