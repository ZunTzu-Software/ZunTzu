// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using ZunTzu.AudioVideo;
using ZunTzu.Graphics;
using ZunTzu.Networking;

//
//  +------------+     +---------------+     +---------+
//  | Networking |<----+ Modelization  |<----+         |
//  +------------+     +---------------+     |         |     
//                             ^             |         |
//                             |             | Control |
//                             |             |         |
//  +------------+     +-------+-------+     |         |     
//  |  Graphics  |<----+ Visualization |<----+         |
//  +------------+     +---------------+     +---------+
// 

namespace ZunTzu.Modelization {

	/// <summary>Current state of the program.</summary>
	public interface IModel {
		/// <summary>The repository for all game boxes.</summary>
		IGameLibrary GameLibrary { get; }
		/// <summary>Checks the validity of a game box file.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		/// <returns>A list of errors and warnings.</returns>
		IEnumerable<string> VerifyGameBox(string fileName);
		/// <summary>Retrieves game box name and hash value from a game data stream.</summary>
		/// <param name="inputStream">The stream from which to read the game data.</param>
		/// <param name="gameBoxName">The variable to hold the game box name.</param>
		/// <param name="gameBoxHash">The variable to hold the game box hash value (null if it wasn't found).</param>
		void RetrieveGameBoxInfoFromGameData(Stream inputStream, out string gameBoxName, out byte[] gameBoxHash);
		/// <summary>Opens a game box file.</summary>
		/// <param name="reference">Library reference of the game box.</param>
		void OpenGameBox(IGameBoxReference reference);
		/// <summary>Opens a game box file.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		void OpenGameBox(string fileName);
		/// <summary>Properties of the last opened game box.</summary>
		IGameBox CurrentGameBox { get; }

		/// <summary>Pieces currently selected.</summary>
		ISelection CurrentSelection { get; set; }
		/// <summary>Position of each piece of the selection in model coordinates.</summary>
		/// <remarks>X is between -1 (out of screen) and 0 (final position). Y is in model coordinates.</remarks>
		PointF[] StackInspectorPositions { get; }
		/// <summary>True to indicate that the user is in measuring mode.</summary>
		bool IsMeasuring { get; set; }
		/// <summary>Position of the extremity of the ruler farther from the mouse cursor in model coordinates.</summary>
		PointF RulerStartPosition { get; set; }
		/// <summary>Position of the extremity of the ruler closer to the mouse cursor in model coordinates.</summary>
		PointF RulerEndPosition { get; set; }

		/// <summary>Remove any transient state before opening a new game.</summary>
		void ClearTransientState();

		/// <summary>The component in charge of sound playback.</summary>
		IAudioManager AudioManager { get; }
		/// <summary>The component in charge of network communication.</summary>
		IClient NetworkClient { get; }
		/// <summary>Handler for user actions.</summary>
		ICommandManager CommandManager { get; }
		/// <summary>The component in charge of running animations.</summary>
		IAnimationManager AnimationManager { get; }
		/// <summary>A random number generator.</summary>
		IRandomNumberGenerator RandomNumberGenerator { get; }

		/// <summary>Set to true if this player is hosting the game.</summary>
		bool IsHosting { get; set; }
		/// <summary>This player.</summary>
		IPlayer ThisPlayer { get; }

		/// <summary>Number of players.</summary>
		int PlayerCount { get; }
		/// <summary>Collection of players.</summary>
		IPlayer[] Players { get; }
		/// <summary>Returns the player with the given id.</summary>
		/// <param name="playerId">A player id.</param>
		/// <returns>A player or null if not found.</returns>
		IPlayer GetPlayer(int playerId);
		/// <summary>Returns the player with the given Guid.</summary>
		/// <param name="playerGuid">A Guid.</param>
		/// <returns>A player or null if not found.</returns>
		IPlayer GetPlayerByGuid(Guid playerGuid);
		/// <summary>Add a player to the player list.</summary>
		/// <param name="id">The player network id.</param>
		/// <param name="firstName">The player's first name.</param>
		/// <param name="lastName">The player's last name.</param>
		/// <param name="guid">The player's persistent id.</param>
		/// <param name="color">The player color.</param>
		/// <param name="cursorPosition">The current cursor position of this player in screen coordinates.</param>
		/// <param name="isCursorVisible">False if the cursor is over a private frame.</param>
		/// <returns>The newly created player.</returns>
		IPlayer AddPlayer(int id, string firstName, string lastName, Guid guid, uint color, Point cursorPosition, bool isCursorVisible);
		/// <summary>Remove a player from the player list.</summary>
		/// <param name="id">The network id of the player to remove.</param>
		void RemovePlayer(int id);
		/// <summary>Remove all players from the player list.</summary>
		void RemoveAllPlayers();

		/// <summary>Sequence number used to resolve conflicting commands received from different players at the same moment.</summary>
		int StateChangeSequenceNumber { get;  set; }

		/// <summary>Computes a rotation in 3D space, from a rotation axis and an angle.</summary>
		/// <param name="axisX">X component of the rotation axis vector.</param>
		/// <param name="axisY">Y component of the rotation axis vector.</param>
		/// <param name="axisZ">Z component of the rotation axis vector.</param>
		/// <param name="angle">Rotation angle.</param>
		/// <returns>A rotation.</returns>
		IRotation ComputeRotationFromAxis(float axisX, float axisY, float axisZ, float angle);
		/// <summary>Computes a rotation in 3D space, by composing three rotations along the Z, X and then Y axis.</summary>
		/// <param name="yaw">Rotation angle along the Y axis.</param>
		/// <param name="pitch">Rotation angle along the X axis.</param>
		/// <param name="roll">Rotation angle along the Z axis.</param>
		/// <returns>A rotation.</returns>
		IRotation ComputeRotationYawPitchRoll(float yaw, float pitch, float roll);
	}

