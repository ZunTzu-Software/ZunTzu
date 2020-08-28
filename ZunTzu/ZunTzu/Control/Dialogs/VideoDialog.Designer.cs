namespace ZunTzu.Control.Dialogs {
	partial class VideoDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VideoDialog));
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.enableVideoCaptureCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
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
            // okButton
            // 
            this.okButton.AccessibleDescription = null;
            this.okButton.AccessibleName = null;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.BackgroundImage = null;
            this.okButton.CausesValidation = false;
            this.okButton.Font = null;
            this.okButton.Name = "okButton";
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // enableVideoCaptureCheckBox
            // 
            this.enableVideoCaptureCheckBox.AccessibleDescription = null;
            this.enableVideoCaptureCheckBox.AccessibleName = null;
            resources.ApplyResources(this.enableVideoCaptureCheckBox, "enableVideoCaptureCheckBox");
            this.enableVideoCaptureCheckBox.BackgroundImage = null;
            this.enableVideoCaptureCheckBox.Font = null;
            this.enableVideoCaptureCheckBox.Name = "enableVideoCaptureCheckBox";
            this.enableVideoCaptureCheckBox.UseVisualStyleBackColor = true;
            // 
            // VideoDialog
            // 
            this.AcceptButton = this.okButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.enableVideoCaptureCheckBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "VideoDialog";
            this.Load += new System.EventHandler(this.VideoDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.CheckBox enableVideoCaptureCheckBox;
	}
}