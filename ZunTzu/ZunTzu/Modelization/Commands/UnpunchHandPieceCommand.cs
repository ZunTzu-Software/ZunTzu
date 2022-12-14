// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>UnpunchHandPieceCommand command.</summary>
	public sealed class UnpunchHandPieceCommand : Command {

		public UnpunchHandPieceCommand(IModel model, Guid playerGuid, IPiece piece)
			: base(model)
		{
			Debug.Assert(piece.Stack.Board == null && playerGuid != Guid.Empty);
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
			rotationAngle = piece.RotationAngle;
			side = piece.Side;

			model.AnimationManager.LaunchAnimationSequence(
				(stackBefore == stackAfter ?
					(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
					(IAnimation) new SplitStackAnimation(stackBefore, new IPiece[] { piece }, stackAfter)),
				new MoveToFrontOfBoardAnimation(stackAfter, piece.CounterSection.CounterSheet),
				new MoveStackInstantlyAnimation(stackAfter, new PointF(piece.PositionWhenAttached.X, piece.CounterSection.CounterSheet.VisibleArea.Bottom + piece.BoundingBox.Height * 0.5f)),
				new ReturnStackFromHandAnimation(stackAfter),
				new AttachStacksAnimation(new IStack[] { stackAfter }));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(stackBefore, stackAfter);

			IStack[] stackAfterAsArray = new IStack[] { stackAfter };
			model.AnimationManager.LaunchAnimationSequence(
				new DetachStacksAnimation(stackAfterAsArray, new Side[] { side }, new float[] { rotationAngle }),
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

			IPiece[] pieceAsArray = new IPiece[] { piece };
			List<IAnimation> animations = new List<IAnimation>(6);
			animations.Add(stackBefore == stackAfter ?
				(IAnimation) new EmptyPlayerHandAnimation(playerGuid, stackBefore) :
				(IAnimation) new SplitStackAnimation(stackBefore, pieceAsArray, stackAfter));
			animations.Add(new MoveToFrontOfBoardAnimation(stackAfter, piece.CounterSection.CounterSheet));
			animations.Add(new MoveStackInstantlyAnimation(stackAfter, new PointF(piece.PositionWhenAttached.X, piece.CounterSection.CounterSheet.VisibleArea.Bottom + piece.BoundingBox.Height * 0.5f)));
			if(side != piece.Side)
				animations.Add(new InstantFlipPiecesAnimation(playerGuid, pieceAsArray));
			animations.Add(new ReturnStackFromHandAnimation(stackAfter));
			animations.Add(new AttachStacksAnimation(new IStack[] { stackAfter }));
			model.AnimationManager.LaunchAnimationSequence(animations.ToArray());
		}

		private Guid playerGuid;
		private IPiece piece;
		private IStack stackBefore;
		private IStack stackAfter;
		private int indexInStackBefore;
		private float rotationAngle;
		private Side side;
	}
}
