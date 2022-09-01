// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Reflection;

namespace ZunTzu.Control.Messages {

	/// <summary>DiceResultsMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class DiceResultsMessage : ReliableMessageFromClientToAll
	{

		internal DiceResultsMessage() {}

		public DiceResultsMessage(int diceHandIndex, int[] diceResults) {
			this.diceHandIndex = diceHandIndex;
			this.diceResults = diceResults;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.DiceResults; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref diceHandIndex);
			int diceCount = 0;
			if(serializer.IsSerializing) {
				diceCount = diceResults.Length;
			}
			serializer.Serialize(ref diceCount);
			if(!serializer.IsSerializing) {
				diceResults = new int[diceCount];
			}
			for(int i = 0; i < diceCount; ++i)
				serializer.Serialize(ref diceResults[i]);
		}

		public sealed override void Handle(Controller controller) {
			controller.View.DiceBag.CastDice(diceHandIndex, diceResults);
		}

		private int diceHandIndex;
		private int[] diceResults;
	}
}
