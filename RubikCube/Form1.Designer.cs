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
            this.controlPanel = new System.Windows.Forms.Panel();
            this.MovesLbl = new System.Windows.Forms.Label();
            this.movesCountLbl = new System.Windows.Forms.Label();
            this.StateBox = new System.Windows.Forms.PictureBox();
            this.TransparencyBox = new System.Windows.Forms.CheckBox();
            this.SolutionLbl = new System.Windows.Forms.Label();
            this.solutionCountLbl = new System.Windows.Forms.Label();
            this.clearSolutionsBtn = new System.Windows.Forms.Button();
            this.IterTimeBox = new System.Windows.Forms.Label();
            this.slicesLbl = new System.Windows.Forms.Label();
            this.SlicesBox = new System.Windows.Forms.NumericUpDown();
            this.shuffleLbl = new System.Windows.Forms.Label();
            this.numShuffleMoves = new System.Windows.Forms.NumericUpDown();
            this.timeLbl = new System.Windows.Forms.Label();
            this.errorLbl = new System.Windows.Forms.Label();
            this.itersValueLbl = new System.Windows.Forms.Label();
            this.itersLbl = new System.Windows.Forms.Label();
            this.fitnessChart = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            this.statesValueLbl = new System.Windows.Forms.Label();
            this.statesLbl = new System.Windows.Forms.Label();
            this.errorValueLbl = new System.Windows.Forms.Label();
            this.shuffleBtn = new System.Windows.Forms.Button();
            this.timeValueLbl = new System.Windows.Forms.Label();
            this.solveBtn = new System.Windows.Forms.Button();
            this.solveWorker = new System.ComponentModel.BackgroundWorker();
            this.mainMenu = new System.Windows.Forms.MenuStrip();
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
            this.cubeView = new TGL.TGLView();
            this.tRubikCubeBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dimensionLbl = new System.Windows.Forms.Label();
            this.DimsBox = new System.Windows.Forms.NumericUpDown();
            this.transparencyLbl = new System.Windows.Forms.Label();
            this.gaConfigGroup = new System.Windows.Forms.GroupBox();
            this.resetGAConfigBtn = new System.Windows.Forms.Button();
            this.savePresetBtn = new System.Windows.Forms.Button();
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
            this.controlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlicesBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShuffleMoves)).BeginInit();
            this.mainMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stateGridBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tRubikCubeBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DimsBox)).BeginInit();
            this.SuspendLayout();
            // 
            // MoveTimer
            // 
            this.MoveTimer.Interval = 4;
            this.MoveTimer.Tick += new System.EventHandler(this.OnMoveTimerTick);
            //
            // panel1
            //
            this.controlPanel.Controls.Add(this.ResetBtn);
            this.controlPanel.Controls.Add(this.gaConfigGroup);
            this.controlPanel.Controls.Add(this.transparencyLbl);
            this.controlPanel.Controls.Add(this.DimsBox);
            this.controlPanel.Controls.Add(this.dimensionLbl);
            this.controlPanel.Controls.Add(this.MovesLbl);
            this.controlPanel.Controls.Add(this.movesCountLbl);
            this.controlPanel.Controls.Add(this.StateBox);
            this.controlPanel.Controls.Add(this.TransparencyBox);
            this.controlPanel.Controls.Add(this.SolutionLbl);
            this.controlPanel.Controls.Add(this.clearSolutionsBtn);
            this.controlPanel.Controls.Add(this.solutionCountLbl);
            this.controlPanel.Controls.Add(this.IterTimeBox);
            this.controlPanel.Controls.Add(this.slicesLbl);
            this.controlPanel.Controls.Add(this.SlicesBox);
            this.controlPanel.Controls.Add(this.shuffleLbl);
            this.controlPanel.Controls.Add(this.numShuffleMoves);
            this.controlPanel.Controls.Add(this.timeLbl);
            this.controlPanel.Controls.Add(this.errorLbl);
            this.controlPanel.Controls.Add(this.itersValueLbl);
            this.controlPanel.Controls.Add(this.itersLbl);
            this.controlPanel.Controls.Add(this.fitnessChart);
            this.controlPanel.Controls.Add(this.statesValueLbl);
            this.controlPanel.Controls.Add(this.statesLbl);
            this.controlPanel.Controls.Add(this.errorValueLbl);
            this.controlPanel.Controls.Add(this.shuffleBtn);
            this.controlPanel.Controls.Add(this.timeValueLbl);
            this.controlPanel.Controls.Add(this.solveBtn);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.controlPanel.Location = new System.Drawing.Point(0, 28);
            this.controlPanel.Name = "panel1";
            this.controlPanel.Size = new System.Drawing.Size(437, 790);
            this.controlPanel.TabIndex = 8;
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
            this.movesCountLbl.AutoSize = true;
            this.movesCountLbl.Location = new System.Drawing.Point(256, 89);
            this.movesCountLbl.Name = "label11";
            this.movesCountLbl.Size = new System.Drawing.Size(85, 16);
            this.movesCountLbl.TabIndex = 31;
            this.movesCountLbl.Text = "Moves Count";
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
            this.TransparencyBox.Location = new System.Drawing.Point(510, 10);
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
            // clearSolutionsBtn
            //
            this.clearSolutionsBtn.Location = new System.Drawing.Point(382, 109);
            this.clearSolutionsBtn.Name = "clearSolutionsBtn";
            this.clearSolutionsBtn.Size = new System.Drawing.Size(23, 23);
            this.clearSolutionsBtn.TabIndex = 65;
            this.clearSolutionsBtn.Text = "🗑";
            this.clearSolutionsBtn.UseVisualStyleBackColor = true;
            this.clearSolutionsBtn.Click += new System.EventHandler(this.OnClearSolutionsClicked);
            //
            // label10
            // 
            this.solutionCountLbl.AutoSize = true;
            this.solutionCountLbl.Location = new System.Drawing.Point(256, 113);
            this.solutionCountLbl.Name = "label10";
            this.solutionCountLbl.Size = new System.Drawing.Size(92, 16);
            this.solutionCountLbl.TabIndex = 27;
            this.solutionCountLbl.Text = "Solution Count";
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
            // ResetBtn
            //
            this.ResetBtn.Location = new System.Drawing.Point(239, 37);
            this.ResetBtn.Name = "ResetBtn";
            this.ResetBtn.Size = new System.Drawing.Size(60, 39);
            this.ResetBtn.TabIndex = 37;
            this.ResetBtn.Text = "↺";
            this.ResetBtn.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ResetBtn.UseVisualStyleBackColor = true;
            this.ResetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            //
            // label9
            // 
            this.slicesLbl.AutoSize = true;
            this.slicesLbl.Location = new System.Drawing.Point(171, 9);
            this.slicesLbl.Name = "label9";
            this.slicesLbl.Size = new System.Drawing.Size(47, 16);
            this.slicesLbl.TabIndex = 22;
            this.slicesLbl.Text = "Slices:";
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
            this.SlicesBox.ValueChanged += new System.EventHandler(this.OnSlicesValueChanged);
            //
            // shuffleLbl
            //
            this.shuffleLbl.AutoSize = true;
            this.shuffleLbl.Location = new System.Drawing.Point(295, 9);
            this.shuffleLbl.Name = "shuffleLbl";
            this.shuffleLbl.Size = new System.Drawing.Size(52, 16);
            this.shuffleLbl.TabIndex = 63;
            this.shuffleLbl.Text = "Shuffle:";
            //
            // numShuffleMoves
            //
            this.numShuffleMoves.Location = new System.Drawing.Point(353, 7);
            this.numShuffleMoves.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numShuffleMoves.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numShuffleMoves.Name = "numShuffleMoves";
            this.numShuffleMoves.Size = new System.Drawing.Size(64, 22);
            this.numShuffleMoves.TabIndex = 64;
            this.numShuffleMoves.Value = new decimal(new int[] { 30, 0, 0, 0 });
            //
            // label8
            //
            this.timeLbl.AutoSize = true;
            this.timeLbl.Location = new System.Drawing.Point(33, 113);
            this.timeLbl.Name = "label8";
            this.timeLbl.Size = new System.Drawing.Size(35, 16);
            this.timeLbl.TabIndex = 20;
            this.timeLbl.Text = "time:";
            // 
            // label7
            // 
            this.errorLbl.AutoSize = true;
            this.errorLbl.Location = new System.Drawing.Point(28, 89);
            this.errorLbl.Name = "label7";
            this.errorLbl.Size = new System.Drawing.Size(38, 16);
            this.errorLbl.TabIndex = 19;
            this.errorLbl.Text = "error:";
            //
            // label6
            //
            this.itersValueLbl.AutoSize = true;
            this.itersValueLbl.Location = new System.Drawing.Point(171, 672);
            this.itersValueLbl.Name = "label6";
            this.itersValueLbl.Size = new System.Drawing.Size(44, 16);
            this.itersValueLbl.TabIndex = 18;
            this.itersValueLbl.Text = "label6";
            //
            // label5
            //
            this.itersLbl.AutoSize = true;
            this.itersLbl.Location = new System.Drawing.Point(136, 672);
            this.itersLbl.Name = "label5";
            this.itersLbl.Size = new System.Drawing.Size(35, 16);
            this.itersLbl.TabIndex = 17;
            this.itersLbl.Text = "iters:";
            //
            // chart1
            //
            this.fitnessChart.Location = new System.Drawing.Point(0, 400);
            this.fitnessChart.Name = "chart1";
            this.fitnessChart.Size = new System.Drawing.Size(421, 269);
            this.fitnessChart.TabIndex = 14;
            //
            // label4
            //
            this.statesValueLbl.AutoSize = true;
            this.statesValueLbl.Location = new System.Drawing.Point(59, 672);
            this.statesValueLbl.Name = "label4";
            this.statesValueLbl.Size = new System.Drawing.Size(44, 16);
            this.statesValueLbl.TabIndex = 13;
            this.statesValueLbl.Text = "label4";
            //
            // label3
            //
            this.statesLbl.AutoSize = true;
            this.statesLbl.Location = new System.Drawing.Point(3, 672);
            this.statesLbl.Name = "label3";
            this.statesLbl.Size = new System.Drawing.Size(46, 16);
            this.statesLbl.TabIndex = 12;
            this.statesLbl.Text = "states:";
            // 
            // label2
            // 
            this.errorValueLbl.AutoSize = true;
            this.errorValueLbl.Location = new System.Drawing.Point(77, 89);
            this.errorValueLbl.Name = "label2";
            this.errorValueLbl.Size = new System.Drawing.Size(44, 16);
            this.errorValueLbl.TabIndex = 11;
            this.errorValueLbl.Text = "0.000";
            //
            // shuffleBtn
            //
            this.shuffleBtn.Location = new System.Drawing.Point(16, 37);
            this.shuffleBtn.Name = "shuffleBtn";
            this.shuffleBtn.Size = new System.Drawing.Size(60, 39);
            this.shuffleBtn.TabIndex = 10;
            this.shuffleBtn.Text = "🔀";
            this.shuffleBtn.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.shuffleBtn.UseVisualStyleBackColor = true;
            this.shuffleBtn.Click += new System.EventHandler(this.OnShuffleClicked);
            // 
            // label1
            // 
            this.timeValueLbl.AutoSize = true;
            this.timeValueLbl.Location = new System.Drawing.Point(77, 113);
            this.timeValueLbl.Name = "label1";
            this.timeValueLbl.Size = new System.Drawing.Size(44, 16);
            this.timeValueLbl.TabIndex = 9;
            this.timeValueLbl.Text = "00:00:00";
            //
            // solveBtn
            //
            this.solveBtn.Location = new System.Drawing.Point(96, 37);
            this.solveBtn.Name = "solveBtn";
            this.solveBtn.Size = new System.Drawing.Size(120, 39);
            this.solveBtn.TabIndex = 8;
            this.solveBtn.Text = "▶ Solve";
            this.solveBtn.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.solveBtn.UseVisualStyleBackColor = true;
            this.solveBtn.Click += new System.EventHandler(this.OnSolveClicked);
            // 
            // menuStrip1
            // 
            this.mainMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.mainMenu.Location = new System.Drawing.Point(0, 0);
            this.mainMenu.Name = "menuStrip1";
            this.mainMenu.Size = new System.Drawing.Size(1202, 28);
            this.mainMenu.TabIndex = 9;
            this.mainMenu.Text = "menuStrip1";
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
            this.cubeView.BackColor = System.Drawing.Color.White;
            this.cubeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cubeView.Location = new System.Drawing.Point(437, 28);
            this.cubeView.Name = "tglView1";
            this.cubeView.Size = new System.Drawing.Size(765, 790);
            this.cubeView.TabIndex = 0;
            this.cubeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnCubeViewMouseDown);
            this.cubeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnCubeViewMouseMove);
            // 
            // tRubikCubeBindingSource
            // 
            this.tRubikCubeBindingSource.DataSource = typeof(RubikCube.TRubikCube);
            // 
            // label12
            // 
            this.dimensionLbl.AutoSize = true;
            this.dimensionLbl.Location = new System.Drawing.Point(12, 9);
            this.dimensionLbl.Name = "label12";
            this.dimensionLbl.Size = new System.Drawing.Size(74, 16);
            this.dimensionLbl.TabIndex = 33;
            this.dimensionLbl.Text = "Dimension:";
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
            this.DimsBox.ValueChanged += new System.EventHandler(this.OnDimensionValueChanged);
            // 
            // label13
            // 
            this.transparencyLbl.AutoSize = true;
            this.transparencyLbl.Location = new System.Drawing.Point(425, 9);
            this.transparencyLbl.Name = "label13";
            this.transparencyLbl.Size = new System.Drawing.Size(94, 16);
            this.transparencyLbl.TabIndex = 35;
            this.transparencyLbl.Text = "Transparency:";
            //
            // gaConfigGroup
            //
            this.gaConfigGroup.Controls.Add(this.resetGAConfigBtn);
            this.gaConfigGroup.Controls.Add(this.savePresetBtn);
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
            this.resetGAConfigBtn.Location = new System.Drawing.Point(220, 18);
            this.resetGAConfigBtn.Name = "resetGAConfigBtn";
            this.resetGAConfigBtn.Size = new System.Drawing.Size(35, 25);
            this.resetGAConfigBtn.TabIndex = 0;
            this.resetGAConfigBtn.Text = "↺";
            this.resetGAConfigBtn.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.resetGAConfigBtn.UseVisualStyleBackColor = true;
            this.resetGAConfigBtn.Click += new System.EventHandler(this.resetGAConfigBtn_Click);
            //
            // savePresetBtn
            //
            this.savePresetBtn.Location = new System.Drawing.Point(180, 18);
            this.savePresetBtn.Name = "savePresetBtn";
            this.savePresetBtn.Size = new System.Drawing.Size(35, 25);
            this.savePresetBtn.TabIndex = 66;
            this.savePresetBtn.Text = "💾";
            this.savePresetBtn.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.savePresetBtn.UseVisualStyleBackColor = true;
            this.savePresetBtn.Click += new System.EventHandler(this.OnSavePresetClicked);
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
            this.ClientSize = new System.Drawing.Size(1980, 1024);
            this.Controls.Add(this.cubeView);
            this.Controls.Add(this.controlPanel);
            this.Controls.Add(this.mainMenu);
            this.Controls.Add(this.pictureBox1);
            this.MainMenuStrip = this.mainMenu;
            this.Name = "TRubikForm";
            this.Text = "Rubik\'s Cube";
            this.Load += new System.EventHandler(this.TRubikForm_Load);
            this.controlPanel.ResumeLayout(false);
            this.controlPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.StateBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SlicesBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numShuffleMoves)).EndInit();
            this.mainMenu.ResumeLayout(false);
            this.mainMenu.PerformLayout();
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

        private TGL.TGLView cubeView;
        private System.Windows.Forms.Timer MoveTimer;
        private System.Windows.Forms.Panel controlPanel;
        private System.Windows.Forms.Label statesValueLbl;
        private System.Windows.Forms.Label statesLbl;
        private System.Windows.Forms.Label errorValueLbl;
        private System.Windows.Forms.Button shuffleBtn;
        private System.Windows.Forms.Label timeValueLbl;
        private System.Windows.Forms.Button solveBtn;
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart fitnessChart;
        private System.ComponentModel.BackgroundWorker solveWorker;
        private System.Windows.Forms.MenuStrip mainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Label itersValueLbl;
        private System.Windows.Forms.Label itersLbl;
        private System.Windows.Forms.Label timeLbl;
        private System.Windows.Forms.Label errorLbl;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label slicesLbl;
        private System.Windows.Forms.NumericUpDown SlicesBox;
        private System.Windows.Forms.Label shuffleLbl;
        private System.Windows.Forms.NumericUpDown numShuffleMoves;
        private System.Windows.Forms.Label IterTimeBox;
        private System.Windows.Forms.Label SolutionLbl;
        private System.Windows.Forms.Label solutionCountLbl;
        private System.Windows.Forms.Button clearSolutionsBtn;
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
        private System.Windows.Forms.Label movesCountLbl;
        private System.Windows.Forms.Label transparencyLbl;
        private System.Windows.Forms.NumericUpDown DimsBox;
        private System.Windows.Forms.Label dimensionLbl;
        private System.Windows.Forms.GroupBox gaConfigGroup;
        private System.Windows.Forms.Button resetGAConfigBtn;
        private System.Windows.Forms.Button savePresetBtn;
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

