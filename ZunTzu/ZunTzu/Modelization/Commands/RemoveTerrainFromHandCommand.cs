// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>RemoveTerrainFromHandCommand command.</summary>
	public sealed class RemoveTerrainFromHandCommand : Command {

		public RemoveTerrainFromHandCommand(IModel model, Guid playerGuid, IPiece piece)
			: base(model)
		{
			Debug.Assert(piece.Stack.Board == null && playerGuid != Guid.Empty && piece is ITerrainClone);
			this.playerGuid = playerGuid;
			this.piece = piece;
			stackBefore = piece.Stack;
			stackAfter = (stackBefore.Pieces.Length == 1 ?
				stackBefore :	// this is the last piece in the hand
				new Stack());
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(stackBefore, stackAfter);

			// memorize current state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				(stackBefore == stackAfter ?
					(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter)),
				new MoveToFrontOfBoardAnimation(stackAfter, piece.CounterSection.CounterSheet),
				new ReturnStackFromHandAnimation(stackAfter),
				new RemoveTerrainAnimation(stackAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stackAfterAsArray = new IStack[] { stackAfter };
			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(stackAfterAsArray, piece.CounterSection.CounterSheet),
				new MoveStackToHandAnimation(stackAfter),
				(stackBefore == stackAfter ?
					(IAnimation) new FillPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new MergeStacksAnimation(stackBefore, stackAfter, indexInStackBefore)));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(stackBefore, stackAfter);

			// update state in hand
			indexInStackBefore = piece.IndexInStackFromBottomToTop;

			model.AnimationManager.LaunchAnimationSequence(
				(stackBefore == stackAfter ?
					(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter)),
				new MoveToFrontOfBoardAnimation(stackAfter, piece.CounterSection.CounterSheet),
				new ReturnStackFromHandAnimation(stackAfter),
				new RemoveTerrainAnimation(stackAfter));
		}

		private Guid playerGuid;
		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private int indexInStackBefore;
	}
}
