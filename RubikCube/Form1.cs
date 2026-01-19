using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGL;
using System.Diagnostics;
using System.Threading;
using System.IO;
using GA;
using TGL.GA;
using TGL.GA.Configuration;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;

namespace RubikCube
{
    public partial class TRubikForm : Form
    {
        //TSolver Solver;
        int GACount;
        TimeSpan IterElapsed;
        TimeSpan Time;
        int MovesCount;
        List<TMove> Moves = new List<TMove>();
        int MoveNo;
        double HighScore;
        public TRubikCube RubikCube;// Display cube
        public TRubikCube _gaCube; // Separate cube for GA calculations
        RubikGASolver? _solver;
        CancellationTokenSource? _cts;
        SolutionDatabase _solutionDb;

        // Background thread for GA
        private Task? _gaTask;
        private bool _isGaRunning;
        private readonly ConcurrentQueue<TMove> _moveQueue = new();

        // Chart data for LiveCharts
        private ObservableCollection<ObservableValue> _fitnessValues = new();

        // GA Configuration (from UI)
        private SolverMode _selectedSolverMode = SolverMode.Iterative;
        private GAConfig _selectedGAConfig = GAPresets.Default;
        private int _generationsPerIteration = 100;
        TShape Root = new TShape();
        public TRubikForm()
        {
            InitializeComponent();
            //Camera = cubeView.Context.Camera;
            //Camera.Parent = Scene.Root;
            //var light = new TLight();
            //light.Parent = Camera;
            //light.Origin = new TVector(0, 0, 1);
            //TransparencyBox.Checked = true;
            cubeView.MouseWheel += TglView1_MouseWheel;
        }

