namespace BasViewer.GUI
{
    partial class frmSearchNav
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
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnNext = new Button();
            btnPrev = new Button();
            labStatus = new Label();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnPrev);
            flowLayoutPanel1.Controls.Add(labStatus);
            flowLayoutPanel1.Controls.Add(btnNext);
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(793, 200);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // btnNext
            // 
            btnNext.Font = new Font("Segoe UI", 10.125F);
            btnNext.Location = new Point(238, 3);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(150, 46);
            btnNext.TabIndex = 0;
            btnNext.Text = ">>";
            btnNext.UseVisualStyleBackColor = true;
            // 
            // btnPrev
            // 
            btnPrev.Font = new Font("Segoe UI", 10.125F);
            btnPrev.Location = new Point(3, 3);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(150, 46);
            btnPrev.TabIndex = 1;
            btnPrev.Text = "<<";
            btnPrev.UseVisualStyleBackColor = true;
            // 
            // labStatus
            // 
            labStatus.AutoSize = true;
            labStatus.Font = new Font("Segoe UI", 10.125F);
            labStatus.Location = new Point(159, 0);
            labStatus.Name = "labStatus";
            labStatus.Size = new Size(73, 37);
            labStatus.TabIndex = 2;
            labStatus.Text = "3/12";
            // 
            // frmSearchNav
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(800, 450);
            Controls.Add(flowLayoutPanel1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "frmSearchNav";
            Padding = new Padding(4);
            Text = "frmSearchNav";
            TopMost = true;
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnNext;
        private Button btnPrev;
        private Label labStatus;
    }
}