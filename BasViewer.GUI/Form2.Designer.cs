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
            chkBoxVars = new CheckBox();
            chkBoxProcFn = new CheckBox();
            chkBoxText = new CheckBox();
            label1 = new Label();
            label2 = new Label();
            chkMatchCase = new CheckBox();
            SuspendLayout();
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Font = new Font("Segoe UI Semibold", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOK.Location = new Point(744, 537);
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
            btnCancel.Location = new Point(67, 537);
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
            chkRem.Location = new Point(699, 409);
            chkRem.Name = "chkRem";
            chkRem.Size = new Size(104, 36);
            chkRem.TabIndex = 14;
            chkRem.Text = "REMs";
            chkRem.UseVisualStyleBackColor = true;
            // 
            // chkLiteralString
            // 
            chkLiteralString.AutoSize = true;
            chkLiteralString.Location = new Point(699, 349);
            chkLiteralString.Name = "chkLiteralString";
            chkLiteralString.Size = new Size(184, 36);
            chkLiteralString.TabIndex = 13;
            chkLiteralString.Text = "String literals";
            chkLiteralString.UseVisualStyleBackColor = true;
            // 
            // chkBoxVars
            // 
            chkBoxVars.AutoSize = true;
            chkBoxVars.Checked = true;
            chkBoxVars.CheckState = CheckState.Checked;
            chkBoxVars.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkBoxVars.Location = new Point(67, 255);
            chkBoxVars.Name = "chkBoxVars";
            chkBoxVars.Size = new Size(156, 41);
            chkBoxVars.TabIndex = 15;
            chkBoxVars.Text = "Variables";
            chkBoxVars.UseVisualStyleBackColor = true;
            chkBoxVars.CheckedChanged += chkBoxVars_CheckedChanged;
            // 
            // chkBoxProcFn
            // 
            chkBoxProcFn.AutoSize = true;
            chkBoxProcFn.Checked = true;
            chkBoxProcFn.CheckState = CheckState.Checked;
            chkBoxProcFn.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkBoxProcFn.Location = new Point(314, 265);
            chkBoxProcFn.Name = "chkBoxProcFn";
            chkBoxProcFn.Size = new Size(245, 41);
            chkBoxProcFn.TabIndex = 16;
            chkBoxProcFn.Text = "PROC/FN names";
            chkBoxProcFn.UseVisualStyleBackColor = true;
            chkBoxProcFn.CheckedChanged += chkBoxProcFn_CheckedChanged;
            // 
            // chkBoxText
            // 
            chkBoxText.AutoSize = true;
            chkBoxText.Checked = true;
            chkBoxText.CheckState = CheckState.Checked;
            chkBoxText.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkBoxText.Location = new Point(650, 265);
            chkBoxText.Name = "chkBoxText";
            chkBoxText.Size = new Size(95, 41);
            chkBoxText.TabIndex = 17;
            chkBoxText.Text = "Text";
            chkBoxText.UseVisualStyleBackColor = true;
            chkBoxText.CheckedChanged += chkBoxText_CheckedChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(39, 202);
            label1.Name = "label1";
            label1.Size = new Size(117, 32);
            label1.TabIndex = 18;
            label1.Text = "Search in:";
            // 
            // label2
            // 
            label2.AutoSize = true;
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
            // frmAdvancedSearch
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(960, 604);
            Controls.Add(chkMatchCase);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(chkBoxText);
            Controls.Add(chkBoxProcFn);
            Controls.Add(chkBoxVars);
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
        private CheckBox chkBoxVars;
        private CheckBox chkBoxProcFn;
        private CheckBox chkBoxText;
        private Label label1;
        private Label label2;
        private CheckBox chkMatchCase;
    }
}