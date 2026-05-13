using BasTools.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BasViewer.GUI
{
    public partial class frmAdvancedSearch : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BasToolsEngine? Engine { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<string, SearchOptions>? RunSearch { get; set; }
        string[] tips;
        int tipsIndex;

        public frmAdvancedSearch()
        {
            InitializeComponent();
            chkBoxVars.Checked = false;
            chkBoxProcFn.Checked = false;
            chkBoxText.Checked = false;
            chkWholeWords.Checked = true;
            tips = new string[] {
                "Search works better when you look for one term at a time",
                "You cannot search for variables when a text file is loaded",
                "For arrays, add empty brackets () and deselect Whole Words" };
            tipsIndex = 0;
        }
        private void DoSearch()
        {
            SearchOptions opts = new();
            opts.whole_word = chkWholeWords.Checked;
            opts.match_case = chkMatchCase.Checked;
            opts.flgRealVars = chkReal.Enabled && chkReal.Checked;
            opts.flgIntegers = chkInt.Enabled && chkInt.Checked;
            opts.flgStrings = chkString.Enabled && chkString.Checked;
            opts.flgProcs = chkProc.Enabled && chkProc.Checked;
            opts.flgFns = chkFn.Enabled && chkFn.Checked;
            opts.flgLiteralStrings = chkLiteralString.Enabled && chkLiteralString.Checked;
            opts.flgRems = chkRem.Enabled && chkRem.Checked;

            // pass control to the callback in Form1
            RunSearch?.Invoke(txtBoxAdvSearch.Text, opts);

            // Hide the dialog (not close)
            this.Hide();
        }
        public void SetVariableEnabled(bool IsTextFile)
        {
            chkBoxVars.Enabled = !IsTextFile;
            chkReal.Enabled = !IsTextFile;
            chkInt.Enabled = !IsTextFile;
            chkString.Enabled = !IsTextFile;
        }
        public void SetMessage(string msg)
        {
            labTip.Visible = false;
            labMessage.Text = msg;
            txtBoxAdvSearch.Focus();
        }
        public void SetTextFocus()
        {
            this.txtBoxAdvSearch.Focus();
            labMessage.Text = tips[(tipsIndex++) % 3];
            labTip.Visible = true;
        }
        private void chkBoxVars_CheckedChanged(object sender, EventArgs e)
        {
            chkReal.Checked = chkBoxVars.Checked;
            chkInt.Checked = chkBoxVars.Checked;
            chkString.Checked = chkBoxVars.Checked;
        }

        private void chkBoxProcFn_CheckedChanged(object sender, EventArgs e)
        {
            chkFn.Checked = chkBoxProcFn.Checked;
            chkProc.Checked = chkBoxProcFn.Checked;
        }

        private void chkBoxText_CheckedChanged(object sender, EventArgs e)
        {
            chkLiteralString.Checked = chkBoxText.Checked;
            chkRem.Checked = chkBoxText.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!Engine.Analyzed)
            {
                bool analyzed = false; // dummy
                Engine.Analyse(Engine, ref analyzed);
            }
            DoSearch();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;   // prevent destruction
                this.Hide();       // just hide it
            }
            base.OnFormClosing(e);
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
           this.Hide();
        }
    }
}
