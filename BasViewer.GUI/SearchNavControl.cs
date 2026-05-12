using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace BasViewer.GUI
{
    public partial class SearchNavControl : UserControl
    {
        public Action? PrevClicked;
        public Action? NextClicked;
        public Action? Closed;
        public SearchNavControl()
        {
            InitializeComponent();
            btnPrev.Click += (s, e) => PrevClicked?.Invoke();
            btnNext.Click += (s, e) => NextClicked?.Invoke();
        }
        public event Action? StopClicked;

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopClicked?.Invoke();
        }

        public void UpdateStatus(int index, int total)
        {
            labStatus.Text = $"{index + 1} / {total}";
        }
    }
}
