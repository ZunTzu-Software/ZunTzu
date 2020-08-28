// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Control.Messages {

	/// <summary>VisibleAreaChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class VisibleAreaChangedMessage : Message {

		internal VisibleAreaChangedMessage() {}

		public VisibleAreaChangedMessage(PointF mousePosition, int visibleBoardId, RectangleF visibleArea) {
			this.mousePosition = mousePosition;
			this.visibleBoardId = visibleBoardId;
			this.visibleArea = visibleArea;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.VisibleAreaChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref mousePosition);
			serializer.Serialize(ref visibleBoardId);
			serializer.Serialize(ref visibleArea);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				// compute mouse screen position in new referential
				Rectangle gameDisplayArea = controller.View.GameDisplayAreaInPixels;
				Point mouseScreenPosition = Point.Truncate(new PointF(
					gameDisplayArea.X + (mousePosition.X - visibleArea.X) * gameDisplayArea.Width / visibleArea.Width,
					gameDisplayArea.Y + (mousePosition.Y - visibleArea.Y) * gameDisplayArea.Height / visibleArea.Height));

				IGame game = controller.Model.CurrentGameBox.CurrentGame;
				IBoard board = game.GetBoardById(visibleBoardId);

				controller.Model.AnimationManager.LaunchAnimationSequence(
					new SmoothScrollAnimation(board, visibleArea, sender, mouseScreenPosition));
			}
		}

		private PointF mousePosition;
		private int visibleBoardId;
		private RectangleF visibleArea;
	}
}
