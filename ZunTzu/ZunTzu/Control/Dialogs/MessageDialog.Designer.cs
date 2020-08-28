namespace ZunTzu.Control.Dialogs {
	partial class MessageDialog {
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
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.okButton = new System.Windows.Forms.Button();
			this.messageLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize) (this.pictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox
			// 
			this.pictureBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.pictureBox.Location = new System.Drawing.Point(35, 16);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(32, 32);
			this.pictureBox.TabIndex = 12;
			this.pictureBox.TabStop = false;
			this.pictureBox.Visible = false;
			// 
			// okButton
			// 
			this.okButton.CausesValidation = false;
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.okButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.okButton.Location = new System.Drawing.Point(144, 86);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 11;
			this.okButton.Text = "OK";
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// messageLabel
			// 
			this.messageLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.messageLabel.Location = new System.Drawing.Point(75, 16);
			this.messageLabel.Name = "messageLabel";
			this.messageLabel.Size = new System.Drawing.Size(251, 67);
			this.messageLabel.TabIndex = 10;
			this.messageLabel.Text = "Message";
			this.messageLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// MessageDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.okButton;
			this.ClientSize = new System.Drawing.Size(360, 124);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.messageLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "MessageDialog";
			this.Text = "MessageDialog";
			((System.ComponentModel.ISupportInitialize) (this.pictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Label messageLabel;
	}
}