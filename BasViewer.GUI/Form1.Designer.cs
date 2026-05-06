namespace BasViewer.GUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStrip1 = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();
            toolStripButton3 = new ToolStripButton();
            toolStripButton4 = new ToolStripButton();
            toolStripLabel1 = new ToolStripLabel();
            comboBoxTheme = new ToolStripComboBox();
            toolStripSeparator1 = new ToolStripSeparator();
            combProcFnFinder = new ToolStripComboBox();
            toolStripButton5 = new ToolStripButton();
            toolStripTextBoxSearch = new ToolStripTextBox();
            webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
            label1 = new Label();
            contextMenuStrip1 = new ContextMenuStrip(components);
            dragFileToLoadToolStripMenuItem = new ToolStripMenuItem();
            advancedSearchToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView2).BeginInit();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Font = new Font("Segoe UI", 10.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusStrip1.ImageScalingSize = new Size(32, 32);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2 });
            statusStrip1.Location = new Point(0, 1040);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1381, 22);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(1366, 12);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(0, 12);
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(32, 32);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1, toolStripButton2, toolStripButton3, toolStripButton4, toolStripLabel1, comboBoxTheme, toolStripSeparator1, combProcFnFinder, toolStripButton5, toolStripTextBoxSearch });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1381, 45);
            toolStrip1.Stretch = true;
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton1.Image = (Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(46, 39);
            toolStripButton1.Text = "toolStripButton1";
            toolStripButton1.Click += toolStripButton1_Click;
            // 
            // toolStripButton2
            // 
            toolStripButton2.CheckOnClick = true;
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(46, 39);
            toolStripButton2.Text = "toolStripButton2";
            toolStripButton2.ToolTipText = "Prettyprint";
            // 
            // toolStripButton3
            // 
            toolStripButton3.Alignment = ToolStripItemAlignment.Right;
            toolStripButton3.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton3.Image = (Image)resources.GetObject("toolStripButton3.Image");
            toolStripButton3.ImageTransparentColor = Color.Magenta;
            toolStripButton3.Name = "toolStripButton3";
            toolStripButton3.Size = new Size(46, 39);
            toolStripButton3.Text = "Menu";
            toolStripButton3.Click += toolStripButton3_Click;
            // 
            // toolStripButton4
            // 
            toolStripButton4.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton4.Image = (Image)resources.GetObject("toolStripButton4.Image");
            toolStripButton4.ImageTransparentColor = Color.Magenta;
            toolStripButton4.Name = "toolStripButton4";
            toolStripButton4.Size = new Size(46, 39);
            toolStripButton4.Text = "toolStripButton4";
            // 
            // toolStripLabel1
            // 
            toolStripLabel1.Name = "toolStripLabel1";
            toolStripLabel1.Size = new Size(100, 39);
            toolStripLabel1.Text = "Theme: ";
            toolStripLabel1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // comboBoxTheme
            // 
            comboBoxTheme.AutoSize = false;
            comboBoxTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxTheme.Items.AddRange(new object[] { "Dark", "Light", "Retro", "Mono", "Typewriter" });
            comboBoxTheme.Name = "comboBoxTheme";
            comboBoxTheme.Size = new Size(170, 40);
            comboBoxTheme.SelectedIndexChanged += comboBoxTheme_SelectedIndexChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 45);
            // 
            // combProcFnFinder
            // 
            combProcFnFinder.DropDownStyle = ComboBoxStyle.DropDownList;
            combProcFnFinder.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            combProcFnFinder.Name = "combProcFnFinder";
            combProcFnFinder.Size = new Size(500, 45);
            combProcFnFinder.SelectedIndexChanged += combProcFnFinder_SelectedIndexChanged;
            // 
            // toolStripButton5
            // 
            toolStripButton5.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton5.Image = (Image)resources.GetObject("toolStripButton5.Image");
            toolStripButton5.ImageTransparentColor = Color.Magenta;
            toolStripButton5.Name = "toolStripButton5";
            toolStripButton5.Size = new Size(46, 39);
            toolStripButton5.Text = "toolStripButton5";
            toolStripButton5.Click += toolStripButton5_Click;
            // 
            // toolStripTextBoxSearch
            // 
            toolStripTextBoxSearch.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            toolStripTextBoxSearch.Name = "toolStripTextBoxSearch";
            toolStripTextBoxSearch.Size = new Size(200, 45);
            toolStripTextBoxSearch.ToolTipText = "Search";
            toolStripTextBoxSearch.KeyDown += toolStripTextBoxSearch_KeyDown;
            // 
            // webView2
            // 
            webView2.AllowExternalDrop = false;
            webView2.CreationProperties = null;
            webView2.DefaultBackgroundColor = Color.White;
            webView2.Dock = DockStyle.Fill;
            webView2.Enabled = false;
            webView2.Location = new Point(0, 45);
            webView2.Name = "webView2";
            webView2.Size = new Size(1381, 995);
            webView2.TabIndex = 3;
            webView2.Visible = false;
            webView2.ZoomFactor = 1D;
            // 
            // label1
            // 
            label1.AllowDrop = true;
            label1.BackColor = Color.Blue;
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(0, 45);
            label1.Name = "label1";
            label1.Size = new Size(1381, 995);
            label1.TabIndex = 4;
            label1.Text = "Drag 'n' Drop files here";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.DragDrop += MainForm_DragDrop;
            label1.DragEnter += MainForm_DragEnter;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("Segoe UI", 10.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            contextMenuStrip1.ImageScalingSize = new Size(32, 32);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { dragFileToLoadToolStripMenuItem, advancedSearchToolStripMenuItem, aboutToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(313, 142);
            // 
            // dragFileToLoadToolStripMenuItem
            // 
            dragFileToLoadToolStripMenuItem.Name = "dragFileToLoadToolStripMenuItem";
            dragFileToLoadToolStripMenuItem.Size = new Size(312, 46);
            dragFileToLoadToolStripMenuItem.Text = "Drag file to load";
            dragFileToLoadToolStripMenuItem.Click += dragFileToLoadToolStripMenuItem_Click;
            // 
            // advancedSearchToolStripMenuItem
            // 
            advancedSearchToolStripMenuItem.Name = "advancedSearchToolStripMenuItem";
            advancedSearchToolStripMenuItem.Size = new Size(312, 46);
            advancedSearchToolStripMenuItem.Text = "Advanced Search";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(312, 46);
            aboutToolStripMenuItem.Text = "About ...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1381, 1062);
            Controls.Add(label1);
            Controls.Add(webView2);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Name = "Form1";
            Text = "Form1";
            DragDrop += MainForm_DragDrop;
            DragEnter += MainForm_DragEnter;
            KeyUp += Form1_KeyUp;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)webView2).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private ToolStripButton toolStripButton2;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView2;
        private ToolStripButton toolStripButton4;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripComboBox comboBoxTheme;
        private ToolStripLabel toolStripLabel1;
        private Label label1;
        private ToolStripButton toolStripButton3;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem dragFileToLoadToolStripMenuItem;
        private ToolStripMenuItem advancedSearchMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripComboBox combProcFnFinder;
        private ToolStripButton toolStripButton5;
        private ToolStripTextBox toolStripTextBoxSearch;
        private ToolStripMenuItem advancedSearchToolStripMenuItem;
    }
}
