// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>ChangeModeMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class ChangeModeMessage : StateChangeRequestMessage {

		internal ChangeModeMessage() { }

		public ChangeModeMessage(int stateChangeSequenceNumber, Mode mode) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.mode = (byte) mode;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.ChangeMode; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref mode);
		}

		public sealed override void HandleAccept(Controller controller) {
			IGame game = controller.Model.CurrentGameBox.CurrentGame;
			if(mode >= 0 && mode <= 1 && mode != (byte) game.Mode) {
				game.Mode = (Mode) mode;
				foreach(IPlayer player in controller.Model.Players) {
					player.StackBeingDragged = null;
					player.PieceBeingDragged = null;
				}
				if(game.Mode == Mode.Terrain) {
					controller.Model.CurrentSelection = null;
				} else {
					ICounterSheet visibleSheet = game.VisibleBoard as ICounterSheet;
					if(visibleSheet != null && visibleSheet.Properties.Type == CounterSheetType.Terrain) {
						IBoard[] boards = game.Boards;
						for(int i = 0; i < boards.Length; ++i) {
							ICounterSheet counterSheet = boards[i] as ICounterSheet;
							if(counterSheet == null || counterSheet.Properties.Type != CounterSheetType.Terrain) {
								game.VisibleBoard = boards[i];
								break;
							}
						}
					}
				}
			}
		}

		private byte mode;
	}
}
