namespace BasViewer.GUI
{
    partial class SearchNavControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnPrev = new Button();
            labStatus = new Label();
            btnStop = new Button();
            btnNext = new Button();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 384F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1483, 209);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnPrev);
            flowLayoutPanel1.Controls.Add(labStatus);
            flowLayoutPanel1.Controls.Add(btnStop);
            flowLayoutPanel1.Controls.Add(btnNext);
            flowLayoutPanel1.Location = new Point(552, 3);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(378, 54);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // btnPrev
            // 
            btnPrev.Font = new Font("Segoe UI", 10.125F);
            btnPrev.Location = new Point(3, 3);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(100, 46);
            btnPrev.TabIndex = 1;
            btnPrev.Text = "<<";
            btnPrev.UseVisualStyleBackColor = true;
            // 
            // labStatus
            // 
            labStatus.AutoSize = true;
            labStatus.Font = new Font("Segoe UI", 10.125F);
            labStatus.Location = new Point(109, 0);
            labStatus.Name = "labStatus";
            labStatus.Size = new Size(103, 37);
            labStatus.TabIndex = 2;
            labStatus.Text = "03/112";
            // 
            // btnStop
            // 
            btnStop.Location = new Point(218, 3);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(50, 46);
            btnStop.TabIndex = 1;
            btnStop.Text = "X";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnNext
            // 
            btnNext.Font = new Font("Segoe UI", 10.125F);
            btnNext.Location = new Point(274, 3);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(100, 46);
            btnNext.TabIndex = 0;
            btnNext.Text = ">>";
            btnNext.UseVisualStyleBackColor = true;
            // 
            // SearchNavControl
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.LightSkyBlue;
            Controls.Add(tableLayoutPanel1);
            Name = "SearchNavControl";
            Size = new Size(1483, 209);
            tableLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnPrev;
        private Label labStatus;
        private Button btnStop;
        private Button btnNext;
    }
}
