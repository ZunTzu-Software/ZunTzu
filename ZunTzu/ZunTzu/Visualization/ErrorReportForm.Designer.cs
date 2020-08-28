namespace ZunTzu.Visualization {
	partial class ErrorReportForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorReportForm));
            this.label1 = new System.Windows.Forms.Label();
            this.contentTextBox = new System.Windows.Forms.TextBox();
            this.closeButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
            // 
            // contentTextBox
            // 
            this.contentTextBox.AccessibleDescription = null;
            this.contentTextBox.AccessibleName = null;
            resources.ApplyResources(this.contentTextBox, "contentTextBox");
            this.contentTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.contentTextBox.BackgroundImage = null;
            this.contentTextBox.Name = "contentTextBox";
            this.contentTextBox.ReadOnly = true;
            this.contentTextBox.TabStop = false;
            // 
            // closeButton
            // 
            this.closeButton.AccessibleDescription = null;
            this.closeButton.AccessibleName = null;
            resources.ApplyResources(this.closeButton, "closeButton");
            this.closeButton.BackgroundImage = null;
            this.closeButton.CausesValidation = false;
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Font = null;
            this.closeButton.Name = "closeButton";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // ErrorReportForm
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.contentTextBox);
            this.Controls.Add(this.label1);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = null;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorReportForm";
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox contentTextBox;
		private System.Windows.Forms.Button closeButton;
	}
}