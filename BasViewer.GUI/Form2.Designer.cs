namespace BasViewer.GUI
{
    partial class frmAdvancedSearch
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnOK = new Button();
            btnCancel = new Button();
            chkReal = new RadioButton();
            chkInt = new RadioButton();
            chkString = new RadioButton();
            chkFn = new RadioButton();
            chkProc = new RadioButton();
            chkRem = new RadioButton();
            chkLiteralString = new RadioButton();
            label2 = new Label();
            labTip = new Label();
            labMessage = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            chkKeywords = new RadioButton();
            cmbBoxAdvSearch = new ComboBox();
            SuspendLayout();
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOK.Location = new Point(742, 509);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(150, 46);
            btnOK.TabIndex = 0;
            btnOK.Text = "Search";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnCancel.Location = new Point(65, 509);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(150, 46);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // chkReal
            // 
            chkReal.AutoSize = true;
            chkReal.Location = new Point(111, 148);
            chkReal.Name = "chkReal";
            chkReal.Size = new Size(89, 36);
            chkReal.TabIndex = 7;
            chkReal.Text = "Real";
            chkReal.UseVisualStyleBackColor = true;
            chkReal.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkInt
            // 
            chkInt.AutoSize = true;
            chkInt.Location = new Point(111, 208);
            chkInt.Name = "chkInt";
            chkInt.Size = new Size(121, 36);
            chkInt.TabIndex = 8;
            chkInt.Text = "Integer";
            chkInt.UseVisualStyleBackColor = true;
            chkInt.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkString
            // 
            chkString.AutoSize = true;
            chkString.Location = new Point(111, 268);
            chkString.Name = "chkString";
            chkString.Size = new Size(107, 36);
            chkString.TabIndex = 9;
            chkString.Text = "String";
            chkString.UseVisualStyleBackColor = true;
            chkString.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkFn
            // 
            chkFn.AutoSize = true;
            chkFn.Location = new Point(349, 228);
            chkFn.Name = "chkFn";
            chkFn.Size = new Size(75, 36);
            chkFn.TabIndex = 11;
            chkFn.Tag = "";
            chkFn.Text = "FN";
            chkFn.UseVisualStyleBackColor = true;
            chkFn.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkProc
            // 
            chkProc.AutoSize = true;
            chkProc.Location = new Point(349, 168);
            chkProc.Name = "chkProc";
            chkProc.Size = new Size(105, 36);
            chkProc.TabIndex = 10;
            chkProc.Text = "PROC";
            chkProc.UseVisualStyleBackColor = true;
            chkProc.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkRem
            // 
            chkRem.AutoSize = true;
            chkRem.Enabled = false;
            chkRem.Location = new Point(695, 208);
            chkRem.Name = "chkRem";
            chkRem.Size = new Size(103, 36);
            chkRem.TabIndex = 14;
            chkRem.Text = "REMs";
            chkRem.UseVisualStyleBackColor = true;
            chkRem.CheckedChanged += Radio_CheckedChanged;
            // 
            // chkLiteralString
            // 
            chkLiteralString.AutoSize = true;
            chkLiteralString.Location = new Point(695, 148);
            chkLiteralString.Name = "chkLiteralString";
            chkLiteralString.Size = new Size(183, 36);
            chkLiteralString.TabIndex = 13;
            chkLiteralString.Text = "String literals";
            chkLiteralString.UseVisualStyleBackColor = true;
            chkLiteralString.CheckedChanged += Radio_CheckedChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = SystemColors.HotTrack;
            label2.Location = new Point(39, 29);
            label2.Name = "label2";
            label2.Size = new Size(127, 32);
            label2.TabIndex = 19;
            label2.Text = "Search for:";
            // 
            // labTip
            // 
            labTip.AutoSize = true;
            labTip.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labTip.ForeColor = SystemColors.HotTrack;
            labTip.Location = new Point(63, 433);
            labTip.Name = "labTip";
            labTip.Size = new Size(63, 37);
            labTip.TabIndex = 21;
            labTip.Text = "TIP:";
            // 
            // labMessage
            // 
            labMessage.AutoSize = true;
            labMessage.Font = new Font("Segoe UI", 10.125F);
            labMessage.ForeColor = SystemColors.HotTrack;
            labMessage.Location = new Point(124, 433);
            labMessage.Name = "labMessage";
            labMessage.Size = new Size(67, 37);
            labMessage.TabIndex = 22;
            labMessage.Text = "msg";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.HotTrack;
            label3.Location = new Point(64, 84);
            label3.Name = "label3";
            label3.Size = new Size(126, 37);
            label3.TabIndex = 23;
            label3.Text = "Variables";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = SystemColors.HotTrack;
            label4.Location = new Point(308, 84);
            label4.Name = "label4";
            label4.Size = new Size(220, 37);
            label4.TabIndex = 24;
            label4.Text = "PROC/FN names";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = SystemColors.HotTrack;
            label5.Location = new Point(646, 84);
            label5.Name = "label5";
            label5.Size = new Size(246, 37);
            label5.TabIndex = 25;
            label5.Text = "Text and Keywords";
            // 
            // chkKeywords
            // 
            chkKeywords.AutoSize = true;
            chkKeywords.Enabled = false;
            chkKeywords.Location = new Point(695, 268);
            chkKeywords.Name = "chkKeywords";
            chkKeywords.Size = new Size(147, 36);
            chkKeywords.TabIndex = 26;
            chkKeywords.Text = "Keywords";
            chkKeywords.UseVisualStyleBackColor = true;
            chkKeywords.CheckedChanged += Radio_CheckedChanged;
            // 
            // cmbBoxAdvSearch
            // 
            cmbBoxAdvSearch.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBoxAdvSearch.FormattingEnabled = true;
            cmbBoxAdvSearch.Location = new Point(64, 347);
            cmbBoxAdvSearch.Name = "cmbBoxAdvSearch";
            cmbBoxAdvSearch.Size = new Size(825, 40);
            cmbBoxAdvSearch.Sorted = true;
            cmbBoxAdvSearch.TabIndex = 2;
            // 
            // frmAdvancedSearch
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(960, 579);
            Controls.Add(cmbBoxAdvSearch);
            Controls.Add(chkKeywords);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(labMessage);
            Controls.Add(labTip);
            Controls.Add(label2);
            Controls.Add(chkRem);
            Controls.Add(chkLiteralString);
            Controls.Add(chkFn);
            Controls.Add(chkProc);
            Controls.Add(chkString);
            Controls.Add(chkInt);
            Controls.Add(chkReal);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmAdvancedSearch";
            ShowIcon = false;
            ShowInTaskbar = false;
            Text = "Advanced Search";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnOK;
        private Button btnCancel;
        private RadioButton chkReal;
        private RadioButton chkInt;
        private RadioButton chkString;
        private RadioButton chkFn;
        private RadioButton chkProc;
        private RadioButton chkRem;
        private RadioButton chkLiteralString;
        private RadioButton chkKeywords;
        private Label label2;
        private Label labTip;
        private Label labMessage;
        private Label label3;
        private Label label4;
        private Label label5;
        private ComboBox cmbBoxAdvSearch;
    }
}