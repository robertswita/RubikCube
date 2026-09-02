namespace RubikCube
{
    partial class TRubikForm
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
            components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            MoveTimer = new System.Windows.Forms.Timer(components);
            panel1 = new System.Windows.Forms.Panel();
            TimeBox = new System.Windows.Forms.TextBox();
            chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            SeqCountLbl = new System.Windows.Forms.Label();
            label13 = new System.Windows.Forms.Label();
            DimsBox = new System.Windows.Forms.NumericUpDown();
            label12 = new System.Windows.Forms.Label();
            MovesLbl = new System.Windows.Forms.Label();
            label11 = new System.Windows.Forms.Label();
            StateBox = new System.Windows.Forms.PictureBox();
            TransparencyBox = new System.Windows.Forms.CheckBox();
            SolutionLbl = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            IterTimeBox = new System.Windows.Forms.Label();
            PauseBtn = new System.Windows.Forms.Button();
            label9 = new System.Windows.Forms.Label();
            SlicesBox = new System.Windows.Forms.NumericUpDown();
            label8 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            button2 = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            solveWorker = new System.ComponentModel.BackgroundWorker();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveClustersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showClusterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            makeMovesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            undoMovesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            stateGridBindingSource = new System.Windows.Forms.BindingSource(components);
            tRubikCubeBindingSource = new System.Windows.Forms.BindingSource(components);
            saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            tglView1 = new TGL.TGLView();
            ItersBox = new System.Windows.Forms.TextBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)chart2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)DimsBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)StateBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)SlicesBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)chart1).BeginInit();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)stateGridBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tRubikCubeBindingSource).BeginInit();
            SuspendLayout();
            // 
            // MoveTimer
            // 
            MoveTimer.Interval = 4;
            MoveTimer.Tick += timer1_Tick;
            // 
            // panel1
            // 
            panel1.Controls.Add(ItersBox);
            panel1.Controls.Add(TimeBox);
            panel1.Controls.Add(chart2);
            panel1.Controls.Add(SeqCountLbl);
            panel1.Controls.Add(label13);
            panel1.Controls.Add(DimsBox);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(MovesLbl);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(StateBox);
            panel1.Controls.Add(TransparencyBox);
            panel1.Controls.Add(SolutionLbl);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(IterTimeBox);
            panel1.Controls.Add(PauseBtn);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(SlicesBox);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(chart1);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(button2);
            panel1.Controls.Add(button1);
            panel1.Dock = System.Windows.Forms.DockStyle.Left;
            panel1.Location = new System.Drawing.Point(0, 28);
            panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(498, 994);
            panel1.TabIndex = 8;
            // 
            // TimeBox
            // 
            TimeBox.Location = new System.Drawing.Point(77, 138);
            TimeBox.Name = "TimeBox";
            TimeBox.Size = new System.Drawing.Size(103, 27);
            TimeBox.TabIndex = 38;
            // 
            // chart2
            // 
            chartArea1.AxisX.Title = "iteration";
            chartArea1.AxisY.Title = "error";
            chartArea1.Name = "ChartArea1";
            chart2.ChartAreas.Add(chartArea1);
            chart2.Location = new System.Drawing.Point(250, 165);
            chart2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chart2.Name = "chart2";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Name = "Series1";
            chart2.Series.Add(series1);
            chart2.Size = new System.Drawing.Size(244, 336);
            chart2.TabIndex = 37;
            chart2.Text = "chart2";
            // 
            // SeqCountLbl
            // 
            SeqCountLbl.AutoSize = true;
            SeqCountLbl.Location = new System.Drawing.Point(337, 505);
            SeqCountLbl.Name = "SeqCountLbl";
            SeqCountLbl.Size = new System.Drawing.Size(76, 20);
            SeqCountLbl.TabIndex = 36;
            SeqCountLbl.Text = "SeqCount:";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(299, 11);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(98, 20);
            label13.TabIndex = 35;
            label13.Text = "Transparency:";
            // 
            // DimsBox
            // 
            DimsBox.Location = new System.Drawing.Point(88, 9);
            DimsBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            DimsBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            DimsBox.Name = "DimsBox";
            DimsBox.Size = new System.Drawing.Size(64, 27);
            DimsBox.TabIndex = 34;
            DimsBox.Value = new decimal(new int[] { 3, 0, 0, 0 });
            DimsBox.ValueChanged += numericUpDown2_ValueChanged;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(12, 11);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(83, 20);
            label12.TabIndex = 33;
            label12.Text = "Dimension:";
            label12.Click += label12_Click;
            // 
            // MovesLbl
            // 
            MovesLbl.AutoSize = true;
            MovesLbl.Location = new System.Drawing.Point(362, 111);
            MovesLbl.Name = "MovesLbl";
            MovesLbl.Size = new System.Drawing.Size(17, 20);
            MovesLbl.TabIndex = 32;
            MovesLbl.Text = "0";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(256, 111);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(95, 20);
            label11.TabIndex = 31;
            label11.Text = "Moves Count";
            // 
            // StateBox
            // 
            StateBox.BackColor = System.Drawing.Color.Black;
            StateBox.Location = new System.Drawing.Point(43, 535);
            StateBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            StateBox.Name = "StateBox";
            StateBox.Size = new System.Drawing.Size(350, 438);
            StateBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            StateBox.TabIndex = 30;
            StateBox.TabStop = false;
            StateBox.Paint += StateBox_Paint;
            // 
            // TransparencyBox
            // 
            TransparencyBox.AutoSize = true;
            TransparencyBox.Location = new System.Drawing.Point(399, 12);
            TransparencyBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            TransparencyBox.Name = "TransparencyBox";
            TransparencyBox.Size = new System.Drawing.Size(18, 17);
            TransparencyBox.TabIndex = 29;
            TransparencyBox.UseVisualStyleBackColor = true;
            TransparencyBox.CheckedChanged += TransparencyBox_CheckedChanged;
            // 
            // SolutionLbl
            // 
            SolutionLbl.AutoSize = true;
            SolutionLbl.Location = new System.Drawing.Point(362, 141);
            SolutionLbl.Name = "SolutionLbl";
            SolutionLbl.Size = new System.Drawing.Size(17, 20);
            SolutionLbl.TabIndex = 28;
            SolutionLbl.Text = "0";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(256, 141);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(107, 20);
            label10.TabIndex = 27;
            label10.Text = "Solution Count";
            // 
            // IterTimeBox
            // 
            IterTimeBox.AutoSize = true;
            IterTimeBox.Location = new System.Drawing.Point(243, 505);
            IterTimeBox.Name = "IterTimeBox";
            IterTimeBox.Size = new System.Drawing.Size(68, 20);
            IterTimeBox.TabIndex = 26;
            IterTimeBox.Text = "Iter time:";
            // 
            // PauseBtn
            // 
            PauseBtn.Location = new System.Drawing.Point(239, 46);
            PauseBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            PauseBtn.Name = "PauseBtn";
            PauseBtn.Size = new System.Drawing.Size(90, 49);
            PauseBtn.TabIndex = 23;
            PauseBtn.Text = "Pause";
            PauseBtn.UseVisualStyleBackColor = true;
            PauseBtn.Click += button3_Click_1;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(171, 11);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(49, 20);
            label9.TabIndex = 22;
            label9.Text = "Slices:";
            // 
            // SlicesBox
            // 
            SlicesBox.Location = new System.Drawing.Point(224, 9);
            SlicesBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            SlicesBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            SlicesBox.Name = "SlicesBox";
            SlicesBox.Size = new System.Drawing.Size(64, 27);
            SlicesBox.TabIndex = 21;
            SlicesBox.Value = new decimal(new int[] { 3, 0, 0, 0 });
            SlicesBox.ValueChanged += numericUpDown1_ValueChanged;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(33, 141);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(42, 20);
            label8.TabIndex = 20;
            label8.Text = "time:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(28, 111);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(44, 20);
            label7.TabIndex = 19;
            label7.Text = "error:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(136, 505);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(40, 20);
            label5.TabIndex = 17;
            label5.Text = "iters:";
            // 
            // chart1
            // 
            chartArea2.AxisX.Title = "iteration";
            chartArea2.AxisY.Title = "error";
            chartArea2.Name = "ChartArea1";
            chart1.ChartAreas.Add(chartArea2);
            chart1.Location = new System.Drawing.Point(0, 165);
            chart1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chart1.Name = "chart1";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series2.Name = "Series1";
            chart1.Series.Add(series2);
            chart1.Size = new System.Drawing.Size(244, 336);
            chart1.TabIndex = 14;
            chart1.Text = "chart1";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(59, 505);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(50, 20);
            label4.TabIndex = 13;
            label4.Text = "label4";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(3, 505);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(50, 20);
            label3.TabIndex = 12;
            label3.Text = "states:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(77, 111);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(50, 20);
            label2.TabIndex = 11;
            label2.Text = "label2";
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(16, 46);
            button2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(89, 49);
            button2.TabIndex = 10;
            button2.Text = "Shuffle";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(125, 46);
            button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(90, 49);
            button1.TabIndex = 8;
            button1.Text = "Solve!";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(1202, 28);
            menuStrip1.TabIndex = 9;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openToolStripMenuItem, saveToolStripMenuItem, saveClustersToolStripMenuItem, showClusterToolStripMenuItem, makeMovesToolStripMenuItem, undoMovesToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveClustersToolStripMenuItem
            // 
            saveClustersToolStripMenuItem.Name = "saveClustersToolStripMenuItem";
            saveClustersToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            saveClustersToolStripMenuItem.Text = "Save Clusters";
            saveClustersToolStripMenuItem.Click += saveClustersToolStripMenuItem_Click;
            // 
            // showClusterToolStripMenuItem
            // 
            showClusterToolStripMenuItem.Name = "showClusterToolStripMenuItem";
            showClusterToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            showClusterToolStripMenuItem.Text = "Show Cluster";
            showClusterToolStripMenuItem.Click += showClusterToolStripMenuItem_Click;
            // 
            // makeMovesToolStripMenuItem
            // 
            makeMovesToolStripMenuItem.Name = "makeMovesToolStripMenuItem";
            makeMovesToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            // 
            // undoMovesToolStripMenuItem
            // 
            undoMovesToolStripMenuItem.Name = "undoMovesToolStripMenuItem";
            undoMovesToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            undoMovesToolStripMenuItem.Text = "Undo Moves";
            undoMovesToolStripMenuItem.Click += undoMovesToolStripMenuItem_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(1004, 35);
            pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(157, 189);
            pictureBox1.TabIndex = 10;
            pictureBox1.TabStop = false;
            pictureBox1.Visible = false;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // stateGridBindingSource
            // 
            stateGridBindingSource.DataMember = "StateGrid";
            stateGridBindingSource.DataSource = tRubikCubeBindingSource;
            // 
            // tRubikCubeBindingSource
            // 
            tRubikCubeBindingSource.DataSource = typeof(TRubikCube);
            // 
            // tglView1
            // 
            tglView1.BackColor = System.Drawing.Color.White;
            tglView1.Dock = System.Windows.Forms.DockStyle.Fill;
            tglView1.Location = new System.Drawing.Point(498, 28);
            tglView1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            tglView1.Name = "tglView1";
            tglView1.Size = new System.Drawing.Size(704, 994);
            tglView1.TabIndex = 0;
            tglView1.MouseDown += tglView1_MouseDown;
            tglView1.MouseMove += tglView1_MouseMove;
            // 
            // ItersBox
            // 
            ItersBox.Location = new System.Drawing.Point(171, 505);
            ItersBox.Name = "ItersBox";
            ItersBox.Size = new System.Drawing.Size(73, 27);
            ItersBox.TabIndex = 39;
            // 
            // TRubikForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1202, 1022);
            Controls.Add(pictureBox1);
            Controls.Add(tglView1);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "TRubikForm";
            Text = "Rubik's Cube";
            FormClosed += TRubikForm_FormClosed;
            Load += TRubikForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)chart2).EndInit();
            ((System.ComponentModel.ISupportInitialize)DimsBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)StateBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)SlicesBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)chart1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)stateGridBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)tRubikCubeBindingSource).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private TGL.TGLView tglView1;
        private System.Windows.Forms.Timer MoveTimer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.ComponentModel.BackgroundWorker solveWorker;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown SlicesBox;
        private System.Windows.Forms.Button PauseBtn;
        private System.Windows.Forms.Label IterTimeBox;
        private System.Windows.Forms.Label SolutionLbl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ToolStripMenuItem saveClustersToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.CheckBox TransparencyBox;
        private System.Windows.Forms.ToolStripMenuItem showClusterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem makeMovesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoMovesToolStripMenuItem;
        private System.Windows.Forms.BindingSource tRubikCubeBindingSource;
        private System.Windows.Forms.BindingSource stateGridBindingSource;
        private System.Windows.Forms.PictureBox StateBox;
        private System.Windows.Forms.Label MovesLbl;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.NumericUpDown DimsBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label SeqCountLbl;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox ItersBox;
        private System.Windows.Forms.TextBox TimeBox;
    }
}

