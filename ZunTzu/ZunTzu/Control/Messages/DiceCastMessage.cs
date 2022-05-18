// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;

namespace ZunTzu.Control.Messages {

	/// <summary>DiceCastMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DiceCastMessage : Message {

		internal DiceCastMessage() {}

		public DiceCastMessage(int diceHandIndex, int diceCount) {
			this.diceHandIndex = diceHandIndex;
			this.diceCount = diceCount;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DiceCast; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref diceHandIndex);
			serializer.Serialize(ref diceCount);
		}

		public sealed override void Handle(Controller controller) {
			Debug.Assert(controller.Model.IsHosting);

			int dieFaceCount;
			switch(controller.Model.CurrentGameBox.CurrentGame.DiceHands[diceHandIndex].DiceType) {
				case DiceType.D4:
					dieFaceCount = 4;
					break;
				case DiceType.D8:
					dieFaceCount = 8;
					break;
				case DiceType.D10:
					dieFaceCount = 10;
					break;
				case DiceType.D12:
					dieFaceCount = 12;
					break;
				case DiceType.D20:
					dieFaceCount = 20;
					break;
				default:
					dieFaceCount = 6;
					break;
			}

			int[] diceResults = new int[diceCount];
			for(int i = 0; i < diceCount; ++i)
				diceResults[i] = controller.DieSimulator.GetDieResult(dieFaceCount);

			controller.NetworkClient.Send(new DiceResultsMessage(diceHandIndex, diceResults));
		}

		private int diceHandIndex;
		private int diceCount;
	}
}