	/// <summary>Information about a player.</summary>
	public interface IPlayer {
		/// <summary>Network id of this player.</summary>
		int Id { get; }
		/// <summary>Fist name of this player.</summary>
		string FirstName { get; set; }
		/// <summary>Last name of this player.</summary>
		string LastName { get; set; }
		/// <summary>Color associated to this player.</summary>
		uint Color { get; set; }
		/// <summary>Position of the mouse cursor of this player</summary>
		/// <remarks>This data is updated only once per frame.</remarks>
		ICursorLocation CursorLocation { get; set; }
		/// <summary>False if cursor is over a private pane.</summary>
		bool IsCursorVisible { get; set; }
		/// <summary>The bottom of the stack being dragged around.</summary>
		/// <remarks>Null if no drag and drop is in progress.</remarks>
		IPiece StackBeingDragged { get; set; }
		/// <summary>Piece being dragged around.</summary>
		/// <remarks>Null if no drag and drop is in progress.</remarks>
		IPiece PieceBeingDragged { get; set; }
		/// <summary>Point of the stack or piece being dragged attached to the cursor hotspot.</summary>
		/// <remarks>Ignored if no drag and drop is in progress.</remarks>
		PointF DragAndDropAnchor { get; set; }
		/// <summary>Indicates that this player is transmitting a voice communication.</summary>
		bool VoicePlaybackInProgress { get; set; }
		/// <summary>Persistent id of this player.</summary>
		Guid Guid { get; set; }
		/// <summary>Index between 0 and 15 of the video resource for this player.</summary>
		int VideoAssetIndex { get; set; }
		/// <summary>Indicates that this player has enabled auto-inspection of cards decks.</summary>
		bool DeckAutoInspect { get; set; }
	}

	/// <summary>Base interface for information regarding the current mouse cursor location.</summary>
	public interface ICursorLocation {
		/// <summary>Position of the mouse cursor in screen coordinates.</summary>
		Point ScreenPosition { get; set; }
		/// <summary>Position of the mouse cursor in model coordinates.</summary>
		PointF ModelPosition { get; set; }
	}

	/// <summary>Game library reference to a game box.</summary>
	public interface IGameBoxReference {
		/// <summary>Name of this game box.</summary>
		string Name { get; }
		/// <summary>Description of this game box.</summary>
		string Description { get; }
		/// <summary>Copyright for this game box maps, pieces and rules.</summary>
		string Copyright { get; }
		/// <summary>Icon of this box.</summary>
		byte[] Icon { get; set; }
		/// <summary>Name of the game box file.</summary>
		string FileName { get; }
		/// <summary>SHA1 hash value for this game box file.</summary>
		byte[] Hash { get; }
	}

	/// <summary>The repository for all game boxes.</summary>
	public interface IGameLibrary {
		/// <summary>All game boxes in the library, except the default one.</summary>
		IEnumerable<IGameBoxReference> GameBoxes { get; }
		/// <summary>The game box loaded at startup.</summary>
		IGameBoxReference DefaultGameBox { get; }
		/// <summary>Looks for a game box file in the library.</summary>
		/// <param name="gameBoxName">Name of the game box.</param>
		/// <returns>A game box reference, or null if not found.</returns>
		IGameBoxReference FindGameBox(string gameBoxName);
		/// <summary>Looks for a game box file in the library.</summary>
		/// <param name="hash">Hash value of the game box.</param>
		/// <returns>A game box reference, or null if not found.</returns>
		IGameBoxReference FindGameBox(byte[] hash);
		/// <summary>Adds a game box reference to this library.</summary>
		/// <param name="gameBoxReference">A game box reference.</param>
		void AddReference(IGameBoxReference gameBoxReference);
		/// <summary>Adds a game box reference to this library.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		void AddReference(string fileName);
		/// <summary>Removes a game box from this library.</summary>
		/// <param name="gameBoxReference">A game box reference.</param>
		void RemoveReference(IGameBoxReference gameBoxReference);
	}

	/// <summary>Reference to a built-in scenario.</summary>
	public interface IScenarioReference {
		/// <summary>Name of this scenario.</summary>
		string Name { get; }
		/// <summary>Description of this scenario.</summary>
		string Description { get; }
		/// <summary>Copyright for this scenario.</summary>
		string Copyright { get; }
		/// <summary>Name of the scenario file in the game box archive.</summary>
		string FileName { get; }
	}

	/// <summary>Game box properties.</summary>
	/// <remarks>This is everything you can find in a game box except the rulebook: boards, dice, pieces.</remarks>
	public interface IGameBox {
		/// <summary>Reference information for this game box.</summary>
		IGameBoxReference Reference { get; }
		/// <summary>Name of the built-in scenario file to load at startup.</summary>
		string StartupScenarioFileName { get; }
		/// <summary>Opens a scenario from the game box buit-in scenario list.</summary>
		/// <param name="fileName">Name of the scenario file.</param>
		void OpenBuiltInScenario(string fileName);
		/// <summary>Opens a scenario from an external scenario file.</summary>
		/// <param name="fileName">Name of the scenario file.</param>
		void OpenScenarioFromScenarioFile(string fileName);
		/// <summary>Opens a previously saved game file.</summary>
		/// <param name="fileName">Name of the game file.</param>
		void OpenGame(string fileName);
		/// <summary>Opens a game received over the network.</summary>
		/// <param name="inputStream">The stream from which to load the data.</param>
		void OpenGame(Stream inputStream);
		/// <summary>Properties of the last opened game.</summary>
		IGame CurrentGame { get; }
		/// <summary>List of all build-in scenarios.</summary>
		IEnumerable<IScenarioReference> BuiltInScenarios { get; }
	}

