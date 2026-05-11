using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BasViewer.GUI
{
    public partial class frmSearchNav : Form
    {
        public Action? PrevClicked;
        public Action? NextClicked;
        public Action? Closed;

        public frmSearchNav()
        {
            InitializeComponent();
            btnPrev.Click += (s, e) => PrevClicked?.Invoke();
            btnNext.Click += (s, e) => NextClicked?.Invoke();
        }

        public void UpdateStatus(int index, int total)
        {
            labStatus.Text = $"{index + 1} / {total}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            Closed?.Invoke();
        }
    }
}
