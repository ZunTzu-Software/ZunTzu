// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>RotatePieceCommand command.</summary>
	public class RotatePieceCommand : AggregableCommand {

		public RotatePieceCommand(IModel model, IPiece piece, int rotationIncrements)
			: base(model)
		{
			this.piece = piece;
			this.rotationIncrements = rotationIncrements;
		}

		/// <summary>Execute this command.</summary>
		public override void Do() {
			preventConflict(piece.Stack);
			model.AnimationManager.LaunchAnimationSequence(new InstantRotatePiecesAnimation(new IPiece[1] { piece }, rotationIncrements));
		}

		/// <summary>Cancel the result of this command.</summary>
		public override void Undo() {
			preventConflict(piece.Stack);
			model.AnimationManager.LaunchAnimationSequence(new RotatePiecesAnimation(new IPiece[1] { piece }, -rotationIncrements));
		}

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public override void Redo() {
			preventConflict(piece.Stack);
			model.AnimationManager.LaunchAnimationSequence(new RotatePiecesAnimation(new IPiece[1] { piece }, rotationIncrements));
		}

		/// <summary>Returns true if this command can be aggregated with another command.</summary>
		/// <param name="otherCommand">The other command to aggregate.</param>
		/// <returns>True if both commands can be aggregated.</returns>
		public override bool CanAggregateWith(AggregableCommand otherCommand) {
			RotatePieceCommand otherRotateCommand = otherCommand as RotatePieceCommand;
			return (otherRotateCommand != null && otherRotateCommand.piece == piece);
		}

		/// <summary>Aggregate two commands and store the result in this command.</summary>
		/// <param name="otherCommand">Another command to aggregate.</param>
		public override void AggregateWith(AggregableCommand otherCommand) {
			Debug.Assert(CanAggregateWith(otherCommand));
			rotationIncrements += ((RotatePieceCommand) otherCommand).rotationIncrements;
		}

		private IPiece piece;
		private int rotationIncrements;
	}
}
