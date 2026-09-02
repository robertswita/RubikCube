using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TGL;
using System.Diagnostics;
using System.IO;
using GA;

namespace RubikCube
{
    public partial class TRubikForm : Form
    {
        //TSolver Solver;
        //int GACount;
        TimeSpan IterElapsed;
        TimeSpan Time;
        int MovesCount;
        //List<TMove> Moves = new List<TMove>();
        Dictionary<string, List<TMove>> Solutions = new Dictionary<string, List<TMove>>();
        int MoveNo;
        public TRubikCube RubikCube;// = new TRubikCube();
        public TRubikSolver Solver;   // created fresh in button1_Click (interactive) / per run in the batch -- exists only while solving
        //public TScene Scene = new TScene();
        //public TCamera Camera;
        //TGA<TRubikGenome> Ga;
        TScene Scene = new TScene();
        TLight Light = new TLight();
        TRubikAnimator Animator = new TRubikAnimator();
        //int Scrambled;
        public TRubikForm()
        {
            InitializeComponent();
            tglView1.Context.Scene = Scene;
            //Camera.Parent = Scene.Root;
            Light.Parent = Scene.Root;
            //Light.Transform.Origin = new TVector(0, 0, 1);
            //TransparencyBox.Checked = true;
            tglView1.MouseWheel += TglView1_MouseWheel;
        }

        private void TglView1_MouseWheel(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < TAffine.Planes.Length; i++)
                RubikCube.Rotate(i, (float)e.Delta / 60);
            //Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 2), (float)e.Delta / 60);
            //Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 3), (float)e.Delta / 60);
            tglView1.Invalidate();
        }

        private void TRubikForm_Load(object sender, EventArgs e)
        {
            //tglView1.Context.Root = Scene.Root;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Scene.Root;
            //LoadSolutions();
        }

        Point StartPos;
        private void tglView1_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void tglView1_MouseMove(object sender, MouseEventArgs e)
        {
            tglView1.Cursor = Cursors.Hand;
            if (e.Button == MouseButtons.Left)
            {
                var rot = new TVector();
                rot.Y = 180 * (e.X - StartPos.X) / tglView1.Width;
                rot.X = 180 * (e.Y - StartPos.Y) / tglView1.Height;
                RubikCube.Rotate(1, rot.Y);
                RubikCube.Rotate(0, rot.X);
                tglView1.Invalidate();
                StartPos = e.Location;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (Animator.State)
            {
                case TAnimState.Running:
                    Animator.Tick();
                    tglView1.Invalidate();
                    break;

                case TAnimState.JustFinished:
                    // Celowa przerwa jednego ticku między animacją a Solve():
                    // 1) daje WinForms szansę odmalować ostatnią klatkę i etykiety,
                    // 2) daje GPU chwilę wytchnienia między rundami GA.
                    // NIE scalać z gałęzią Idle poniżej.
                    Animator.Reset();
                    label4.Text = Solver.TotalScrambled.ToString();
                    MovesLbl.Text = MovesCount.ToString();
                    UpdateClusterInfo();
                    TimeBox.Text = Time.ToString();
                    ErrorBox.Text = (100 * Solver.Score).ToString();
                    RubikCube.StateGrid = null;
                    StateBox.Invalidate();
                    break;

                case TAnimState.Idle:
                    MoveTimer.Stop();
                    if (Solver.Best != null)
                        Solve();
                    break;
            }
        }

        void DisplayState(Graphics gc)
        {
            var grid = RubikCube.StateGrid;
            var gridSize = grid.GetLength(0);
            var pal = new Color[256];
            for (int i = 0; i < pal.Length; i++)
                pal[i] = Color.FromArgb(255 - i, 255 - i, 255 - i);
            TMatrix.Palette = pal;
            var bmp = new Bitmap(gridSize * TAffine.N, gridSize * TAffine.N);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                for (int y = 0; y < gridSize; y++)
                    for (int x = 0; x < gridSize; x++)
                    {
                        var M = grid[y, x];
                        if (M != null)
                        {
                            //for (int m = 0; m < M.RowsCount; m++)
                            //    for (int n = 0; n < M.ColsCount; n++)
                            //    {
                            //        var x_ = x * TAffine.N + m;
                            //        var y_ = y * TAffine.N + n;
                            //        if (M[m, n] > 0.1)
                            //            bmp.SetPixel(x_, y_, Color.Black);
                            //        else if (M[m, n] < -0.1)
                            //            bmp.SetPixel(x_, y_, Color.Gray);
                            //    }
                            g.DrawImage(M.Image, x * TAffine.N, y * TAffine.N);
                        }
                        //if (grid[y, x] != 0)
                        //{
                        //    var r = 255 / 16.0 * (1 + grid[y, x] & 0xF);
                        //    var g = 255 / 16.0 * (1 + (grid[y, x] >> 4) & 0xF);
                        //    var b = 255 / 16.0 * (1 + (grid[y, x] >> 8) & 0xF);
                        //    bmp.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));
                        //}
                    }
            }
            gc.DrawImage(bmp, StateBox.ClientRectangle);

            // Thin pale-red grid, one line per cubie-matrix cell, to help locate a cubie's block. Low alpha so
            // the dense grid stays light and doesn't glare (raise the alpha in the Pen if you want it stronger).
            var rect = StateBox.ClientRectangle;
            if (gridSize < rect.Width / 2)
            {
                using (var gridPen = new Pen(Color.FromArgb(70, 210, 40, 40)))
                    for (int k = 0; k <= gridSize; k++)
                    {
                        float gx = rect.X + rect.Width * k / (float)gridSize;
                        float gy = rect.Y + rect.Height * k / (float)gridSize;
                        gc.DrawLine(gridPen, gx, rect.Y, gx, rect.Bottom);
                        gc.DrawLine(gridPen, rect.X, gy, rect.Right, gy);
                    }
            }
        }

