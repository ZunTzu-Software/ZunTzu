// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>GrabPieceCommand command.</summary>
	public sealed class GrabPieceCommand : Command {

		public GrabPieceCommand(IModel model, Guid playerGuid, IPiece piece)
			: base(model)
		{
			Debug.Assert(piece.Stack.Pieces.Length > 1 && playerGuid != Guid.Empty);
			this.playerGuid = playerGuid;
			this.piece = piece;
			stackBefore = piece.Stack;
			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			transitionStack = new Stack();
			stackAfter = (playerHand == null || playerHand.Count == 0 ?
				transitionStack :	// the hand is currently empty
				playerHand.Stack);
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);
			insertionIndex = (playerHand != null ? playerHand.Count : 0);

			indexInStackBefore = piece.IndexInStackFromBottomToTop;
			rotationAngleBefore = piece.RotationAngle;
			sideBefore = piece.Side;

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));
			animations.Add(new SplitStackAnimation(stackBefore, new IPiece[] { piece }, transitionStack));
			animations.Add(new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board));
			animations.Add(new MoveStackInstantlyAnimation(transitionStack, stackBefore.Position));
			animations.Add(new MoveStackToHandAnimation(transitionStack));
			if(transitionStack == stackAfter)
				animations.Add(new FillPlayerHandAnimation(playerGuid, transitionStack));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			List<IAnimation> animations = new List<IAnimation>(6);
			if(transitionStack == stackAfter) {
				animations.Add(new EmptyPlayerHandAnimation(playerGuid, transitionStack));
			} else {
				animations.Add(new SplitStackAnimation(stackAfter, new IPiece[] { piece }, transitionStack));
			}
			animations.Add(new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board));
			if(piece.RotationAngle != rotationAngleBefore) {
				int totalDetentsBefore = (int) (piece.RotationAngle * (12.0f / (float) Math.PI) + 0.5f) * 120;
				int totalDetentsAfter = (int) (rotationAngleBefore * (12.0f / (float) Math.PI) + 0.5f) * 120;
				int rotationIncrements = totalDetentsAfter - totalDetentsBefore;
				animations.Add(new InstantRotatePiecesAnimation(playerGuid, new IPiece[] { piece }, rotationIncrements));
			}
			if(piece.Side != sideBefore)
				animations.Add(new InstantFlipPiecesAnimation(playerGuid, new IPiece[] { piece }));
			animations.Add(new MoveStackFromHandAnimation(transitionStack, stackBefore.Position));
			animations.Add(new MergeStacksAnimation(stackBefore, transitionStack, indexInStackBefore));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter, transitionStack);

			PlayerHand playerHand = (PlayerHand) model.CurrentGameBox.CurrentGame.GetPlayerHand(playerGuid);

			List<IAnimation> animations = new List<IAnimation>(4);
			if(playerHand == null)
				animations.Add(new AddPlayerHandAnimation(playerGuid));
			animations.Add(new SplitStackAnimation(stackBefore, new IPiece[] { piece }, transitionStack));
			animations.Add(new MoveToFrontOfBoardAnimation(transitionStack, stackBefore.Board));
			animations.Add(new MoveStackInstantlyAnimation(transitionStack, stackBefore.Position));
			animations.Add(new MoveStackToHandAnimation(transitionStack));
			if(transitionStack == stackAfter)
				animations.Add(new FillPlayerHandAnimation(playerGuid, transitionStack));
			else
				animations.Add(new MergeStacksAnimation(stackAfter, transitionStack, insertionIndex));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IStack stackBefore;
		private IStack stackAfter;
		private IStack transitionStack;
		private int insertionIndex;
		private IPiece piece;
		private float rotationAngleBefore;
		private Side sideBefore;
		private int indexInStackBefore;
	}
}
