// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>DragDropAttachedStackIntoHandCommand command.</summary>
	public sealed class DragDropAttachedStackIntoHandCommand : Command {

		public DragDropAttachedStackIntoHandCommand(IModel model, Guid playerGuid, IStack stack, int insertionIndex)
			: base(model)
		{
			Debug.Assert(stack.AttachedToCounterSection && playerGuid != Guid.Empty);
			this.playerGuid = playerGuid;
			piece = stack.Pieces[0];
			this.insertionIndex = insertionIndex;
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			Debug.Assert(playerHand != null && insertionIndex <= playerHand.Count);
			stackBefore = (piece is ITerrainPrototype ? null : stack);
			stackAfter = (playerHand.Count == 0 ?
				stackBefore :	// the hand is currently empty
				playerHand.Stack);
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			if(piece is ITerrainPrototype) {
				if(stackAfter != null) {
					preventConflict(stackAfter);
					// does the hand already contain an identical clone?
					for(int i = 0; i < stackAfter.Pieces.Length; ++i) {
						ITerrainClone handPiece = stackAfter.Pieces[i] as ITerrainClone;
						if(handPiece != null && handPiece.Prototype == piece) {
							// yes -> simply move the piece to the new insertion index
							IPlayerHand playerHand = model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
							model.AnimationManager.LaunchAnimationSequence(
								new RearrangePlayerHandAnimation(playerHand, i, insertionIndex));
							return;
						}
					}
				}
				// add the piece
				clone = new TerrainClone((TerrainPrototype) piece);
				stackBefore = clone.Stack;
				sideBefore = piece.Side;
				if(playerGuid == model.ThisPlayer.Guid) {
					model.AnimationManager.LaunchAnimationSequence(
						new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
						(stackAfter == null ?
							(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
							(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
				} else {
					model.AnimationManager.LaunchAnimationSequence(
						new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
						new MoveStackToHandAnimation(stackBefore),
						(stackAfter == null ?
							(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
							(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
				}
			} else {
				preventConflict(stackBefore, stackAfter);

				sideBefore = piece.Side;

				if(playerGuid == model.ThisPlayer.Guid) {
					model.AnimationManager.LaunchAnimationSequence(
						new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { sideBefore }),
						new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
						(stackBefore == stackAfter ?
							(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
							(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
				} else {
					model.AnimationManager.LaunchAnimationSequence(
						new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { sideBefore }),
						new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
						new MoveStackToHandAnimation(stackBefore),
						(stackBefore == stackAfter ?
							(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
							(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
				}
			}
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			if(piece is ITerrainPrototype) {
				if(clone == null) {
					// nothing to undo (ordering in the hand is not tansactional)
				} else {
					IStack[] stackAsArray = new IStack[] { stackBefore };
					List<IAnimation> animations = new List<IAnimation>(4);
					if(stackAfter == null) {
						preventConflict(stackBefore);
						animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackAfter));
					} else {
						preventConflict(stackBefore, stackAfter);
						animations.Add(new SplitStackAnimation(stackAfter, new IPiece[] { clone }, stackBefore));
					}
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
				preventConflict(stackBefore, stackAfter);

				List<IAnimation> animations = new List<IAnimation>(4);
				if(stackBefore == stackAfter) {
					animations.Add(new EmptyPlayerHandAnimation(playerGuid, stackAfter));
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
			if(piece is ITerrainPrototype) {
				if(clone == null) {
					// nothing to redo (ordering in the hand is not tansactional)
				} else {
					if(stackAfter == null)
						preventConflict(stackBefore);
					else
						preventConflict(stackBefore, stackAfter);
					model.AnimationManager.LaunchAnimationSequence(
						new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
						new MoveStackToHandAnimation(stackBefore),
						(stackAfter == null ?
							(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
							(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
				}
			} else {
				preventConflict(stackBefore, stackAfter);

				model.AnimationManager.LaunchAnimationSequence(
					new DetachStacksAnimation(new IStack[] { stackBefore }, new Side[] { sideBefore }),
					new MoveToFrontOfBoardAnimation(stackBefore, piece.CounterSection.CounterSheet),
					new MoveStackToHandAnimation(stackBefore),
					(stackBefore == stackAfter ?
						(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
						(IAnimation) new MergeStacksAnimation(stackAfter, stackBefore, insertionIndex)));
			}
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
