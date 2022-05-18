// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>GrabTerrainMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GrabTerrainMessage : StateChangeRequestMessage {

		internal GrabTerrainMessage() { }

		public GrabTerrainMessage(int stateChangeSequenceNumber, int boardId, int zOrder) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.boardId = boardId;
			this.zOrder = zOrder;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GrabTerrain; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref boardId);
			serializer.Serialize(ref zOrder);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null && sender.Guid != Guid.Empty && boardId != -1) {
				IGame game = model.CurrentGameBox.CurrentGame;
				IBoard board = game.GetBoardById(boardId);
				if(board != null) {
					IStack stackBeingGrabbed = board.GetStackFromZOrder(zOrder);
					ITerrainClone pieceBeingGrabbed = (ITerrainClone) stackBeingGrabbed.Pieces[0];
					CommandContext context = new CommandContext(stackBeingGrabbed.Board, stackBeingGrabbed.BoundingBox);
					model.CommandManager.ExecuteCommandSequence(
						context, context,
						new GrabTerrainCommand(model, sender.Guid, stackBeingGrabbed));
				}
			}
		}

		private int boardId;
		private int zOrder;
	}
}
