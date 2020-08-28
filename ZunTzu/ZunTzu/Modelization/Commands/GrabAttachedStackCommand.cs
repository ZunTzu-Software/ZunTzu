// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>GrabAttachedStackCommand command.</summary>
	public sealed class GrabAttachedStackCommand : Command {

		public GrabAttachedStackCommand(IModel model, Guid playerGuid, IStack stack)
			: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection && playerGuid != Guid.Empty);
			this.playerGuid = playerGuid;
			piece = stack.Pieces[0];
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			stackBefore = (piece is ITerrainPrototype ? null : stack);
			stackAfter = (playerHand == null || playerHand.Count == 0 ?
				null :	// the hand is currently empty
				playerHand.Stack);
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			if(stackAfter != null)
				preventConflict(stackBefore, stackAfter);
			else
				preventConflict(stackBefore);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			insertionIndex = (playerHand != null ? playerHand.Count : 0);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));

			if(piece is ITerrainPrototype) {
				if(stackAfter != null) {
					preventConflict(stackAfter);
					// does the hand already contain an identical clone?
					for(int i = 0; i < stackAfter.Pieces.Length; ++i) {
						ITerrainClone handPiece = stackAfter.Pieces[i] as ITerrainClone;
						if(handPiece != null && handPiece.Prototype == piece) {
							// yes -> simply move the piece to the new insertion index
							animations.Add(new RearrangePlayerHandAnimation(playerHand, i, insertionIndex));
							model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
							return;
						}
					}
				}
				// add the piece
				clone = new TerrainClone((TerrainPrototype) piece);
				stackBefore = clone.Stack;
				sideBefore = piece.Side;
			} else {
				sideBefore = piece.Side;
				animations.Add(new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { sideBefore }));
			}
			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet));
			animations.Add(new MoveStackToHandAnimation(stackBefore));
			if(stackAfter == null)
				animations.Add(new FillPlayerHandAnimation(playerGuid, stackBefore));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			if(stackAfter != null)
				preventConflict(stackBefore, stackAfter);
			else
				preventConflict(stackBefore);

			if(piece is ITerrainPrototype) {
				if(clone == null) {
					// nothing to undo (ordering in the hand is not tansactional)
				} else {
					IStack[] stackAsArray = new IStack[] { stackBefore };
					List<IAnimation> animations = new List<IAnimation>(4);
					if(stackAfter == null)
						animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackBefore));
					else
						animations.Add(new SplitStackAnimation(stackAfter, new IPiece[] { clone }, stackBefore));
					animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet));
					if(clone.RotationAngle != 0) {
						int totalDetentsBefore = (int) (clone.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
						int totalDetentsAfter = 0;
						int rotationIncrements = totalDetentsAfter - totalDetentsBefore;
						animations.Add(new InstantRotatePiecesAnimation(new IPiece[] { clone }, rotationIncrements));
					}
					if(clone.Side != sideBefore)
						animations.Add(new InstantFlipPiecesAnimation(new IPiece[] { clone }));
					animations.Add(new ReturnStackFromHandAnimation(stackBefore));
					animations.Add(new RemoveTerrainAnimation(stackBefore));
					model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
				}
			} else {
				List<IAnimation> animations = new List<IAnimation>(4);
				if(stackAfter == null) {
					animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackBefore));
				} else {
					animations.Add(new SplitStackAnimation(stackAfter, new IPiece[] { piece }, stackBefore));
				}
				animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet));
				if(piece.Side != sideBefore)
					animations.Add(new InstantFlipPiecesAnimation(new IPiece[] { piece }));
				animations.Add(new ReturnStackFromHandAnimation(stackBefore));
				animations.Add(new AttachStacksAnimation(new IStack[] { stackBefore }));
				model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
			}
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			if(stackAfter != null)
				preventConflict(stackBefore, stackAfter);
			else
				preventConflict(stackBefore);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));

			if(piece is ITerrainPrototype) {
				if(clone == null) {
					// nothing to redo (ordering in the hand is not tansactional)
					return;
				}
			} else {
				animations.Add(new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { sideBefore }));
			}
			animations.Add(new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet));
			animations.Add(new MoveStackToHandAnimation(stackBefore));
			if(stackAfter == null)
				animations.Add(new FillPlayerHandAnimation(playerGuid, stackBefore));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IStack stackBefore;
		private IStack stackAfter;
		private IPiece piece;
		private ITerrainClone clone = null;
		private int insertionIndex;
		private Side sideBefore;
	}
}