	/// <summary>Operating mode of the user interface.</summary>
	[ObfuscationAttribute(Exclude = true)]
	public enum Mode { Default = 0, Terrain = 1 };

	/// <summary>Scenario and game data.</summary>
	/// <remarks>
	/// A scenario is one possible way of playing using a game box. It defines which boards and pieces will be used, and also the starting position of the pieces.
	/// Scenarios are usually part of the game box ("built-in"), but they can also come from external sources.
	/// Scenario data is saved along with game data each time a game is saved. Actually, there is no difference between a scenario and a saved game.
	/// </remarks>
	public interface IGame {
		/// <summary>Name of this scenario.</summary>
		string ScenarioName { get; }
		/// <summary>Description of this scenario.</summary>
		string ScenarioDescription { get; }
		/// <summary>Copyright for this scenario.</summary>
		string ScenarioCopyright { get; }
		/// <summary>Name of the file from which this scenario or game was loaded.</summary>
		string FileName { get; set; }
		/// <summary>Saves the current game.</summary>
		/// <param name="outputStream">The stream to write this game data to.</param>
		/// <param name="indented">True to write indentation.</param>
		void Save(Stream outputStream, bool indented);
		/// <summary>All the boards used by this game.</summary>
		IBoard[] Boards { get; }
		/// <summary>The board currently visible.</summary>
		IBoard VisibleBoard { get; set; }
		/// <summary>Returns the board matching a given id.</summary>
		/// <param name="id">Id of the board to return.</param>
		/// <returns>Board.</returns>
		IBoard GetBoardById(int id);
		/// <summary>Finds the stack matching a given id.</summary>
		/// <param name="id">Unique identifier of the stack.</param>
		/// <returns>Stack.</returns>
		IStack GetStackById(int id);
		/// <summary>Finds the piece matching a given id.</summary>
		/// <param name="id">Unique identifier of the piece.</param>
		/// <returns>Piece.</returns>
		IPiece GetPieceById(int id);
		/// <summary>All the dice sets used by this game.</summary>
		DiceHand[] DiceHands { get; }
		/// <summary>Returns the player hand matching a given id.</summary>
		/// <param name="guid">Persistent id of the player whose hand is to be returned.</param>
		/// <returns>A player hand or null if not found.</returns>
		IPlayerHand GetPlayerHand(Guid guid);
		/// <summary>Adds a player hand.</summary>
		/// <param name="guid">Persistent id of the player whose hand will be added.</param>
		/// <returns>The newly created player hand.</returns>
		IPlayerHand AddPlayerHand(Guid guid);
		/// <summary>Removes a player hand.</summary>
		/// <param name="guid">Persistent id of the player whose hand will be removed.</param>
		void RemovePlayerHand(Guid guid);
		/// <summary>The operating mode of the user interface.</summary>
		Mode Mode { get; set; }
		/// <summary>Indicates if stacking happens when pieces are dropped on each others.</summary>
		bool StackingEnabled { get; set; }
	}

	/// <summary>Player hand.</summary>
	public interface IPlayerHand {
		/// <summary>Number of pieces in this hand.</summary>
		int Count { get; }
		/// <summary>List of pieces in this hand.</summary>
		/// <remarks>The pieces are sorted left to right.</remarks>
		IPiece[] Pieces { get; }
	}

	/// <summary>Board.</summary>
	/// <remarks>This is the interface for maps and counter sheets</remarks>
	public interface IBoard {
		/// <summary>Unique identifier for this board.</summary>
		int Id { get; }
		/// <summary>Name of this board.</summary>
		string Name { get; }
		/// <summary>Returns the top-most piece (but not terrain) at a given position on this board.</summary>
		/// <param name="position">Position in local coordinates.</param>
		/// <returns>A piece or null.</returns>
		IPiece GetPieceAtPosition(PointF position);
		/// <summary>Returns the top-most terrain at a given position on this board.</summary>
		/// <param name="position">Position in local coordinates.</param>
		/// <returns>A terrain or null.</returns>
		ITerrain GetTerrainAtPosition(PointF position);
		/// <summary>Visible area of this board.</summary>
		/// <remarks>Zooming and scrolling affect the visible area of the visible board.</remarks>
		RectangleF VisibleArea { get; set; }
		/// <summary>Total area of this board.</summary>
		/// <remarks>Area outside of this area will be displayed in black.</remarks>
		RectangleF TotalArea { get; }
		/// <summary>Returns all the unfolded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from top to bottom.</remarks>
		void FillListWithUnfoldedStacksWithinAreaFrontToBack(List<IStack> list, RectangleF area);
		/// <summary>Returns all the unfolded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from bottom to top.</remarks>
		void FillListWithUnfoldedStacksWithinAreaBackToFront(List<IStack> list, RectangleF area);
		/// <summary>Returns all the folded stacks intersecting a given area.</summary>
		/// <param name="list">A list of stacks (normally empty).</param>
		/// <param name="area">Area in local coordinates.</param>
		/// <remarks>Stacks are sorted from bottom to top.</remarks>
		void FillListWithFoldedStacksWithinAreaBackToFront(List<IStack> list, RectangleF area);
		/// <summary>Returns the z-order of this stack, i.e. how many stacks are behind it.</summary>
		/// <param name="stack">Stack.</param>
		int GetZOrder(IStack stack);
		/// <summary>Returns the stack at the given z-order.</summary>
		/// <param name="zOrder">Z-order of this stack, i.e. how many stacks are behind it.</param>
		/// <returns>A stack or null if not found.</returns>
		IStack GetStackFromZOrder(int zOrder);
		/// <summary>The only player who can see this board, or Guid.Empty.</summary>
		Guid Owner { get; set; }
	}

