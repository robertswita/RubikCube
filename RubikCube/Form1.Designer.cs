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
            this.MoveTimer = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.MovesLbl = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.StateBox = new System.Windows.Forms.PictureBox();
            this.TransparencyBox = new System.Windows.Forms.CheckBox();
            this.SolutionLbl = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.IterTimeBox = new System.Windows.Forms.Label();
            this.PauseBtn = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.SlicesBox = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.chart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
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
            this.tglView1 = new TGL.TGLView();
            this.tRubikCubeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.label12 = new System.Windows.Forms.Label();
            this.DimsBox = new System.Windows.Forms.NumericUpDown();
            this.label13 = new System.Windows.Forms.Label();
            this.gaConfigGroup = new System.Windows.Forms.GroupBox();
            this.resetGAConfigBtn = new System.Windows.Forms.Button();
            this.lblSolverMode = new System.Windows.Forms.Label();
            this.cmbSolverMode = new System.Windows.Forms.ComboBox();
            this.lblPreset = new System.Windows.Forms.Label();
            this.cmbPreset = new System.Windows.Forms.ComboBox();
            this.lblPopulation = new System.Windows.Forms.Label();
            this.numPopulation = new System.Windows.Forms.NumericUpDown();
            this.lblMutation = new System.Windows.Forms.Label();
            this.numMutation = new System.Windows.Forms.NumericUpDown();
            this.lblMutationPercent = new System.Windows.Forms.Label();
            this.lblGenerations = new System.Windows.Forms.Label();
            this.numGenerations = new System.Windows.Forms.NumericUpDown();
            this.lblSelection = new System.Windows.Forms.Label();
            this.cmbSelection = new System.Windows.Forms.ComboBox();
            this.lblCrossover = new System.Windows.Forms.Label();
            this.cmbCrossover = new System.Windows.Forms.ComboBox();
            this.lblElite = new System.Windows.Forms.Label();
            this.numElite = new System.Windows.Forms.NumericUpDown();
            this.lblMutationType = new System.Windows.Forms.Label();
            this.cmbMutationType = new System.Windows.Forms.ComboBox();
            this.lblChromosomeLength = new System.Windows.Forms.Label();
            this.numChromosomeLength = new System.Windows.Forms.NumericUpDown();
            this.ResetBtn = new System.Windows.Forms.Button();
            this.gaConfigGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPopulation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMutation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGenerations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numElite)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numChromosomeLength)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlicesBox)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stateGridBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tRubikCubeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DimsBox)).BeginInit();
            this.SuspendLayout();
            // 
            // MoveTimer
            // 
            this.MoveTimer.Interval = 4;
            this.MoveTimer.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // panel1
            //
            this.panel1.Controls.Add(this.ResetBtn);
            this.panel1.Controls.Add(this.gaConfigGroup);
            this.panel1.Controls.Add(this.label13);
            this.panel1.Controls.Add(this.DimsBox);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Controls.Add(this.MovesLbl);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.StateBox);
            this.panel1.Controls.Add(this.TransparencyBox);
            this.panel1.Controls.Add(this.SolutionLbl);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.IterTimeBox);
            this.panel1.Controls.Add(this.PauseBtn);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.SlicesBox);
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
            this.panel1.Size = new System.Drawing.Size(437, 790);
            this.panel1.TabIndex = 8;
            // 
            // MovesLbl
            // 
            this.MovesLbl.AutoSize = true;
            this.MovesLbl.Location = new System.Drawing.Point(362, 89);
            this.MovesLbl.Name = "MovesLbl";
            this.MovesLbl.Size = new System.Drawing.Size(14, 16);
            this.MovesLbl.TabIndex = 32;
            this.MovesLbl.Text = "0";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(256, 89);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(85, 16);
            this.label11.TabIndex = 31;
            this.label11.Text = "Moves Count";
            //
            // StateBox
            //
            this.StateBox.BackColor = System.Drawing.Color.Black;
            this.StateBox.Location = new System.Drawing.Point(43, 696);
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
            this.TransparencyBox.Location = new System.Drawing.Point(399, 10);
            this.TransparencyBox.Name = "TransparencyBox";
            this.TransparencyBox.Size = new System.Drawing.Size(18, 17);
            this.TransparencyBox.TabIndex = 29;
            this.TransparencyBox.UseVisualStyleBackColor = true;
            this.TransparencyBox.CheckedChanged += new System.EventHandler(this.TransparencyBox_CheckedChanged);
            // 
            // SolutionLbl
            // 
            this.SolutionLbl.AutoSize = true;
            this.SolutionLbl.Location = new System.Drawing.Point(362, 113);
            this.SolutionLbl.Name = "SolutionLbl";
            this.SolutionLbl.Size = new System.Drawing.Size(14, 16);
            this.SolutionLbl.TabIndex = 28;
            this.SolutionLbl.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(256, 113);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(92, 16);
            this.label10.TabIndex = 27;
            this.label10.Text = "Solution Count";
            //
            // IterTimeBox
            //
            this.IterTimeBox.AutoSize = true;
            this.IterTimeBox.Location = new System.Drawing.Point(243, 672);
            this.IterTimeBox.Name = "IterTimeBox";
            this.IterTimeBox.Size = new System.Drawing.Size(56, 16);
            this.IterTimeBox.TabIndex = 26;
            this.IterTimeBox.Text = "Iter time:";
            // 
            // PauseBtn
            // 
            this.PauseBtn.Location = new System.Drawing.Point(239, 37);
            this.PauseBtn.Name = "PauseBtn";
            this.PauseBtn.Size = new System.Drawing.Size(90, 39);
            this.PauseBtn.TabIndex = 23;
            this.PauseBtn.Text = "Pause";
            this.PauseBtn.UseVisualStyleBackColor = true;
            this.PauseBtn.Click += new System.EventHandler(this.button3_Click_1);
            //
            // ResetBtn
            //
            this.ResetBtn.Location = new System.Drawing.Point(335, 37);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(90, 39);
            this.ResetBtn.TabIndex = 37;
            this.ResetBtn.Text = "Reset";
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            //
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(171, 9);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(47, 16);
            this.label9.TabIndex = 22;
            this.label9.Text = "Slices:";
            // 
            // SlicesBox
            // 
            this.SlicesBox.Location = new System.Drawing.Point(224, 7);
            this.SlicesBox.Name = "SlicesBox";
            this.SlicesBox.Size = new System.Drawing.Size(64, 22);
            this.SlicesBox.TabIndex = 21;
            this.SlicesBox.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.SlicesBox.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(33, 113);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(35, 16);
            this.label8.TabIndex = 20;
            this.label8.Text = "time:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(28, 89);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(38, 16);
            this.label7.TabIndex = 19;
            this.label7.Text = "error:";
            //
            // label6
            //
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(171, 672);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(44, 16);
            this.label6.TabIndex = 18;
            this.label6.Text = "label6";
            //
            // label5
            //
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(136, 672);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 16);
            this.label5.TabIndex = 17;
            this.label5.Text = "iters:";
            //
            // chart1
            //
            this.chart1.Location = new System.Drawing.Point(0, 400);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(421, 269);
            this.chart1.TabIndex = 14;
            //
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(59, 672);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 16);
            this.label4.TabIndex = 13;
            this.label4.Text = "label4";
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 672);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "states:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(77, 89);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "label2";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(16, 37);
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
            this.label1.Location = new System.Drawing.Point(77, 113);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "label1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(125, 37);
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
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(46, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveClustersToolStripMenuItem
            // 
            this.saveClustersToolStripMenuItem.Name = "saveClustersToolStripMenuItem";
            this.saveClustersToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.saveClustersToolStripMenuItem.Text = "Save Clusters";
            this.saveClustersToolStripMenuItem.Click += new System.EventHandler(this.saveClustersToolStripMenuItem_Click);
            // 
            // showClusterToolStripMenuItem
            // 
            this.showClusterToolStripMenuItem.Name = "showClusterToolStripMenuItem";
            this.showClusterToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            this.showClusterToolStripMenuItem.Text = "Show Cluster";
            this.showClusterToolStripMenuItem.Click += new System.EventHandler(this.showClusterToolStripMenuItem_Click);
            // 
            // makeMovesToolStripMenuItem
            // 
            this.makeMovesToolStripMenuItem.Name = "makeMovesToolStripMenuItem";
            this.makeMovesToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
            // 
            // undoMovesToolStripMenuItem
            // 
            this.undoMovesToolStripMenuItem.Name = "undoMovesToolStripMenuItem";
            this.undoMovesToolStripMenuItem.Size = new System.Drawing.Size(178, 26);
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
            // tglView1
            // 
            this.tglView1.BackColor = System.Drawing.Color.White;
            this.tglView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tglView1.Location = new System.Drawing.Point(437, 28);
            this.tglView1.Name = "tglView1";
            this.tglView1.Size = new System.Drawing.Size(765, 790);
            this.tglView1.TabIndex = 0;
            this.tglView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseDown);
            this.tglView1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tglView1_MouseMove);
            // 
            // tRubikCubeBindingSource
            // 
            this.tRubikCubeBindingSource.DataSource = typeof(RubikCube.TRubikCube);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 9);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(74, 16);
            this.label12.TabIndex = 33;
            this.label12.Text = "Dimension:";
            // 
            // DimsBox
            // 
            this.DimsBox.Location = new System.Drawing.Point(88, 7);
            this.DimsBox.Name = "DimsBox";
            this.DimsBox.Size = new System.Drawing.Size(64, 22);
            this.DimsBox.TabIndex = 34;
            this.DimsBox.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.DimsBox.ValueChanged += new System.EventHandler(this.numericUpDown2_ValueChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(299, 9);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(94, 16);
            this.label13.TabIndex = 35;
            this.label13.Text = "Transparency:";
            //
            // gaConfigGroup
            //
            this.gaConfigGroup.Controls.Add(this.resetGAConfigBtn);
            this.gaConfigGroup.Controls.Add(this.lblSolverMode);
            this.gaConfigGroup.Controls.Add(this.cmbSolverMode);
            this.gaConfigGroup.Controls.Add(this.lblPreset);
            this.gaConfigGroup.Controls.Add(this.cmbPreset);
            this.gaConfigGroup.Controls.Add(this.lblPopulation);
            this.gaConfigGroup.Controls.Add(this.numPopulation);
            this.gaConfigGroup.Controls.Add(this.lblMutation);
            this.gaConfigGroup.Controls.Add(this.numMutation);
            this.gaConfigGroup.Controls.Add(this.lblMutationPercent);
            this.gaConfigGroup.Controls.Add(this.lblGenerations);
            this.gaConfigGroup.Controls.Add(this.numGenerations);
            this.gaConfigGroup.Controls.Add(this.lblSelection);
            this.gaConfigGroup.Controls.Add(this.cmbSelection);
            this.gaConfigGroup.Controls.Add(this.lblCrossover);
            this.gaConfigGroup.Controls.Add(this.cmbCrossover);
            this.gaConfigGroup.Controls.Add(this.lblMutationType);
            this.gaConfigGroup.Controls.Add(this.cmbMutationType);
            this.gaConfigGroup.Controls.Add(this.lblElite);
            this.gaConfigGroup.Controls.Add(this.numElite);
            this.gaConfigGroup.Controls.Add(this.lblChromosomeLength);
            this.gaConfigGroup.Controls.Add(this.numChromosomeLength);
            this.gaConfigGroup.Location = new System.Drawing.Point(6, 132);
            this.gaConfigGroup.Name = "gaConfigGroup";
            this.gaConfigGroup.Size = new System.Drawing.Size(425, 260);
            this.gaConfigGroup.TabIndex = 36;
            this.gaConfigGroup.TabStop = false;
            this.gaConfigGroup.Text = "GA Configuration";
            //
            // resetGAConfigBtn
            //
            this.resetGAConfigBtn.Location = new System.Drawing.Point(280, 18);
            this.resetGAConfigBtn.Name = "resetGAConfigBtn";
            this.resetGAConfigBtn.Size = new System.Drawing.Size(130, 25);
            this.resetGAConfigBtn.TabIndex = 0;
            this.resetGAConfigBtn.Text = "Reset to Default";
            this.resetGAConfigBtn.UseVisualStyleBackColor = true;
            this.resetGAConfigBtn.Click += new System.EventHandler(this.resetGAConfigBtn_Click);
            //
            // lblSolverMode
            //
            this.lblSolverMode.AutoSize = true;
            this.lblSolverMode.Location = new System.Drawing.Point(10, 50);
            this.lblSolverMode.Name = "lblSolverMode";
            this.lblSolverMode.Size = new System.Drawing.Size(45, 16);
            this.lblSolverMode.TabIndex = 1;
            this.lblSolverMode.Text = "Mode:";
            //
            // cmbSolverMode
            //
            this.cmbSolverMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSolverMode.FormattingEnabled = true;
            this.cmbSolverMode.Items.AddRange(new object[] {
            "Iterative",
            "Complete",
            "Adaptive"});
            this.cmbSolverMode.Location = new System.Drawing.Point(100, 47);
            this.cmbSolverMode.Name = "cmbSolverMode";
            this.cmbSolverMode.Size = new System.Drawing.Size(120, 24);
            this.cmbSolverMode.TabIndex = 2;
            this.cmbSolverMode.SelectedIndexChanged += new System.EventHandler(this.cmbSolverMode_SelectedIndexChanged);
            //
            // lblPreset
            //
            this.lblPreset.AutoSize = true;
            this.lblPreset.Location = new System.Drawing.Point(230, 50);
            this.lblPreset.Name = "lblPreset";
            this.lblPreset.Size = new System.Drawing.Size(47, 16);
            this.lblPreset.TabIndex = 3;
            this.lblPreset.Text = "Preset:";
            //
            // cmbPreset
            //
            this.cmbPreset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPreset.FormattingEnabled = true;
            this.cmbPreset.Items.AddRange(new object[] {
            "Default",
            "Fast",
            "Exploratory",
            "Exploitative",
            "LongRun"});
            this.cmbPreset.Location = new System.Drawing.Point(290, 47);
            this.cmbPreset.Name = "cmbPreset";
            this.cmbPreset.Size = new System.Drawing.Size(120, 24);
            this.cmbPreset.TabIndex = 4;
            this.cmbPreset.SelectedIndexChanged += new System.EventHandler(this.cmbPreset_SelectedIndexChanged);
            //
            // lblPopulation
            //
            this.lblPopulation.AutoSize = true;
            this.lblPopulation.Location = new System.Drawing.Point(10, 80);
            this.lblPopulation.Name = "lblPopulation";
            this.lblPopulation.Size = new System.Drawing.Size(75, 16);
            this.lblPopulation.TabIndex = 5;
            this.lblPopulation.Text = "Population:";
            //
            // numPopulation
            //
            this.numPopulation.Location = new System.Drawing.Point(100, 78);
            this.numPopulation.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.numPopulation.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numPopulation.Name = "numPopulation";
            this.numPopulation.Size = new System.Drawing.Size(80, 22);
            this.numPopulation.TabIndex = 6;
            this.numPopulation.Value = new decimal(new int[] { 100, 0, 0, 0 });
            this.numPopulation.ValueChanged += new System.EventHandler(this.numGAParam_ValueChanged);
            //
            // lblMutation
            //
            this.lblMutation.AutoSize = true;
            this.lblMutation.Location = new System.Drawing.Point(200, 80);
            this.lblMutation.Name = "lblMutation";
            this.lblMutation.Size = new System.Drawing.Size(61, 16);
            this.lblMutation.TabIndex = 7;
            this.lblMutation.Text = "Mutation:";
            //
            // numMutation
            //
            this.numMutation.Location = new System.Drawing.Point(270, 78);
            this.numMutation.Name = "numMutation";
            this.numMutation.Size = new System.Drawing.Size(60, 22);
            this.numMutation.TabIndex = 8;
            this.numMutation.Value = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMutation.ValueChanged += new System.EventHandler(this.numGAParam_ValueChanged);
            //
            // lblMutationPercent
            //
            this.lblMutationPercent.AutoSize = true;
            this.lblMutationPercent.Location = new System.Drawing.Point(335, 80);
            this.lblMutationPercent.Name = "lblMutationPercent";
            this.lblMutationPercent.Size = new System.Drawing.Size(20, 16);
            this.lblMutationPercent.TabIndex = 9;
            this.lblMutationPercent.Text = "%";
            //
            // lblGenerations
            //
            this.lblGenerations.AutoSize = true;
            this.lblGenerations.Location = new System.Drawing.Point(10, 110);
            this.lblGenerations.Name = "lblGenerations";
            this.lblGenerations.Size = new System.Drawing.Size(82, 16);
            this.lblGenerations.TabIndex = 10;
            this.lblGenerations.Text = "Generations:";
            //
            // numGenerations
            //
            this.numGenerations.Location = new System.Drawing.Point(100, 108);
            this.numGenerations.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.numGenerations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numGenerations.Name = "numGenerations";
            this.numGenerations.Size = new System.Drawing.Size(80, 22);
            this.numGenerations.TabIndex = 11;
            this.numGenerations.Value = new decimal(new int[] { 100, 0, 0, 0 });
            this.numGenerations.ValueChanged += new System.EventHandler(this.numGAParam_ValueChanged);
            //
            // lblSelection
            //
            this.lblSelection.AutoSize = true;
            this.lblSelection.Location = new System.Drawing.Point(10, 140);
            this.lblSelection.Name = "lblSelection";
            this.lblSelection.Size = new System.Drawing.Size(66, 16);
            this.lblSelection.TabIndex = 12;
            this.lblSelection.Text = "Selection:";
            //
            // cmbSelection
            //
            this.cmbSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSelection.FormattingEnabled = true;
            this.cmbSelection.Items.AddRange(new object[] {
            "Unique",
            "Tournament",
            "Rank",
            "Roulette",
            "RouletteRank"});
            this.cmbSelection.Location = new System.Drawing.Point(100, 137);
            this.cmbSelection.Name = "cmbSelection";
            this.cmbSelection.Size = new System.Drawing.Size(120, 24);
            this.cmbSelection.TabIndex = 13;
            this.cmbSelection.SelectedIndexChanged += new System.EventHandler(this.cmbSelection_SelectedIndexChanged);
            //
            // lblCrossover
            //
            this.lblCrossover.AutoSize = true;
            this.lblCrossover.Location = new System.Drawing.Point(10, 170);
            this.lblCrossover.Name = "lblCrossover";
            this.lblCrossover.Size = new System.Drawing.Size(72, 16);
            this.lblCrossover.TabIndex = 14;
            this.lblCrossover.Text = "Crossover:";
            //
            // cmbCrossover
            //
            this.cmbCrossover.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCrossover.FormattingEnabled = true;
            this.cmbCrossover.Items.AddRange(new object[] {
            "SinglePoint",
            "TwoPoint",
            "Uniform",
            "SegmentPreserving"});
            this.cmbCrossover.Location = new System.Drawing.Point(100, 167);
            this.cmbCrossover.Name = "cmbCrossover";
            this.cmbCrossover.Size = new System.Drawing.Size(120, 24);
            this.cmbCrossover.TabIndex = 15;
            this.cmbCrossover.SelectedIndexChanged += new System.EventHandler(this.cmbCrossover_SelectedIndexChanged);
            //
            // lblMutationType
            //
            this.lblMutationType.AutoSize = true;
            this.lblMutationType.Location = new System.Drawing.Point(230, 170);
            this.lblMutationType.Name = "lblMutationType";
            this.lblMutationType.Size = new System.Drawing.Size(65, 16);
            this.lblMutationType.TabIndex = 18;
            this.lblMutationType.Text = "Mut.Type:";
            //
            // cmbMutationType
            //
            this.cmbMutationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMutationType.FormattingEnabled = true;
            this.cmbMutationType.Items.AddRange(new object[] {
            "SingleGene",
            "Random",
            "Swap",
            "Inversion",
            "Scramble",
            "Conjugation",
            "Commutator",
            "Neighbor",
            "Simplify",
            "InverseSequence",
            "Insert"});
            this.cmbMutationType.Location = new System.Drawing.Point(300, 167);
            this.cmbMutationType.Name = "cmbMutationType";
            this.cmbMutationType.Size = new System.Drawing.Size(110, 24);
            this.cmbMutationType.TabIndex = 19;
            this.cmbMutationType.SelectedIndexChanged += new System.EventHandler(this.cmbMutationType_SelectedIndexChanged);
            //
            // lblElite
            //
            this.lblElite.AutoSize = true;
            this.lblElite.Location = new System.Drawing.Point(10, 200);
            this.lblElite.Name = "lblElite";
            this.lblElite.Size = new System.Drawing.Size(36, 16);
            this.lblElite.TabIndex = 16;
            this.lblElite.Text = "Elite:";
            //
            // numElite
            //
            this.numElite.Location = new System.Drawing.Point(100, 198);
            this.numElite.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numElite.Name = "numElite";
            this.numElite.Size = new System.Drawing.Size(60, 22);
            this.numElite.TabIndex = 17;
            this.numElite.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.numElite.ValueChanged += new System.EventHandler(this.numGAParam_ValueChanged);
            //
            // lblChromosomeLength
            //
            this.lblChromosomeLength.AutoSize = true;
            this.lblChromosomeLength.Location = new System.Drawing.Point(170, 200);
            this.lblChromosomeLength.Name = "lblChromosomeLength";
            this.lblChromosomeLength.Size = new System.Drawing.Size(85, 16);
            this.lblChromosomeLength.TabIndex = 20;
            this.lblChromosomeLength.Text = "Chr. Length:";
            //
            // numChromosomeLength
            //
            this.numChromosomeLength.Location = new System.Drawing.Point(260, 198);
            this.numChromosomeLength.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.numChromosomeLength.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numChromosomeLength.Name = "numChromosomeLength";
            this.numChromosomeLength.Size = new System.Drawing.Size(60, 22);
            this.numChromosomeLength.TabIndex = 21;
            this.numChromosomeLength.Value = new decimal(new int[] { 50, 0, 0, 0 });
            this.numChromosomeLength.ValueChanged += new System.EventHandler(this.numGAParam_ValueChanged);
            //
            // TRubikForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2404, 1636);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tglView1);
            this.Controls.Add(this.pictureBox1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TRubikForm";
            this.Text = "Rubik\'s Cube";
            this.Load += new System.EventHandler(this.TRubikForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlicesBox)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stateGridBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tRubikCubeBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DimsBox)).EndInit();
            this.gaConfigGroup.ResumeLayout(false);
            this.gaConfigGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPopulation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMutation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGenerations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numElite)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numChromosomeLength)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart chart1;
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
        private System.Windows.Forms.GroupBox gaConfigGroup;
        private System.Windows.Forms.Button resetGAConfigBtn;
        private System.Windows.Forms.Label lblSolverMode;
        private System.Windows.Forms.ComboBox cmbSolverMode;
        private System.Windows.Forms.Label lblPreset;
        private System.Windows.Forms.ComboBox cmbPreset;
        private System.Windows.Forms.Label lblPopulation;
        private System.Windows.Forms.NumericUpDown numPopulation;
        private System.Windows.Forms.Label lblMutation;
        private System.Windows.Forms.NumericUpDown numMutation;
        private System.Windows.Forms.Label lblMutationPercent;
        private System.Windows.Forms.Label lblGenerations;
        private System.Windows.Forms.NumericUpDown numGenerations;
        private System.Windows.Forms.Label lblSelection;
        private System.Windows.Forms.ComboBox cmbSelection;
        private System.Windows.Forms.Label lblCrossover;
        private System.Windows.Forms.ComboBox cmbCrossover;
        private System.Windows.Forms.Label lblElite;
        private System.Windows.Forms.NumericUpDown numElite;
        private System.Windows.Forms.Label lblMutationType;
        private System.Windows.Forms.ComboBox cmbMutationType;
        private System.Windows.Forms.Label lblChromosomeLength;
        private System.Windows.Forms.NumericUpDown numChromosomeLength;
        private System.Windows.Forms.Button ResetBtn;
    }
}

