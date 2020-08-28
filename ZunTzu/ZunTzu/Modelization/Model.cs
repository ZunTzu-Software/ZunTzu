// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ZunTzu.AudioVideo;
using ZunTzu.Networking;

namespace ZunTzu.Modelization {

	/// <summary>Current state of the program.</summary>
	public sealed class Model : IModel {
		/// <summary>Constructor.</summary>
		public Model(Form mainForm, AudioProperties audioProperties, string playerFirstName, string playerLastName, Guid playerGuid) {
			audioManager = new AudioManager(mainForm, audioProperties);
			networkClient = new DXClient(mainForm, audioManager);
			commandManager = new CommandManager(this);
			animationManager = new AnimationManager(this);
			gameLibrary = new GameLibrary();
			thisPlayer = new Player(networkClient.PlayerId, playerFirstName, playerLastName, playerGuid, 0xffffff00, Point.Empty, true);
			players = new Player[] { thisPlayer };
		}

		/// <summary>The repository for all game boxes.</summary>
		public IGameLibrary GameLibrary { get { return gameLibrary; } }

		/// <summary>Opens a game box file.</summary>
		/// <param name="reference">Library reference of the game box.</param>
		public void OpenGameBox(IGameBoxReference reference) {
			Debug.Assert(currentSelection == null);
			currentGameBox = new GameBox((GameBoxReference) reference);
		}

		/// <summary>Opens a game box file.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		public void OpenGameBox(string fileName) {
			Debug.Assert(currentSelection == null);
			currentGameBox = new GameBox(fileName);
		}

		/// <summary>Retrieves game box name and hash value from a game data stream.</summary>
		/// <param name="inputStream">The stream from which to read the game data.</param>
		/// <param name="gameBoxName">The variable to hold the game box name.</param>
		/// <param name="gameBoxHash">The variable to hold the game box hash value (null if it wasn't found).</param>
		public void RetrieveGameBoxInfoFromGameData(Stream inputStream, out string gameBoxName, out byte[] gameBoxHash) {
			Game.RetrieveGameBoxInfo(inputStream, out gameBoxName, out gameBoxHash);
		}

		/// <summary>Checks the validity of a game box file.</summary>
		/// <param name="fileName">Name of the game box file.</param>
		/// <returns>A list of errors and warnings.</returns>
		public IEnumerable<string> VerifyGameBox(string fileName) {
			foreach(string error in GameBoxVerifier.VerifyGameBox(fileName))
				yield return error;
		}

		/// <summary>Properties of the last opened game box.</summary>
		public IGameBox CurrentGameBox { get { return currentGameBox; } }

		/// <summary>Pieces currently selected.</summary>
		public ISelection CurrentSelection {
			get { return currentSelection; }
			set {
				if(value != currentSelection) {
					if(value == null) {
						stackInspectorPositions = null;
					} else if(currentSelection == null || value.Stack != currentSelection.Stack) {
						stackInspectorPositions = new PointF[value.Stack.Pieces.Length];

						// arrange pieces in stack inspector
						float yPos = 0.0f;
						for(int i = stackInspectorPositions.Length - 1; i >= 0; --i) {
							// to allow rotations, the room to allocate for each piece
							// must be at least equal to its diagonal
							stackInspectorPositions[i] = new PointF(0.0f, yPos);
							yPos += value.Stack.Pieces[i].Diagonal;
						}

						// abort piece drags
						foreach(Player player in players)
							player.PieceBeingDragged = null;
					}
					currentSelection = (Selection) value;
				}
			}
		}

		/// <summary>True to indicate that the user is in rotating mode.</summary>
		public bool IsRotating { get { return isRotating; } set { isRotating = value; } }
		/// <summary>Current rotation angle.</summary>
		public float RotationAngle { get { return rotationAngle; } set { rotationAngle = value; } }

		/// <summary>True to indicate that the user is in measuring mode.</summary>
		public bool IsMeasuring { get { return isMeasuring; } set { isMeasuring = value; } }
		/// <summary>Position of the extremity of the ruler farther from the mouse cursor in model coordinates.</summary>
		public PointF RulerStartPosition { get { return rulerStartPosition; } set { rulerStartPosition = value; } }
		/// <summary>Position of the extremity of the ruler closer to the mouse cursor in model coordinates.</summary>
		public PointF RulerEndPosition { get { return rulerEndPosition; } set { rulerEndPosition = value; } }

		/// <summary>The component in charge of sound playback.</summary>
		public IAudioManager AudioManager { get { return audioManager; } }

		/// <summary>The component in charge of network communication.</summary>
		public IClient NetworkClient { get { return networkClient; } }

		/// <summary>Handler for user actions</summary>
		public ICommandManager CommandManager { get { return commandManager; } }

		/// <summary>The component in charge of running animations.</summary>
		public IAnimationManager AnimationManager { get { return animationManager; } }

		/// <summary>A random number generator.</summary>
		public IRandomNumberGenerator RandomNumberGenerator { get { return randomNumberGenerator; } }

		/// <summary>Collection of player names indexed by player id.</summary>
		public IPlayer[] Players { get { return players; } }

		/// <summary>Set to true if this player is hosting the game.</summary>
		public bool IsHosting { get { return isHosting; } set { isHosting = value; } }

		/// <summary>This player.</summary>
		public IPlayer ThisPlayer { get { return thisPlayer; } }

		/// <summary>Number of players.</summary>
		public int PlayerCount { get { return players.Length; } }

