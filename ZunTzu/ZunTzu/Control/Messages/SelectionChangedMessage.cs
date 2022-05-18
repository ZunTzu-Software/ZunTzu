// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>SelectionChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class SelectionChangedMessage : StateChangeRequestMessage {

		internal SelectionChangedMessage() {}

		public struct SelectionInfo {
			public bool IsEmpty { get { return StackId == -1; } }
			public int StackId;
			public int[] PieceIds;
		}

		public SelectionChangedMessage(int stateChangeSequenceNumber, SelectionInfo newSelection) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.newSelection = newSelection;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.SelectionChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref newSelection.StackId);
			if(!newSelection.IsEmpty) {
				int selectionSize = 0;
				if(serializer.IsSerializing) {
					selectionSize = newSelection.PieceIds.Length;
				}
				serializer.Serialize(ref selectionSize);
				if(!serializer.IsSerializing) {
					newSelection.PieceIds = new int[selectionSize];
				}
				for(int i = 0; i < selectionSize; ++i)
					serializer.Serialize(ref newSelection.PieceIds[i]);
			}
		}

		public sealed override void HandleAccept(Controller controller) {
			IModel model = controller.Model;
			IGame game = model.CurrentGameBox.CurrentGame;
			if(newSelection.IsEmpty) {
				model.CurrentSelection = null;
			} else {
				IStack stack = game.GetStackById(newSelection.StackId);
				if(model.AnimationManager.IsBeingAnimated(stack))
					model.AnimationManager.EndAllAnimations();

				ISelection selection = stack.Select().RemoveAllPieces();
				foreach(int pieceId in newSelection.PieceIds) {
					IPiece piece = game.GetPieceById(pieceId);
					selection = selection.AddPiece(piece);
				}
				model.CurrentSelection = selection;
			}
		}

		private SelectionInfo newSelection;
	}
}
