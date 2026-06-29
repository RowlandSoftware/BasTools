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
            txtBoxAdvSearch = new TextBox();
            chkWholeWords = new CheckBox();
            chkReal = new CheckBox();
            chkInt = new CheckBox();
            chkString = new CheckBox();
            chkFn = new CheckBox();
            chkProc = new CheckBox();
            chkRem = new CheckBox();
            chkLiteralString = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            chkMatchCase = new CheckBox();
            labTip = new Label();
            labMessage = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            chkKeywords = new CheckBox();
            btnSelAll = new Button();
            btnDeselAll = new Button();
            SuspendLayout();
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOK.Location = new Point(746, 676);
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
            btnCancel.Location = new Point(69, 676);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(150, 46);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtBoxAdvSearch
            // 
            txtBoxAdvSearch.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtBoxAdvSearch.Location = new Point(68, 80);
            txtBoxAdvSearch.Name = "txtBoxAdvSearch";
            txtBoxAdvSearch.Size = new Size(825, 43);
            txtBoxAdvSearch.TabIndex = 2;
            // 
            // chkWholeWords
            // 
            chkWholeWords.AutoSize = true;
            chkWholeWords.Checked = true;
            chkWholeWords.CheckState = CheckState.Checked;
            chkWholeWords.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkWholeWords.Location = new Point(67, 138);
            chkWholeWords.Name = "chkWholeWords";
            chkWholeWords.Size = new Size(307, 41);
            chkWholeWords.TabIndex = 3;
            chkWholeWords.Text = "Whole words / names";
            chkWholeWords.UseVisualStyleBackColor = true;
            // 
            // chkReal
            // 
            chkReal.AutoSize = true;
            chkReal.Location = new Point(115, 329);
            chkReal.Name = "chkReal";
            chkReal.Size = new Size(90, 36);
            chkReal.TabIndex = 7;
            chkReal.Text = "Real";
            chkReal.UseVisualStyleBackColor = true;
            // 
            // chkInt
            // 
            chkInt.AutoSize = true;
            chkInt.Location = new Point(115, 389);
            chkInt.Name = "chkInt";
            chkInt.Size = new Size(122, 36);
            chkInt.TabIndex = 8;
            chkInt.Text = "Integer";
            chkInt.UseVisualStyleBackColor = true;
            // 
            // chkString
            // 
            chkString.AutoSize = true;
            chkString.Location = new Point(115, 449);
            chkString.Name = "chkString";
            chkString.Size = new Size(108, 36);
            chkString.TabIndex = 9;
            chkString.Text = "String";
            chkString.UseVisualStyleBackColor = true;
            // 
            // chkFn
            // 
            chkFn.AutoSize = true;
            chkFn.Location = new Point(353, 409);
            chkFn.Name = "chkFn";
            chkFn.Size = new Size(76, 36);
            chkFn.TabIndex = 11;
            chkFn.Text = "FN";
            chkFn.UseVisualStyleBackColor = true;
            // 
            // chkProc
            // 
            chkProc.AutoSize = true;
            chkProc.Location = new Point(353, 349);
            chkProc.Name = "chkProc";
            chkProc.Size = new Size(106, 36);
            chkProc.TabIndex = 10;
            chkProc.Text = "PROC";
            chkProc.UseVisualStyleBackColor = true;
            // 
            // chkRem
            // 
            chkRem.AutoSize = true;
            chkRem.Location = new Point(699, 389);
            chkRem.Name = "chkRem";
            chkRem.Size = new Size(104, 36);
            chkRem.TabIndex = 14;
            chkRem.Text = "REMs";
            chkRem.UseVisualStyleBackColor = true;
            // 
            // chkLiteralString
            // 
            chkLiteralString.AutoSize = true;
            chkLiteralString.Location = new Point(699, 329);
            chkLiteralString.Name = "chkLiteralString";
            chkLiteralString.Size = new Size(184, 36);
            chkLiteralString.TabIndex = 13;
            chkLiteralString.Text = "String literals";
            chkLiteralString.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.ForeColor = SystemColors.HotTrack;
            label1.Location = new Point(39, 202);
            label1.Name = "label1";
            label1.Size = new Size(117, 32);
            label1.TabIndex = 18;
            label1.Text = "Search in:";
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
            // chkMatchCase
            // 
            chkMatchCase.AutoSize = true;
            chkMatchCase.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkMatchCase.Location = new Point(479, 138);
            chkMatchCase.Name = "chkMatchCase";
            chkMatchCase.Size = new Size(181, 41);
            chkMatchCase.TabIndex = 20;
            chkMatchCase.Text = "Match case";
            chkMatchCase.UseVisualStyleBackColor = true;
            // 
            // labTip
            // 
            labTip.AutoSize = true;
            labTip.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labTip.ForeColor = SystemColors.HotTrack;
            labTip.Location = new Point(68, 521);
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
            labMessage.Location = new Point(156, 521);
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
            label3.Location = new Point(68, 265);
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
            label4.Location = new Point(314, 265);
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
            label5.Location = new Point(650, 265);
            label5.Name = "label5";
            label5.Size = new Size(246, 37);
            label5.TabIndex = 25;
            label5.Text = "Text and Keywords";
            // 
            // chkKeywords
            // 
            chkKeywords.AutoSize = true;
            chkKeywords.Location = new Point(699, 449);
            chkKeywords.Name = "chkKeywords";
            chkKeywords.Size = new Size(148, 36);
            chkKeywords.TabIndex = 26;
            chkKeywords.Text = "Keywords";
            chkKeywords.UseVisualStyleBackColor = true;
            // 
            // btnSelAll
            // 
            btnSelAll.DialogResult = DialogResult.Cancel;
            btnSelAll.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnSelAll.Location = new Point(262, 593);
            btnSelAll.Name = "btnSelAll";
            btnSelAll.Size = new Size(199, 46);
            btnSelAll.TabIndex = 28;
            btnSelAll.Text = "Select All";
            btnSelAll.UseVisualStyleBackColor = true;
            btnSelAll.Click += btnSelAll_Click;
            // 
            // btnDeselAll
            // 
            btnDeselAll.DialogResult = DialogResult.OK;
            btnDeselAll.Font = new Font("Segoe UI", 10.125F);
            btnDeselAll.Location = new Point(504, 593);
            btnDeselAll.Name = "btnDeselAll";
            btnDeselAll.Size = new Size(199, 46);
            btnDeselAll.TabIndex = 27;
            btnDeselAll.Text = "Deselect All";
            btnDeselAll.UseVisualStyleBackColor = true;
            btnDeselAll.Click += btnDeselAll_Click;
            // 
            // frmAdvancedSearch
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(960, 762);
            Controls.Add(btnSelAll);
            Controls.Add(btnDeselAll);
            Controls.Add(chkKeywords);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(labMessage);
            Controls.Add(labTip);
            Controls.Add(chkMatchCase);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(chkRem);
            Controls.Add(chkLiteralString);
            Controls.Add(chkFn);
            Controls.Add(chkProc);
            Controls.Add(chkString);
            Controls.Add(chkInt);
            Controls.Add(chkReal);
            Controls.Add(chkWholeWords);
            Controls.Add(txtBoxAdvSearch);
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
        private TextBox txtBoxAdvSearch;
        private CheckBox chkWholeWords;
        private CheckBox chkReal;
        private CheckBox chkInt;
        private CheckBox chkString;
        private CheckBox chkFn;
        private CheckBox chkProc;
        private CheckBox chkRem;
        private CheckBox chkLiteralString;
        private Label label1;
        private Label label2;
        private CheckBox chkMatchCase;
        private Label labTip;
        private Label labMessage;
        private Label label3;
        private Label label4;
        private Label label5;
        private CheckBox chkKeywords;
        private Button btnSelAll;
        private Button btnDeselAll;
    }
}