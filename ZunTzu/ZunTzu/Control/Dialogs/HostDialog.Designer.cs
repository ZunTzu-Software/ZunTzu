namespace ZunTzu.Control.Dialogs {
	partial class HostDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HostDialog));
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.copyToClipboardCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
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
            this.okButton.TabStop = false;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // copyToClipboardCheckBox
            // 
            this.copyToClipboardCheckBox.AccessibleDescription = null;
            this.copyToClipboardCheckBox.AccessibleName = null;
            resources.ApplyResources(this.copyToClipboardCheckBox, "copyToClipboardCheckBox");
            this.copyToClipboardCheckBox.BackgroundImage = null;
            this.copyToClipboardCheckBox.Checked = true;
            this.copyToClipboardCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.copyToClipboardCheckBox.Font = null;
            this.copyToClipboardCheckBox.Name = "copyToClipboardCheckBox";
            this.copyToClipboardCheckBox.UseVisualStyleBackColor = true;
            // 
            // HostDialog
            // 
            this.AcceptButton = this.okButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.copyToClipboardCheckBox);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "HostDialog";
            this.Load += new System.EventHandler(this.HostDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox portTextBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.CheckBox copyToClipboardCheckBox;
	}
}