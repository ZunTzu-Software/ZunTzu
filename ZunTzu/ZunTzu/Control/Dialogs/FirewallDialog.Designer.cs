namespace ZunTzu.Control.Dialogs {
	partial class FirewallDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FirewallDialog));
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.okButton = new System.Windows.Forms.Button();
            this.captionLabel = new System.Windows.Forms.Label();
            this.messageLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.AccessibleDescription = null;
            this.pictureBox.AccessibleName = null;
            resources.ApplyResources(this.pictureBox, "pictureBox");
            this.pictureBox.BackgroundImage = null;
            this.pictureBox.Font = null;
            this.pictureBox.ImageLocation = null;
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.TabStop = false;
            // 
            // okButton
            // 
            this.okButton.AccessibleDescription = null;
            this.okButton.AccessibleName = null;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.BackgroundImage = null;
            this.okButton.CausesValidation = false;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.okButton.Font = null;
            this.okButton.Name = "okButton";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // captionLabel
            // 
            this.captionLabel.AccessibleDescription = null;
            this.captionLabel.AccessibleName = null;
            resources.ApplyResources(this.captionLabel, "captionLabel");
            this.captionLabel.Name = "captionLabel";
            // 
            // messageLabel
            // 
            this.messageLabel.AccessibleDescription = null;
            this.messageLabel.AccessibleName = null;
            resources.ApplyResources(this.messageLabel, "messageLabel");
            this.messageLabel.Font = null;
            this.messageLabel.Name = "messageLabel";
            // 
            // cancelButton
            // 
            this.cancelButton.AccessibleDescription = null;
            this.cancelButton.AccessibleName = null;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.BackgroundImage = null;
            this.cancelButton.CausesValidation = false;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Font = null;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // FirewallDialog
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.captionLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.pictureBox);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "FirewallDialog";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Label captionLabel;
		private System.Windows.Forms.Label messageLabel;
		private System.Windows.Forms.Button cancelButton;
	}
}