	/// <summary>Image resolution.</summary>
	/// <remarks>This is the resolution at which the image has been scanned, expressed in dots per inch (dpi).</remarks>
	public enum ImageResolution { Dpi600 = 0, Dpi300 = 1, Dpi150 = 2 }

	/// <summary>Map properties.</summary>
	/// <remarks>
	/// A map is a rectangular piece of printed paper or cardboard on which pieces will be moved.
	/// Only one side of a map is used (the printed side).
	/// </remarks>
	public sealed class MapProperties {
		/// <summary>Name of this map.</summary>
		/// <remarks>This information is displayed in the tabs at the bottom of the screen.</remarks>
		public string Name;
		/// <summary>Name of the scanned image file for this map.</summary>
		public string ImageFileName;
		/// <summary>Resolution in dots per inch.</summary>
		/// <remarks>Not used yet.</remarks>
		public ImageResolution ImageResolution;
	}

	/// <summary>Counter sheet type.</summary>
	public enum CounterSheetType { Piece, Terrain };

	/// <summary>Counter sheet properties.</summary>
	/// <remarks>
	/// A counter sheet is a rectangular piece of cardboard from which pieces are cut from.
	/// Both sides of a counter sheet are used, allowing for double-sided pieces.
	/// For convenience, a counter sheet also acts as a regular board.
	/// </remarks>
	public sealed class CounterSheetProperties {
		/// <summary>Name of this counter sheet.</summary>
		/// <remarks>This information is displayed in the tabs at the bottom of the screen.</remarks>
		public string Name;
		/// <summary>Name of the scanned image file for the recto side of this sheet.</summary>
		public string FrontImageFileName;
		/// <summary>Resolution in dots per inch (recto).</summary>
		public ImageResolution FrontImageResolution;
		/// <summary>Name of the scanned image file for the verso side of this sheet.</summary>
		public string BackImageFileName;
		/// <summary>Resolution in dots per inch (verso).</summary>
		public ImageResolution BackImageResolution;
		/// <summary>Name of the scanned image file for the transparency mask of the recto side of this sheet.</summary>
		/// <remarks>Resolution must be the same as for the recto image.</remarks>
		public string FrontMaskFileName;
		/// <summary>Name of the scanned image file for the transparency mask of the verso side of this sheet.</summary>
		/// <remarks>Resolution must be the same as for the verso image.</remarks>
		public string BackMaskFileName;
		/// <summary>List of all the counter sections "precut" in this sheet.</summary>
		public CounterSectionProperties[] CounterSections;
		/// <summary>List of all the card sections "precut" in this sheet.</summary>
		public CardSectionProperties[] CardSections;
		/// <summary>Indicates if it's a terrain sheet or a regular counter sheet</summary>
		public CounterSheetType Type;
	}

	/// <summary>Counter section, part of a counter sheet, properties.</summary>
	/// <remarks>
	/// A counter section is basically a rectangular grid that marks the piece boundaries.
	/// There can be several counter sections on the same counter sheets.
	/// All the pieces of the same section have the same size.
	/// Sections can be single-sided or double-sided.
	/// </remarks>
	public sealed class CounterSectionProperties {
		/// <summary>Indicates if the pieces are single-sided or double-sided.</summary>
		public CounterSectionType Type;
		/// <summary>Row count.</summary>
		public int Rows;
		/// <summary>Column count.</summary>
		public int Columns;
		/// <summary>Location of this grid on the counter sheet scanned image (recto).</summary>
		public RectangleF FrontImageLocation;
		/// <summary>Location of this grid on the counter sheet scanned image (verso).</summary>
		public RectangleF BackImageLocation;
		/// <summary>Offset of the shadow of the pieces of this counter section.</summary>
		public float ShadowLength;
		/// <summary>Number of copies of each piece.</summary>
		public int Supply;
	}

	/// <summary>Card section, part of a counter sheet, properties.</summary>
	/// <remarks>
	/// A card section is basically a rectangular grid that marks the card boundaries.
	/// There can be several counter sections and/or card sections on the same counter sheets.
	/// All the cards of the same section have the same size.
	/// Sections can be single-sided or double-sided. All cards share the same back.
	/// </remarks>
	public sealed class CardSectionProperties {
		/// <summary>Indicates if the cards are on the recto or verso, and if the cards can be flipped to their back.</summary>
		public CounterSectionType Type;
		/// <summary>Row count.</summary>
		public int Rows;
		/// <summary>Column count.</summary>
		public int Columns;
		/// <summary>Location of this grid on the counter sheet scanned image (recto or verso).</summary>
		public RectangleF FaceImageLocation;
		/// <summary>Location of the back image on the counter sheet scanned image (same or different side).</summary>
		public RectangleF BackImageLocation;
		/// <summary>Offset of the shadow of the cards of this card section.</summary>
		public float ShadowLength;
		/// <summary>Number of copies of each card.</summary>
		public int Supply;
	}

	/// <summary>Map.</summary>
	/// <remarks>
	/// A map is a rectangular piece of printed paper or cardboard on which pieces will be moved.
	/// Only one side of a map is used (the printed side).
	/// </remarks>
	public interface IMap : IBoard {
		/// <summary>Static properties of this map.</summary>
		MapProperties Properties { get; }
		/// <summary>Texture sets used to display the background of this map.</summary>
		ITileSet BackgroundGraphics { get; set; }
	}

	/// <summary>Visible side of a piece or a counter sheet.</summary>
	[ObfuscationAttribute(Exclude = true)]
	public enum Side { Front = 0x00, Back = 0x01 };

