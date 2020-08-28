// Copyright (c) 2020 ZunTzu Software and contributors

using System;
using System.Reflection;
using ZunTzu.Graphics;
using ZunTzu.Visualization;

namespace ZunTzu.Control.Messages {

	/// <summary>GameAspectRatioChangedMessage.</summary>
	[ObfuscationAttribute(Exclude = true, ApplyToMembers = false)]
	public sealed class GameAspectRatioChangedMessage : Message {

		internal GameAspectRatioChangedMessage() { }

		public GameAspectRatioChangedMessage(bool widescreen) {
			this.widescreen = widescreen;
		}

		public override NetworkMessageType Type { get { return NetworkMessageType.GameAspectRatioChanged; } }

		protected sealed override void SerializeDeserialize(ISerializer serializer) {
			serializer.Serialize(ref widescreen);
		}

		public sealed override void Handle(Controller controller) {
			DisplayProperties properties = controller.View.DisplayProperties;
			if(widescreen != (properties.GameAspectRatio == AspectRatioType.SixteenToTen)) {
				properties.GameAspectRatio = (widescreen ? AspectRatioType.SixteenToTen : AspectRatioType.FourToThree);
				controller.View.DisplayProperties = properties;
			}
		}

		private bool widescreen;
	}
}
