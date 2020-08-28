// Copyright (c) 2020 ZunTzu Software and contributors

using System;
//using System.Collections;
using System.Diagnostics;

namespace ZunTzu.Modelization {

	/// <summary>
	/// Summary description for Selection.
	/// </summary>
	internal sealed class Selection : ISelection {
		/// <summary>Stack that contains this selection.</summary>
		/// <remarks>It is not possible to select pieces from several stacks.</remarks>
		IStack ISelection.Stack { get { return Stack; } }
		public IStack Stack;

		/// <summary>List of pieces in this selection.</summary>
		/// <remarks>The pieces are sorted back to front.</remarks>
		IPiece[] ISelection.Pieces { get { return Pieces; } }
		public IPiece[] Pieces;

		public Selection(IStack stack) {
			Stack = stack;
			Pieces = Stack.Pieces;
		}

		public Selection(IPiece piece) {
			Stack = piece.Stack;
			Pieces = new IPiece[1] { piece };
		}

		private Selection(IStack stack, IPiece[] pieces) {
			Stack = stack;
			Pieces = pieces;
		}

		/// <summary>Creates a new selection by adding another piece of the same stack to this selection.</summary>
		/// <param name="piece">Piece to select.</param>
		/// <returns>Result selection.</returns>
		public ISelection AddPiece(IPiece piece) {
			Debug.Assert(piece.Stack == Stack && !Contains(piece));

			IPiece[] updatedList = new IPiece[Pieces.Length + 1];
			int updatedListIndex = 0;
			foreach(IPiece c in Stack.Pieces) {
				if(c == piece || Contains(c))
					updatedList[updatedListIndex++] = c;
			}
			return new Selection(Stack, updatedList);
		}
		/// <summary>Remove a piece from this selection.</summary>
		/// <param name="piece">Piece to deselect.</param>
		/// <returns>Result selection</returns>
		public ISelection RemovePiece(IPiece piece){
			Debug.Assert(Contains(piece));

			IPiece[] updatedList = new IPiece[Pieces.Length - 1];
			int updatedListIndex = 0;
			foreach(IPiece c in Pieces) {
				if(c != piece)
					updatedList[updatedListIndex++] = c;
			}
			return new Selection(Stack, updatedList);
		}
		/// <summary>Creates a new selection by removing all pieces from this selection.</summary>
		/// <returns>Result selection.</returns>
		public ISelection RemoveAllPieces() {
			return new Selection(Stack, new IPiece[0]);
		}
		/// <summary>Creates a new selection by rearranging the pieces from back to top.</summary>
		/// <returns>Result selection.</returns>
		public ISelection RearrangePieces() {
			IPiece[] updatedList = new IPiece[Pieces.Length];
			int updatedListIndex = 0;
			foreach(IPiece c in Stack.Pieces) {
				if(Contains(c))
					updatedList[updatedListIndex++] = c;
			}
			return new Selection(Stack, updatedList);
		}

		/// <summary>True if no piece is part of this selection.</summary>
		public bool Empty { get { return Pieces.Length == 0; } }

		/// <summary>Test if a piece is part of this selection.</summary>
		/// <param name="piece">Piece to test.</param>
		/// <returns>True if piece is part of this selection.</returns>
		public bool Contains(IPiece piece) {
			if(Stack == piece.Stack) {
				foreach(IPiece selectedPiece in Pieces)
					if(selectedPiece == piece)
						return true;
			}
			return false;
		}
	}
}