	/// <summary>Counter sheet.</summary>
	/// <remarks>
	/// A counter sheet is a rectangular piece of cardboard from which a pieces are cut from.
	/// Both sides of a counter sheet are used, allowing for double-sided pieces.
	/// For convenience, a counter sheet also acts as a regular board.
	/// </remarks>
	public interface ICounterSheet : IBoard {
		/// <summary>Static properties of this counter sheet.</summary>
		CounterSheetProperties Properties { get; }
		/// <summary>Side currently visible.</summary>
		Side Side { get; set; }
		/// <summary>Texture sets used to display the recto side of this sheet.</summary>
		ITileSet FrontGraphics { get; set; }
		/// <summary>Texture sets used to display the verso side of this sheet.</summary>
		ITileSet BackGraphics { get; set; }
		/// <summary>List of all the counter sections "precut" in this sheet.</summary>
		ICounterSection[] CounterSections { get; }
	}

	/// <summary>Sidedness of the pieces of a single section.</summary>
	[Flags, ObfuscationAttribute(Exclude = true)]
	public enum CounterSectionType {
		// counters
		FrontSideOnly = 0x01, BackSideOnly = 0x02, TwoSided = 0x03,
		// cards
		CardFacesOnFront = 0x08, CardFacesOnBack = 0x09,
		CardFacesAndBackOnFront = 0x0a, CardFacesOnBackBackOnOtherSide = 0x0b,
		CardFacesOnFrontBackOnOtherSide = 0x0e, CardFacesAndBackOnBack = 0x0f
	};

	/// <summary>Counter section, part of a counter sheet.</summary>
	/// <remarks>
	/// A counter section is basically a rectangular grid that marks the piece boundaries.
	/// All the pieces of the same section have the same size.
	/// </remarks>
	public interface ICounterSection {
		/// <summary>Counter sheet this section is part of.</summary>
		ICounterSheet CounterSheet { get; }
		/// <summary>Indicates if the pieces are single-sided or double-sided.</summary>
		CounterSectionType Type { get; }
		/// <summary>Location of this grid on the counter sheet scanned image (recto).</summary>
		RectangleF FrontImageLocation { get; }
		/// <summary>Location of this grid on the counter sheet scanned image (verso).</summary>
		RectangleF BackImageLocation { get; }
		/// <summary>Size of the pieces of this counter section (recto side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		SizeF PieceFrontSize { get; }
		/// <summary>Size of the pieces of this counter section (verso side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		SizeF PieceBackSize { get; }
		/// <summary>Diagonal length of the pieces of this counter section (recto side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		float PieceFrontDiagonal { get; }
		/// <summary>Diagonal length of the pieces of this counter section (verso side).</summary>
		/// <remarks>All the pieces of the same section have the same size.</remarks>
		float PieceBackDiagonal { get; }
		/// <summary>Offset of the shadow of the pieces of this counter section.</summary>
		float ShadowLength { get; }
		/// <summary>Number of copies of each piece.</summary>
		int Supply { get; }
		/// <summary>List of all pieces cut from this counter section.</summary>
		IPiece[] Pieces { get; }

		// Tests about the counter section type
		bool ContainsCounters { get; }
		bool IsSingleSided { get; }
		bool HasCardFaceOnFront { get; }
		bool HasCardBackOnFront { get; }

		//bool ContainsCards { get; }
		//bool ContainsSingleSidedCards { get; }
		//bool HasCardFaceOnBack { get; }
		//bool HasCardBackOnBack { get; }
	}

	/// <summary>Stack of pieces.</summary>
	/// <remarks>
	/// A player selects and moves stacks of pieces.
	/// A Piece that stands alone is actually part of a stack which contains only that piece.
	/// </remarks>
	public interface IStack {
		SizeF IndentationBetweenPieces { get; }

		/// <summary>Unique identifier for this stack.</summary>
		int Id { get; }
		/// <summary>Indicates if the stack is for a piece still attached to the counter sheet.</summary>
		bool AttachedToCounterSection { get; }
		/// <summary>Board on which this stack stays.</summary>
		IBoard Board { get; }
		/// <summary>Position relative to the board.</summary>
		PointF Position { get; }
		/// <summary>List of pieces in this stack.</summary>
		/// <remarks>The pieces are sorted back to front.</remarks>
		IPiece[] Pieces { get; }
		/// <summary>Gets the selection encompassing all the pieces of this stack.</summary>
		/// <returns>Selection object.</returns>
		ISelection Select();
		/// <summary>Smallest rectangular area which contains this stack.</summary>
		RectangleF BoundingBox { get; }
		/// <summary>Indicates if a mouse cursor is over the stack.</summary>
		bool Unfolded { get; set; }
	}