		/// <summary>Returns the player with the given id.</summary>
		/// <param name="playerId">A player id.</param>
		/// <returns>A player or null if not found.</returns>
		public IPlayer GetPlayer(int playerId) {
			for(int i = 0; i < players.Length; ++i)
				if(players[i].Id == playerId)
					return players[i];
			return null;
		}

		/// <summary>Returns the player with the given Guid.</summary>
		/// <param name="playerGuid">A Guid.</param>
		/// <returns>A player or null if not found.</returns>
		public IPlayer GetPlayerByGuid(Guid playerGuid) {
			for(int i = 0; i < players.Length; ++i)
				if(players[i].Guid == playerGuid)
					return players[i];
			return null;
		}

		/// <summary>Add a player to the player list.</summary>
		/// <param name="id">The player network id.</param>
		/// <param name="firstName">The player's first name.</param>
		/// <param name="lastName">The player's last name.</param>
		/// <param name="guid">The player's peristent id.</param>
		/// <param name="color">The player color.</param>
		/// <param name="cursorPosition">The current cursor position of this player in screen coordinates.</param>
		/// <param name="isCursorVisible">False if the cursor is over a private frame.</param>
		/// <returns>The newly created player.</returns>
		public IPlayer AddPlayer(int id, string firstName, string lastName, Guid guid, uint color, Point cursorPosition, bool isCursorVisible) {
			if(id == networkClient.PlayerId) {
				thisPlayer.Id = id;
				thisPlayer.Color = color;
				return thisPlayer;
			} else {
				Player newPlayer = new Player(id, firstName, lastName, guid, color, cursorPosition, isCursorVisible);
				Player[] oldArray = players;
				players = new Player[oldArray.Length + 1];
				Array.Copy(oldArray, players, oldArray.Length);
				players[players.Length - 1] = newPlayer;
				return newPlayer;
			}
		}

		/// <summary>Sequence number used to resolve conflicting commands received from different players at the same moment.</summary>
		public int StateChangeSequenceNumber {
			get { return stateChangeSequenceNumber; } 
			set { stateChangeSequenceNumber = value; }
		}

		/// <summary>Remove a player from the player list.</summary>
		public void RemovePlayer(int id) {
			for(int i = 0; i < players.Length; ++i)
				if(players[i].Id == id) {
					Player[] oldArray = players;
					players = new Player[oldArray.Length - 1];
					Array.Copy(oldArray, players, i);
					Array.Copy(oldArray, i + 1, players, i, oldArray.Length - i - 1);
					break;
				}
		}

		/// <summary>Remove all players from the player list.</summary>
		public void RemoveAllPlayers() {
			players = new Player[] { thisPlayer };
		}

		/// <summary>Computes a rotation in 3D space, from a rotation axis and an angle.</summary>
		/// <param name="axisX">X component of the rotation axis vector.</param>
		/// <param name="axisY">Y component of the rotation axis vector.</param>
		/// <param name="axisZ">Z component of the rotation axis vector.</param>
		/// <param name="angle">Rotation angle.</param>
		/// <returns>A rotation.</returns>
		public IRotation ComputeRotationFromAxis(float axisX, float axisY, float axisZ, float angle) {
			return new Rotation(axisX, axisY, axisZ, angle);
		}

		/// <summary>Computes a rotation in 3D space, by composing three rotations along the Z, X and then Y axis.</summary>
		/// <param name="yaw">Rotation angle along the Y axis.</param>
		/// <param name="pitch">Rotation angle along the X axis.</param>
		/// <param name="roll">Rotation angle along the Z axis.</param>
		/// <returns>A rotation.</returns>
		public IRotation ComputeRotationYawPitchRoll(float yaw, float pitch, float roll) {
			return new Rotation(yaw, pitch, roll);
		}

		/// <summary>Zoom factor used in the stack inspector</summary>
		public float StackInspectorScale { get { return stackInspectorScale; } set { stackInspectorScale = value; } }

		/// <summary>Position of each piece of the selection in screen coordinates.</summary>
		public PointF[] StackInspectorPositions { get { return stackInspectorPositions; } set { stackInspectorPositions = value; } }

		/// <summary>Remove any transient state before opening a new game.</summary>
		public void ClearTransientState() {
			animationManager.EndAllAnimations();
			commandManager.ClearUndoStack();
			currentSelection = null;
			isRotating = false;
			foreach(Player player in players) {
				player.StackBeingDragged = null;
				player.PieceBeingDragged = null;
			}
		}

		private GameLibrary gameLibrary;
		private AudioManager audioManager;
		private DXClient networkClient;
		private CommandManager commandManager;
		private AnimationManager animationManager;
		private RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator();
		private Player[] players;
		private bool isHosting = true;
		private Player thisPlayer = null;
		//private string playerFirstName;
		//private string playerLastName;
		private int stateChangeSequenceNumber = 0;
		private GameBox currentGameBox = null;
		private Selection currentSelection = null;
		//private Stack stackBeingDragged = null;
		//private Piece pieceBeingDragged = null;
		//private PointF dragAndDropAnchor = PointF.Empty;
		private bool isRotating = false;
		private float rotationAngle = 0.0f;
		private bool isMeasuring = false;
		private PointF rulerStartPosition = PointF.Empty;
		private PointF rulerEndPosition = PointF.Empty;
		private float stackInspectorScale = 1.0f;
		private PointF[] stackInspectorPositions = null;
	}
}
