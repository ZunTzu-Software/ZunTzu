// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Collections.Generic;
using ZunTzu.Modelization.Animations;
using ZunTzu.Modelization.Commands;

namespace ZunTzu.Modelization {

	/// <summary>Used by the controller to execute user actions</summary>
	/// <remarks>
	/// Commands are grouped in atomic command sequences.
	/// Only whole sequences can be undone/redone.
	/// </remarks>
	internal sealed class CommandManager : ICommandManager {
		private IModel model;
		private List<CommandSequence> undoableCommands = new List<CommandSequence>();
		private Stack<CommandSequence> redoableCommands = new Stack<CommandSequence>();

		internal CommandManager(IModel model) {
			this.model = model;
		}

		private struct CommandSequence {
			public CommandSequence(CommandContext contextBefore, CommandContext contextAfter, ICommand[] commands) {
				ContextBefore = contextBefore;
				ContextAfter = contextAfter;
				Commands = commands;
			}
			public CommandContext ContextBefore;
			public CommandContext ContextAfter;
			public ICommand[] Commands;
		}

		/// <summary>Execute a sequence of commands.</summary>
		public void ExecuteCommandSequence(params ICommand[] commands) {
			CommandContext context = new CommandContext(model.CurrentGameBox.CurrentGame.VisibleBoard);
			ExecuteCommandSequence(context, context, commands);
		}

		/// <summary>Execute a sequence of commands.</summary>
		public void ExecuteCommandSequence(CommandContext contextBefore, CommandContext contextAfter, params ICommand[] commands) {
			redoableCommands.Clear();

			foreach(ICommand command in commands)
				command.Do();

			// aggregate command if possible
			if(commands.Length == 1 && undoableCommands.Count > 0) {
				AggregableCommand thisCommand = commands[0] as AggregableCommand;
				if(thisCommand != null) {
					CommandSequence previousCommandSequence = undoableCommands[undoableCommands.Count - 1];
					if(previousCommandSequence.Commands.Length == 1) {
						AggregableCommand previousCommand = previousCommandSequence.Commands[0] as AggregableCommand;
						if(previousCommand != null && previousCommand.CanAggregateWith(thisCommand)) {
							previousCommand.AggregateWith(thisCommand);
							return;
						}
					}
				}
			}

			// aggregation was not possible -> stack command normally
			if(undoableCommands.Count > 99)
				undoableCommands.RemoveAt(0);
			undoableCommands.Add(new CommandSequence(contextBefore, contextAfter, commands));
		}

		/// <summary>Cancel the result of the latest command.</summary>
		public void Undo() {
			if(undoableCommands.Count > 0) {
				CommandSequence commandSequence = undoableCommands[undoableCommands.Count - 1];
				restoreContext(commandSequence.ContextBefore);
				for(int i = commandSequence.Commands.Length - 1; i >= 0; --i)
					commandSequence.Commands[i].Undo();
				undoableCommands.RemoveAt(undoableCommands.Count - 1);
				redoableCommands.Push(commandSequence);
			}
		}

		/// <summary>Rollback the previous cancellation of the latest command.</summary>
		public void Redo() {
			if(redoableCommands.Count > 0) {
				CommandSequence commandSequence = redoableCommands.Pop();
				restoreContext(commandSequence.ContextAfter);
				foreach(ICommand command in commandSequence.Commands)
					command.Redo();
				undoableCommands.Add(commandSequence);
			}
		}

		/// <summary>Erases all previous commands from the undo stack.</summary>
		/// <remarks>Must be called when a game is loaded, or when a player joins.</remarks>
		public void ClearUndoStack() {
			undoableCommands.Clear();
			redoableCommands.Clear();
		}

		/// <summary>True if the latest command can be canceled.</summary>
		public bool CanUndo { get { return (undoableCommands.Count > 0); } }

		/// <summary>True if the previous cancellation can be rolledback.</summary>
		public bool CanRedo { get { return (redoableCommands.Count > 0); } }

		private void restoreContext(CommandContext context) {
			model.AnimationManager.LaunchAnimationSequence(new RestoreContextAnimation(context));
		}
	}
}
