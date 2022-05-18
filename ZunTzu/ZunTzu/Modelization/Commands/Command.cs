// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using ZunTzu.Visualization;

namespace ZunTzu.Modelization {

	/// <summary>Summary description for Command.</summary>
	public abstract class Command : ICommand {

		/// <summary>Constructor.</summary>
		protected Command(IModel model) {
			this.model = model;
		}

		public abstract void Do();

		public abstract void Undo();

		/// <summary>Rollback the previous cancellation of this command.</summary>
		public virtual void Redo() {
			Do();
		}

		protected void preventConflict(IStack stack) {
			if(model.AnimationManager.IsBeingAnimated(stack))
				model.AnimationManager.EndAllAnimations();

			foreach(IPlayer player in model.Players) {
				if(player.StackBeingDragged != null) {
					if(player.StackBeingDragged.Stack == stack)
						player.StackBeingDragged = null;
				} else if(player.PieceBeingDragged != null) {
					if(player.PieceBeingDragged.Stack == stack)
						player.PieceBeingDragged = null;
				}
			}
		}

		protected void preventConflict(IStack stack0, IStack stack1) {
			if(model.AnimationManager.IsBeingAnimated(stack0) || model.AnimationManager.IsBeingAnimated(stack1))
				model.AnimationManager.EndAllAnimations();

			foreach(IPlayer player in model.Players) {
				if(player.StackBeingDragged != null) {
					if(player.StackBeingDragged.Stack == stack0 || player.StackBeingDragged.Stack == stack1)
						player.StackBeingDragged = null;
				} else if(player.PieceBeingDragged != null) {
					if(player.PieceBeingDragged.Stack == stack0 || player.PieceBeingDragged.Stack == stack1)
						player.PieceBeingDragged = null;
				}
			}
		}

		protected void preventConflict(IStack stack0, IStack stack1, IStack stack2) {
			if(model.AnimationManager.IsBeingAnimated(stack0) || model.AnimationManager.IsBeingAnimated(stack1) || model.AnimationManager.IsBeingAnimated(stack2))
				model.AnimationManager.EndAllAnimations();

			foreach(IPlayer player in model.Players) {
				if(player.StackBeingDragged != null) {
					if(player.StackBeingDragged.Stack == stack0 || player.StackBeingDragged.Stack == stack1 || player.StackBeingDragged.Stack == stack2)
						player.StackBeingDragged = null;
				} else if(player.PieceBeingDragged != null) {
					if(player.PieceBeingDragged.Stack == stack0 || player.PieceBeingDragged.Stack == stack1 || player.PieceBeingDragged.Stack == stack2)
						player.PieceBeingDragged = null;
				}
			}
		}

		protected IModel model;
	}
}