	/// <summary>Game piece.</summary>
	/// <remarks>
	/// A piece is cut from a counter section.
	/// It is always part of a stack.
	/// </remarks>
	public interface IPiece {
		/// <summary>Unique identifier for this piece.</summary>
		int Id { get; }
		/// <summary>Counter section from which this piece is cut.</summary>
		ICounterSection CounterSection { get; }
		/// <summary>Row of this piece in the counter section.</summary>
		int Row { get; }
		/// <summary>Column of this piece in the counter section.</summary>
		int Column { get; }
		/// <summary>Stack that contains this piece.</summary>
		/// <remarks>A piece is always part of a stack.</remarks>
		IStack Stack { get; }
		/// <summary>Rotation angle in Radians.</summary>
		/// <remarks>
		/// The value for an upside up position is zero.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		float RotationAngle { get; }
		/// <summary>Cosinus value of the flip angle.</summary>
		/// <remarks>
		/// The value for a piece at rest (i.e. not flipping) is 1.0f.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		float FlipAngleCosinus { get; }
		/// <summary>Visible side.</summary>
		/// <remarks>This value is not used if the piece is still attached to the counter section.</remarks>
		Side Side { get; }
		/// <summary>Gets the selection encompassing this piece only.</summary>
		/// <returns>Selection object.</returns>
		ISelection Select();
		/// <summary>Size of this piece (for the currently displayed side).</summary>
		SizeF Size { get; }
		/// <summary>Diagonal of this piece (for the currently displayed side).</summary>
		float Diagonal { get; }
		/// <summary>Texture sets used to display this piece (recto).</summary>
		IImage FrontGraphics { get; set; }
		/// <summary>Texture sets used to display this piece (verso).</summary>
		IImage BackGraphics { get; set; }
		/// <summary>Texture sets used to display this piece (for the currently displayed side).</summary>
		IImage Graphics { get; }
		/// <summary>Smallest rectangular area which contains this piece.</summary>
		RectangleF BoundingBox { get; }
		/// <summary>Position of the center of this piece relative to the board.</summary>
		PointF Position { get; }
		/// <summary>Position of the center of this piece relative to the board when attached to the counter section.</summary>
		PointF PositionWhenAttached { get; }
		/// <summary>Position of this piece in its stack relative to the other pieces.</summary>
		int IndexInStackFromBottomToTop { get; }
		/// <summary>Indicates if a player as moved his mouse cursor over this card.</summary>
		bool RolledOver { get; set; }
	}

	/// <summary>A playing card.</summary>
	public interface ICard : IPiece { }

	/// <summary>A regular counter.</summary>
	public interface ICounter : IPiece { }

	/// <summary>A terrain piece.</summary>
	public interface ITerrain : IPiece { }

	/// <summary>A terrain prototype, attached to a terrain sheet or in a player's hand.</summary>
	public interface ITerrainPrototype : ITerrain {	}

	/// <summary>Instance of a terrain piece.</summary>
	/// <remarks>
	/// A terrain clone is created based on a terrain prototype.
	/// It is never attached.
	/// </remarks>
	public interface ITerrainClone : ITerrain {
		/// <summary>Prototype of this piece.</summary>
		ITerrainPrototype Prototype { get; }
	}

	/// <summary>Set of pieces in one stack</summary>
	public interface ISelection {
		/// <summary>Stack that contains this selection.</summary>
		/// <remarks>It is not possible to select pieces from several stacks.</remarks>
		IStack Stack { get; }
		/// <summary>List of pieces in this selection.</summary>
		/// <remarks>The pieces are sorted back to front.</remarks>
		IPiece[] Pieces { get; }
		/// <summary>Creates a new selection by adding another piece of the same stack to this selection.</summary>
		/// <param name="piece">Piece to select.</param>
		/// <returns>Result selection.</returns>
		ISelection AddPiece(IPiece piece);
		/// <summary>Creates a new selection by removing a piece from this selection.</summary>
		/// <param name="piece">Piece to deselect.</param>
		/// <returns>Result selection.</returns>
		ISelection RemovePiece(IPiece piece);
		/// <summary>Creates a new selection by removing all pieces from this selection.</summary>
		/// <returns>Result selection.</returns>
		ISelection RemoveAllPieces();
		/// <summary>True if no piece is part of this selection.</summary>
		bool Empty { get; }
		/// <summary>Test if a piece is part of this selection.</summary>
		/// <param name="piece">Piece to test.</param>
		/// <returns>True if piece is part of this selection.</returns>
		bool Contains(IPiece piece);
	}

	/// <summary>Undoable user action</summary>
	/// <remarks>
	/// Commands are grouped in atomic command sequences.
	/// Only whole sequences can be undone/redone
	/// </remarks>
	public interface ICommand {
		/// <summary>Execute this command.</summary>
		void Do();
		/// <summary>Cancel the result of this command.</summary>
		void Undo();
		/// <summary>Rollback the previous cancellation of this command.</summary>
		void Redo();
	}

	/// <summary>View context before or after a user action</summary>
	public struct CommandContext {
		/// <summary>Constructor.</summary>
		public CommandContext(IBoard VisibleBoard) {
			this.VisibleBoard = VisibleBoard;
			VisibleArea = VisibleBoard.VisibleArea;
		}
		/// <summary>Constructor.</summary>
		public CommandContext(IBoard VisibleBoard, RectangleF ensureVisible) {
			this.VisibleBoard = VisibleBoard;
			VisibleArea = VisibleBoard.VisibleArea;

			if(ensureVisible.Width > VisibleArea.Width) {
				VisibleArea.X = ensureVisible.X;
				VisibleArea.Width = ensureVisible.Width;
			}
			if(ensureVisible.Height > VisibleArea.Height) {
				VisibleArea.Y = ensureVisible.Y;
				VisibleArea.Height = ensureVisible.Height;
			}
			if(ensureVisible.X < VisibleArea.X)
				VisibleArea.X = ensureVisible.X;
			else if(ensureVisible.Right > VisibleArea.Right)
				VisibleArea.X += ensureVisible.Right - VisibleArea.Right;
			if(ensureVisible.Y < VisibleArea.Y)
				VisibleArea.Y = ensureVisible.Y;
			else if(ensureVisible.Bottom > VisibleArea.Bottom)
				VisibleArea.Y += ensureVisible.Bottom - VisibleArea.Bottom;
		}
		/// <summary>The board currently visible.</summary>
		public IBoard VisibleBoard;
		/// <summary>Visible area of the board.</summary>
		/// <remarks>Zooming and scrolling affect the visible area of the visible board.</remarks>
		public RectangleF VisibleArea;
	}

