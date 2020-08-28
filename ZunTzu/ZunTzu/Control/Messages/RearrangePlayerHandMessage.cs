// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Control.Messages {

	/// <summary>RearrangePlayerHandMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class RearrangePlayerHandMessage : StateChangeRequestMessage {

		internal RearrangePlayerHandMessage() { }

		public RearrangePlayerHandMessage(int stateChangeSequenceNumber, int currentIndex, int insertionIndex) {
			this.stateChangeSequenceNumber = stateChangeSequenceNumber;
			this.currentIndex = currentIndex;
			this.insertionIndex = insertionIndex;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.RearrangePlayerHand; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref stateChangeSequenceNumber);
			serializer.Serialize(ref currentIndex);
			serializer.Serialize(ref insertionIndex);
		}

		public sealed override void HandleAccept(Controller controller) {
			// this is a private action -> it can't be undone
			IModel model = controller.Model;
			IPlayer sender = model.GetPlayer(senderId);
			if(sender != null) {
				if(sender.Guid != Guid.Empty) {
					IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(sender.Guid);
					if(playerHand != null && playerHand.Count > 0) {
						if(model.AnimationManager.IsBeingAnimated(playerHand.Pieces[0].Stack))
							model.AnimationManager.EndAllAnimations();
						model.AnimationManager.LaunchAnimationSequence(
							new RearrangePlayerHandAnimation(playerHand, currentIndex, insertionIndex));
					}
				}
				sender.PieceBeingDragged = null;
			}
		}

		/// <summary>Handles the rejection of this state change.</summary>
		public sealed override void HandleReject(Controller controller) {
			controller.NetworkClient.Send(new DragDropAbortedMessage());
		}

		private int currentIndex;
		private int insertionIndex;
	}
}
