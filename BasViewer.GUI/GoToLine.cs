using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BasViewer.GUI
{
    public partial class GoToLine : Form
    {
        private readonly Form1 _owner;
        public GoToLine(Form1 owner)
        {
            InitializeComponent();
            _owner = owner;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            _owner.DoGotoLine(textBox1.Text);
        }
        public void FocusTextbox()
        {
            textBox1.Focus();
            textBox1.SelectAll();
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                e.Handled = true;
                this.Hide();
                return;
            }
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                _owner.DoGotoLine(textBox1.Text);
                this.Hide();
                return;
            }
            // Allow digits
            if (char.IsDigit(e.KeyChar))
                return;

            // Allow control keys that appear as KeyChar = 0
            if (char.IsControl(e.KeyChar))
                return;

            // Block everything else
            e.Handled = true;
        }
    }
}