	/// <summary>Used by the controller to execute user actions</summary>
	/// <remarks>
	/// Commands are grouped in atomic command sequences.
	/// Only whole sequences can be undone/redone.
	/// </remarks>
	public interface ICommandManager {
		/// <summary>Execute a sequence of commands.</summary>
		void ExecuteCommandSequence(params ICommand[] commands);
		/// <summary>Execute a sequence of commands.</summary>
		void ExecuteCommandSequence(CommandContext contextBefore, CommandContext contextAfter, params ICommand[] commands);
		/// <summary>Cancel the result of the latest command.</summary>
		void Undo();
		/// <summary>Rollback the previous cancellation of the latest command.</summary>
		void Redo();
		/// <summary>Erases all previous commands from the undo stack.</summary>
		/// <remarks>Must be called when a game is loaded, or when a player joins.</remarks>
		void ClearUndoStack();
		/// <summary>True if the latest command can be canceled.</summary>
		bool CanUndo { get; }
		/// <summary>True if the previous cancellation can be rolledback.</summary>
		bool CanRedo { get; }
	}

	/// <summary>The component in charge of running animations.</summary>
	public interface IAnimationManager {
		/// <summary>Starts an animation sequence.</summary>
		/// <param name="animationSequence">All the animations to chain, ordered as in the sequence.</param>
		void LaunchAnimationSequence(params IAnimation[] animationSequence);
		/// <summary>Updates the game state according to all animations still running.</summary>
		/// <param name="currentTimeInMicroseconds">The current time of this frame.</param>
		void Animate(long currentTimeInMicroseconds);
		/// <summary>Stops all animations immediately.</summary>
		/// <remarks>This is used for instance when a game is loaded.</remarks>
		void EndAllAnimations();
		/// <summary>Determines if a stack is currently involved in an animation.</summary>
		/// <param name="stack">A stack.</param>
		/// <returns>True if any piece of the stack is being animated.</returns>
		bool IsBeingAnimated(IStack stack);
	}

	/// <summary>Sequence of game state changes used to implement an animation.</summary>
	public interface IAnimation {
	}

	/// <summary>Rotation in 3D space.</summary>
	public interface IRotation {
		/// <summary>Computes the composition of two rotations in 3D space.</summary>
		/// <param name="secondRotation">Another rotation.</param>
		/// <returns>The composition rotation.</returns>
		IRotation ComposeWith(IRotation secondRotation);
		/// <summary>Interpolates between two rotations in 3D space, using spherical linear interpolation.</summary>
		/// <param name="finalRotation">Another rotation.</param>
		/// <param name="coefficient">Parameter that indicates how far to interpolate between the rotations.</param>
		/// <returns>The interpolation rotation.</returns>
		IRotation InterpolateWith(IRotation finalRotation, float coefficient);
		/// <summary>Converts to a rotation matrix.</summary>
		/// <returns>A 3x3 matrix.</returns>
		float[,] ToRotationMatrix();
	}

	[ObfuscationAttribute(Exclude = true)]
	public enum DiceType { D4, D6, D8, D10, D12, D20 };

	/// <summary>A set of dice, to be cast together.</summary>
	public struct DiceHand {
		/// <summary>Type of dice used.</summary>
		public DiceType DiceType;
		/// <summary>True if the dice is being cast (and so can't be cast again until it is rearmed).</summary>
		public bool BeingCast;
		/// <summary>Number of dice being cast.</summary>
		public int DiceCountBeingCast;
		/// <summary>The dice of this set.</summary>
		public Die[] Dice;
	}

	/// <summary>A die.</summary>
	public struct Die {
		/// <summary>Constructor.</summary>
		/// <param name="color">Color.</param>
		/// <param name="pips">Pips color.</param>
		public Die(uint color, uint pips) {
			Color = color;
			Pips = pips;
			TextureFileName = null;
			DieMesh = null;
			DockedSize = 0.0f;
			Size = 0.0f;
			DockedPosition = PointF.Empty;
			Position = PointF.Empty;
			Orientation = null;
		}
		/// <summary>Constructor.</summary>
		/// <param name="textureFileName">Name of the scanned image file for the texture of this die.</param>
		public Die(string textureFileName) {
			Color = 0xffffffff;
			Pips = 0xff000000;
			TextureFileName = textureFileName;
			DieMesh = null;
			DockedSize = 0.0f;
			Size = 0.0f;
			DockedPosition = PointF.Empty;
			Position = PointF.Empty;
			Orientation = null;
		}
		/// <summary>Color.</summary>
		public readonly uint Color;
		/// <summary>Pips color.</summary>
		public readonly uint Pips;
		/// <summary>Name of the scanned image file for the texture of this die.</summary>
		public readonly string TextureFileName;
		/// <summary>3D model of this type of die.</summary>
		public IDieMesh DieMesh;
		/// <summary>Size when not cast, in screen coordinates.</summary>
		public float DockedSize;
		/// <summary>Size in screen coordinates.</summary>
		public float Size;
		/// <summary>Position when not cast, in screen coordinates.</summary>
		public PointF DockedPosition;
		/// <summary>Position in screen coordinates.</summary>
		public PointF Position;
		/// <summary>Orientation.</summary>
		public IRotation Orientation;
	}

	/// <summary>A random number generator.</summary>
	public interface IRandomNumberGenerator {
		/// <summary>Generates a random integer number.</summary>
		/// <param name="lowerBound">Minimum eligible value.</param>
		/// <param name="upperBound">Maximum eligible value.</param>
		/// <returns>A random integer number.</returns>
		int GenerateInt32(int lowerBound, int upperBound);
		/// <summary>Generates a random float number.</summary>
		/// <param name="lowerBound">Minimum eligible value.</param>
		/// <param name="upperBound">Maximum eligible value.</param>
		/// <returns>A random float number.</returns>
		float GenerateSingle(float lowerBound, float upperBound);
	}

