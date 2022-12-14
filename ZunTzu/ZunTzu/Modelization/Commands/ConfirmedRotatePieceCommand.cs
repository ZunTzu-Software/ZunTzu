// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>ConfirmedRotatePieceCommand command.</summary>
	public sealed class ConfirmedRotatePieceCommand : RotatePieceCommand {

		public ConfirmedRotatePieceCommand(Guid executorPlayerGuid, IModel model, IPiece piece, int rotationIncrements)
			: base(executorPlayerGuid, model, piece, rotationIncrements) {}

		/// <summary>Execute this command.</summary>
		public override void Do() {}
	}
}
