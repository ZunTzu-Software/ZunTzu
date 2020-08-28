// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Reflection;
using ZunTzu.Modelization;
using ZunTzu.Modelization.Animations;

namespace ZunTzu.Control.Messages {

	/// <summary>MouseMovedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class MouseMovedMessage : Message {

		internal MouseMovedMessage() {}

		public MouseMovedMessage(PointF position) {
			this.position = position;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.MouseMoved; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref position);
		}

		public sealed override void Handle(Controller controller) {
			IPlayer sender = controller.Model.GetPlayer(senderId);
			if(sender != null) {
				Point screenPosition = Point.Truncate(controller.View.ConvertModelToScreenCoordinates(position));
				controller.Model.AnimationManager.LaunchAnimationSequence(
					new SmoothMouseAnimation(sender, screenPosition));
			}
		}

		private PointF position;
	}
}
