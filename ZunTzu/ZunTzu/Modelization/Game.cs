// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace ZunTzu.Modelization {

	/// <summary>Storage mode of a scenario.</summary>
	internal enum ScenarioType { BuiltIn, FromScenarioFile, FromGameFile, FromOverTheNetwork };

	/// <summary>Scenario and game data.</summary>
	/// <remarks>
	/// A scenario is one possible way of playing using a game box. It defines which boards and pieces will be used, and also the starting position of the pieces.
	/// Scenarios are usually part of the game box ("built-in"), but they can also come from external sources.
	/// Scenario data is saved along with game data each time a game is saved. Actually, there is no difference between a scenario and a saved game.
	/// </remarks>
	internal sealed class Game : IGame {
		/// <summary>Name of this scenario.</summary>
		public string ScenarioName { get { return scenarioName; } }
		/// <summary>Description of this scenario.</summary>
		public string ScenarioDescription { get { return scenarioDescription; } }
		/// <summary>Copyright for this scenario.</summary>
		public string ScenarioCopyright { get { return scenarioCopyright; } }
		/// <summary>Origin of this scenario: part of the game box, or from an external source.</summary>
		internal ScenarioType ScenarioType { get { return scenarioType; } }
		/// <summary>Name of the file from which this scenario or game was loaded.</summary>
		public string FileName { get { return fileName; } set { fileName = value; } }
		/// <summary>All the boards used by this game.</summary>
		public IBoard[] Boards { get { return boards; } }
		/// <summary>The board currently visible.</summary>
		public IBoard VisibleBoard {
			get { return visibleBoard; }
			set { visibleBoard = (Board) value; }
		}
		/// <summary>Finds the stack matching a given id.</summary>
		/// <param name="id">Unique identifier of the stack.</param>
		/// <returns>Stack.</returns>
		public IStack GetStackById(int id) {
			IPiece bottomOfStack = GetPieceById(id);
			return bottomOfStack.Stack;
		}
		/// <summary>Finds the piece matching a given id.</summary>
		/// <param name="id">Unique identifier of the piece.</param>
		/// <returns>Piece.</returns>
		public IPiece GetPieceById(int id) {
			return allPieces[id];
		}

		private GameBox gameBox;
		private ScenarioType scenarioType;
		private string fileName;
		private string scenarioName;
		private string scenarioDescription;
		private string scenarioCopyright;
		/// <summary>Collection of all pieces indexed by piece id.</summary>
		private Piece[] allPieces = null;

		/// <summary>All the boards used by this game.</summary>
		private Board[] boards;
		/// <summary>Returns the board which id is equal to the parameter string.</summary>
		/// <param name="id">Id of the board to return.</param>
		public IBoard GetBoardById(int id) {
			foreach(Board board in boards) {
				if(board.Id == id)
					return board;
			}
			return null;
		}
		/// <summary>The board currently visible.</summary>
		private Board visibleBoard;

		/// <summary>Saves the current game.</summary>
		/// <param name="outputStream">The stream to write this game data to.</param>
		/// <param name="indented">True to write indentation.</param>
		public void Save(Stream outputStream, bool indented) {
			XmlTextWriter writer = null;
			try {
				writer = new XmlTextWriter(outputStream, Encoding.GetEncoding("windows-1252"));
				if(indented) {
					writer.Formatting = Formatting.Indented;
					writer.IndentChar = '	';
					writer.Indentation = 1;
				}
				writer.WriteStartDocument(true);

				writer.WriteStartElement("game");
				//writer.WriteAttributeString("version", "1.0");

				writer.WriteAttributeString("game-box", gameBox.Reference.Name);
				writer.WriteAttributeString("hash", Convert.ToBase64String(gameBox.Reference.Hash));

				writer.WriteAttributeString("scenario-name", scenarioName);
				if(scenarioDescription != null && scenarioDescription != "")
					writer.WriteAttributeString("scenario-description", scenarioDescription);
				if(scenarioCopyright != null && scenarioCopyright != "")
					writer.WriteAttributeString("scenario-copyright", scenarioCopyright);

				if(mode != Mode.Default)
					writer.WriteAttributeString("mode", mode.ToString());
				if(!stackingEnabled)
					writer.WriteAttributeString("stacking", "disabled");

				foreach(Board board in boards) {
					writer.WriteStartElement("layout");
					if(board is Map) {
						MapProperties mapProperties = ((Map) board).Properties;
						if(mapProperties != null) {
							writer.WriteAttributeString("board", mapProperties.Name);
							if(mapProperties.Name != board.Name)
								writer.WriteAttributeString("tab", board.Name);
						} else {
							writer.WriteAttributeString("tab", board.Name);
						}
					} else {
						CounterSheetProperties counterSheetProperties = ((CounterSheet) board).Properties;
						writer.WriteAttributeString("board", counterSheetProperties.Name);
						if(counterSheetProperties.Name != board.Name)
							writer.WriteAttributeString("tab", board.Name);
					}
					writer.WriteAttributeString("left", board.VisibleArea.Left.ToString("F2", NumberFormatInfo.InvariantInfo));
					writer.WriteAttributeString("top", board.VisibleArea.Top.ToString("F2", NumberFormatInfo.InvariantInfo));
					writer.WriteAttributeString("right", board.VisibleArea.Right.ToString("F2", NumberFormatInfo.InvariantInfo));
					writer.WriteAttributeString("bottom", board.VisibleArea.Bottom.ToString("F2", NumberFormatInfo.InvariantInfo));
					if(board is CounterSheet && ((CounterSheet) board).Side == Side.Back)
						writer.WriteAttributeString("side", Side.Back.ToString());
					if(board == visibleBoard)
						writer.WriteAttributeString("visible", "true");
					if(board.Owner != Guid.Empty)
						writer.WriteAttributeString("owner", board.Owner.ToString());

					foreach(Stack stack in board.Stacks) {
						if(!stack.AttachedToCounterSection) {
							if(stack.Pieces.Length == 1) {
								Piece piece = (Piece) stack.Pieces[0];
								writer.WriteStartElement("counter");
								writer.WriteAttributeString("id", XmlConvert.ToString(piece.Id));
								if(piece.Side == Side.Back)
									writer.WriteAttributeString("side", Side.Back.ToString());
								writer.WriteAttributeString("x", stack.Position.X.ToString("F2", NumberFormatInfo.InvariantInfo));
								writer.WriteAttributeString("y", stack.Position.Y.ToString("F2", NumberFormatInfo.InvariantInfo));
								if(piece.RotationAngle != 0.0f)
									writer.WriteAttributeString("rot", (piece.RotationAngle * (180.0f / (float) Math.PI)).ToString("F0", NumberFormatInfo.InvariantInfo));
								if (piece.Owner != Guid.Empty)
									writer.WriteAttributeString("owner", piece.Owner.ToString());
								writer.WriteEndElement();
							} else {
								writer.WriteStartElement("stack");
								writer.WriteAttributeString("x", stack.Position.X.ToString("F2", NumberFormatInfo.InvariantInfo));
								writer.WriteAttributeString("y", stack.Position.Y.ToString("F2", NumberFormatInfo.InvariantInfo));
								for(int i = 0; i < stack.Pieces.Length; ++i) {
									Piece piece = (Piece) stack.Pieces[i];
									writer.WriteStartElement("counter");
									writer.WriteAttributeString("id", XmlConvert.ToString(piece.Id));
									if(piece.Side == Side.Back)
										writer.WriteAttributeString("side", Side.Back.ToString());
									if(piece.RotationAngle != 0.0f)
										writer.WriteAttributeString("rot", (piece.RotationAngle * (180.0f / (float) Math.PI)).ToString("F0", NumberFormatInfo.InvariantInfo));
									if (piece.Owner != Guid.Empty)
										writer.WriteAttributeString("owner", piece.Owner.ToString());
									writer.WriteEndElement();
								}
								writer.WriteEndElement();
							}
						}
					}
					writer.WriteEndElement();
				}

				foreach(KeyValuePair<Guid, PlayerHand> playerHand in playerHands) {
					writer.WriteStartElement("hand");
					writer.WriteAttributeString("owner", playerHand.Key.ToString());
					if(playerHand.Value.Count > 0) {
						foreach(Piece piece in playerHand.Value.Pieces) {
							writer.WriteStartElement("counter");
							writer.WriteAttributeString("id", XmlConvert.ToString(piece.Id));
							if(piece.Side == Side.Back)
								writer.WriteAttributeString("side", Side.Back.ToString());
							if(piece.RotationAngle != 0.0f)
								writer.WriteAttributeString("rot", (piece.RotationAngle * (180.0f / (float) Math.PI)).ToString("F0", NumberFormatInfo.InvariantInfo));
							writer.WriteEndElement();
						}
					}
					writer.WriteEndElement();
				}

				writer.WriteEndElement();

				writer.WriteEndDocument();
			} finally {
				if(writer != null) writer.Close();
			}
		}

		/// <summary>Returns the player hand matching a given id.</summary>
		/// <param name="guid">Persistent id of the player whose hand is to be returned.</param>
		/// <returns>A player hand or null if not found.</returns>
		public IPlayerHand GetPlayerHand(Guid guid) {
			PlayerHand playerHand;
			return (playerHands.TryGetValue(guid, out playerHand) ? playerHand : null);
		}

		/// <summary>Adds a player hand.</summary>
		/// <param name="guid">Persistent id of the player whose hand will be added.</param>
		/// <returns>The newly created player hand.</returns>
		public IPlayerHand AddPlayerHand(Guid guid) {
			PlayerHand playerHand = new PlayerHand();
			playerHands.Add(guid, playerHand);
			return playerHand;
		}

		/// <summary>Removes a player hand.</summary>
		/// <param name="guid">Persistent id of the player whose hand will be removed.</param>
		public void RemovePlayerHand(Guid guid) {
			PlayerHand playerHand;
			if(playerHands.TryGetValue(guid, out playerHand)) {
				if(playerHand.Stack != null)
					throw new InvalidOperationException();
				playerHands.Remove(guid);
			}
		}

		/// <summary>The operating mode of the user interface.</summary>
		public Mode Mode { get { return mode; } set { mode = value; } }
		private Mode mode = Mode.Default;

		/// <summary>Indicates if stacking happens when pieces are dropped on each others.</summary>
		public bool StackingEnabled { get { return stackingEnabled; } set { stackingEnabled = value; } }
		private bool stackingEnabled = true;

		/// <summary>Retrieves game box name and hash value from a game data stream.</summary>
		/// <param name="inputStream">The stream from which to read the game data.</param>
		/// <param name="gameBoxName">The variable to hold the game box name.</param>
		/// <param name="gameBoxHash">The variable to hold the game box hash value (null if it wasn't found).</param>
		static internal void RetrieveGameBoxInfo(Stream inputStream, out string gameBoxName, out byte[] gameBoxHash) {
			XmlDocument xml = new XmlDocument();
			xml.Load(inputStream);
			XmlElement gameNode = xml.DocumentElement;
			if(gameNode.SelectSingleNode("game-box") != null) {
				// old format
				XmlElement gameBoxNode = (XmlElement) gameNode.SelectSingleNode("game-box");
				gameBoxName = gameBoxNode.InnerText;
				gameBoxHash = (gameBoxNode.HasAttribute("hash") ?
					Convert.FromBase64String(gameBoxNode.GetAttribute("hash")) :
					null);
			} else {
				// new format
				gameBoxName = gameNode.GetAttribute("game-box");
				gameBoxHash = (gameNode.HasAttribute("hash") ?
					Convert.FromBase64String(gameNode.GetAttribute("hash")) :
					null);
			}
		}

		internal Game(GameBox gameBox, ScenarioType scenarioType, string fileName, Stream stream) {
			this.gameBox = gameBox;
			this.scenarioType = scenarioType;
			this.fileName = fileName;

			XmlDocument xml = new XmlDocument();
			xml.Load(stream);

			// Dice

			diceHands = new DiceHand[gameBox.DiceHands.Length];
			for(int i = 0; i < diceHands.Length; ++i) {
				diceHands[i].DiceType = gameBox.DiceHands[i].DiceType;
				diceHands[i].BeingCast = false;
				int diceCount = gameBox.DiceHands[i].Count;
				diceHands[i].Dice = new Die[diceCount];
				for(int j = 0; j < diceCount; ++j) {
					diceHands[i].Dice[j] = (gameBox.DiceHands[i].TextureFileName[j] != null ?
						new Die(gameBox.DiceHands[i].TextureFileName[j]) :
						new Die(gameBox.DiceHands[i].Colors[j], gameBox.DiceHands[i].Pips[j]));
				}
			}

			XmlElement gameNode = xml.DocumentElement;
			if(gameNode.SelectSingleNode("game-box") != null) {
				// old format

				// Scenario data

				scenarioName = xml.SelectSingleNode("/game/scenario/name").InnerText;
				XmlElement descriptionNode = (XmlElement) xml.SelectSingleNode("/game/scenario/description");
				scenarioDescription = (descriptionNode != null ? descriptionNode.InnerText : null);
				XmlElement copyrightNode = (XmlElement) xml.SelectSingleNode("/game/scenario/copyright");
				scenarioCopyright = (copyrightNode != null ? copyrightNode.InnerText : null);

				// Game data
				XmlNodeList layoutNodeList = gameNode.SelectNodes("layout");
				boards = new Board[layoutNodeList.Count];
				List<Piece> pieceList = new List<Piece>();
				for(int i = 0; i < boards.Length; ++i) {
					string boardId = ((XmlElement) layoutNodeList[i]).GetAttribute("board");
					string boardName = ((XmlElement) xml.SelectSingleNode("/game/scenario/map[@id='" + boardId + "']|/game/scenario/counter-sheet[@id='" + boardId + "']")).InnerText;

					// retrieve board properties
					foreach(MapProperties properties in gameBox.Maps) {
						if(properties.Name == boardName) {
							boards[i] = new Map(i, properties);
							break;
						}
					}
					if(boards[i] == null) {
						foreach(CounterSheetProperties properties in gameBox.CounterSheets) {
							if(properties.Name == boardName) {
								CounterSheet counterSheet = new CounterSheet(i, properties, pieceList);
								boards[i] = counterSheet;

								// register each piece's stack as belonging to the counter sheet
								foreach(ICounterSection counterSection in counterSheet.CounterSections) {
									foreach(Piece piece in counterSection.Pieces) {
										Stack stack = piece.Stack;
										counterSheet.MoveStackToBack(stack);
										stack.AttachedToCounterSection = true;
									}
								}

								break;
							}
						}
					}
				}
				allPieces = pieceList.ToArray();
				visibleBoard = boards[0];

				for(int i = 0; i < layoutNodeList.Count; ++i) {
					XmlElement layoutNode = (XmlElement) layoutNodeList[i];
					Board board = boards[i];

					board.VisibleArea = RectangleF.FromLTRB(
						XmlConvert.ToSingle(layoutNode.GetAttribute("left")),
						XmlConvert.ToSingle(layoutNode.GetAttribute("top")),
						XmlConvert.ToSingle(layoutNode.GetAttribute("right")),
						XmlConvert.ToSingle(layoutNode.GetAttribute("bottom")));
					if(board is CounterSheet) {
						if(layoutNode.HasAttribute("side")) {
							((CounterSheet) board).Side = (Side) Enum.Parse(typeof(Side), layoutNode.GetAttribute("side"));
						} else {
							((CounterSheet) board).Side = Side.Front;
						}
					}
					if(layoutNode.HasAttribute("visible") && layoutNode.GetAttribute("visible") == "true") {
						visibleBoard = board;
					}

					foreach(XmlElement pieceNode in layoutNode.SelectNodes("counter")) {
						Piece piece = (Piece) GetPieceById(XmlConvert.ToInt32(pieceNode.GetAttribute("id")));
						Stack stack = (Stack) piece.Stack;
						stack.AttachedToCounterSection = false;
						if(pieceNode.HasAttribute("rot"))
							piece.RotationAngle = XmlConvert.ToSingle(pieceNode.GetAttribute("rot")) * (float) Math.PI / 180.0f;
						if(pieceNode.HasAttribute("side"))
							piece.Side = (Side) Enum.Parse(typeof(Side), pieceNode.GetAttribute("side"));
						else
							piece.Side = Side.Front;
						board.MoveStackToFront(stack);
						stack.Position = new PointF(
							XmlConvert.ToSingle(pieceNode.GetAttribute("x")),
							XmlConvert.ToSingle(pieceNode.GetAttribute("y")));
					}

					foreach(XmlElement stackNode in layoutNode.SelectNodes("stack")) {
						Stack stack = null;
						foreach(XmlElement pieceNode in stackNode.SelectNodes("counter")) {
							Piece piece = (Piece) GetPieceById(XmlConvert.ToInt32(pieceNode.GetAttribute("id")));

							if(stack == null) {
								stack = (Stack) piece.Stack;
								stack.AttachedToCounterSection = false;
								board.MoveStackToFront(stack);
								stack.Position = new PointF(
									XmlConvert.ToSingle(stackNode.GetAttribute("x")),
									XmlConvert.ToSingle(stackNode.GetAttribute("y")));
							} else {
								Stack stackToMerge = (Stack) piece.Stack;
								((Board) stackToMerge.Board).RemoveStack(stackToMerge);
								stack.MergeToPosition(stackToMerge, stackToMerge.Pieces, stack.Pieces.Length);
							}

							if(pieceNode.HasAttribute("rot"))
								piece.RotationAngle = XmlConvert.ToSingle(pieceNode.GetAttribute("rot")) * ((float) Math.PI / 180.0f);
							if(pieceNode.HasAttribute("side"))
								piece.Side = (Side) Enum.Parse(typeof(Side), pieceNode.GetAttribute("side"));
							else
								piece.Side = Side.Front;
						}
					}
				}

			} else {
				// new format

				// Scenario data

				scenarioName = gameNode.GetAttribute("scenario-name");
				scenarioDescription = gameNode.GetAttribute("scenario-description");
				scenarioCopyright = gameNode.GetAttribute("scenario-copyright");

				// Operating mode
				mode = (gameNode.HasAttribute("mode") ?
					(Mode) Enum.Parse(typeof(Mode), gameNode.GetAttribute("mode")) :
					Mode.Default);

				// Stacking
				stackingEnabled = (!gameNode.HasAttribute("stacking") || gameNode.GetAttribute("stacking") != "disabled");

				// Game data
				XmlNodeList layoutNodeList = gameNode.SelectNodes("layout");
				boards = new Board[layoutNodeList.Count];
				List<Piece> pieceList = new List<Piece>();
				for(int i = 0; i < boards.Length; ++i) {
					XmlElement layoutNode = (XmlElement) layoutNodeList[i];

					string boardName = layoutNode.GetAttribute("board");

					// retrieve board properties
					foreach(MapProperties properties in gameBox.Maps) {
						if(properties.Name == boardName) {
							boards[i] = new Map(i, properties);
							break;
						}
					}
					if(boards[i] == null) {
						foreach(CounterSheetProperties properties in gameBox.CounterSheets) {
							if(properties.Name == boardName) {
								CounterSheet counterSheet = new CounterSheet(i, properties, pieceList);
								boards[i] = counterSheet;

								// register each piece's stack as belonging to the counter sheet
								foreach(ICounterSection counterSection in counterSheet.CounterSections) {
									foreach(Piece piece in counterSection.Pieces) {
										Stack stack = piece.Stack;
										counterSheet.MoveStackToBack(stack);
										stack.AttachedToCounterSection = true;
									}
								}

								break;
							}
						}
					}
					if(boards[i] == null) {
						boards[i] = new Map(i, null);
					}
					if(layoutNode.HasAttribute("tab"))
						boards[i].Name = layoutNode.GetAttribute("tab");
				}
				allPieces = pieceList.ToArray();
				visibleBoard = boards[0];

				for(int i = 0; i < layoutNodeList.Count; ++i) {
					XmlElement layoutNode = (XmlElement) layoutNodeList[i];
					Board board = boards[i];

					board.VisibleArea = RectangleF.FromLTRB(
						(layoutNode.HasAttribute("left") ? XmlConvert.ToSingle(layoutNode.GetAttribute("left")) : 0.0f),
						(layoutNode.HasAttribute("top") ? XmlConvert.ToSingle(layoutNode.GetAttribute("top")) : 0.0f),
						(layoutNode.HasAttribute("right") ? XmlConvert.ToSingle(layoutNode.GetAttribute("right")) : 1024.0f * 4),
						(layoutNode.HasAttribute("bottom") ? XmlConvert.ToSingle(layoutNode.GetAttribute("bottom")) : 768.0f * 4));
					if(board is CounterSheet) {
						if(layoutNode.HasAttribute("side")) {
							((CounterSheet) board).Side = (Side) Enum.Parse(typeof(Side), layoutNode.GetAttribute("side"));
						} else {
							((CounterSheet) board).Side = Side.Front;
						}
					}
					if(layoutNode.HasAttribute("visible") && layoutNode.GetAttribute("visible") == "true") {
						visibleBoard = board;
					}
					if(layoutNode.HasAttribute("owner")) {
						board.Owner = new Guid(layoutNode.GetAttribute("owner"));
					}

					foreach(XmlElement pieceNode in layoutNode.SelectNodes("counter")) {
						Piece piece = (Piece) GetPieceById(XmlConvert.ToInt32(pieceNode.GetAttribute("id")));
						if(piece is TerrainPrototype)
							piece = new TerrainClone((TerrainPrototype) piece);
						Stack stack = (Stack) piece.Stack;
						stack.AttachedToCounterSection = false;
						if(pieceNode.HasAttribute("rot"))
							piece.RotationAngle = XmlConvert.ToSingle(pieceNode.GetAttribute("rot")) * (float) Math.PI / 180.0f;
						if(pieceNode.HasAttribute("side"))
							piece.Side = (Side) Enum.Parse(typeof(Side), pieceNode.GetAttribute("side"));
						else
							piece.Side = Side.Front;
						board.MoveStackToFront(stack);
						stack.Position = new PointF(
							XmlConvert.ToSingle(pieceNode.GetAttribute("x")),
							XmlConvert.ToSingle(pieceNode.GetAttribute("y")));
						if (pieceNode.HasAttribute("owner"))
							piece.Owner = new Guid(pieceNode.GetAttribute("owner"));
					}

					foreach(XmlElement stackNode in layoutNode.SelectNodes("stack")) {
						Stack stack = null;
						foreach(XmlElement pieceNode in stackNode.SelectNodes("counter")) {
							Piece piece = (Piece) GetPieceById(XmlConvert.ToInt32(pieceNode.GetAttribute("id")));
							if(piece is TerrainPrototype)
								piece = new TerrainClone((TerrainPrototype) piece);

							if(stack == null) {
								stack = (Stack) piece.Stack;
								stack.AttachedToCounterSection = false;
								board.MoveStackToFront(stack);
								stack.Position = new PointF(
									XmlConvert.ToSingle(stackNode.GetAttribute("x")),
									XmlConvert.ToSingle(stackNode.GetAttribute("y")));
							} else {
								Stack stackToMerge = (Stack) piece.Stack;
								((Board) stackToMerge.Board).RemoveStack(stackToMerge);
								stack.MergeToPosition(stackToMerge, stackToMerge.Pieces, stack.Pieces.Length);
							}

							if(pieceNode.HasAttribute("rot"))
								piece.RotationAngle = XmlConvert.ToSingle(pieceNode.GetAttribute("rot")) * ((float) Math.PI / 180.0f);
							if(pieceNode.HasAttribute("side"))
								piece.Side = (Side) Enum.Parse(typeof(Side), pieceNode.GetAttribute("side"));
							else
								piece.Side = Side.Front;

							if (pieceNode.HasAttribute("owner"))
								piece.Owner = new Guid(pieceNode.GetAttribute("owner"));
						}
					}
				}

				foreach(XmlElement handNode in gameNode.SelectNodes("hand")) {
					Guid guid = new Guid(handNode.GetAttribute("owner"));
					PlayerHand hand = new PlayerHand();
					Stack stack = null;
					foreach(XmlElement pieceNode in handNode.SelectNodes("counter")) {
						Piece piece = (Piece) GetPieceById(XmlConvert.ToInt32(pieceNode.GetAttribute("id")));
						if(piece is TerrainPrototype)
							piece = new TerrainClone((TerrainPrototype) piece);

						if(stack == null) {
							stack = (Stack) piece.Stack;
							if(stack.Board != null)
								((Board) stack.Board).RemoveStack(stack);
							stack.AttachedToCounterSection = false;
						} else {
							Stack stackToMerge = (Stack) piece.Stack;
							if(stackToMerge.Board != null)
								((Board) stackToMerge.Board).RemoveStack(stackToMerge);
							stack.MergeToPosition(stackToMerge, stackToMerge.Pieces, stack.Pieces.Length);
						}

						if(pieceNode.HasAttribute("rot"))
							piece.RotationAngle = XmlConvert.ToSingle(pieceNode.GetAttribute("rot")) * ((float) Math.PI / 180.0f);
						if(pieceNode.HasAttribute("side"))
							piece.Side = (Side) Enum.Parse(typeof(Side), pieceNode.GetAttribute("side"));
						else
							piece.Side = Side.Front;

						if (pieceNode.HasAttribute("owner"))
							piece.Owner = new Guid(pieceNode.GetAttribute("owner"));
					}
					hand.Stack = stack;
					playerHands.Add(guid, hand);
				}
			}
		}

		/// <summary>All the dice sets used by this game.</summary>
		public DiceHand[] DiceHands { get { return diceHands; } }
		private DiceHand[] diceHands;

		/// <summary>All the player hands that are not hidden.</summary>
		private Dictionary<Guid, PlayerHand> playerHands = new Dictionary<Guid, PlayerHand>();
	}
}
