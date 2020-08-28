namespace ZunTzu.Visualization {
	partial class ErrorForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.showReportContent = new System.Windows.Forms.LinkLabel();
            this.label4 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AccessibleDescription = null;
            this.panel1.AccessibleName = null;
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.BackgroundImage = null;
            this.panel1.Controls.Add(this.pictureBox);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Font = null;
            this.panel1.Name = "panel1";
            // 
            // pictureBox
            // 
            this.pictureBox.AccessibleDescription = null;
            this.pictureBox.AccessibleName = null;
            resources.ApplyResources(this.pictureBox, "pictureBox");
            this.pictureBox.BackColor = System.Drawing.SystemColors.Window;
            this.pictureBox.BackgroundImage = null;
            this.pictureBox.Font = null;
            this.pictureBox.ImageLocation = null;
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.TabStop = false;
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            this.label2.AccessibleDescription = null;
            this.label2.AccessibleName = null;
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            this.label3.AccessibleDescription = null;
            this.label3.AccessibleName = null;
            resources.ApplyResources(this.label3, "label3");
            this.label3.Font = null;
            this.label3.Name = "label3";
            // 
            // showReportContent
            // 
            this.showReportContent.AccessibleDescription = null;
            this.showReportContent.AccessibleName = null;
            resources.ApplyResources(this.showReportContent, "showReportContent");
            this.showReportContent.Font = null;
            this.showReportContent.Name = "showReportContent";
            this.showReportContent.TabStop = true;
            this.showReportContent.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.showReportContent_LinkClicked);
            // 
            // label4
            // 
            this.label4.AccessibleDescription = null;
            this.label4.AccessibleName = null;
            resources.ApplyResources(this.label4, "label4");
            this.label4.Font = null;
            this.label4.Name = "label4";
            // 
            // okButton
            // 
            this.okButton.AccessibleDescription = null;
            this.okButton.AccessibleName = null;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.BackgroundImage = null;
            this.okButton.CausesValidation = false;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Font = null;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
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
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // ErrorForm
            // 
            this.AcceptButton = this.okButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.showReportContent);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panel1);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.LinkLabel showReportContent;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
	}
}