        void OnProgress(TRubikGenome specimen)
        {
            //if (chart1.Series[0].Points.Count % 300 == 0)
            //    chart1.Series[0].Points.Clear();
            //var ga = (TGA<TRubikGenome>)sender;
            chart1.Series[0].Points.AddXY(Solver.IterationsCount, 100 * specimen.Fitness);
            chart1.Refresh();
            var iterTime = Solver.ComputeTime.Elapsed - IterElapsed;
            IterTimeBox.Text = "Iter time:" + iterTime.Milliseconds;
            IterTimeBox.Refresh();
            //IterElapsed += iterTime;
            IterElapsed = Solver.ComputeTime.Elapsed;
            //GACount = Iteration;// StartGACount + Ga.IterCount;
            //ItersBox.Text = GACount.ToString();
            //ItersBox.Refresh();
            //chart2.Series[0].Points.Clear();
            //for (int i = 0; i < Ga.Population.Count; i++)
            //    chart2.Series[0].Points.AddY(Ga.Population[i].Fitness);
            //chart2.Refresh();

            //label2.Refresh();
            //label4.Refresh();
        }

        bool TrySolutions = true;

        // Interactive driver: one GaStep, then hand the accepted moves to the timer to ANIMATE (the timer's
        // move-branch Turns them frame by frame, its MoveNo>0 branch latches coherence and re-calls Solve). All
        // the UI (chart, labels, solution-cache probe) lives here; the decision lives in GaStep.
        void Solve()
        {
            bool newCluster = Solver.Score == 0;
            if (newCluster) chart1.Series[0].Points.Clear();

            var moves = Solver.GetNextMoves();

            if (newCluster) UpdateClusterInfo();
            ItersBox.Text = Solver.IterationsCount.ToString();
            ItersBox.Refresh();
            if (Solver.Best == null) return;                            // whole cube solved -> stop (timer not restarted)

            if (moves.Count > 0)                              // a move was accepted
            {
                OnProgress(Solver.Best);
                Animator.Animate(RubikCube, moves);
                TrySolutions = true;
            }
            // Solutions cache probe -- DISABLED during the "search state -> solver" refactor. It scored a
            // hypothetical CUBE, but the target (ActiveCluster/ClusterMoves) now lives on the solver, so scoring
            // it needs a temporary solver bound to the copy (a solver copy ctor that also copies the target).
            // Unused today; revive with that ctor when the solution cache is needed again.
#if false
            if (TrySolutions)
            {
                foreach (var solution in Solutions)
                {
                    var tryMoves = DecodeSolution(solution.Value);
                    for (int j = -1; j < 0 * Solver.ClusterMoves.Count; j++)
                    {
                        var speedMoves = new List<TMove>();
                        if (j < 0)
                            speedMoves.AddRange(tryMoves);
                        else
                        {
                            var move = TMove.Decode(Solver.ClusterMoves[j]);
                            speedMoves.Add(move);
                            speedMoves.AddRange(tryMoves);
                            move = TMove.Decode(Solver.ClusterMoves[j]);
                            move.Angle = 2 - move.Angle;
                            speedMoves.Add(move);
                        }
                        var cube = new TRubikCube(RubikCube);
                        foreach (var move in speedMoves)
                            cube.Turn(move);
                        var score = Gpu.ScoreCube(new TRubikSolver(cube));
                        if (score < Solver.Score)
                        {
                            Solver.Score = score;
                            Moves = speedMoves;
                        }
                    }
                }
            }
#endif
            if (Animator.Moves.Count == 0)
                TrySolutions = false;
            MovesCount += Animator.Moves.Count;
            Time = Solver.ComputeTime.Elapsed;   // authoritative compute time now lives in the solver (works headless too)
            MoveTimer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            MovesCount = 0;
            Time = TimeSpan.Zero;
            IterElapsed = TimeSpan.Zero;
            Solver = new TRubikSolver(RubikCube);   // fresh solver bound to the current cube (construction IS the reset)
            Solver.TrackTrajectory = TrajectoryBox.Checked;
            Solve();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            RubikCube.Scramble();
            RubikCube.StateGrid = null;
            StateBox.Invalidate();
            tglView1.Invalidate();
        }

