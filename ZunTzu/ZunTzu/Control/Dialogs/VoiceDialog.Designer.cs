namespace ZunTzu.Control.Dialogs {
	partial class VoiceDialog {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VoiceDialog));
            this.okButton = new System.Windows.Forms.Button();
            this.muteSoundEffectsCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.CausesValidation = false;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.okButton.Name = "okButton";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // muteSoundEffectsCheckBox
            // 
            resources.ApplyResources(this.muteSoundEffectsCheckBox, "muteSoundEffectsCheckBox");
            this.muteSoundEffectsCheckBox.Name = "muteSoundEffectsCheckBox";
            // 
            // VoiceDialog
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.okButton;
            this.Controls.Add(this.muteSoundEffectsCheckBox);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "VoiceDialog";
            this.Load += new System.EventHandler(this.VoiceDialog_Load);
            this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.CheckBox muteSoundEffectsCheckBox;
    }
}