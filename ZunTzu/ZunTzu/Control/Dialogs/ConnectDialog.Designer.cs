namespace ZunTzu.Control.Dialogs {
	partial class ConnectDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectDialog));
            this.hostTextBox = new System.Windows.Forms.TextBox();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // hostTextBox
            // 
            this.hostTextBox.AccessibleDescription = null;
            this.hostTextBox.AccessibleName = null;
            resources.ApplyResources(this.hostTextBox, "hostTextBox");
            this.hostTextBox.BackgroundImage = null;
            this.hostTextBox.Font = null;
            this.hostTextBox.Name = "hostTextBox";
            // 
            // portTextBox
            // 
            this.portTextBox.AccessibleDescription = null;
            this.portTextBox.AccessibleName = null;
            resources.ApplyResources(this.portTextBox, "portTextBox");
            this.portTextBox.BackgroundImage = null;
            this.portTextBox.Font = null;
            this.portTextBox.Name = "portTextBox";
            // 
            // label4
            // 
            this.label4.AccessibleDescription = null;
            this.label4.AccessibleName = null;
            resources.ApplyResources(this.label4, "label4");
            this.label4.Font = null;
            this.label4.Name = "label4";
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
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
            // ConnectDialog
            // 
            this.AcceptButton = this.okButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.hostTextBox);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "ConnectDialog";
            this.Load += new System.EventHandler(this.ConnectDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox hostTextBox;
		private System.Windows.Forms.TextBox portTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
	}
}