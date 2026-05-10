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
        public frmAdvancedSearch()
        {
            InitializeComponent();
            chkBoxVars.Checked = false;
            chkBoxProcFn.Checked = false;
            chkBoxText.Checked = false;
        }

        private void chkBoxVars_CheckedChanged(object sender, EventArgs e)
        {
            chkReal.Enabled = chkBoxVars.Checked;
            chkInt.Enabled = chkBoxVars.Checked;
            chkString.Enabled = chkBoxVars.Checked;
        }

        private void chkBoxProcFn_CheckedChanged(object sender, EventArgs e)
        {
            chkFn.Enabled = chkBoxProcFn.Checked;
            chkProc.Enabled = chkBoxProcFn.Checked;
        }

        private void chkBoxText_CheckedChanged(object sender, EventArgs e)
        {
            chkLiteralString.Enabled = chkBoxText.Checked;
            chkRem.Enabled = chkBoxText.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            //BasTools.BAnalysis.
        }
    }
}
