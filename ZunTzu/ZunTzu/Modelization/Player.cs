// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;

namespace ZunTzu.Modelization {

	/// <summary>Player that has joined an online game.</summary>
	public sealed class Player : IPlayer {

		/// <summary>Constructor.</summary>
		/// <param name="id">Network id of this player</param>
		/// <param name="firstName">First name of this player</param>
		/// <param name="lastName">Last name of this player</param>
		/// <param name="color">Color of this player</param>
		/// <param name="cursorPosition">Position of the cursor of this player in screen coordinates</param>
		/// <param name="isCursorVisible">True if the cursor is not above a private frame</param>
		public Player(int id, string firstName, string lastName, Guid guid, uint color, Point cursorScreenPosition, bool isCursorVisible) {
			this.id = id;
			this.firstName = firstName;
			this.lastName = lastName;
			this.guid = guid;
			this.color = color;
			TemporaryCursorLocation location = new TemporaryCursorLocation();
			location.ScreenPosition = cursorScreenPosition;
			this.cursorLocation = location;
			this.isCursorVisible = isCursorVisible;
			voicePlaybackInProgress = false;
			videoAssetIndex = -1;	// video capture is off
		}

		private class TemporaryCursorLocation : ICursorLocation {
			/// <summary>Position of the mouse cursor in screen coordinates.</summary>
			public Point ScreenPosition { get { return screenPosition; } set { screenPosition = value; } }
			/// <summary>Position of the mouse cursor in model coordinates.</summary>
			public PointF ModelPosition { get { return modelPosition; } set { modelPosition = value; } }
			private Point screenPosition;
			private PointF modelPosition;
		}

		/// <summary>Network id of this player.</summary>
		public int Id { get { return id; } set { id = value; } }
		private int id;

		/// <summary>First name of this player.</summary>
		public string FirstName { get { return firstName; } set { firstName = value; } }
		private string firstName;

		/// <summary>Last name of this player.</summary>
		public string LastName { get { return lastName; } set { lastName = value; } }
		private string lastName;

		/// <summary>Color associated to this player.</summary>
		public uint Color { get { return color; } set { color = value; } }
		private uint color;

		/// <summary>Position of the mouse cursor of this player</summary>
		/// <remarks>This data is updated only once per frame.</remarks>
		public ICursorLocation CursorLocation { get { return cursorLocation; } set { cursorLocation = value; } }
		private ICursorLocation cursorLocation = null;

		/// <summary>False if cursor is overa private pane.</summary>
		public bool IsCursorVisible { get { return isCursorVisible; } set { isCursorVisible = value; } }
		private bool isCursorVisible;

		/// <summary>The bottom of the stack being dragged around.</summary>
		/// <remarks>Null if no drag and drop is in progress.</remarks>
		public IPiece StackBeingDragged { get { return stackBeingDragged; } set { stackBeingDragged = (Piece)value; } }
		private Piece stackBeingDragged = null;

		/// <summary>Piece being dragged around.</summary>
		/// <remarks>Null if no drag and drop is in progress.</remarks>
		public IPiece PieceBeingDragged { get { return pieceBeingDragged; } set { pieceBeingDragged = (Piece)value; } }
		private Piece pieceBeingDragged = null;

		/// <summary>Point of the stack being dragged attached to the cursor hotspot.</summary>
		/// <remarks>Ignored if no drag and drop is in progress.</remarks>
		public PointF DragAndDropAnchor { get { return dragAndDropAnchor; } set { dragAndDropAnchor = value; } }
		private PointF dragAndDropAnchor;

		/// <summary>Indicates that this player is transmitting a voice communication.</summary>
		public bool VoicePlaybackInProgress { get { return voicePlaybackInProgress; } set { voicePlaybackInProgress = value; } }
		private bool voicePlaybackInProgress;

		/// <summary>Persistent id of this player.</summary>
		public Guid Guid { get { return guid; } set { guid = value; } }
		private Guid guid;

		/// <summary>Index between 0 and 15 of the video resource for this player.</summary>
		public int VideoAssetIndex { get { return videoAssetIndex; } set { videoAssetIndex = value; } }
		private int videoAssetIndex;

		/// <summary>Indicates that this player has enabled auto-inspection of cards decks.</summary>
		public bool DeckAutoInspect { get { return deckAutoInspect; } set { deckAutoInspect = value; } }
		private bool deckAutoInspect = false;
	}
}