	/*
	/// <summary>Block sheet properties.</summary>
	/// <remarks>
	/// A block sheet is a rectangular piece of paper from which the block stickers are cut from.
	/// For convenience, a block sheet also acts as a regular board.
	/// </remarks>
	public sealed class BlockSheetProperties {
		/// <summary>Name of this block sheet.</summary>
		/// <remarks>This information is displayed in the tabs at the bottom of the screen.</remarks>
		public string Name;
		/// <summary>Name of the scanned image file for this sheet.</summary>
		public string ImageFileName;
		/// <summary>Resolution in dots per inch.</summary>
		public ImageResolution ImageResolution;
		/// <summary>List of all the block sections "precut" in this sheet.</summary>
		public BlockSectionProperties[] BlockSections;
	}

	/// <summary>Block section, part of a block sheet, properties.</summary>
	/// <remarks>
	/// A block section is basically a rectangular grid that marks the block stickers boundaries.
	/// Stickers are then glued on larger colored wooden blocks.
	/// There can be several block sections on the same block sheets.
	/// All the blocks of the same section have the same size, and the same color.
	/// </remarks>
	public sealed class BlockSectionProperties {
		/// <summary>Row count.</summary>
		public int Rows;
		/// <summary>Column count.</summary>
		public int Columns;
		/// <summary>Location of this grid on the block sheet scanned image.</summary>
		public RectangleF ImageLocation;
		/// <summary>Color of the blocks.</summary>
		public uint BlockColor;
		/// <summary>Size of the blocks.</summary>
		public SizeF BlockSize;
		/// <summary>Thickness of the blocks.</summary>
		public float BlockThickness;
	}

	/// <summary>Block sheet.</summary>
	/// <remarks>
	/// A block sheet is a rectangular piece of paper from which the block stickers are cut from.
	/// For convenience, a block sheet also acts as a regular board.
	/// </remarks>
	public interface IBlockSheet : IBoard {
		/// <summary>Static properties of this block sheet.</summary>
		BlockSheetProperties Properties { get; }
		/// <summary>Texture sets used to display this sheet.</summary>
		ITileSet Graphics { get; set; }
		/// <summary>List of all the block sections "precut" in this sheet.</summary>
		IBlockSection[] BlockSections { get; }
	}

	/// <summary>Block section, part of a block sheet.</summary>
	/// <remarks>
	/// A block section is basically a rectangular grid that marks the block sticker boundaries.
	/// All the blocks of the same section have the same size.
	/// </remarks>
	public interface IBlockSection {
		/// <summary>Block sheet this section is part of.</summary>
		IBlockSheet BlockSheet { get; }
		/// <summary>Location of this grid on the block sheet scanned image.</summary>
		RectangleF ImageLocation { get; }
		/// <summary>Size of the stickers of this block section.</summary>
		/// <remarks>All the stickers of the same section have the same size.</remarks>
		SizeF StickerSize { get; }
		/// <summary>Size of the blocks of this block section.</summary>
		/// <remarks>All the blocks of the same section have the same size.</remarks>
		SizeF BlockSize { get; }
		/// <summary>Color of the blocks of this block section.</summary>
		/// <remarks>All the blocks of the same section have the same color.</remarks>
		uint BlockColor { get; }
		/// <summary>Thickness of the blocks of this block section.</summary>
		/// <remarks>All the blocks of the same section have the same thickness.</remarks>
		float BlockThickness { get; }
		/// <summary>List of all blocks cut from this block section.</summary>
		IBlock[,] Blocks { get; }
	}

	/// <summary>Block.</summary>
	/// <remarks>
	/// A player selects and moves individual blocks.
	/// </remarks>
	public interface IBlock {
		/// <summary>Unique identifier for this block.</summary>
		int Id { get; }
		/// <summary>Indicates if the block is still attached to the block sheet.</summary>
		bool AttachedToBlockSection { get; }
		/// <summary>Board on which this block stays.</summary>
		IBoard Board { get; }
		/// <summary>Gets the selection encompassing this block only.</summary>
		/// <returns>Selection object.</returns>
		ISelection Select();
		/// <summary>Smallest rectangular area which contains this block.</summary>
		RectangleF BoundingBox { get; }
		/// <summary>Block section from which this block is cut.</summary>
		IBlockSection BlockSection { get; }
		/// <summary>Row of this piece in the counter section.</summary>
		int Row { get; }
		/// <summary>Column of this piece in the counter section.</summary>
		int Column { get; }
		/// <summary>Rotation angle in Radians.</summary>
		/// <remarks>
		/// The value for an upside up position is zero.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		float RotationAngle { get; }
		/// <summary>Value of the flip angle.</summary>
		/// <remarks>
		/// The value for a piece at rest (i.e. not flipping) is 0.0f.
		/// This value is not used if the piece is still attached to the counter section.
		/// </remarks>
		//float FlipAngle { get; }
		/// <summary>Visible side.</summary>
		/// <remarks>This value is not used if the piece is still attached to the counter section.</remarks>
		//Side Side { get; }
		/// <summary>Size of this piece.</summary>
		//SizeF Size { get; }
		/// <summary>Diagonal of this piece.</summary>
		//float Diagonal { get; }
		/// <summary>Texture sets used to display this block.</summary>
		IImage Graphics { get; set; }
		/// <summary>Position of the center of this block relative to the board.</summary>
		PointF Position { get; }
		/// <summary>Position of the center of this block relative to the board when attached to the block section.</summary>
		PointF PositionWhenAttached { get; }
	}
	*/
}
