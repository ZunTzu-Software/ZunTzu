// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>MoveSelectionMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class MoveSelectionMessage : StateChangeRequestMessage {

		internal MoveSelectionMessage() {}

		public MoveSelectionMessage(int stateChangeSequenceNumber, PointF newPosition) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.newPosition = newPosition;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.MoveSelection; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref newPosition);
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			ISelection selection = model.CurrentSelection;
			IBoard visibleBoard = model.CurrentGameBox.CurrentGame.VisibleBoard;
			if(selection != null && !selection.Empty) {
				IStack stack = selection.Stack;
				if(stack.AttachedToCounterSection) {
					if(stack.Board == visibleBoard) {
						model.CommandManager.ExecuteCommandSequence(
							new MoveAttachedStackCommand(model, stack, newPosition));
					} else {
						model.CommandManager.ExecuteCommandSequence(
							new MoveAttachedStackFromOtherBoardCommand(model, stack, visibleBoard, newPosition));
					}
				} else {
					if(stack.Pieces.Length == selection.Pieces.Length) {
						if(stack.Board == visibleBoard) {
							model.CommandManager.ExecuteCommandSequence(
								new CommandContext(visibleBoard, stack.BoundingBox),
								new CommandContext(visibleBoard),
								new MoveStackCommand(model, stack, newPosition));
						} else {
							model.CommandManager.ExecuteCommandSequence(
								new CommandContext(stack.Board, stack.BoundingBox),
								new CommandContext(visibleBoard),
								new MoveStackFromOtherBoardCommand(model, stack, visibleBoard, newPosition));
						}
					} else {
						if(stack.Board == visibleBoard) {
							model.CommandManager.ExecuteCommandSequence(
								new CommandContext(visibleBoard, stack.BoundingBox),
								new CommandContext(visibleBoard),
								new MoveSubSelectionCommand(model, selection, newPosition));
						} else {
							model.CommandManager.ExecuteCommandSequence(
								new CommandContext(stack.Board, stack.BoundingBox),
								new CommandContext(visibleBoard),
								new MoveSubSelectionFromOtherBoardCommand(model, selection, visibleBoard, newPosition));
						}
					}
				}
			}
		}

		private PointF newPosition;
	}
}
