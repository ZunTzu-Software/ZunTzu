// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.IO;

namespace ZunTzu.Modelization.Animations {

	/// <summary>An animation.</summary>
	public sealed class SaveGameAnimation : InstantaneousAnimation {

		/// <summary>Constructor</summary>
		public SaveGameAnimation(string fileName) {
			this.fileName = fileName;
		}

		/// <summary>Called once when time is EndTimeInMicroseconds.</summary>
		protected override sealed void SetFinalState(IModel model) {
			if(model.CurrentGameBox.Reference != model.GameLibrary.DefaultGameBox) {
				string temporaryFileName = Path.GetTempFileName();
				using(Stream stream = File.Open(temporaryFileName, FileMode.Create, FileAccess.Write)) {
					model.CurrentGameBox.CurrentGame.Save(stream, true);
				}
				if(File.Exists(fileName))
					File.Delete(fileName);
				File.Move(temporaryFileName, fileName);
			}
		}

		private string fileName;
	}
}