        private void TglView1_MouseWheel(object sender, MouseEventArgs e)
        {
            Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 2), (float)e.Delta / 60);
            Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 3), (float)e.Delta / 60);
            cubeView.Invalidate();
        }

        private void TRubikForm_Load(object sender, EventArgs e)
        {
            cubeView.Context.Root = Root;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;
            _gaCube = new TRubikCube(); // Separate cube for GA

            // Initialize solution database
            _solutionDb = new SolutionDatabase(SolutionPath);
            _solutionDb.Load();
            _solutionDb.SolutionSaved += count => BeginInvoke(new Action(() => SolutionLbl.Text = count.ToString()));
            SolutionLbl.Text = _solutionDb.Count.ToString();

            // Initialize LiveCharts
            fitnessChart.Series = new ISeries[]
            {
                new LineSeries<ObservableValue>
                {
                    Values = _fitnessValues,
                    Fill = null,
                    GeometrySize = 0
                }
            };
            fitnessChart.XAxes = new Axis[] { new Axis { Name = "Generation" } };
            fitnessChart.YAxes = new Axis[] { new Axis { Name = "Fitness" } };

            // Initialize GA configuration controls
            cmbSolverMode.SelectedIndex = 0; // Iterative
            cmbPreset.SelectedIndex = 0; // Default
            cmbSelection.SelectedIndex = 0; // Unique
            cmbCrossover.SelectedIndex = 0; // SinglePoint
            cmbMutationType.SelectedIndex = 0; // SingleGene
            UpdateGAConfigFromUI();
        }

        Point StartPos;
        private void OnCubeViewMouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void OnCubeViewMouseMove(object sender, MouseEventArgs e)
        {
            cubeView.Cursor = Cursors.Hand;
            if (e.Button == MouseButtons.Left)
            {
                var rot = new TVector();
                rot.Y = -180 * (e.X - StartPos.X) / cubeView.Width;
                rot.X = -180 * (e.Y - StartPos.Y) / cubeView.Height;
                Root.Rotate(1, rot.Y);
                Root.Rotate(0, rot.X);
                cubeView.Invalidate();
                StartPos = e.Location;
            }
        }
        int FrameNo;
        int FrameCount = 10;
        bool IsPaused = true;

        TShape ActSlice;
        public void Group(List<TCubie> selection)
        {
            ActSlice = new TShape();
            for (int i = 0; i < selection.Count; i++)
                selection[i].Parent = ActSlice;
            ActSlice.Parent = RubikCube;
        }
        public void UnGroup()
        {
            for (int i = ActSlice.Children.Count - 1; i >= 0; i--)
                ActSlice.Children[i].Parent = RubikCube;
            ActSlice.Parent = null;
        }

        private void OnMoveTimerTick(object sender, EventArgs e)
        {
            // Process moves from the queue if Moves list is empty
            if (MoveNo >= Moves.Count && Moves.Count == 0)
            {
                // Try to get moves from the queue
                while (_moveQueue.TryDequeue(out var queuedMove))
                {
                    Moves.Add(queuedMove);
                }
                MoveNo = 0;
            }

            if (MoveNo < Moves.Count)
            {
                TMove move = Moves[MoveNo];
                if (FrameNo == 0)
                    Group(RubikCube.SelectSlice(move));
                FrameNo++;
                if (FrameNo <= FrameCount)
                {
                    double angle = 90 * (move.Angle + 1);
                    if (angle > 180) angle -= 360;
                    angle *= (double)FrameNo / FrameCount;
                    ActSlice.Transform = TAffine.CreateRotation(move.Plane, angle);
                }
                else
                {
                    UnGroup();
                    RubikCube.Turn(move);
                    FrameNo = 0;
                    MoveNo++;
                }
                cubeView.Invalidate();
            }
            else if (MoveNo > 0)
            {
                MoveNo = 0;
                Moves.Clear();
                errorValueLbl.Text = HighScore.ToString("F3");
                statesValueLbl.Text = RubikCube.Code.Count(x => x != '\0').ToString();
                GACount++;
                itersValueLbl.Text = GACount.ToString();
                MovesLbl.Text = MovesCount.ToString();
                StateBox.Invalidate();
            }
            else
            {
                // No moves to process
                timeValueLbl.Text = Time.ToString(@"hh\:mm\:ss");

                // Only stop timer if GA is not running and no moves in queue
                if (!_isGaRunning && _moveQueue.IsEmpty)
                {
                    MoveTimer.Stop();
                }
            }
        }

        //int ActIdx;

        void DisplayState(Graphics gc)
        {
            var grid = RubikCube.StateGrid;
            var bmp = new Bitmap(grid.GetLength(0), grid.GetLength(1));
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (grid[y, x] == 0)
                        bmp.SetPixel(x, y, Color.White);
                    else
                    {
                        var r = 255 / 16.0 * (1 + grid[y, x] & 0xF);
                        var g = 255 / 16.0 * (1 + (grid[y, x] >> 4) & 0xF);
                        var b = 255 / 16.0 * (1 + (grid[y, x] >> 8) & 0xF);
                        bmp.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));
                    }
                }
            gc.DrawImage(bmp, StateBox.ClientRectangle);
        }

        void OnProgress(TRubikGenome specimen)
        {
            _fitnessValues.Add(new ObservableValue(specimen.Fitness));
            if (_fitnessValues.Count > 100)
                _fitnessValues.RemoveAt(0);

            var iterTime = Watch.Elapsed - IterElapsed;
            IterTimeBox.Text = "Iter time:" + iterTime.Milliseconds;
            IterTimeBox.Refresh();
            IterElapsed += iterTime;
        }

        // Thread-safe version called from background thread via BeginInvoke
        void OnProgressThreadSafe(double fitness, TimeSpan elapsed)
        {
            _fitnessValues.Add(new ObservableValue(fitness));
            if (_fitnessValues.Count > 100)
                _fitnessValues.RemoveAt(0);

            var iterTime = elapsed - IterElapsed;
            IterTimeBox.Text = "Iter time:" + iterTime.TotalMilliseconds.ToString("F0") + "ms";
            IterElapsed = elapsed;
        }

        Stopwatch Watch;

        void StartGaBackground()
        {
            if (_isGaRunning) return;

            _isGaRunning = true;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // Update UI
            solveBtn.Enabled = false;

            // Get UI values before starting background task
            var populationSize = (int)numPopulation.Value;
            var mutationRate = (double)numMutation.Value / 100.0;
            var eliteCount = (int)numElite.Value;
            var genomeLength = (int)numChromosomeLength.Value;
            var maxGenerations = (int)numGenerations.Value;
            var solverMode = _selectedSolverMode;
            var baseConfig = _selectedGAConfig;

            // Start animation timer if not already running
            if (!MoveTimer.Enabled)
                MoveTimer.Start();

            // Run GA on background thread
            _gaTask = Task.Run(() => RunGaLoop(token, populationSize, mutationRate, eliteCount, genomeLength, maxGenerations, solverMode, baseConfig), token);
        }

        void RunGaLoop(CancellationToken token, int populationSize, double mutationRate, int eliteCount, int genomeLength, int maxGenerations, SolverMode solverMode, GAConfig baseConfig)
        {
            Watch = Stopwatch.StartNew();
            IterElapsed = TimeSpan.Zero;

            try
            {
                // Build GA config
                var gaConfig = baseConfig with
                {
                    PopulationSize = populationSize,
                    MutationRate = mutationRate,
                    EliteCount = eliteCount,
                    GenomeLength = genomeLength,
                    Termination = baseConfig.Termination with
                    {
                        MaxGenerations = maxGenerations
                    }
                };

                var solverConfig = new SolverConfig
                {
                    Mode = solverMode,
                    GenerationsPerIteration = maxGenerations
                };

                // Create solver once - it handles everything internally
                _solver = new RubikGASolver(_gaCube, gaConfig, solverConfig);
                _solver.SolutionDb = _solutionDb;

                // Subscribe to events
                _solver.GenerationCompleted += state =>
                {
                    if (state.Best != null)
                    {
                        var elapsed = Watch?.Elapsed ?? TimeSpan.Zero;
                        var fitness = state.Best.Fitness;
                        BeginInvoke(new Action(() => OnProgressThreadSafe(fitness, elapsed)));
                    }
                };

                _solver.MovesReady += moves =>
                {
                    // Queue moves for display cube animation
                    foreach (var move in moves)
                        _moveQueue.Enqueue(move);

                    // Wait for animation to catch up
                    while (!_moveQueue.IsEmpty && !token.IsCancellationRequested)
                    {
                        Thread.Sleep(50);
                    }
                };

                _solver.IterationCompleted += result =>
                {
                    var elapsed = Watch.Elapsed;
                    BeginInvoke(new Action(() =>
                    {
                        MovesCount += result.Moves.Count;
                        Time = elapsed;
                        HighScore = result.Fitness;
                    }));
                };

                _solver.ClusterChanged += cube =>
                {
                    // Cluster changed - reset high score for new cluster
                    HighScore = cube.Evaluate();
                };

                // Run solver - handles clusters, TrySolutions, everything
                var finalResult = _solver.Solve(token);
            }
            catch (OperationCanceledException)
            {
                // GA was cancelled
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => MessageBox.Show($"GA Error: {ex.Message}")));
            }
            finally
            {
                // GA finished or cancelled - update UI on UI thread
                BeginInvoke(new Action(() =>
                {
                    _isGaRunning = false;
                    _solver = null;
                    solveBtn.Enabled = true;
                }));
            }
        }

        private void OnSolveClicked(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled && Moves.Count > 0) return;
            if (_isGaRunning) return;

            MovesCount = 0;
            Time = TimeSpan.Zero;
            GACount = 0;
            IsPaused = false;
            HighScore = 0;
            _fitnessValues.Clear();

            StartGaBackground();
        }

        private void OnShuffleClicked(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled || _isGaRunning) return;
            IsPaused = true;
            var rnd = TChromosome.Rnd;
            for (int i = 0; i < 30; i++)
            {
                // Use _gaCube consistently for both selecting moves AND applying them
                // (like MAUI does) to keep _gaCube state consistent
                _gaCube.ActiveCubie = _gaCube.Cubies[rnd.Next(_gaCube.Cubies.Length)];
                var freeMoves = _gaCube.GetFreeMoves();
                var code = freeMoves[rnd.Next(freeMoves.Count)];
                var move = TMove.Decode(code);

                // Apply move to GA cube immediately
                _gaCube.Turn(move);

                // Queue move for display cube animation
                Moves.Add(move);
            }
            MoveTimer.Start();
        }

        string SolutionPath = "solutions.bin";

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //var S = new StreamWriter(Application.StartupPath + "\\Moves.txt");
            //using (S)
            //{
            //    for (int i = 0; i < AllMoves.Count; i++)
            //    {
            //        var move = AllMoves[i];
            //        S.Write(move.Axis);
            //        S.Write(";");
            //        S.Write(move.Slice);
            //        S.Write(";");
            //        S.WriteLine(move.Angle);
            //    }
            //}
            SaveConfig();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Moves.Clear();
            //var S = new StreamReader(Application.StartupPath + "\\Moves.txt");
            //using (S)
            //{
            //    while (!S.EndOfStream)
            //    {
            //        var line = S.ReadLine().Split(';');
            //        var move = new TMove();
            //        move.Axis = int.Parse(line[0]);
            //        move.Slice = int.Parse(line[1]);
            //        move.Angle = int.Parse(line[2]);
            //        Moves.Add(move);
            //    }
            //}
            //MoveNo = 0;
            //timer1.Start();
            openFileDialog1.InitialDirectory = Application.StartupPath;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadConfig(Path.GetFileName(openFileDialog1.FileName));
            }
        }

        string ConfigPath = "Config.bin";
        void SaveConfig(string fileName = null)
        {
            if (fileName == null) fileName = ConfigPath;
            var S = new StreamWriter(Application.StartupPath + "/" + fileName);
            using (S)
            {
                S.Write(RubikCube.Code);
            }
        }

        void LoadConfig(string fileName = null)
        {
            if (fileName == null) fileName = ConfigPath;
            var S = new StreamReader(Application.StartupPath + "/" + fileName);
            using (S)
            {
                var code = S.ReadLine();
                TRubikCube.Size = (int)Math.Round(Math.Pow(code.Length, 0.33));
                RubikCube.Parent = null;
                RubikCube = new TRubikCube();
                RubikCube.Parent = Root;
                RubikCube.Code = code;
                cubeView.Invalidate();
            }
        }

        private void stateSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var spaceForm = new TSpaceForm();
            //spaceForm.Solutions = Solutions;
            spaceForm.ShowDialog();
        }

        private void OnSlicesValueChanged(object sender, EventArgs e)
        {
            TRubikCube.Size = (int)SlicesBox.Value;
            UpdateView();
        }

        void UpdateView()
        {
            Root = new TShape();
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;
            _gaCube = new TRubikCube(); // Separate cube for GA, same initial state
            cubeView.Context.Root = Root;
            cubeView.Invalidate();
            StateBox.Invalidate();
            Moves.Clear();
            // Clear move queue
            while (_moveQueue.TryDequeue(out _)) { }
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            IsPaused = !IsPaused;
            if (IsPaused)
            {
                // Stop GA
                _cts?.Cancel();
                _isGaRunning = false;
                PauseBtn.BackColor = Color.Red;
                solveBtn.Enabled = true;
            }
            else
            {
                PauseBtn.BackColor = DefaultBackColor;
                _fitnessValues.Clear();
                // Resume by starting GA again
                StartGaBackground();
            }
        }

        private void saveClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TRubikCube.Size = 7;
            RubikCube.Parent = null;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;

            var cubies = new List<TCubie>();
            cubies.Add(RubikCube.Cubies[0]);
            cubies.Add(RubikCube.Cubies[1]);
            cubies.Add(RubikCube.Cubies[2]);
            cubies.Add(RubikCube.Cubies[3]);
            cubies.Add(RubikCube.Cubies[4]);
            cubies.Add(RubikCube.Cubies[5]);

            for (int i = 0; i < cubies.Count; i++)
            {
                foreach (var cubie in RubikCube.Cubies)
                    cubie.State = 3;
                RubikCube.ActiveCubie = cubies[i];
                var cluster = RubikCube.ActiveCluster;
                foreach (var cubie in cluster)
                    cubie.State = 0;
                SaveConfig("Cluster" + i.ToString() + ".cfg");
            }
        }

        private void TransparencyBox_CheckedChanged(object sender, EventArgs e)
        {
            cubeView.Context.IsTransparencyOn = TransparencyBox.Checked;
        }

        private void showClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TRubikCube.Size = 5;
            //RubikCube.Parent = null;
            //RubikCube = new TRubikCube();
            //RubikCube.Parent = Root;
            ////cubeView.Context.Root.LoadIdentity();
            //Root.Rotation = new TVector();
            //foreach (var cubie in RubikCube.Cubies)
            //{
            //    cubie.State = 3;
            //    cubie.Transparent = true;
            //}
            ////var ccubie = RubikCube.Cubies[0, 2, 0];
            //for (var level = 1; level <= 9; level++)
            //{
            //    RubikCube.GetActCubie();
            //    foreach (var ccubie in RubikCube.ActCluster)
            //    {
            //        ccubie.State = 0;
            //        ccubie.Transparent = false;
            //    }
            //}
            //Camera.Roll(45);
            ////cubeView.Context.Root.RotateY(90);
            //Camera.Pitch(225);
            //cubeView.Invalidate();
        }

        private void undoMovesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //var actCubie = RubikCube.Cubies[0];
            //var idx = new int[] { actCubie.X, actCubie.Y, actCubie.Z };
            //var A = new TMove();
            //var B = new TMove();
            //var C = new TMove();
            //for (int j = 1; j < 3; j++)
            //    if (Math.Abs(idx[j] - TRubikCube.C) > Math.Abs(idx[A.Plane] - TRubikCube.C))
            //        A.Plane = j;
            //B.Plane = (A.Plane + 1) % 3;
            //C.Plane = (A.Plane + 2) % 3;

        }

        private void StateBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            DisplayState(e.Graphics);
        }

        private void OnDimensionValueChanged(object sender, EventArgs e)
        {
            TAffine.N = (int)DimsBox.Value;
            UpdateView();
        }

        #region GA Configuration Event Handlers

        private void cmbSolverMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedSolverMode = cmbSolverMode.SelectedIndex switch
            {
                0 => SolverMode.Iterative,
                1 => SolverMode.Complete,
                2 => SolverMode.Adaptive,
                _ => SolverMode.Iterative
            };
        }

        private void cmbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            _selectedGAConfig = cmbPreset.SelectedIndex switch
            {
                0 => GAPresets.Default,
                1 => GAPresets.Fast,
                2 => GAPresets.Exploratory,
                3 => GAPresets.Exploitative,
                4 => GAPresets.LongRun,
                _ => GAPresets.Default
            };

            // Update UI controls to match preset
            UpdateGAConfigUI();
        }

        private void cmbSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selection = cmbSelection.SelectedIndex switch
            {
                0 => SelectionStrategy.Unique,
                1 => SelectionStrategy.Tournament,
                2 => SelectionStrategy.Rank,
                3 => SelectionStrategy.Roulette,
                4 => SelectionStrategy.RouletteRank,
                _ => SelectionStrategy.Unique
            };
            _selectedGAConfig = _selectedGAConfig with { Selection = selection };
        }

        private void cmbCrossover_SelectedIndexChanged(object sender, EventArgs e)
        {
            var crossover = cmbCrossover.SelectedIndex switch
            {
                0 => CrossoverStrategy.SinglePoint,
                1 => CrossoverStrategy.TwoPoint,
                2 => CrossoverStrategy.Uniform,
                3 => CrossoverStrategy.SegmentPreserving,
                _ => CrossoverStrategy.SinglePoint
            };
            _selectedGAConfig = _selectedGAConfig with { Crossover = crossover };
        }

        private void cmbMutationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mutation = cmbMutationType.SelectedIndex switch
            {
                0 => MutationStrategy.SingleGene,
                1 => MutationStrategy.Random,
                2 => MutationStrategy.Swap,
                3 => MutationStrategy.Inversion,
                4 => MutationStrategy.Scramble,
                5 => MutationStrategy.Conjugation,
                6 => MutationStrategy.Commutator,
                7 => MutationStrategy.Neighbor,
                8 => MutationStrategy.Simplify,
                9 => MutationStrategy.InverseSequence,
                10 => MutationStrategy.Insert,
                _ => MutationStrategy.SingleGene
            };
            _selectedGAConfig = _selectedGAConfig with { Mutation = mutation };
        }

        private void numGAParam_ValueChanged(object sender, EventArgs e)
        {
            UpdateGAConfigFromUI();
        }

        private void resetGAConfigBtn_Click(object sender, EventArgs e)
        {
            // Reset to default values
            _selectedSolverMode = SolverMode.Iterative;
            _selectedGAConfig = GAPresets.Default;
            _generationsPerIteration = 100;

            // Update combo boxes
            cmbSolverMode.SelectedIndex = 0;
            cmbPreset.SelectedIndex = 0;
            cmbSelection.SelectedIndex = 0;
            cmbCrossover.SelectedIndex = 0;
            cmbMutationType.SelectedIndex = 0;

            // Update numeric controls
            UpdateGAConfigUI();
        }

        private void UpdateGAConfigUI()
        {
            numPopulation.Value = _selectedGAConfig.PopulationSize;
            numMutation.Value = (int)(_selectedGAConfig.MutationRate * 100);
            numGenerations.Value = Math.Min(numGenerations.Maximum, _selectedGAConfig.Termination.MaxGenerations);
            numElite.Value = _selectedGAConfig.EliteCount;
            numChromosomeLength.Value = Math.Min(numChromosomeLength.Maximum, _selectedGAConfig.GenomeLength);

            // Update selection combo
            cmbSelection.SelectedIndex = _selectedGAConfig.Selection switch
            {
                SelectionStrategy.Unique => 0,
                SelectionStrategy.Tournament => 1,
                SelectionStrategy.Rank => 2,
                SelectionStrategy.Roulette => 3,
                SelectionStrategy.RouletteRank => 4,
                _ => 0
            };

            // Update crossover combo
            cmbCrossover.SelectedIndex = _selectedGAConfig.Crossover switch
            {
                CrossoverStrategy.SinglePoint => 0,
                CrossoverStrategy.TwoPoint => 1,
                CrossoverStrategy.Uniform => 2,
                CrossoverStrategy.SegmentPreserving => 3,
                _ => 0
            };

            // Update mutation type combo
            cmbMutationType.SelectedIndex = _selectedGAConfig.Mutation switch
            {
                MutationStrategy.SingleGene => 0,
                MutationStrategy.Random => 1,
                MutationStrategy.Swap => 2,
                MutationStrategy.Inversion => 3,
                MutationStrategy.Scramble => 4,
                MutationStrategy.Conjugation => 5,
                MutationStrategy.Commutator => 6,
                MutationStrategy.Neighbor => 7,
                MutationStrategy.Simplify => 8,
                MutationStrategy.InverseSequence => 9,
                MutationStrategy.Insert => 10,
                _ => 0
            };
        }

        private void UpdateGAConfigFromUI()
        {
            _selectedGAConfig = _selectedGAConfig with
            {
                PopulationSize = (int)numPopulation.Value,
                MutationRate = (double)numMutation.Value / 100.0,
                EliteCount = (int)numElite.Value,
                GenomeLength = (int)numChromosomeLength.Value,
                Termination = _selectedGAConfig.Termination with
                {
                    MaxGenerations = (int)numGenerations.Value
                }
            };
            _generationsPerIteration = (int)numGenerations.Value;
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            // Stop the solver if running
            _cts?.Cancel();
            _isGaRunning = false;
            _solver = null;
            MoveTimer.Stop();

            // Clear moves and move queue
            Moves.Clear();
            while (_moveQueue.TryDequeue(out _)) { }
            MoveNo = 0;

            // Reset animation state
            if (ActSlice != null)
            {
                UnGroup();
            }
            FrameNo = 0;

            // Recreate cube
            UpdateView();

            // Reset statistics
            MovesCount = 0;
            Time = TimeSpan.Zero;
            GACount = 0;
            HighScore = 0;

            // Re-enable solve button
            solveBtn.Enabled = true;

            // Update UI
            errorValueLbl.Text = "0.000";
            timeValueLbl.Text = "00:00:00";
            statesValueLbl.Text = "0";
            itersValueLbl.Text = "0";
            MovesLbl.Text = "0";
            SolutionLbl.Text = _solutionDb.Count.ToString();
            _fitnessValues.Clear();
        }

        #endregion
    }
}
