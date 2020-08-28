namespace ZunTzu.Control.Dialogs {
	partial class QuitDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QuitDialog));
            this.cancelButton = new System.Windows.Forms.Button();
            this.noButton = new System.Windows.Forms.Button();
            this.yesButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
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
            // noButton
            // 
            this.noButton.AccessibleDescription = null;
            this.noButton.AccessibleName = null;
            resources.ApplyResources(this.noButton, "noButton");
            this.noButton.BackgroundImage = null;
            this.noButton.CausesValidation = false;
            this.noButton.Font = null;
            this.noButton.Name = "noButton";
            this.noButton.Click += new System.EventHandler(this.noButton_Click);
            // 
            // yesButton
            // 
            this.yesButton.AccessibleDescription = null;
            this.yesButton.AccessibleName = null;
            resources.ApplyResources(this.yesButton, "yesButton");
            this.yesButton.BackgroundImage = null;
            this.yesButton.CausesValidation = false;
            this.yesButton.Font = null;
            this.yesButton.Name = "yesButton";
            this.yesButton.Click += new System.EventHandler(this.yesButton_Click);
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
            // 
            // QuitDialog
            // 
            this.AcceptButton = this.yesButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.noButton);
            this.Controls.Add(this.yesButton);
            this.Controls.Add(this.label1);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "QuitDialog";
            this.Load += new System.EventHandler(this.QuitDialog_Load);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button noButton;
		private System.Windows.Forms.Button yesButton;
		private System.Windows.Forms.Label label1;
	}
}