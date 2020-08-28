namespace ZunTzu.Control.Dialogs {
	partial class LibraryDialog {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LibraryDialog));
            this.closeButton = new System.Windows.Forms.Button();
            this.openButton = new System.Windows.Forms.Button();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.listView = new System.Windows.Forms.ListView();
            this.name = new System.Windows.Forms.ColumnHeader();
            this.copyright = new System.Windows.Forms.ColumnHeader();
            this.description = new System.Windows.Forms.ColumnHeader();
            this.largeImageList = new System.Windows.Forms.ImageList(this.components);
            this.smallImageList = new System.Windows.Forms.ImageList(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
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
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // openButton
            // 
            this.openButton.AccessibleDescription = null;
            this.openButton.AccessibleName = null;
            resources.ApplyResources(this.openButton, "openButton");
            this.openButton.BackgroundImage = null;
            this.openButton.CausesValidation = false;
            this.openButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.openButton.Font = null;
            this.openButton.Name = "openButton";
            this.openButton.Click += new System.EventHandler(this.openButton_Click);
            // 
            // addButton
            // 
            this.addButton.AccessibleDescription = null;
            this.addButton.AccessibleName = null;
            resources.ApplyResources(this.addButton, "addButton");
            this.addButton.BackgroundImage = null;
            this.addButton.CausesValidation = false;
            this.addButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.addButton.Font = null;
            this.addButton.Name = "addButton";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.AccessibleDescription = null;
            this.removeButton.AccessibleName = null;
            resources.ApplyResources(this.removeButton, "removeButton");
            this.removeButton.BackgroundImage = null;
            this.removeButton.CausesValidation = false;
            this.removeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.removeButton.Font = null;
            this.removeButton.Name = "removeButton";
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // listView
            // 
            this.listView.AccessibleDescription = null;
            this.listView.AccessibleName = null;
            resources.ApplyResources(this.listView, "listView");
            this.listView.BackgroundImage = null;
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.name,
            this.copyright,
            this.description});
            this.listView.Font = null;
            this.listView.FullRowSelect = true;
            this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView.LargeImageList = this.largeImageList;
            this.listView.Name = "listView";
            this.listView.SmallImageList = this.smallImageList;
            this.listView.TileSize = new System.Drawing.Size(625, 56);
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Tile;
            this.listView.DoubleClick += new System.EventHandler(this.openButton_Click);
            this.listView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listView_ItemSelectionChanged);
            this.listView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listView_KeyUp);
            // 
            // name
            // 
            resources.ApplyResources(this.name, "name");
            // 
            // copyright
            // 
            resources.ApplyResources(this.copyright, "copyright");
            // 
            // description
            // 
            resources.ApplyResources(this.description, "description");
            // 
            // largeImageList
            // 
            this.largeImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.largeImageList, "largeImageList");
            this.largeImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // smallImageList
            // 
            this.smallImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.smallImageList, "smallImageList");
            this.smallImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // label1
            // 
            this.label1.AccessibleDescription = null;
            this.label1.AccessibleName = null;
            resources.ApplyResources(this.label1, "label1");
            this.label1.Font = null;
            this.label1.Name = "label1";
            // 
            // LibraryDialog
            // 
            this.AcceptButton = this.openButton;
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.CancelButton = this.closeButton;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.closeButton);
            this.Font = null;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = null;
            this.Name = "LibraryDialog";
            this.Load += new System.EventHandler(this.LibraryDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Button openButton;
		private System.Windows.Forms.Button addButton;
		private System.Windows.Forms.Button removeButton;
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ColumnHeader name;
		private System.Windows.Forms.ColumnHeader copyright;
		private System.Windows.Forms.ColumnHeader description;
		private System.Windows.Forms.ImageList smallImageList;
		private System.Windows.Forms.ImageList largeImageList;
	}
}