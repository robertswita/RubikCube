using System;
using System.Collections.Generic;
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
        Dictionary<string, List<int>> Solutions;
        int MoveNo;
        double HighScore;
        public TRubikCube RubikCube;// = new TRubikCube();
        public TRubikForm()
        {
            InitializeComponent();
            //TransparencyBox.Checked = true;
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
                var rot = new TPoint3D();
                rot.Y = 180 * (e.X - StartPos.X) / tglView1.Width;
                rot.X = 180 * (e.Y - StartPos.Y) / tglView1.Height;
                tglView1.Context.Root.RotateY(rot.Y);
                tglView1.Context.Root.RotateX(rot.X);
                tglView1.Invalidate();
                StartPos = e.Location;
            }
        }

        // Mouse handlers for tglView2 (XYW)
        private void tglView2_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void tglView2_MouseMove(object sender, MouseEventArgs e)
        {
            tglView2.Cursor = Cursors.Hand;
            if (e.Button == MouseButtons.Left)
            {
                var rot = new TPoint3D();
                rot.Y = 180 * (e.X - StartPos.X) / tglView2.Width;
                rot.X = 180 * (e.Y - StartPos.Y) / tglView2.Height;
                tglView2.Context.Root.RotateY(rot.Y);
                tglView2.Context.Root.RotateX(rot.X);
                tglView2.Invalidate();
                StartPos = e.Location;
            }
        }

        // Mouse handlers for tglView3 (XZW)
        private void tglView3_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void tglView3_MouseMove(object sender, MouseEventArgs e)
        {
            tglView3.Cursor = Cursors.Hand;
            if (e.Button == MouseButtons.Left)
            {
                var rot = new TPoint3D();
                rot.Y = 180 * (e.X - StartPos.X) / tglView3.Width;
                rot.X = 180 * (e.Y - StartPos.Y) / tglView3.Height;
                tglView3.Context.Root.RotateY(rot.Y);
                tglView3.Context.Root.RotateX(rot.X);
                tglView3.Invalidate();
                StartPos = e.Location;
            }
        }

        // Mouse handlers for tglView4 (YZW)
        private void tglView4_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
        }

        private void tglView4_MouseMove(object sender, MouseEventArgs e)
        {
            tglView4.Cursor = Cursors.Hand;
            if (e.Button == MouseButtons.Left)
            {
                var rot = new TPoint3D();
                rot.Y = 180 * (e.X - StartPos.X) / tglView4.Width;
                rot.X = 180 * (e.Y - StartPos.Y) / tglView4.Height;
                tglView4.Context.Root.RotateY(rot.Y);
                tglView4.Context.Root.RotateX(rot.X);
                tglView4.Invalidate();
                StartPos = e.Location;
            }
        }

        // Slice selector event handlers
        private void trackBarXYZ_ValueChanged(object sender, EventArgs e)
        {
            lblSliceXYZ.Text = "W=" + trackBarXYZ.Value;
            lblViewXYZ.Text = "XYZ (W=" + trackBarXYZ.Value + ")";
            UpdateAllViews();
        }

        private void trackBarXYW_ValueChanged(object sender, EventArgs e)
        {
            lblSliceXYW.Text = "Z=" + trackBarXYW.Value;
            lblViewXYW.Text = "XYW (Z=" + trackBarXYW.Value + ")";
            UpdateAllViews();
        }

        private void trackBarXZW_ValueChanged(object sender, EventArgs e)
        {
            lblSliceXZW.Text = "Y=" + trackBarXZW.Value;
            lblViewXZW.Text = "XZW (Y=" + trackBarXZW.Value + ")";
            UpdateAllViews();
        }

        private void trackBarYZW_ValueChanged(object sender, EventArgs e)
        {
            lblSliceYZW.Text = "X=" + trackBarYZW.Value;
            lblViewYZW.Text = "YZW (X=" + trackBarYZW.Value + ")";
            UpdateAllViews();
        }

        int FrameNo;
        int FrameCount = 0;  // 0 = no animation, instant moves
        bool IsPaused = true;

        TObject3D ActSlice;
        public void Group(List<TCubie> selection)
        {
            ActSlice = new TObject3D();
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

        private void timer1_Tick(object sender, EventArgs e)
        {
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
                    ActSlice.LoadIdentity();

                    // Map 4D rotation planes to 3D rotations for XYZ view animation
                    // Plane: 0=XY, 1=XZ, 2=XW, 3=YZ, 4=YW, 5=ZW
                    switch (move.Plane)
                    {
                        case 0: // XY plane → rotate around Z axis
                            ActSlice.RotateZ(angle);
                            break;
                        case 1: // XZ plane → rotate around Y axis
                            ActSlice.RotateY(angle);
                            break;
                        case 2: // XW plane → rotate around Y axis (W not visible in XYZ)
                            ActSlice.RotateY(angle);
                            break;
                        case 3: // YZ plane → rotate around X axis
                            ActSlice.RotateX(angle);
                            break;
                        case 4: // YW plane → rotate around X axis (W not visible in XYZ)
                            ActSlice.RotateX(angle);
                            break;
                        case 5: // ZW plane → rotate around Z axis (W not visible in XYZ)
                            ActSlice.RotateZ(angle);
                            break;
                    }
                }
                else
                {
                    UnGroup();
                    RubikCube.Turn(move);
                    UpdateAllViews();  // Update all 4 views after each move
                    FrameNo = 0;
                    MoveNo++;
                }
                // Invalidate all views during animation
                tglView1.Invalidate();
                tglView2.Invalidate();
                tglView3.Invalidate();
                tglView4.Invalidate();
            }
            else
            {
                MoveNo = 0;
                Moves.Clear();
                MoveTimer.Stop();
                //DrawState();
                //label2.Text = RubikCube.Evaluate().ToString();
                label1.Text = Time.ToString();
                label2.Text = HighScore.ToString();
                label4.Text = RubikCube.Code.Count(x => x != '\0').ToString();
                label6.Text = GACount++.ToString();
                MovesLbl.Text = MovesCount.ToString();
                //DrawSolution(Moves);
                //StateGridView.DataSource = RubikCube.StateGrid;
                //DisplayState();
                StateBox.Invalidate();
                if (RubikCube.ActCubie != null)
                    RubikCube.ActCubie.Selected = false;
                RubikCube.GetActCubie();
                HighScore = RubikCube.Evaluate();
                //ActIdx++;
                //if (ActIdx == RubikCube.Cubies.Length)
                //    ActIdx = 0;
                //var Z = ActIdx / (TRubikCube.N * TRubikCube.N);
                //var Y = (ActIdx % (TRubikCube.N * TRubikCube.N)) / TRubikCube.N;
                //var X = (ActIdx % (TRubikCube.N * TRubikCube.N)) % TRubikCube.N;
                //RubikCube.ActCubie = RubikCube.Cubies[Z, Y, X];
                if (HighScore > 0 && !IsPaused)
                {
                    RubikCube.ActCubie.Selected = true;
                    Solve();
                }
                else
                    IsPaused = true;
            }
        }

        //int ActIdx;

        void DisplayState(Graphics gc)
        {
            var grid = RubikCube.StateGrid;
            //StateGridView.ColumnCount = grid.GetLength(1);
            //StateGridView.Rows.Clear();
            //for (int i = 0; i < grid.GetLength(0); i++)// array rows
            //{
            //    string[] row = new string[grid.GetLength(1)];
            //    for (int j = 0; j < grid.GetLength(1); j++)
            //    {
            //        row[j] = grid[i, j].ToString();
            //    }
            //    StateGridView.Rows.Add(row);
            //}
            var bmp = new Bitmap(grid.GetLength(0), grid.GetLength(1));
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (grid[y, x] == 0)
                        bmp.SetPixel(x, y, Color.White);
                    else
                    {
                        var r = 255 / 4.0 * (1 + grid[y, x] & 3);
                        var g = 255 / 4.0 * (1 + (grid[y, x] >> 2) & 3);
                        var b = 255 / 4.0 * (1 + (grid[y, x] >> 4) & 3);
                        bmp.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));
                    }
                }
            //StateBox.Image = bmp;
            gc.DrawImage(bmp, StateBox.ClientRectangle);
        }

        void OnProgress(TRubikGenome specimen)
        {
            //if (chart1.Series[0].Points.Count % 300 == 0)
            //    chart1.Series[0].Points.Clear();
            //var ga = (TGA<TRubikGenome>)sender;
            chart1.Series[0].Points.AddY(specimen.Fitness);
            chart1.Refresh();
            var iterTime = Watch.Elapsed - IterElapsed;
            IterTimeBox.Text = "Iter time:" + iterTime.Milliseconds;
            IterTimeBox.Refresh();
            IterElapsed += iterTime;

            //label2.Refresh();
            //label4.Refresh();
        }

        double OnEvaluate(TRubikGenome specimen)
        {
            specimen.Check();
            //specimen.Mutate(RubikCube.ActCubie);
            specimen.Fitness = double.MaxValue;
            var cube = new TRubikCube(RubikCube);
            string startCode = cube.Code;
            for (int i = 0; i < specimen.Genes.Length; i++)
            {
                var move = TMove.Decode((int)specimen.Genes[i]);
                if (i == 0)
                {
                    // For 4D: Determine which axis is fixed and set slice to ActCubie's position on that axis
                    var idx = new int[4] { RubikCube.ActCubie.X, RubikCube.ActCubie.Y, RubikCube.ActCubie.Z, RubikCube.ActCubie.W };

                    // Get the fixed axis for this plane rotation
                    int[] remainingAxes = RubikCube.GetRemainingAxes(move.Plane);
                    int fixedAxisIdx = remainingAxes[move.FixedAxis];

                    move.Slice = idx[fixedAxisIdx];
                    specimen.Genes[0] = move.Encode();
                }
                cube.Turn(move);
                double fitness = cube.Evaluate();
                if (fitness < specimen.Fitness && cube.Code != startCode)
                {
                    specimen.Fitness = fitness;
                    specimen.MovesCount = i + 1;
                    //if (fitness == 0) break;
                }
            }
            return specimen.Fitness;
        }

        bool TrySolutions = true;
        Stopwatch Watch;
        void Solve()
        {
            Watch = Stopwatch.StartNew();
            IterElapsed = TimeSpan.Zero;
            chart1.Series[0].Points.Clear();

            // 4D Genetic Algorithm Parameters (tuned for 2×2×2×2 hypercube)
            TChromosome.GenesLength = 54;  // Doubled from 27 for 4D complexity (6 planes vs 3 axes)
            var ga = new TGA<TRubikGenome>();
            ga.PopulationCount = 200;      // Increased from default ~100 for better exploration
            ga.GenerationsCount = 100;     // Increased from 50 for thorough solving
            ga.WinnerRatio = 0.1;          // Keep top 10% of population
            ga.MutationRatio = 1;          // 100% mutation rate for diversity
            ga.SelectionType = TGA<TRubikGenome>.TSelectionType.Unique;
            ga.Evaluate = OnEvaluate;
            ga.Progress = OnProgress;

            // Ensure ActCubie is set before getting free genes
            RubikCube.GetActCubie();
            TRubikGenome.FreeGenes = RubikCube.GetFreeGenes();
            ga.HighScore = HighScore;
            ga.Execute();

            //var actCubie = RubikCube.Cubies[0, 0, 0];
            //var idx = new int[] { RubikCube.ActCubie.X, RubikCube.ActCubie.Y, RubikCube.ActCubie.Z };
            //var A = new TMove();
            //var B = new TMove();
            //var C = new TMove();


            
            ////B.Axis = 2;
            ////A.Axis = 1; 
            //A.Slice = idx[A.Axis];
            //B.Slice = idx[B.Axis];
            //C.Slice = idx[C.Axis];
            //var alpha = RubikCube.ActCubie.State & 3;
            //var beta = (RubikCube.ActCubie.State >> 2) & 3;
            //var gamma = (RubikCube.ActCubie.State >> 4) & 3;
            //if (alpha == 0)
            //{
            //    A.Axis = 1;
            //    B.Axis = 2;
            //}
            //else if (beta == 0)
            //{
            //    A.Axis = 0;
            //    B.Axis = 2;
            //}
            //else
            //{
            //    A.Axis = 0;
            //    B.Axis = 1;
            //}
            ////if (alpha == 0) alpha = 2;
            ////if (beta == 0) beta = 2;
            ////if (gamma == 0) gamma = 2;
            //A.Axis = 0;
            //for (int j = 1; j < 3; j++)
            //    if (Math.Abs(idx[j] - TRubikCube.C) > Math.Abs(idx[A.Axis] - TRubikCube.C))
            //        A.Axis = j;
            ////B.Axis = (A.Axis + 1) % 3;
            ////C.Axis = (B.Axis + 1) % 3;
            //var angles = new int[] { alpha - 1, beta - 1, gamma - 1 };
            //A.Angle = angles[A.Axis];
            //B.Angle = angles[B.Axis];
            //C.Angle = angles[C.Axis];

            //var C_ = new TMove();
            //C_.Axis = C.Axis;
            //C_.Slice = C.Slice;
            //C_.Angle = 2 - C.Angle;
            //var B_ = new TMove();
            //B_.Axis = B.Axis;
            //B_.Slice = B.Slice; 
            //B_.Angle = 2 - B.Angle;
            //var A_ = new TMove();
            //A_.Axis = A.Axis;
            //A_.Slice = A.Slice;
            //A_.Angle = 2 - A.Angle;
            ////Moves.AddRange(new TMove[] { A, B, A_, B_, A, B, A_, B_ });
            ////Moves.AddRange(new TMove[] { C, B, A, B_, C_ });
            //if (A.Angle < 0)
            //    A.Angle = 1;
            //if (B.Angle < 0)
            //{
            //    B.Angle = 1;
            //    //Moves.AddRange(new TMove[] { A_, B_, A, B });
            //}
            //if (RubikCube.StateGrid[ActIdx, ActIdx] == 0)
            //{
            //    if (B.Axis > A.Axis)
            //        Moves.AddRange(new TMove[] { B_, A_, B, A });
            //    else
            //        Moves.AddRange(new TMove[] { A_, B_, A, B });
            //    // Moves.AddRange(new TMove[] { C, B, A, B_, A_, C_ });
            //}

            //IterCount = ga.IterCount;
            if (ga.HighScore == 0 && RubikCube.ActCluster.Count > 1)
            {
                SaveSolution(ga.Best);
            }
            if (ga.HighScore < HighScore)
            {
                HighScore = ga.HighScore;
                for (int i = 0; i < ga.Best.MovesCount; i++)
                    Moves.Add(TMove.Decode((int)ga.Best.Genes[i]));
                TrySolutions = true;
            }
            //chart2.Series[0].Points.AddY(ga.IterCount);
            if (TrySolutions)
            {
                foreach (var solution in Solutions)
                {
                    var tryMoves = DecodeSolution(solution.Value);
                    for (int j = -1; j < TRubikGenome.FreeGenes.Count; j++)
                    {
                        var moves = new List<TMove>();
                        if (j < 0)
                            moves.AddRange(tryMoves);
                        else
                        {
                            var move = TMove.Decode(TRubikGenome.FreeGenes[j]);
                            moves.Add(move);
                            moves.AddRange(tryMoves);
                            move = TMove.Decode(TRubikGenome.FreeGenes[j]);
                            move.Angle = 2 - move.Angle;
                            moves.Add(move);
                        }
                        var cube = new TRubikCube(RubikCube);
                        foreach (var move in moves)
                            cube.Turn(move);
                        var score = cube.Evaluate();
                        if (score < HighScore)
                        {
                            HighScore = score;
                            Moves = moves;
                        }
                    }
                }
            }
            if (Moves.Count == 0)
                TrySolutions = false;
            //if (HighScore == 0 && RubikCube.ActCubie != null)
            //    HighScore = double.MaxValue;
            //if (HighScore == 0)
            //{
            //    PauseBtn.BackColor = Color.Red;
            //    IsPaused = true;
            //    //chart1.Series[0].Points.Clear();
            //}
            MovesCount += Moves.Count;
            Time += Watch.Elapsed;
            MoveTimer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            //Highscore = RubikCube.Evaluate();
            MovesCount = 0;
            Time = TimeSpan.Zero;
            GACount = 0;
            LoadSolutions();
            IsPaused = false;
            RubikCube.GetActCubie();
            HighScore = RubikCube.Evaluate();
            Solve();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            IsPaused = true;
            for (int i = 0; i < 100; i++)
            {
                // 4D move encoding: 18 * Plane + 9 * FixedAxis + 3 * Slice + Angle
                // Max value: 18*5 + 9*1 + 3*(N-1) + 2 = 99 + 3*N
                // For N=2: range is 0-104, so we need Rnd.Next(105)
                var maxCode = 99 + 3 * TRubikCube.N;

                // Keep generating until we get a valid move
                TMove move;
                do
                {
                    var code = TChromosome.Rnd.Next(maxCode);
                    move = TMove.Decode(code);
                } while (move.Slice >= TRubikCube.N);  // Reject moves with invalid slice

                Moves.Add(move);
            }
            MoveTimer.Start();
        }

        string SolutionPath = "solutions.bin";
        List<TMove> DecodeSolution(List<int> solution)
        {
            var v = new int[] { RubikCube.ActCubie.X, RubikCube.ActCubie.Y, RubikCube.ActCubie.Z };
            var map = new List<int>();
            var result = new List<TMove>();
            for (var i = 0; i < solution.Count; i++)
            {
                var move = TMove.Decode(solution[i]);
                var idx = map.IndexOf(move.Slice);
                if (idx < 0)
                {
                    idx = map.IndexOf(TRubikCube.N - 1 - move.Slice);
                    if (idx < 0)
                    {
                        idx = map.Count;
                        map.Add(move.Slice);
                    }
                    else
                        idx += 3;
                }
                if (idx < 3)
                    move.Slice = v[idx];
                else
                    move.Slice = TRubikCube.N - 1 - v[idx - 3];
                result.Add(move);
            }
            return result;
        }

        void LoadSolutions()
        {
            Solutions = new Dictionary<string, List<int>>();
            try
            {
                var file = new FileStream(SolutionPath, FileMode.OpenOrCreate);
                using (var reader = new BinaryReader(file))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var key = reader.ReadString();
                        var movesCount = reader.ReadInt32();
                        var genes = new List<int>(movesCount);
                        for (int i = 0; i < movesCount; i++)
                            genes.Add(reader.ReadInt32());
                        Solutions.Add(key, genes);
                    }
                }
            }
            catch (Exception) { };
            SolutionLbl.Text = Solutions.Count.ToString();
        }

        void SaveSolution(TRubikGenome solution)
        {
            var code = RubikCube.Code;
            if (!Solutions.ContainsKey(code))
            {
                var file = new FileStream(SolutionPath, FileMode.Append);
                using (var writer = new BinaryWriter(file))
                {
                    writer.Write(code);
                    writer.Write(solution.MovesCount);
                    var genes = new List<int>(solution.MovesCount);
                    for (int i = 0; i < solution.MovesCount; i++)
                    {
                        genes.Add((int)solution.Genes[i]);
                        writer.Write(genes[i]);
                    }
                    Solutions.Add(code, genes);
                }
            }
            SolutionLbl.Text = Solutions.Count.ToString();
        }

        void DrawState()
        {
            Pen pen = new Pen(Color.Red);
            pen.Width = 2;
            var bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            var gc = Graphics.FromImage(bmp);
            PointF actPos = new PointF(bmp.Width / 2, bmp.Height / 2);
            var pts = new List<PointF>();
            pts.Add(actPos);
            var scale = pictureBox1.Width / RubikCube.Children.Count / 2;
            for (int i = 0; i < RubikCube.Children.Count; i++)
            {
                var cubie = (RubikCube.Children[i] as TCubie).Copy();
                var transform = cubie.Transform;
                var gamma = Math.Atan2(transform[4], transform[0]) * 180 / Math.PI;
                cubie.RotateZ(-gamma);
                var beta = Math.Atan2(-transform[8], transform[0]) * 180 / Math.PI;
                cubie.RotateY(-beta);
                var alpha = Math.Atan2(transform[9], transform[5]) * 180 / Math.PI;
                cubie.RotateX(-alpha);
                var angle = (alpha + beta + gamma) / 3;
                var v = new SizeF(scale * (float)Math.Cos(angle), scale * (float)Math.Sin(angle));
                actPos += v;
                pts.Add(actPos);
            }
            gc.DrawLines(pen, pts.ToArray());
            pictureBox1.Image = bmp;
        }

        private void TRubikForm_Load(object sender, EventArgs e)
        {
            RubikCube = new TRubikCube();

            // Initialize view labels with current slice values
            int middleSlice = TRubikCube.N / 2;
            lblViewXYZ.Text = "XYZ (W=" + middleSlice + ")";
            lblViewXYW.Text = "XYW (Z=" + middleSlice + ")";
            lblViewXZW.Text = "XZW (Y=" + middleSlice + ")";
            lblViewYZW.Text = "YZW (X=" + middleSlice + ")";

            UpdateAllViews();
            LayoutViews();  // Initial layout
        }

        private void TRubikForm_Resize(object sender, EventArgs e)
        {
            LayoutViews();
        }

        /// <summary>
        /// Calculates and applies layout for the 4 viewport views in a 2x2 grid
        /// </summary>
        private void LayoutViews()
        {
            // Calculate available space for views (right side of the form)
            int leftPanelWidth = 445;  // Width of the left control panel
            int margin = 10;
            int labelHeight = 25;
            int sliderHeight = 45;
            int sliderLabelWidth = 50;

            int availableWidth = this.ClientSize.Width - leftPanelWidth - margin * 3;
            int availableHeight = this.ClientSize.Height - margin * 3 - labelHeight * 2 - sliderHeight;

            // Calculate size for each view (half of available space)
            int viewWidth = (availableWidth - margin) / 2;
            int viewHeight = (availableHeight - margin) / 2;

            // Make views square (use smaller dimension)
            int viewSize = Math.Min(viewWidth, viewHeight);

            // Position views in 2x2 grid
            int startX = leftPanelWidth + margin;
            int startY = labelHeight + margin;

            // Top-left: tglView1 (XYZ)
            tglView1.Location = new Point(startX, startY);
            tglView1.Size = new Size(viewSize, viewSize);
            lblViewXYZ.Location = new Point(startX + 5, startY + 5);
            lblViewXYZ.BringToFront();

            // Slider below view1
            trackBarXYZ.Location = new Point(startX, startY + viewSize + 5);
            trackBarXYZ.Size = new Size(viewSize - sliderLabelWidth, sliderHeight);
            lblSliceXYZ.Location = new Point(startX + viewSize - sliderLabelWidth + 5, startY + viewSize + 10);

            // Top-right: tglView2 (XYW)
            tglView2.Location = new Point(startX + viewSize + margin, startY);
            tglView2.Size = new Size(viewSize, viewSize);
            lblViewXYW.Location = new Point(startX + viewSize + margin + 5, startY + 5);
            lblViewXYW.BringToFront();

            // Slider below view2
            trackBarXYW.Location = new Point(startX + viewSize + margin, startY + viewSize + 5);
            trackBarXYW.Size = new Size(viewSize - sliderLabelWidth, sliderHeight);
            lblSliceXYW.Location = new Point(startX + viewSize + margin + viewSize - sliderLabelWidth + 5, startY + viewSize + 10);

            // Bottom-left: tglView3 (XZW)
            int view3Y = startY + viewSize + margin + sliderHeight + 5;
            tglView3.Location = new Point(startX, view3Y);
            tglView3.Size = new Size(viewSize, viewSize);
            lblViewXZW.Location = new Point(startX + 5, view3Y + 5);
            lblViewXZW.BringToFront();

            // Slider below view3
            trackBarXZW.Location = new Point(startX, view3Y + viewSize + 5);
            trackBarXZW.Size = new Size(viewSize - sliderLabelWidth, sliderHeight);
            lblSliceXZW.Location = new Point(startX + viewSize - sliderLabelWidth + 5, view3Y + viewSize + 10);

            // Bottom-right: tglView4 (YZW)
            tglView4.Location = new Point(startX + viewSize + margin, view3Y);
            tglView4.Size = new Size(viewSize, viewSize);
            lblViewYZW.Location = new Point(startX + viewSize + margin + 5, view3Y + 5);
            lblViewYZW.BringToFront();

            // Slider below view4
            trackBarYZW.Location = new Point(startX + viewSize + margin, view3Y + viewSize + 5);
            trackBarYZW.Size = new Size(viewSize - sliderLabelWidth, sliderHeight);
            lblSliceYZW.Location = new Point(startX + viewSize + margin + viewSize - sliderLabelWidth + 5, view3Y + viewSize + 10);
        }

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
                TRubikCube.N = (int)Math.Round(Math.Pow(code.Length, 0.33));
                RubikCube.Parent = null;
                RubikCube = new TRubikCube();
                RubikCube.Code = code;
                UpdateAllViews();
            }
        }

        private void stateSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var spaceForm = new TSpaceForm();
            //spaceForm.Solutions = Solutions;
            spaceForm.ShowDialog();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            TRubikCube.N = (int)numericUpDown1.Value;
            RubikCube.Parent = null;
            RubikCube = new TRubikCube();

            // Update trackbar ranges for new N value
            // For N=2, slices are 0-1; for N=3, slices are 0-2, etc.
            trackBarXYZ.Maximum = TRubikCube.N - 1;
            trackBarXYW.Maximum = TRubikCube.N - 1;
            trackBarXZW.Maximum = TRubikCube.N - 1;
            trackBarYZW.Maximum = TRubikCube.N - 1;

            // Set to middle slice by default
            int middleSlice = TRubikCube.N / 2;
            trackBarXYZ.Value = middleSlice;
            trackBarXYW.Value = middleSlice;
            trackBarXZW.Value = middleSlice;
            trackBarYZW.Value = middleSlice;

            // Update view labels with current slice values
            lblViewXYZ.Text = "XYZ (W=" + middleSlice + ")";
            lblViewXYW.Text = "XYW (Z=" + middleSlice + ")";
            lblViewXZW.Text = "XZW (Y=" + middleSlice + ")";
            lblViewYZW.Text = "YZW (X=" + middleSlice + ")";

            UpdateAllViews();
            StateBox.Invalidate();
            Moves.Clear();
            MoveTimer.Start();
            //RubikCube.Cubies[0, 1, 2].Selected = true;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //Solver = null;
            IsPaused = !IsPaused;
            PauseBtn.BackColor = Color.Red;
            if (!IsPaused)
            {
                PauseBtn.BackColor = DefaultBackColor;
                chart1.Series[0].Points.Clear();
                MoveTimer.Start();
            }
        }

        private void saveClustersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TRubikCube.N = 7;
            RubikCube.Parent = null;
            RubikCube = new TRubikCube();
            UpdateAllViews();

            var cubies = new List<TCubie>();
            // Updated for 4D: N=2, so valid indices are 0 or 1
            cubies.Add(RubikCube.Cubies[1, 1, 1, 1]);  // Corner hypercubie
            cubies.Add(RubikCube.Cubies[0, 0, 0, 0]);  // Opposite corner
            cubies.Add(RubikCube.Cubies[1, 1, 0, 0]);  // Edge hypercubie
            cubies.Add(RubikCube.Cubies[0, 0, 1, 1]);  // Edge hypercubie
            cubies.Add(RubikCube.Cubies[1, 0, 1, 0]);  // Face hypercubie
            cubies.Add(RubikCube.Cubies[0, 1, 0, 1]);  // Face hypercubie

            for (int i = 0; i < cubies.Count; i++)
            {
                foreach (var cubie in RubikCube.Cubies)
                    cubie.State = 3;
                var cluster = RubikCube.GetCluster(cubies[i]);
                foreach (var cubie in cluster)
                    cubie.State = 0;
                SaveConfig("Cluster" + i.ToString() + ".cfg");
            }
        }

        private void TransparencyBox_CheckedChanged(object sender, EventArgs e)
        {
            // Apply transparency setting to all views
            tglView1.Context.IsTransparencyOn = TransparencyBox.Checked;
            tglView2.Context.IsTransparencyOn = TransparencyBox.Checked;
            tglView3.Context.IsTransparencyOn = TransparencyBox.Checked;
            tglView4.Context.IsTransparencyOn = TransparencyBox.Checked;
        }

        private void showClusterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TRubikCube.N = 5;
            RubikCube.Parent = null;
            RubikCube = new TRubikCube();
            UpdateAllViews();
            // Reset all views to identity
            tglView1.Context.Root.LoadIdentity();
            tglView2.Context.Root.LoadIdentity();
            tglView3.Context.Root.LoadIdentity();
            tglView4.Context.Root.LoadIdentity();
            foreach (var cubie in RubikCube.Cubies)
            {
                cubie.State = 3;
                cubie.Transparent = true;
            }
            //var ccubie = RubikCube.Cubies[0, 2, 0];
            for (var level = 1; level <= 9; level++)
            {
                RubikCube.GetActCubie();
                foreach (var ccubie in RubikCube.ActCluster)
                {
                    ccubie.State = 0;
                    ccubie.Transparent = false;
                }
            }
            tglView1.Context.Root.RotateZ(45);
            //tglView1.Context.Root.RotateY(90);
            tglView1.Context.Root.RotateX(225);
            tglView1.Invalidate();
        }

        private void makeMovesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //RubikCube.Cubies[0, 0, 0].Selected = true;
            var C = new TMove();
            var B = new TMove();
            var A = new TMove();
            A.Axis = 0;
            B.Axis = 1;
            C.Axis = 2;
            A.Slice = 0;
            B.Slice = 0;
            C.Slice = 1;
            A.Angle = 0;
            B.Angle = 1;
            C.Angle = 0;
            var B_ = new TMove();
            B_.Axis = B.Axis;
            B_.Slice = B.Slice;
            B_.Angle = 2 - B.Angle;
            var C_ = new TMove();
            C_.Axis = C.Axis;
            C_.Slice = C.Slice;
            C_.Angle = 2 - C.Angle;
            var A_ = new TMove();
            A_.Axis = A.Axis;
            A_.Slice = A.Slice;
            A_.Angle = 2 - A.Angle;
            //Moves.AddRange(new TMove[] { B, A, B_ });
            //Moves.AddRange(new TMove[] { A, B, A_, C, B_, C_ });
            //Moves.AddRange(new TMove[] { C, B, A, B_, C_, A_ });
            //Moves.AddRange(new TMove[] { A, C, B, C_, B_, A_ });
            Moves.AddRange(new TMove[] { C, B, C_, B_ });
            //Moves.AddRange(new TMove[] { A, B, C });
            //Moves.Add(B);
            MoveTimer.Start();
        }

        private void undoMovesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var actCubie = RubikCube.Cubies[0, 0, 0, 0];  // Updated for 4D
            var idx = new int[] { actCubie.X, actCubie.Y, actCubie.Z, actCubie.W };  // Include W
            var A = new TMove();
            var B = new TMove();
            var C = new TMove();
            for (int j = 1; j < 3; j++)
                if (Math.Abs(idx[j] - TRubikCube.C) > Math.Abs(idx[A.Axis] - TRubikCube.C))
                    A.Axis = j;
            B.Axis = (A.Axis + 1) % 3;
            C.Axis = (A.Axis + 2) % 3;

        }

        private void StateBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            DisplayState(e.Graphics);
        }
        
        /// <summary>
        /// Updates all 4 viewport views with 3D slices of the 4D hypercube
        /// </summary>
        private void UpdateAllViews()
        {
            if (RubikCube == null) return;

            // Extract slices from 4D hypercube using trackbar values
            // XYZ view: Fix W dimension at trackBarXYZ value
            // XYW view: Fix Z dimension at trackBarXYW value
            // XZW view: Fix Y dimension at trackBarXZW value
            // YZW view: Fix X dimension at trackBarYZW value

            UpdateSliceView(tglView1, RubikCube.GetSliceXYZ(trackBarXYZ.Value), "XYZ", trackBarXYZ.Value);
            UpdateSliceView(tglView2, RubikCube.GetSliceXYW(trackBarXYW.Value), "XYW", trackBarXYW.Value);
            UpdateSliceView(tglView3, RubikCube.GetSliceXZW(trackBarXZW.Value), "XZW", trackBarXZW.Value);
            UpdateSliceView(tglView4, RubikCube.GetSliceYZW(trackBarYZW.Value), "YZW", trackBarYZW.Value);
        }

        /// <summary>
        /// Updates a single viewport with a 3D slice from the 4D hypercube
        /// </summary>
        /// <param name="view">The TGLView control to update</param>
        /// <param name="slice">3D array of cubies to display</param>
        /// <param name="viewName">Name of the view (XYZ, XYW, XZW, or YZW)</param>
        /// <param name="sliceValue">The value of the fixed dimension</param>
        private void UpdateSliceView(TGLView view, TCubie[,,] slice, string viewName, int sliceValue)
        {
            if (view == null || slice == null) return;

            // Clear existing content
            view.Context.Root.Children.Clear();

            // Create a container for this slice
            var sliceContainer = new TObject3D();
            sliceContainer.Parent = view.Context.Root;

            // Scale factor for spacing between cubies
            double cubieSize = 1.0;
            double spacing = 0.1;
            double step = cubieSize + spacing;

            // Create visual copies of each cubie and reposition them in 3D grid
            for (int z = 0; z < TRubikCube.N; z++)
                for (int y = 0; y < TRubikCube.N; y++)
                    for (int x = 0; x < TRubikCube.N; x++)
                    {
                        var originalCubie = slice[z, y, x];
                        if (originalCubie != null)
                        {
                            // Create a visual copy of the cubie
                            var cubieCopy = originalCubie.Copy();

                            // Add W dimension colors based on view type
                            // XYZ view (W=sliceValue): W is fixed/hidden, don't show W colors
                            // XYW view (Z=sliceValue): W varies, show W colors where Z would be
                            // XZW view (Y=sliceValue): W varies, show W colors where Y would be
                            // YZW view (X=sliceValue): W varies, show W colors where X would be
                            int wValue = originalCubie.W;

                            if (viewName == "XYW")
                            {
                                // Z is fixed, W varies - show W colors on Z faces at W boundaries
                                // Face 0=-Z, Face 3=+Z
                                // Only set if face doesn't already have a color
                                if (wValue == 0 && cubieCopy.FaceColors[0] == -1)
                                    cubieCopy.FaceColors[0] = 6;  // -Z face (represents -W) shows Purple
                                if (wValue == TRubikCube.N - 1 && cubieCopy.FaceColors[3] == -1)
                                    cubieCopy.FaceColors[3] = 7;  // +Z face (represents +W) shows Magenta
                            }
                            else if (viewName == "XZW")
                            {
                                // Y is fixed, W varies - show W colors on Y faces at W boundaries
                                // Face 2=-Y, Face 5=+Y
                                // Only set if face doesn't already have a color
                                if (wValue == 0 && cubieCopy.FaceColors[2] == -1)
                                    cubieCopy.FaceColors[2] = 6;  // -Y face (represents -W) shows Purple
                                if (wValue == TRubikCube.N - 1 && cubieCopy.FaceColors[5] == -1)
                                    cubieCopy.FaceColors[5] = 7;  // +Y face (represents +W) shows Magenta
                            }
                            else if (viewName == "YZW")
                            {
                                // X is fixed, W varies - show W colors on X faces at W boundaries
                                // Face 1=-X, Face 4=+X
                                // Only set if face doesn't already have a color
                                if (wValue == 0 && cubieCopy.FaceColors[1] == -1)
                                    cubieCopy.FaceColors[1] = 6;  // -X face (represents -W) shows Purple
                                if (wValue == TRubikCube.N - 1 && cubieCopy.FaceColors[4] == -1)
                                    cubieCopy.FaceColors[4] = 7;  // +X face (represents +W) shows Magenta
                            }
                            // XYZ view: W is fixed/hidden dimension, so no W colors are shown

                            // Reposition based on slice indices to form a proper 3D cube
                            // Center the cube around origin
                            double centerOffset = (TRubikCube.N - 1) * step / 2.0;
                            cubieCopy.LoadIdentity();
                            cubieCopy.Scale(cubieSize * 0.45, cubieSize * 0.45, cubieSize * 0.45);  // Scale down to avoid overlap
                            cubieCopy.Translate(
                                x * step - centerOffset,
                                y * step - centerOffset,
                                z * step - centerOffset
                            );

                            // Copy the color/state from original
                            cubieCopy.State = originalCubie.State;

                            cubieCopy.Parent = sliceContainer;
                        }
                    }

            // Move the container back a bit so it's visible in the camera view
            // sliceContainer.Translate(0, 0, 0);

            // Invalidate the view to trigger redraw
            view.Invalidate();
        }
    }
}
