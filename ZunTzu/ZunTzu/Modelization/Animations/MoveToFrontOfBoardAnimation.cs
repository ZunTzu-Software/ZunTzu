// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class MoveToFrontOfBoardAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public MoveToFrontOfBoardAnimation(IStack stack, IBoard board) {
			this.stacks = new IStack[] { stack };
			this.board = (Board) board;
		}

		/// <summary>Constructor</summary>
		public MoveToFrontOfBoardAnimation(IStack[] stacks, IBoard board) {
			this.stacks = stacks;
			this.board = (Board) board;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			for(int i = 0; i < stacks.Length; ++i)
				board.MoveStackToFront(stacks[i]);
		}

		/// <summary>Determines if a stack is currently involved in this animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		internal override bool IsBeingAnimated(IStack stack) {
			foreach(IStack animatedStack in stacks)
				if(animatedStack == stack)
					return true;
			return false;
		}

		private IStack[] stacks;
		private Board board;
	}
}
