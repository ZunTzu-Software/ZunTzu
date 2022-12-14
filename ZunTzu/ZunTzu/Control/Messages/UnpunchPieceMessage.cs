// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>UnpunchPieceMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class UnpunchPieceMessage : StateChangeRequestMessage {

		internal UnpunchPieceMessage() { }

		public UnpunchPieceMessage(int stateChangeSequenceNumber, int pieceBeingUnpunchedId) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.pieceBeingUnpunchedId = pieceBeingUnpunchedId;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.UnpunchPiece; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref pieceBeingUnpunchedId);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPiece pieceBeingUnpunched = model.CurrentGameBox.CurrentGame.GetPieceById(pieceBeingUnpunchedId);
			IStack stack = pieceBeingUnpunched.Stack;
			IPlayer sender = model.GetPlayer(senderId);

			if (!pieceBeingUnpunched.IsBlock || (pieceBeingUnpunched.IsBlock && (pieceBeingUnpunched.Owner == Guid.Empty || pieceBeingUnpunched.Owner == sender.Guid))) {
				if (stack.Board == null) {
					// piece is currently in the player's hand
					if (sender != null && sender.Guid != Guid.Empty) {

						PointF positionWhenAttached = pieceBeingUnpunched.PositionWhenAttached;
						SizeF size = pieceBeingUnpunched.Size;
						RectangleF boundingBoxWhenAttached = new RectangleF(
							positionWhenAttached.X - size.Width * 0.5f,
							positionWhenAttached.Y - size.Height * 0.5f,
							size.Width,
							size.Height);
						CommandContext context = new CommandContext(pieceBeingUnpunched.CounterSection.CounterSheet, boundingBoxWhenAttached);
						model.CommandManager.ExecuteCommandSequence(
							context, context,
							new UnpunchHandPieceCommand(model, sender.Guid, pieceBeingUnpunched));
					}
				} else {
					CommandContext context = new CommandContext(stack.Board, stack.BoundingBox);
					if (stack.Pieces.Length == 1) {
						model.CommandManager.ExecuteCommandSequence(
							context, context,
							new UnpunchSelectionCommand(model, stack));
					} else {
						model.CommandManager.ExecuteCommandSequence(
							context, context,
							new UnpunchSubSelectionCommand(model, pieceBeingUnpunched.Select()));
					}
				}
			}

			if(sender != null)
				sender.PieceBeingDragged = null;
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int pieceBeingUnpunchedId;
	}
}