        string SolutionPath = "solutions.bin";
        // DISABLED with the Solutions probe (uses the target's ActiveCubie, now on the solver). Revive together.
#if false
        List<TMove> DecodeSolution(List<TMove> solution)
        {
            var pos = RubikCube.ActiveCubie.Position;
            var map = new List<int>();
            var result = new List<TMove>();
            var sliceCountInCluster = TAffine.N;
            for (var i = 0; i < solution.Count; i++)
            {
                var move = solution[i];
                if (!move.IsValid) continue;
                var idx = map.IndexOf(move.Slice);
                if (idx < 0)
                {
                    idx = map.IndexOf(TRubikCube.Size - 1 - move.Slice);
                    if (idx < 0)
                    {
                        idx = map.Count;
                        map.Add(move.Slice);
                    }
                    else
                        idx += sliceCountInCluster;
                }
                if (idx < sliceCountInCluster)
                    move.Slice = pos[idx];
                else
                    move.Slice = TRubikCube.Size - 1 - pos[idx - sliceCountInCluster];
                result.Add(TMove.Decode(move.Encode()));
            }
            return result;
        }
#endif

        void LoadSolutions()
        {
            Solutions = new Dictionary<string, List<TMove>>();
            try
            {
                var file = new FileStream(SolutionPath, FileMode.OpenOrCreate);
                using (var reader = new BinaryReader(file))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var key = reader.ReadString();
                        var movesCount = reader.ReadInt32();
                        var solution = new List<TMove>();
                        for (int i = 0; i < movesCount; i++)
                        {
                            var move = new TMove();
                            move.Axis = reader.ReadByte();
                            move.Slice = reader.ReadByte();
                            move.Plane = reader.ReadByte();
                            move.Angle = reader.ReadByte();
                            solution.Add(move);
                        }
                        Solutions.Add(key, solution);
                    }
                }
            }
            catch (Exception) { }
            ;
            SolutionLbl.Text = Solutions.Count.ToString();
        }

        void SaveSolution(TRubikGenome specimen)
        {
            var code = RubikCube.Code;
            if (!Solutions.ContainsKey(code))
            {
                var solution = new List<TMove>();
                var file = new FileStream(SolutionPath, FileMode.Append);
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(code);
                    writer.Write(specimen.BestMovesCount);
                    //var genes = new List<int>(solution.MovesCount);
                    for (int i = 0; i < specimen.BestMovesCount; i++)
                    {
                        //genes.Add((int)solution.Genes[i]);
                        //writer.Write((int)solution.Genes[i]);
                        var move = TMove.Decode((int)specimen.Genes[i]);
                        writer.Write((byte)move.Axis);
                        writer.Write((byte)move.Slice);
                        writer.Write((byte)move.Plane);
                        writer.Write((byte)move.Angle);
                        solution.Add(move);
                    }
                    Solutions.Add(code, solution);
                }
            }
            SolutionLbl.Text = Solutions.Count.ToString();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = Application.StartupPath;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                SaveConfig(saveFileDialog1.FileName);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Application.StartupPath;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                LoadConfig(openFileDialog1.FileName);
        }

        string ConfigPath = "Config.bin";
        void SaveConfig(string fileName = null)
        {
            if (fileName == null) fileName = ConfigPath;
            var S = new StreamWriter(fileName);
            using (S)
            {
                S.WriteLine(TAffine.N);
                S.WriteLine(TRubikCube.Size);
                S.Write(RubikCube.Code);
            }
        }

        void LoadConfig(string fileName = null)
        {
            if (fileName == null) fileName = ConfigPath;
            var S = new StreamReader(fileName);
            using (S)
            {
                Scene.Root = null;
                DimsBox.Value = int.Parse(S.ReadLine());
                SlicesBox.Value = int.Parse(S.ReadLine());
                UpdateView();
                RubikCube.Code = S.ReadToEnd();
            }
        }

        private void stateSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var spaceForm = new TSpaceForm();
            spaceForm.ShowDialog();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            TRubikCube.Size = (int)SlicesBox.Value;
            UpdateView();
        }

        void UpdateView()
        {
            if (Scene.Root != null)
            {
                Scene.Root = new TShape();   // TScene.Root setter re-establishes the scene back-link
                RubikCube = new TRubikCube();
                RubikCube.Parent = Scene.Root;

                // The light persists across dimension changes, but its affine transform is sized to
                // TAffine.N at construction time - stale after N changed, and GatherInstances does
                // parent(N-D) * light.Transform. Rebuild it for the current N (identity + origin on Z)
                // before re-attaching the light to the freshly created Root.
                Light.Transform = new TAffine();
                Light.Parent = Scene.Root;

                tglView1.Invalidate();
                StateBox.Invalidate();
                Animator.State = TAnimState.Idle;
                MoveTimer.Stop();
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            MoveTimer.Enabled = !MoveTimer.Enabled;
            PauseBtn.BackColor = MoveTimer.Enabled ? DefaultBackColor : Color.Red;
        }

        private void saveClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TRubikCube.Size = 7;
            RubikCube.Parent = null;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Scene.Root;

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
                var cluster = RubikCube.Clusters[cubies[i].ClusterIndex - 1];   // the cubie's cluster (geometry, no solver target)
                foreach (var cubie in cluster.Cubies)
                    cubie.State = 0;
                SaveConfig("Cluster" + i.ToString() + ".cfg");
            }
        }

        private void TransparencyBox_CheckedChanged(object sender, EventArgs e)
        {
            tglView1.Context.IsTransparencyOn = TransparencyBox.Checked;
        }

        private void showClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TRubikCube.Size = 5;
            //RubikCube.Parent = null;
            //RubikCube = new TRubikCube();
            //RubikCube.Parent = Root;
            ////tglView1.Context.Root.LoadIdentity();
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
            ////tglView1.Context.Root.RotateY(90);
            //Camera.Pitch(225);
            //tglView1.Invalidate();
        }

        private void greedyTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = TGreedyDiversity.Run();
            System.Diagnostics.Debug.WriteLine(report);
            ShowTextDialog("Greedy decomposition diversity", report);
        }

        private void orientClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = TGreedyDiversity.FindOrientationCluster();
            System.Diagnostics.Debug.WriteLine(report);
            ShowTextDialog("Orientation cluster", report);
        }

        private void verifyManoeuvresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = TGreedyDiversity.Verify();
            System.Diagnostics.Debug.WriteLine(report);
            ShowTextDialog("Manoeuvre verification", report);
        }

        private void seedStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = Gpu.SeedStats(Solver);
            System.Diagnostics.Debug.WriteLine(report);
            ShowTextDialog("Seed pool stats", report);
        }

        private void verifyClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var report = TGreedyDiversity.VerifyClusterOrbits();
            System.Diagnostics.Debug.WriteLine(report);
            ShowTextDialog("Cluster-orbit verification", report);
        }

        // --- Batch measurement: solve N fresh scrambles headless, record the GA count of each, append a summary
        //     to Docs/batch_results.txt. Runs on the UI thread (freezes the window for the duration) -- it is a
        //     measurement tool, not interactive. A run that exceeds BATCH_CAP GA is recorded as a hang (DNF).
        const int BATCH_RUNS = 10;
        const int BATCH_CAP = 30000;
        static readonly string BatchFile =
            @"C:\_Moje Dane\_Moje Programy\Visual C#\RubikCubeND\Docs\batch_results.txt";

        private void batch10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;                 // don't fight an interactive solve in progress
            var title = Text;
            var results = new int[BATCH_RUNS];
            var hung = new bool[BATCH_RUNS];
            var times = new double[BATCH_RUNS];             // per-run GA compute time (ms) -- the cost the gen count hides
            List<string> worstTraj = null; int worstResult = -1, worstRun = -1;

            var cube = new TRubikCube();
            for (int r = 0; r < BATCH_RUNS; r++)
            {
                Text = $"BATCH {r + 1}/{BATCH_RUNS} ...";
                Application.DoEvents();

                cube.Scramble();
                var solver = new TRubikSolver(cube) { TrackTrajectory = true };
                var result = solver.RunHeadless(BATCH_CAP);

                results[r] = result.Iterations;
                hung[r] = result.Hung;
                times[r] = result.ComputeMs;
                if (results[r] > worstResult) { worstResult = results[r]; worstTraj = result.Trajectory; worstRun = r; }
            }

            Text = title;
            tglView1.Invalidate();

            // Stats over ALL runs (hung ones counted at the cap -- conservative, and flagged).
            var sorted = (int[])results.Clone();
            Array.Sort(sorted);
            double mean = results.Average();
            double var = results.Select(v => (v - mean) * (v - mean)).Average();
            double median = BATCH_RUNS % 2 == 1
                ? sorted[BATCH_RUNS / 2]
                : (sorted[BATCH_RUNS / 2 - 1] + sorted[BATCH_RUNS / 2]) / 2.0;
            int hangCount = hung.Count(h => h);

            var sb = new StringBuilder();
            var measure = Gpu.TryGetDefine("MEASURE");
            var pair = Gpu.TryGetDefine("PAIR");               // logged only while the experiment's define exists
            var ssign = Gpu.TryGetDefine("SSIGN");             // +1 -> asymmetry PENALISED (+S), -1 -> REWARDED (-S)
            sb.AppendLine("======================================================================");
            sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  N={TAffine.N}  Slices={TRubikCube.Size}  " +
                          $"Floor={TRubikSolver.Floor}  Population={TGA<TRubikGenome>.PopulationCount}  cap={BATCH_CAP}" +
                          (measure != null ? $"  MEASURE={measure}" : "") +
                          (pair != null ? $"  PAIR={pair}" : "") +
                          (ssign != null ? $"  variant={(ssign.Contains("-") ? "-S" : ssign == "0" ? "0" : "+S")}" : ""));
            sb.AppendLine("runs: " + string.Join(", ",
                results.Select((v, i) => hung[i] ? v + "(HANG)" : v.ToString())));
            sb.AppendLine($"min {sorted[0]}   median {median}   tail {sorted[BATCH_RUNS - 1]}   " +
                          $"mean {mean:F0}   std {Math.Sqrt(var):F0}   var {var:F0}" +
                          (hangCount > 0 ? $"   HANGS {hangCount}/{BATCH_RUNS}" : ""));
            double totMs = times.Sum(), totGen = results.Sum();   // ms/1k-gen = per-generation cost -> the GPU-efficiency knob (coherence-on costs more)
            sb.AppendLine($"time ms: total {totMs:F0}   mean {times.Average():F0}   tail {times.Max():F0}   " +
                          $"ms/1k-gen {(totGen > 0 ? 1000.0 * totMs / totGen : 0):F1}");
            var report = sb.ToString();

            try { System.IO.File.AppendAllText(BatchFile, report); }
            catch (Exception ex) { report += "\n(could not write file: " + ex.Message + ")"; }
            if (worstTraj != null)                          // dump ONLY the worst run's per-step trajectory (overwrite each batch)
            {
                var sbT = new StringBuilder();
                sbT.AppendLine($"# WORST run {worstRun + 1}/{BATCH_RUNS}: {worstResult} gens{(hung[worstRun] ? " (HANG)" : "")}" + (measure != null ? $"   MEASURE={measure}" : ""));
                sbT.AppendLine("# gen\tscrambled\tstructure\tbestFit\tscore\tmoves\tstepMs");
                foreach (var l in worstTraj) sbT.AppendLine(l);
                var safeMeasure = string.IsNullOrEmpty(measure)                   // per-MEASURE file so each metric keeps its own trace
                    ? "none"
                    : string.Concat(measure.Trim().Split(System.IO.Path.GetInvalidFileNameChars()));   // strip '*' etc., keep '+'
                var trajFile = BatchFile.Replace("batch_results.txt", $"worst_trajectory_{safeMeasure}.txt");
                try { System.IO.File.WriteAllText(trajFile, sbT.ToString()); } catch { }
            }
            ShowTextDialog("Batch " + BATCH_RUNS, report);
        }

        // Status labels: which cluster is being solved (by ClusterIndex, 1-based) out of the total, and how
        // many of the active cluster's cubies are already solved. ActiveCubie == null means the cube is solved.
        private void UpdateClusterInfo()
        {
            if (RubikCube == null || RubikCube.Cubies == null) return;
            if (Solver == null || Solver.ActiveCubie == null)
            {
                ClusterLbl.Text = $"Cluster {RubikCube.Clusters.Count}";
                ScrambledLbl.Text = "Scrambled -";
                StructureBox.Text = "Struct -";
                return;
            }
            int scrambled = Solver.ActiveCluster.ScrambledCount;
            ClusterLbl.Text = $"Cluster {RubikCube.Clusters.Count - Solver.ActiveCubie.ClusterIndex}";
            ScrambledLbl.Text = $"Scrambled {scrambled}";
            // Coherence decomposition of the residual, as the METRIC sees it ("4+2", "2+1+1") - reported by the
            // evaluator itself, so it can never drift from the metric the way an eyeball reading does.
            StructureBox.Text = "Struct " + (Solver.Best == null ? "-" : TRubikGenome.DescribeStructure(Solver.Best.Structure, TAffine.N));
            //StructureBox.Refresh();
        }

        // Read-only monospace text box in a small dialog: the report stays selectable/copyable (Ctrl+A,
        // Ctrl+C) and the histogram columns line up (unlike a proportional-font MessageBox).
        static void ShowTextDialog(string title, string text)
        {
            using var form = new Form
            {
                Text = title,
                Width = 560,
                Height = 480,
                StartPosition = FormStartPosition.CenterParent
            };
            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10f),
                Text = text
            };
            box.Select(0, 0);
            form.Controls.Add(box);
            form.ShowDialog();
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

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            TAffine.N = (int)DimsBox.Value;
            TRubikCube.EulerOrder = null;
            UpdateView();
        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void TrajectoryBox_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
