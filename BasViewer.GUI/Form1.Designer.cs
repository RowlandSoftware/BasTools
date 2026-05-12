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
            webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();
            label1 = new Label();
            contextMenuStrip1 = new ContextMenuStrip(components);
            dragFileToLoadToolStripMenuItem = new ToolStripMenuItem();
            gotoLineToolStripMenuItem = new ToolStripMenuItem();
            toolStripTextBoxGoto = new ToolStripTextBox();
            advancedSearchToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            topPanel = new Panel();
            toolStripLeft = new ToolStrip();
            toolStripButton7 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();
            toolStripButton4 = new ToolStripButton();
            toolStripLabel2 = new ToolStripLabel();
            comboBoxTheme = new ToolStripComboBox();
            toolStripSeparator2 = new ToolStripSeparator();
            combProcFnFinder = new ToolStripComboBox();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripButton10 = new ToolStripButton();
            toolStripTextBoxSearch = new ToolStripTextBox();
            menuStripRight = new ToolStrip();
            toolStripButtonMenu = new ToolStripButton();
            panelSearchNav = new Panel();
            ((System.ComponentModel.ISupportInitialize)webView2).BeginInit();
            contextMenuStrip1.SuspendLayout();
            topPanel.SuspendLayout();
            toolStripLeft.SuspendLayout();
            menuStripRight.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Font = new Font("Segoe UI", 10.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusStrip1.ImageScalingSize = new Size(32, 32);
            statusStrip1.Location = new Point(0, 1040);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1381, 22);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // webView2
            // 
            webView2.AllowExternalDrop = false;
            webView2.CreationProperties = null;
            webView2.DefaultBackgroundColor = Color.White;
            webView2.Dock = DockStyle.Fill;
            webView2.Enabled = false;
            webView2.Location = new Point(0, 54);
            webView2.Name = "webView2";
            webView2.Size = new Size(1381, 986);
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
            label1.Location = new Point(0, 54);
            label1.Name = "label1";
            label1.Size = new Size(1381, 986);
            label1.TabIndex = 4;
            label1.Text = "Drag 'n' Drop a file here";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.DragDrop += MainForm_DragDrop;
            label1.DragEnter += MainForm_DragEnter;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("Segoe UI", 10.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            contextMenuStrip1.ImageScalingSize = new Size(32, 32);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { dragFileToLoadToolStripMenuItem, gotoLineToolStripMenuItem, advancedSearchToolStripMenuItem, aboutToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(313, 188);
            // 
            // dragFileToLoadToolStripMenuItem
            // 
            dragFileToLoadToolStripMenuItem.Name = "dragFileToLoadToolStripMenuItem";
            dragFileToLoadToolStripMenuItem.Size = new Size(312, 46);
            dragFileToLoadToolStripMenuItem.Text = "Drag file to load";
            dragFileToLoadToolStripMenuItem.Click += dragFileToLoadToolStripMenuItem_Click;
            // 
            // gotoLineToolStripMenuItem
            // 
            gotoLineToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripTextBoxGoto });
            gotoLineToolStripMenuItem.Name = "gotoLineToolStripMenuItem";
            gotoLineToolStripMenuItem.Size = new Size(312, 46);
            gotoLineToolStripMenuItem.Text = "Goto Line...";
            gotoLineToolStripMenuItem.Click += gotoLineToolStripMenuItem_Click;
            // 
            // toolStripTextBoxGoto
            // 
            toolStripTextBoxGoto.Name = "toolStripTextBoxGoto";
            toolStripTextBoxGoto.Size = new Size(100, 39);
            toolStripTextBoxGoto.KeyPress += toolStripTextBoxGoto_KeyPress;
            // 
            // advancedSearchToolStripMenuItem
            // 
            advancedSearchToolStripMenuItem.Name = "advancedSearchToolStripMenuItem";
            advancedSearchToolStripMenuItem.Size = new Size(312, 46);
            advancedSearchToolStripMenuItem.Text = "Advanced Search";
            advancedSearchToolStripMenuItem.Click += advancedSearchToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(312, 46);
            aboutToolStripMenuItem.Text = "About ...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // topPanel
            // 
            topPanel.BackColor = Color.LightBlue;
            topPanel.Controls.Add(toolStripLeft);
            topPanel.Controls.Add(menuStripRight);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1381, 54);
            topPanel.TabIndex = 5;
            // 
            // toolStripLeft
            // 
            toolStripLeft.BackColor = Color.LightSkyBlue;
            toolStripLeft.GripStyle = ToolStripGripStyle.Hidden;
            toolStripLeft.ImageScalingSize = new Size(48, 48);
            toolStripLeft.Items.AddRange(new ToolStripItem[] { toolStripButton7, toolStripButton2, toolStripButton4, toolStripLabel2, comboBoxTheme, toolStripSeparator2, combProcFnFinder, toolStripSeparator1, toolStripButton10, toolStripTextBoxSearch });
            toolStripLeft.Location = new Point(0, 0);
            toolStripLeft.MinimumSize = new Size(0, 54);
            toolStripLeft.Name = "toolStripLeft";
            toolStripLeft.Size = new Size(1327, 54);
            toolStripLeft.Stretch = true;
            toolStripLeft.TabIndex = 3;
            toolStripLeft.Text = "toolStripLeft";
            toolStripLeft.SizeChanged += toolStripLeft_SizeChanged;
            // 
            // toolStripButton7
            // 
            toolStripButton7.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton7.Image = (Image)resources.GetObject("toolStripButton7.Image");
            toolStripButton7.ImageTransparentColor = Color.Magenta;
            toolStripButton7.Name = "toolStripButton7";
            toolStripButton7.Size = new Size(52, 48);
            toolStripButton7.Text = "toolStripButton1";
            toolStripButton7.Click += toolStripButton1_Click;
            // 
            // toolStripButton2
            // 
            toolStripButton2.Checked = true;
            toolStripButton2.CheckOnClick = true;
            toolStripButton2.CheckState = CheckState.Checked;
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(52, 48);
            toolStripButton2.Text = "toolStripButton2";
            toolStripButton2.ToolTipText = "Prettyprint";
            toolStripButton2.CheckedChanged += toolStripButton2_CheckedChanged;
            // 
            // toolStripButton4
            // 
            toolStripButton4.Checked = true;
            toolStripButton4.CheckOnClick = true;
            toolStripButton4.CheckState = CheckState.Checked;
            toolStripButton4.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton4.Image = (Image)resources.GetObject("toolStripButton4.Image");
            toolStripButton4.ImageTransparentColor = Color.Magenta;
            toolStripButton4.Name = "toolStripButton4";
            toolStripButton4.Size = new Size(52, 48);
            toolStripButton4.Text = "toolStripButton4";
            toolStripButton4.ToolTipText = "Split lines";
            toolStripButton4.CheckedChanged += toolStripButton4_CheckedChanged;
            // 
            // toolStripLabel2
            // 
            toolStripLabel2.Name = "toolStripLabel2";
            toolStripLabel2.Size = new Size(100, 48);
            toolStripLabel2.Text = "Theme: ";
            toolStripLabel2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // comboBoxTheme
            // 
            comboBoxTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxTheme.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            comboBoxTheme.Items.AddRange(new object[] { "Dark", "Light", "Retro", "Mono", "Typewriter" });
            comboBoxTheme.Name = "comboBoxTheme";
            comboBoxTheme.Size = new Size(170, 54);
            comboBoxTheme.SelectedIndexChanged += comboBoxTheme_SelectedIndexChanged;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 54);
            // 
            // combProcFnFinder
            // 
            combProcFnFinder.DropDownStyle = ComboBoxStyle.DropDownList;
            combProcFnFinder.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            combProcFnFinder.Name = "combProcFnFinder";
            combProcFnFinder.Size = new Size(500, 54);
            combProcFnFinder.ToolTipText = "PROC and FN finder";
            combProcFnFinder.SelectedIndexChanged += combProcFnFinder_SelectedIndexChanged;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 54);
            // 
            // toolStripButton10
            // 
            toolStripButton10.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton10.Image = (Image)resources.GetObject("toolStripButton10.Image");
            toolStripButton10.ImageTransparentColor = Color.Magenta;
            toolStripButton10.Name = "toolStripButton10";
            toolStripButton10.Size = new Size(52, 48);
            toolStripButton10.Text = "toolStripButton5";
            toolStripButton10.ToolTipText = "Quick Search";
            // 
            // toolStripTextBoxSearch
            // 
            toolStripTextBoxSearch.AutoSize = false;
            toolStripTextBoxSearch.Name = "toolStripTextBoxSearch";
            toolStripTextBoxSearch.Size = new Size(100, 54);
            toolStripTextBoxSearch.ToolTipText = "Quick Search";
            toolStripTextBoxSearch.KeyDown += toolStripTextBoxSearch_KeyDown;
            toolStripTextBoxSearch.KeyUp += toolStripTextBoxSearch_KeyDown;
            // 
            // menuStripRight
            // 
            menuStripRight.AutoSize = false;
            menuStripRight.Dock = DockStyle.Right;
            menuStripRight.GripStyle = ToolStripGripStyle.Hidden;
            menuStripRight.ImageScalingSize = new Size(32, 32);
            menuStripRight.Items.AddRange(new ToolStripItem[] { toolStripButtonMenu });
            menuStripRight.LayoutStyle = ToolStripLayoutStyle.Flow;
            menuStripRight.Location = new Point(1327, 0);
            menuStripRight.Name = "menuStripRight";
            menuStripRight.RenderMode = ToolStripRenderMode.Professional;
            menuStripRight.Size = new Size(54, 54);
            menuStripRight.TabIndex = 0;
            menuStripRight.Text = "menuStripRight";
            // 
            // toolStripButtonMenu
            // 
            toolStripButtonMenu.Alignment = ToolStripItemAlignment.Right;
            toolStripButtonMenu.AutoSize = false;
            toolStripButtonMenu.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonMenu.Image = (Image)resources.GetObject("toolStripButtonMenu.Image");
            toolStripButtonMenu.ImageTransparentColor = Color.Magenta;
            toolStripButtonMenu.Margin = new Padding(0);
            toolStripButtonMenu.Name = "toolStripButtonMenu";
            toolStripButtonMenu.Size = new Size(54, 54);
            toolStripButtonMenu.Text = "Menu";
            toolStripButtonMenu.Click += toolStripButtonMenu_Click;
            // 
            // panelSearchNav
            // 
            panelSearchNav.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelSearchNav.Dock = DockStyle.Bottom;
            panelSearchNav.Location = new Point(0, 984);
            panelSearchNav.Name = "panelSearchNav";
            panelSearchNav.Size = new Size(1381, 56);
            panelSearchNav.TabIndex = 6;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1381, 1062);
            Controls.Add(label1);
            Controls.Add(webView2);
            Controls.Add(topPanel);
            Controls.Add(panelSearchNav);
            Controls.Add(statusStrip1);
            Name = "Form1";
            Text = "Form1";
            DragDrop += MainForm_DragDrop;
            DragEnter += MainForm_DragEnter;
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyDown;
            ((System.ComponentModel.ISupportInitialize)webView2).EndInit();
            contextMenuStrip1.ResumeLayout(false);
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            toolStripLeft.ResumeLayout(false);
            toolStripLeft.PerformLayout();
            menuStripRight.ResumeLayout(false);
            menuStripRight.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusLeft;
        private ToolStripStatusLabel statusRight;

        private ToolStripButton zoomOutButton;
        private ToolStripButton zoomInButton;
        private ToolStripLabel zoomPercentLabel;
        private TrackBar zoomSlider;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView2;
        private Label label1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem dragFileToLoadToolStripMenuItem;
        private ToolStripMenuItem advancedSearchMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem advancedSearchToolStripMenuItem;
        private ToolStripMenuItem gotoLineToolStripMenuItem;
        private ToolStripTextBox toolStripTextBoxGoto;
        private ToolStripControlHost zoomHost;       // the host that goes in the StatusStrip
        private Panel topPanel;
        private ToolStrip menuStripRight;
        private ToolStripButton toolStripButtonMenu;
        private ToolStrip toolStripLeft;
        private ToolStripButton toolStripButton7;
        private ToolStripButton toolStripButton2;
        private ToolStripButton toolStripButton4;
        private ToolStripLabel toolStripLabel2;
        private ToolStripComboBox comboBoxTheme;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripComboBox combProcFnFinder;
        private ToolStripButton toolStripButton10;
        private ToolStripTextBox toolStripTextBoxSearch;
        private Panel panelSearchNav;
    }
}
