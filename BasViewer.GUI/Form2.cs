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

            tips = new string[] {
                "Select category first, then choose the item",
                "When a text file is loaded, search for variables with Quick Search"
            };
            tipsIndex = 0;

            chkFn.Tag = SymbolKind.Fn;
            chkProc.Tag = SymbolKind.Proc;
            chkReal.Tag = SymbolKind.RealVar;
            chkInt.Tag = SymbolKind.IntVar;
            chkString.Tag = SymbolKind.StringVar;
            chkLiteralString.Tag = SymbolKind.LiteralString;
            chkRemContains.Tag = SymbolKind.RemText;
            chkStringContains.Tag = "STRING";
        }
        private void DoSearch()
        {
            bool textsearch = txtSearchString.Visible;
            string searchTerm = textsearch ? txtSearchString.Text : cmbBoxAdvSearch.SelectedItem.ToString();

            SearchOptions opts = new();

            opts.flgRealVars = chkReal.Enabled && chkReal.Checked;
            opts.flgIntegers = chkInt.Enabled && chkInt.Checked;
            opts.flgStrings = chkString.Enabled && chkString.Checked;
            opts.flgProcs = chkProc.Enabled && chkProc.Checked;
            opts.flgFns = chkFn.Enabled && chkFn.Checked;
            opts.flgLiteralStrings = chkLiteralString.Enabled && chkLiteralString.Checked;
            opts.flgRemContains = chkRemContains.Enabled && chkRemContains.Checked;
            opts.flgStringContains = chkStringContains.Enabled && chkStringContains.Checked;
            opts.flgTextSearch = textsearch;
            opts.whole_word = textsearch && chkWholeWords.Checked;
            opts.match_case = textsearch && chkCaseSens.Checked;

            // pass control to the callback in Form1
            if (!string.IsNullOrEmpty(searchTerm))
            {
                RunSearch?.Invoke(searchTerm, opts);
            }
            // Hide the dialog (not close)
            this.Hide();
        }
        public void SetVariableEnabled(bool IsTextFile)
        {
            chkReal.Enabled = !IsTextFile;
            chkInt.Enabled = !IsTextFile;
            chkString.Enabled = !IsTextFile;
        }
        public void SetMessage(string msg)
        {
            labTip.Visible = false;
            labMessage.Text = msg;
        }
        public void SetTextFocus()
        {
            if (txtSearchString.Visible)
                return;

            this.cmbBoxAdvSearch.Focus();
            if (tipsIndex == 1)
            {
                if (chkReal.Enabled) tipsIndex = 0;
            }
            labMessage.Text = tips[(tipsIndex++) % 2];
            labTip.Visible = true;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!Engine.Analyzed)
            {
                SetMessage("Select category and item");
            }
            else
            {
                DoSearch();
            }
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
        private void Radio_CheckedChanged(object sender, EventArgs e)
        {
            var rb = (RadioButton)sender;

            // Ignore the "unchecked" event
            if (!rb.Checked)
                return;

            // Lazy analysis
            if (!Engine.Analyzed)
            {
                bool analyzed = false;
                Engine.Analyse(Engine, ref analyzed);
            }

            cmbBoxAdvSearch.Items.Clear();

            // What kind of item are we looking for?
            if ( rb.Tag == "STRING" || (SymbolKind)rb.Tag == SymbolKind.RemText) // the order here is important
            {
                txtSearchString.Visible = true;
                chkCaseSens.Visible = true;
                chkWholeWords.Visible = true;
                txtSearchString.Focus();
                //txtSearchString.SelectAll();
            }
            else
            {
                txtSearchString.Visible = false;
                chkCaseSens.Visible = false;
                chkWholeWords.Visible = false;

                // Extract the SymbolKind from the Tag
                var kind = (SymbolKind)rb.Tag;

                FillCombobox(kind, Engine.Symbols);

                SetTextFocus();
            }
        }
        private void FillCombobox(SymbolKind kind, Dictionary<string, SymbolInfo> Symbols)
        {
            var list = Symbols.Values.Where(s => s.Kind == kind || (kind == SymbolKind.IntVar && s.Kind == SymbolKind.StaticInt)).OrderBy(s => s.Name).ToList<SymbolInfo>();
            if (list.Count == 0) return;

            foreach (SymbolInfo symInfo in list)
            {
                cmbBoxAdvSearch.Items.Add(symInfo.Name);
            }
            if (cmbBoxAdvSearch.Items.Count > 0)
                cmbBoxAdvSearch.SelectedIndex = 0;
        }
        public void Clear()
        {
            cmbBoxAdvSearch.Items.Clear();

            chkReal.Checked = false;
            chkInt.Checked = false;
            chkString.Checked = false;
            chkFn.Checked = false;
            chkProc.Checked = false;
            chkRemContains.Checked = false;
            chkLiteralString.Checked = false;
            chkStringContains.Checked = false;
        }

        private void cmbBoxAdvSearch_Click(object sender, EventArgs e)
        {
            if (cmbBoxAdvSearch.Items.Count == 0)
                SetMessage("Select target type first");
        }
    }
}
