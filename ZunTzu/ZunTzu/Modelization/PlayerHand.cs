// Copyright (c) 2020 ZunTzu Software and contributors

using System;

namespace ZunTzu.Modelization {

	/// <summary>Player hand.</summary>
	internal class PlayerHand : IPlayerHand {

		/// <summary>Number of pieces in this hand.</summary>
		public int Count { get { return (stack == null ? 0 : stack.Pieces.Length); } }

		/// <summary>List of pieces in this hand.</summary>
		/// <remarks>The pieces are sorted left to right.</remarks>
		public IPiece[] Pieces { get { return stack.Pieces; } }

		/// <summary></summary>
		public Stack Stack { get { return stack; } set { stack = value; } }

		private Stack stack = null;
	}
}
