// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>RotateTopOfStackCommand command.</summary>
	public class RotateTopOfStackCommand : AggregableCommand {

		public RotateTopOfStackCommand(Guid executorPlayerGuid, IModel model, IPiece stackBottom, int rotationIncrements)
			: base(model)
		{
			this.executorPlayerGuid = executorPlayerGuid;
			Debug.Assert(!stackBottom.Stack.AttachedToCounterSection && stackBottom.Stack.Pieces.Length > 1);
			int bottomIndex = stackBottom.IndexInStackFromBottomToTop;
			IPiece[] stackPieces = stackBottom.Stack.Pieces;
			pieces = new IPiece[stackPieces.Length - bottomIndex];
			for(int i = 0; i < pieces.Length; ++i)
				pieces[i] = stackPieces[i + bottomIndex];
			this.rotationIncrements = rotationIncrements;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(pieces[0].Stack);
			model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(executorPlayerGuid, pieces, rotationIncrements));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(pieces[0].Stack);
			model.AnimationManager.LaunchAnimationSequence(new RotatePiecesAnimation(executorPlayerGuid, pieces, -rotationIncrements));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(pieces[0].Stack);
			model.AnimationManager.LaunchAnimationSequence(new RotatePiecesAnimation(executorPlayerGuid, pieces, rotationIncrements));
		}

		/// <summary>Returns true if this command can be aggregated with another command.</summary>
		/// <param name="otherCommand">The other command to aggregate.</param>
		/// <returns>True if both commands can be aggregated.</returns>
		public override bool CanAggregateWith(AggregableCommand otherCommand) {
			RotateTopOfStackCommand otherRotateCommand = otherCommand as RotateTopOfStackCommand;
			if(otherRotateCommand == null)
				return false;
			if(pieces.Length != otherRotateCommand.pieces.Length)
				return false;
			else
				for(int i = 0; i < pieces.Length; ++i)
					if(pieces[i] != otherRotateCommand.pieces[i])
						return false;
			return true;
		}

		/// <summary>Aggregate two commands and store the result in this command.</summary>
		/// <param name="otherCommand">Another command to aggregate.</param>
		public override void AggregateWith(AggregableCommand otherCommand) {
			Debug.Assert(CanAggregateWith(otherCommand));
			rotationIncrements += ((RotateTopOfStackCommand) otherCommand).rotationIncrements;
		}

		private IPiece[] pieces;
		private int rotationIncrements;
		private Guid executorPlayerGuid;
	}
}
