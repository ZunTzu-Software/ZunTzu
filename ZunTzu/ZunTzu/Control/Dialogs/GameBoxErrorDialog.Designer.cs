namespace ZunTzu.Control.Dialogs {
	partial class GameBoxErrorDialog {
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameBoxErrorDialog));
            this.largeImageList = new System.Windows.Forms.ImageList(this.components);
            this.gameBoxPathLabel = new System.Windows.Forms.Label();
            this.smallImageList = new System.Windows.Forms.ImageList(this.components);
            this.listView = new System.Windows.Forms.ListView();
            this.description = new System.Windows.Forms.ColumnHeader();
            this.abortButton = new System.Windows.Forms.Button();
            this.addButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // largeImageList
            // 
            this.largeImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("largeImageList.ImageStream")));
            this.largeImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.largeImageList.Images.SetKeyName(0, "Error");
            this.largeImageList.Images.SetKeyName(1, "Warning");
            // 
            // gameBoxPathLabel
            // 
            this.gameBoxPathLabel.AccessibleDescription = null;
            this.gameBoxPathLabel.AccessibleName = null;
            resources.ApplyResources(this.gameBoxPathLabel, "gameBoxPathLabel");
            this.gameBoxPathLabel.Font = null;
            this.gameBoxPathLabel.Name = "gameBoxPathLabel";
            // 
            // smallImageList
            // 
            this.smallImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("smallImageList.ImageStream")));
            this.smallImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.smallImageList.Images.SetKeyName(0, "Error");
            this.smallImageList.Images.SetKeyName(1, "Warning");
            // 
            // listView
            // 
            this.listView.AccessibleDescription = null;
            this.listView.AccessibleName = null;
            resources.ApplyResources(this.listView, "listView");
            this.listView.BackgroundImage = null;
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.description});
            this.listView.Font = null;
            this.listView.FullRowSelect = true;
            this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView.LargeImageList = this.largeImageList;
            this.listView.Name = "listView";
            this.listView.SmallImageList = this.smallImageList;
            this.listView.TileSize = new System.Drawing.Size(518, 20);
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            // 
            // description
            // 
            resources.ApplyResources(this.description, "description");
            // 
            // abortButton
            // 
            this.abortButton.AccessibleDescription = null;
            this.abortButton.AccessibleName = null;
            resources.ApplyResources(this.abortButton, "abortButton");
            this.abortButton.BackgroundImage = null;
            this.abortButton.CausesValidation = false;
            this.abortButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.abortButton.Font = null;
            this.abortButton.Name = "abortButton";
            this.abortButton.Click += new System.EventHandler(this.abortButton_Click);
            // 
            // addButton
            // 
            this.addButton.AccessibleDescription = null;
            this.addButton.AccessibleName = null;
            this.addButton.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            resources.ApplyResources(this.addButton, "addButton");
            this.addButton.BackgroundImage = null;
            this.addButton.CausesValidation = false;
            this.addButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.addButton.Font = null;
            this.addButton.Name = "addButton";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // label2
            // 
            this.label2.AccessibleDescription = null;
            this.label2.AccessibleName = null;
            resources.ApplyResources(this.label2, "label2");
            this.label2.Font = null;
            this.label2.Name = "label2";
            // 
            // GameBoxErrorDialog
            // 
            this.AcceptButton = this.addButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.abortButton;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.gameBoxPathLabel);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.abortButton);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "GameBoxErrorDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ImageList largeImageList;
		private System.Windows.Forms.Label gameBoxPathLabel;
		private System.Windows.Forms.ImageList smallImageList;
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Button abortButton;
		private System.Windows.Forms.Button addButton;
		private System.Windows.Forms.ColumnHeader description;
		private System.Windows.Forms.Label label2;
	}
}