// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>ConfirmedRotateTopOfStackCommand command.</summary>
	public sealed class ConfirmedRotateTopOfStackCommand : RotateTopOfStackCommand {

		public ConfirmedRotateTopOfStackCommand(IModel model, IPiece stackBottom, int rotationIncrements)
			: base(model, stackBottom, rotationIncrements) {}

		/// <summary>Execute this command.</summary>
		public override void Do() {}
	}
}
