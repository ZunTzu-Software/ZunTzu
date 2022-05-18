// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>CloneTerrainFromHandCommand command.</summary>
	public sealed class CloneTerrainFromHandCommand : Command {

		public CloneTerrainFromHandCommand(IModel model, Guid playerGuid, IPiece piece, PointF positionAfter)
			: base(model)
		{
			Debug.Assert(piece.Stack.Board == null && playerGuid != Guid.Empty && piece is ITerrainClone);
			this.piece = (ITerrainClone) piece;
			this.playerGuid = playerGuid;
			this.positionAfter = positionAfter;
			boardAfter = model.CurrentGameBox.CurrentGame.VisibleBoard;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			clone = new TerrainClone((TerrainClone) piece);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(clone.Stack, boardAfter),
				new MoveStackInstantlyAnimation(clone.Stack, positionAfter));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(clone.Stack);

			if(model.CurrentSelection != null && model.CurrentSelection.Stack == clone.Stack)
				model.CurrentSelection = null;

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(clone.Stack, boardAfter),
				new MoveStackToHandAnimation(clone.Stack),
				new RemoveTerrainAnimation(clone.Stack));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(clone.Stack);

			model.AnimationManager.LaunchAnimationSequence(
				new MoveToFrontOfBoardAnimation(clone.Stack, boardAfter),
				new MoveStackFromHandAnimation(clone.Stack, positionAfter));
		}

		private Guid playerGuid;
		private ITerrainClone piece;
		private ITerrainClone clone = null;
		private IBoard boardAfter;
		private PointF positionAfter;
	}
}
