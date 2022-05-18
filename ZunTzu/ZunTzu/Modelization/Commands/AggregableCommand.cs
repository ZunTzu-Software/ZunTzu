// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Diagnostics;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Modelization.Commands {

	/// <summary>AggregableCommand command.</summary>
	public abstract class AggregableCommand : Command {

		public AggregableCommand(IModel model) : base(model) {}

		/// <summary>Returns true if this command can be aggregated with another command.</summary>
		/// <param name="otherCommand">The other command to aggregate.</param>
		/// <returns>True if both commands can be aggregated.</returns>
		public abstract bool CanAggregateWith(AggregableCommand otherCommand);

		/// <summary>Aggregate two commands and store the result in this command.</summary>
		/// <param name="otherCommand">Another command to aggregate.</param>
		public abstract void AggregateWith(AggregableCommand otherCommand);
	}
}
