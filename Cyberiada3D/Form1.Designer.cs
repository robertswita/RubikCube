namespace Cyberiada3D
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.MoveTimer = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SegCountBox = new System.Windows.Forms.NumericUpDown();
            this.button1 = new System.Windows.Forms.Button();
            this.tglView1 = new TGL.TGLView();
            this.DataChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.ErrorLbl = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SegCountBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataChart)).BeginInit();
            this.SuspendLayout();
            // 
            // MoveTimer
            // 
            this.MoveTimer.Interval = 10;
            this.MoveTimer.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.ErrorLbl);
            this.panel1.Controls.Add(this.DataChart);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.SegCountBox);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(838, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(298, 716);
            this.panel1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(75, 446);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(126, 54);
            this.button2.TabIndex = 3;
            this.button2.Text = "Solve!";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 564);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(116, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Segments Count:";
            // 
            // SegCountBox
            // 
            this.SegCountBox.Location = new System.Drawing.Point(25, 595);
            this.SegCountBox.Name = "SegCountBox";
            this.SegCountBox.Size = new System.Drawing.Size(78, 22);
            this.SegCountBox.TabIndex = 1;
            this.SegCountBox.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.SegCountBox.ValueChanged += new System.EventHandler(this.SegCountBox_ValueChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(88, 638);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 0;
            this.button1.Text = "Shuffle";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tglView1
            // 
            this.tglView1.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.tglView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tglView1.Location = new System.Drawing.Point(0, 0);
            this.tglView1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tglView1.Name = "tglView1";
            this.tglView1.Size = new System.Drawing.Size(838, 716);
            this.tglView1.TabIndex = 0;
            this.tglView1.Load += new System.EventHandler(this.tglView1_Load);
            this.tglView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseDown);
            this.tglView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseMove);
            this.tglView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseUp);
            // 
            // DataChart
            // 
            chartArea1.Name = "ChartArea1";
            this.DataChart.ChartAreas.Add(chartArea1);
            this.DataChart.Location = new System.Drawing.Point(0, 25);
            this.DataChart.Name = "DataChart";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Name = "Series1";
            this.DataChart.Series.Add(series1);
            this.DataChart.Size = new System.Drawing.Size(300, 300);
            this.DataChart.TabIndex = 4;
            this.DataChart.Text = "chart1";
            // 
            // ErrorLbl
            // 
            this.ErrorLbl.AutoSize = true;
            this.ErrorLbl.Location = new System.Drawing.Point(100, 353);
            this.ErrorLbl.Name = "ErrorLbl";
            this.ErrorLbl.Size = new System.Drawing.Size(46, 17);
            this.ErrorLbl.TabIndex = 5;
            this.ErrorLbl.Text = "label2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1136, 716);
            this.Controls.Add(this.tglView1);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SegCountBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DataChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TGL.TGLView tglView1;
        private System.Windows.Forms.Timer MoveTimer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown SegCountBox;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.DataVisualization.Charting.Chart DataChart;
        private System.Windows.Forms.Label ErrorLbl;
    }
}

