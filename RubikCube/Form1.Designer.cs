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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.MoveTimer = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.StateBox = new System.Windows.Forms.PictureBox();
            this.TransparencyBox = new System.Windows.Forms.CheckBox();
            this.SolutionLbl = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.IterTimeBox = new System.Windows.Forms.Label();
            this.PauseBtn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.solveWorker = new System.ComponentModel.BackgroundWorker();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveClustersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showClusterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.makeMovesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoMovesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.stateGridBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tRubikCubeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tglView1 = new TGL.TGLView();
            this.tglView2 = new TGL.TGLView();
            this.tglView3 = new TGL.TGLView();
            this.tglView4 = new TGL.TGLView();
            this.lblViewXYZ = new System.Windows.Forms.Label();
            this.lblViewXYW = new System.Windows.Forms.Label();
            this.lblViewXZW = new System.Windows.Forms.Label();
            this.lblViewYZW = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.MovesLbl = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stateGridBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tRubikCubeBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // MoveTimer
            // 
            this.MoveTimer.Interval = 4;
            this.MoveTimer.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.MovesLbl);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.StateBox);
            this.panel1.Controls.Add(this.TransparencyBox);
            this.panel1.Controls.Add(this.SolutionLbl);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.IterTimeBox);
            this.panel1.Controls.Add(this.PauseBtn);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.numericUpDown1);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.chart1);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(445, 790);
            this.panel1.TabIndex = 8;
            // 
            // StateBox
            // 
            this.StateBox.BackColor = System.Drawing.Color.Black;
            this.StateBox.Location = new System.Drawing.Point(43, 428);
            this.StateBox.Name = "StateBox";
            this.StateBox.Size = new System.Drawing.Size(350, 350);
            this.StateBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.StateBox.TabIndex = 30;
            this.StateBox.TabStop = false;
            this.StateBox.Paint += new System.Windows.Forms.PaintEventHandler(this.StateBox_Paint);
            // 
            // TransparencyBox
            // 
            this.TransparencyBox.AutoSize = true;
            this.TransparencyBox.Location = new System.Drawing.Point(411, 28);
            this.TransparencyBox.Name = "TransparencyBox";
            this.TransparencyBox.Size = new System.Drawing.Size(18, 17);
            this.TransparencyBox.TabIndex = 29;
            this.TransparencyBox.UseVisualStyleBackColor = true;
            this.TransparencyBox.CheckedChanged += new System.EventHandler(this.TransparencyBox_CheckedChanged);
            // 
            // SolutionLbl
            // 
            this.SolutionLbl.AutoSize = true;
            this.SolutionLbl.Location = new System.Drawing.Point(358, 78);
            this.SolutionLbl.Name = "SolutionLbl";
            this.SolutionLbl.Size = new System.Drawing.Size(16, 17);
            this.SolutionLbl.TabIndex = 28;
            this.SolutionLbl.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(252, 78);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(100, 17);
            this.label10.TabIndex = 27;
            this.label10.Text = "Solution Count";
            // 
            // IterTimeBox
            // 
            this.IterTimeBox.AutoSize = true;
            this.IterTimeBox.Location = new System.Drawing.Point(243, 404);
            this.IterTimeBox.Name = "IterTimeBox";
            this.IterTimeBox.Size = new System.Drawing.Size(62, 17);
            this.IterTimeBox.TabIndex = 26;
            this.IterTimeBox.Text = "Iter time:";
            // 
            // PauseBtn
            // 
            this.PauseBtn.Location = new System.Drawing.Point(207, 3);
            this.PauseBtn.Name = "PauseBtn";
            this.PauseBtn.Size = new System.Drawing.Size(90, 39);
            this.PauseBtn.TabIndex = 23;
            this.PauseBtn.Text = "Pause";
            this.PauseBtn.UseVisualStyleBackColor = true;
            this.PauseBtn.Click += new System.EventHandler(this.button3_Click_1);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(320, 3);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(109, 17);
            this.label9.TabIndex = 22;
            this.label9.Text = "Segment Count:";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(329, 23);
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(64, 22);
            this.numericUpDown1.TabIndex = 21;
            this.numericUpDown1.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(29, 78);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(38, 17);
            this.label8.TabIndex = 20;
            this.label8.Text = "time:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(24, 54);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 17);
            this.label7.TabIndex = 19;
            this.label7.Text = "error:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(171, 404);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(46, 17);
            this.label6.TabIndex = 18;
            this.label6.Text = "label6";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(136, 404);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 17);
            this.label5.TabIndex = 17;
            this.label5.Text = "iters:";
            // 
            // chart1
            // 
            chartArea1.AxisX.Title = "iteration";
            chartArea1.AxisY.Title = "error";
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Location = new System.Drawing.Point(0, 98);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(439, 303);
            this.chart1.TabIndex = 14;
            this.chart1.Text = "chart1";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(59, 404);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 17);
            this.label4.TabIndex = 13;
            this.label4.Text = "label4";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 404);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 17);
            this.label3.TabIndex = 12;
            this.label3.Text = "states:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(73, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "label2";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(16, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(89, 39);
            this.button2.TabIndex = 10;
            this.button2.Text = "Shuffle";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(73, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 9;
            this.label1.Text = "label1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(111, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 39);
            this.button1.TabIndex = 8;
            this.button1.Text = "Solve!";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1202, 28);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveClustersToolStripMenuItem,
            this.showClusterToolStripMenuItem,
            this.makeMovesToolStripMenuItem,
            this.undoMovesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveClustersToolStripMenuItem
            // 
            this.saveClustersToolStripMenuItem.Name = "saveClustersToolStripMenuItem";
            this.saveClustersToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.saveClustersToolStripMenuItem.Text = "Save Clusters";
            this.saveClustersToolStripMenuItem.Click += new System.EventHandler(this.saveClustersToolStripMenuItem_Click);
            // 
            // showClusterToolStripMenuItem
            // 
            this.showClusterToolStripMenuItem.Name = "showClusterToolStripMenuItem";
            this.showClusterToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.showClusterToolStripMenuItem.Text = "Show Cluster";
            this.showClusterToolStripMenuItem.Click += new System.EventHandler(this.showClusterToolStripMenuItem_Click);
            // 
            // makeMovesToolStripMenuItem
            // 
            this.makeMovesToolStripMenuItem.Name = "makeMovesToolStripMenuItem";
            this.makeMovesToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.makeMovesToolStripMenuItem.Text = "Make Moves";
            this.makeMovesToolStripMenuItem.Click += new System.EventHandler(this.makeMovesToolStripMenuItem_Click);
            // 
            // undoMovesToolStripMenuItem
            // 
            this.undoMovesToolStripMenuItem.Name = "undoMovesToolStripMenuItem";
            this.undoMovesToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.undoMovesToolStripMenuItem.Text = "Undo Moves";
            this.undoMovesToolStripMenuItem.Click += new System.EventHandler(this.undoMovesToolStripMenuItem_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(1004, 28);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(157, 151);
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // stateGridBindingSource
            // 
            this.stateGridBindingSource.DataMember = "StateGrid";
            this.stateGridBindingSource.DataSource = this.tRubikCubeBindingSource;
            // 
            // tRubikCubeBindingSource
            // 
            this.tRubikCubeBindingSource.DataSource = typeof(RubikCube.TRubikCube);
            // 
            // tglView1 (XYZ - top left)
            //
            this.tglView1.BackColor = System.Drawing.Color.White;
            this.tglView1.Location = new System.Drawing.Point(445, 50);
            this.tglView1.Name = "tglView1";
            this.tglView1.Size = new System.Drawing.Size(370, 370);
            this.tglView1.TabIndex = 0;
            this.tglView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseDown);
            this.tglView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseMove);
            //
            // tglView2 (XYW - top right)
            //
            this.tglView2.BackColor = System.Drawing.Color.White;
            this.tglView2.Location = new System.Drawing.Point(825, 50);
            this.tglView2.Name = "tglView2";
            this.tglView2.Size = new System.Drawing.Size(370, 370);
            this.tglView2.TabIndex = 1;
            this.tglView2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView2_MouseDown);
            this.tglView2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView2_MouseMove);
            //
            // tglView3 (XZW - bottom left)
            //
            this.tglView3.BackColor = System.Drawing.Color.White;
            this.tglView3.Location = new System.Drawing.Point(445, 440);
            this.tglView3.Name = "tglView3";
            this.tglView3.Size = new System.Drawing.Size(370, 370);
            this.tglView3.TabIndex = 2;
            this.tglView3.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView3_MouseDown);
            this.tglView3.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView3_MouseMove);
            //
            // tglView4 (YZW - bottom right)
            //
            this.tglView4.BackColor = System.Drawing.Color.White;
            this.tglView4.Location = new System.Drawing.Point(825, 440);
            this.tglView4.Name = "tglView4";
            this.tglView4.Size = new System.Drawing.Size(370, 370);
            this.tglView4.TabIndex = 3;
            this.tglView4.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView4_MouseDown);
            this.tglView4.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView4_MouseMove);
            //
            // lblViewXYZ
            //
            this.lblViewXYZ.AutoSize = true;
            this.lblViewXYZ.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblViewXYZ.Location = new System.Drawing.Point(445, 28);
            this.lblViewXYZ.Name = "lblViewXYZ";
            this.lblViewXYZ.Size = new System.Drawing.Size(100, 17);
            this.lblViewXYZ.TabIndex = 100;
            this.lblViewXYZ.Text = "XYZ (W=1)";
            //
            // lblViewXYW
            //
            this.lblViewXYW.AutoSize = true;
            this.lblViewXYW.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblViewXYW.Location = new System.Drawing.Point(825, 28);
            this.lblViewXYW.Name = "lblViewXYW";
            this.lblViewXYW.Size = new System.Drawing.Size(100, 17);
            this.lblViewXYW.TabIndex = 101;
            this.lblViewXYW.Text = "XYW (Z=1)";
            //
            // lblViewXZW
            //
            this.lblViewXZW.AutoSize = true;
            this.lblViewXZW.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblViewXZW.Location = new System.Drawing.Point(445, 418);
            this.lblViewXZW.Name = "lblViewXZW";
            this.lblViewXZW.Size = new System.Drawing.Size(100, 17);
            this.lblViewXZW.TabIndex = 102;
            this.lblViewXZW.Text = "XZW (Y=1)";
            //
            // lblViewYZW
            //
            this.lblViewYZW.AutoSize = true;
            this.lblViewYZW.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblViewYZW.Location = new System.Drawing.Point(825, 418);
            this.lblViewYZW.Name = "lblViewYZW";
            this.lblViewYZW.Size = new System.Drawing.Size(100, 17);
            this.lblViewYZW.TabIndex = 103;
            this.lblViewYZW.Text = "YZW (X=1)";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(252, 54);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(90, 17);
            this.label11.TabIndex = 31;
            this.label11.Text = "Moves Count";
            // 
            // MovesLbl
            // 
            this.MovesLbl.AutoSize = true;
            this.MovesLbl.Location = new System.Drawing.Point(358, 54);
            this.MovesLbl.Name = "MovesLbl";
            this.MovesLbl.Size = new System.Drawing.Size(16, 17);
            this.MovesLbl.TabIndex = 32;
            this.MovesLbl.Text = "0";
            // 
            // TRubikForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1202, 818);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblViewYZW);
            this.Controls.Add(this.lblViewXZW);
            this.Controls.Add(this.lblViewXYW);
            this.Controls.Add(this.lblViewXYZ);
            this.Controls.Add(this.tglView4);
            this.Controls.Add(this.tglView3);
            this.Controls.Add(this.tglView2);
            this.Controls.Add(this.tglView1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TRubikForm";
            this.Text = "Rubik\'s Cube";
            this.Load += new System.EventHandler(this.TRubikForm_Load);
            this.Resize += new System.EventHandler(this.TRubikForm_Resize);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stateGridBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tRubikCubeBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TGL.TGLView tglView1;
        private TGL.TGLView tglView2;
        private TGL.TGLView tglView3;
        private TGL.TGLView tglView4;
        private System.Windows.Forms.Label lblViewXYZ;
        private System.Windows.Forms.Label lblViewXYW;
        private System.Windows.Forms.Label lblViewXZW;
        private System.Windows.Forms.Label lblViewYZW;
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
        private System.Windows.Forms.NumericUpDown numericUpDown1;
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
    }
}

