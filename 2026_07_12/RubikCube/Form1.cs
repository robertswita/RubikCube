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
        Dictionary<string, List<TMove>> Solutions = new Dictionary<string, List<TMove>>();
        int MoveNo;
        public TRubikCube RubikCube;// = new TRubikCube();
        int Iteration;
        int Stall;
        //public TScene Scene = new TScene();
        //public TCamera Camera;
        //TGA<TRubikGenome> Ga;
        TRubikGenome Best;
        TShape Root = new TShape();
        //int Scrambled;
        public TRubikForm()
        {
            InitializeComponent();
            //Camera = tglView1.Context.Camera;
            //Camera.Parent = Scene.Root;
            //var light = new TLight();
            //light.Parent = Camera;
            //light.Origin = new TVector(0, 0, 1);
            //TransparencyBox.Checked = true;
            tglView1.MouseWheel += TglView1_MouseWheel;
        }

        private void TglView1_MouseWheel(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < TAffine.Planes.Length; i++)
                Root.Rotate(i, (float)e.Delta / 60);
            //Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 2), (float)e.Delta / 60);
            //Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 3), (float)e.Delta / 60);
            tglView1.Invalidate();
        }

        private void TRubikForm_Load(object sender, EventArgs e)
        {
            tglView1.Context.Root = Root;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;
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
                Root.Rotate(1, rot.Y);
                Root.Rotate(0, rot.X);
                tglView1.Invalidate();
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
                    double angle = 90 * move.Angle;
                    if (angle > 180) angle -= 360;
                    angle *= (double)FrameNo / FrameCount;
                    ActSlice.Transform = TAffine.CreateRotation(move.Plane, angle);
                    //ActSlice.Transform = new TAffine();
                    //ActSlice.Rotate(move.Plane, angle);
                }
                else
                {
                    UnGroup();
                    RubikCube.Turn(move);
                    FrameNo = 0;
                    MoveNo++;
                }
                tglView1.Invalidate();
            }
            else if (MoveNo > 0)
            {
                MoveNo = 0;
                Moves.Clear();
                //label2.Text = HighScore.ToString();
                //var scrambled = RubikCube.Code.Count(x => x != '\0');
                label4.Text = RubikCube.ScrambledCount().ToString();
                //Scrambled = scrambled;
                MovesLbl.Text = MovesCount.ToString();
                RubikCube.StateGrid = null;
                StateBox.Invalidate();
                //if (RubikCube.ActiveCubie != null)
                //    RubikCube.ActiveCubie.Selected = false;
                //RubikCube.GetActCubie();
                //HighScore = RubikCube.Evaluate();
                //if (HighScore > 0 && !IsPaused)
                //{
                //    RubikCube.ActiveCubie.Selected = true;
                //    Solve();
                //}
                //else
                //    IsPaused = true;
                //if (Ga != null)
                //{
                //    Solve();
                //}
            }
            else
            {
                TimeBox.Text = Time.ToString();
                //SeqCountLbl.Text = "SeqCount:" +Ga.Best.MoveCount.ToString();
                MoveTimer.Stop();
                var freeCubies = new List<TCubie>();
                foreach (var cubie in RubikCube.ActiveCluster)
                    if (cubie.State != 0)
                        freeCubies.Add(cubie);
                if (freeCubies.Count > 0)
                    RubikCube.ActiveCubie = freeCubies[TChromosome.Rnd.Next(freeCubies.Count)];
                //if (Best.Fitness < RubikCube.Score)
                //{ 
                //    RubikCube.Score = Best.Fitness;
                //}
                //else
                //{
                //    //if (!TRubikCube.IsEulerOrderReversed)
                //    TRubikCube.EulerOrder = RubikCube.GetOrder();
                //    //TRubikCube.IsEulerOrderReversed = !TRubikCube.IsEulerOrderReversed;// RubikCube.ActiveCubie.IsReversedSeq;
                //    //RubikCube.ActiveCubie.IsReversedSeq = !RubikCube.ActiveCubie.IsReversedSeq;
                //    foreach (var cubie in RubikCube.ActiveCluster)
                //        cubie.ValidState = false;
                //    //HighScore = RubikCube.Evaluate();
                //}
                //RubikCube.GetSolveSeq();
                label2.Text = (100 * RubikCube.Score).ToString();
                label2.Refresh();
                if (Best != null)
                    Solve();
            }
        }

        //int ActIdx;

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
        }

        void OnProgress(TRubikGenome specimen)
        {
            //if (chart1.Series[0].Points.Count % 300 == 0)
            //    chart1.Series[0].Points.Clear();
            //var ga = (TGA<TRubikGenome>)sender;
            chart1.Series[0].Points.AddXY(Iteration, 100 * specimen.Fitness);
            chart1.Refresh();
            var iterTime = Watch.Elapsed - IterElapsed;
            IterTimeBox.Text = "Iter time:" + iterTime.Milliseconds;
            IterTimeBox.Refresh();
            //IterElapsed += iterTime;
            IterElapsed = Watch.Elapsed;
            GACount = Iteration;// StartGACount + Ga.IterCount;
            ItersBox.Text = GACount.ToString();
            ItersBox.Refresh();
            //chart2.Series[0].Points.Clear();
            //for (int i = 0; i < Ga.Population.Count; i++)
            //    chart2.Series[0].Points.AddY(Ga.Population[i].Fitness);
            //chart2.Refresh();

            //label2.Refresh();
            //label4.Refresh();
        }

        bool TrySolutions = true;
        Stopwatch Watch;
        int StartGACount;
        void Solve()
        {
            Watch = Stopwatch.StartNew();
            if (RubikCube.Score == 0)
            {
                chart1.Series[0].Points.Clear();
                RubikCube.NextCluster();
                if (RubikCube.ActiveCubie != null)
                {
                    TRubikGenome.FreeMoves = RubikCube.GetFreeMoves();
                    RubikCube.GetSolveSeq();
                    RubikCube.Score = Gpu.ScoreCube(RubikCube);
                }
            }
            if (RubikCube.ActiveCubie != null)
            {
                IterElapsed = TimeSpan.Zero;
                StartGACount = GACount;

                //TChromosome.GenesLength = 32;
                TRubikGenome.RubikCube = RubikCube;
                TRubikGenome.FreeMoves = RubikCube.GetFreeMoves();
                //TRubikGenome.Genesis(new List<TRubikGenome>(), 1);
                //////Ga.GenerationsCount = 50;
                //////var level = 1 + (int)(10 - HighScore / 10) + RubikCube.SolvedCubies.Count;
                ////TGA<TRubikGenome>.PopulationCount = 512;//TRubikGenome.FreeMoves.Count * 100;
                ////Ga.WinnerRatio = 0.1;
                ////Ga.MutationRatio = 0.01;// .3;// .5;// 1.5;// 0.05;
                ////Ga.MutationAuxRatio = 0.01;// 0.1;// .1;// .05;// .05;// 1.5;// 0.05;
                ////Ga.Select = TRubikGenome.SelectUnique;
                ////Ga.NextGeneration = TRubikGenome.NextGeneration;
                //////Ga.SelectionType = TGA<TRubikGenome>.TSelectionType.Unique;
                ////Ga.Evaluate = TRubikGenome.Evaluate;
                ////Ga.Progress = OnProgress;
                ////Ga.HighScore = HighScore;
                ////Ga.Genesis = TRubikGenome.Genesis;
                //OpenGL.BindBuffer(OpenGL.GL_SHADER_STORAGE_BUFFER, tglView1.Context.SsboCubies[0]);
                //var matSize = TAffine.N * TAffine.N;
                //var pos = 0;
                //var buffer = new float[(matSize + TAffine.N) * RubikCube.Cubies.Length];
                //for (int i = 0; i < RubikCube.Cubies.Length; i++)
                //{
                //    Array.Copy(RubikCube.Cubies[i].Transform.M.Data, 0, buffer, pos, matSize);
                //    pos += matSize;
                //    Array.Copy(RubikCube.Cubies[i].Transform.Origin.Data, 0, buffer, pos, TAffine.N);
                //    pos += TAffine.N;
                //}
                //OpenGL.BufferDatafv(OpenGL.GL_SHADER_STORAGE_BUFFER, buffer​, OpenGL.GL_STATIC_DRAW);
                //OpenGL.BindBuffer(OpenGL.GL_SHADER_STORAGE_BUFFER, tglView1.Context.SsboActiveCubies[0]);
                //var idxBuffer = new int[RubikCube.ActiveCluster.Count];
                //for (int i = 0; i < idxBuffer.Length; i++)
                //    idxBuffer[i] = RubikCube.ActiveCluster[i].StartIndex;
                //OpenGL.BufferDataiv(OpenGL.GL_SHADER_STORAGE_BUFFER, idxBuffer​, OpenGL.GL_STATIC_DRAW);
                //OpenGL.BindBuffer(OpenGL.GL_SHADER_STORAGE_BUFFER, tglView1.Context.SsboSolvedCubies[0]);
                //idxBuffer = new int[RubikCube.SolvedCubies.Count];
                //for (int i = 0; i < idxBuffer.Length; i++)
                //    idxBuffer[i] = RubikCube.SolvedCubies[i].StartIndex;
                //OpenGL.BufferDataiv(OpenGL.GL_SHADER_STORAGE_BUFFER, idxBuffer​, OpenGL.GL_STATIC_DRAW);
                //TRubikGenome best = null;
                //if (Moves.Count == 0)
                    //Ga.Execute();
                Best = Gpu.ExecuteGA();
                Iteration += Gpu.GenerationsCount;
                Stall++;

                //if (Ga.HighScore == 0 && RubikCube.ActiveCluster.Count > 1)
                //{
                //    //SaveSolution(Ga.Best);
                //}
                //if (Ga.HighScore < HighScore)
                if (Best.Fitness < RubikCube.Score || Stall >= TGA<TRubikGenome>.StallLimit)
                {
                    Stall = 0;
                    OnProgress(Best);
                    //Ga.Best.Correct();
                    //Ga.Best.Evaluate();
                    //Iteration = 0;
                    //HighScore = Ga.HighScore;
                    //Ga.Best.Correct();
                    //for (int i = Ga.Best.StartIndex; i < Ga.Best.BestMovesCount; i++)
                    //    Moves.Add(TMove.Decode((int)Ga.Best.Genes[i]));
                    //if (Ga.Best.FromPredict)
                    //    for (int i = 0; i < Ga.Best.PredictSeq.Count; i++)
                    //        Moves.Add(Ga.Best.PredictSeq[i]);
                    Best.Correct();
                    for (int i = 0; i < Best.BestMovesCount; i++)
                        Moves.Add(TMove.Decode((int)Best.Genes[i]));
                    RubikCube.Score = Best.Fitness;
                    TrySolutions = true;
                }
                if (TrySolutions)
                {
                    foreach (var solution in Solutions)
                    {
                        var tryMoves = DecodeSolution(solution.Value);
                        for (int j = -1; j < 0 * TRubikGenome.FreeMoves.Count; j++)
                        {
                            var moves = new List<TMove>();
                            if (j < 0)
                                moves.AddRange(tryMoves);
                            else
                            {
                                var move = TMove.Decode(TRubikGenome.FreeMoves[j]);
                                moves.Add(move);
                                moves.AddRange(tryMoves);
                                move = TMove.Decode(TRubikGenome.FreeMoves[j]);
                                move.Angle = 2 - move.Angle;
                                moves.Add(move);
                            }
                            var cube = new TRubikCube(RubikCube);
                            foreach (var move in moves)
                                cube.Turn(move);
                            var score = Gpu.ScoreCube(cube);
                            if (score < RubikCube.Score)
                            {
                                RubikCube.Score = score;
                                Moves = moves;
                            }
                        }
                    }
                }
                if (Moves.Count == 0)
                    TrySolutions = false;
                MovesCount += Moves.Count;
                Time += Watch.Elapsed;
                MoveTimer.Start();
            }
            else
            {
                Best = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            MovesCount = 0;
            Time = TimeSpan.Zero;
            GACount = 0;
            Iteration = 0;
            IsPaused = false;
            //RubikCube.GetActCubie();
            RubikCube.Score = 0;
            RubikCube.ActiveCubie = null;
            Solve();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            IsPaused = true;
            var size = TRubikCube.Size;
            var rnd = TChromosome.Rnd;
            for (int i = 0; i < 200 * RubikCube.Cubies.Length; i++)
            {
                RubikCube.ActiveCubie = RubikCube.Cubies[rnd.Next(RubikCube.Cubies.Length)];
                var allMoves = RubikCube.GetAllMoves();
                var code = allMoves[rnd.Next(allMoves.Count)];
                var move = TMove.Decode(code);
                //Moves.Add(move);
                RubikCube.Turn(move);
                //RubikCube.ActiveCubie.State = RubikCube.ActiveCubie.State;
            }
            //MoveTimer.Start();
            RubikCube.StateGrid = null;
            StateBox.Invalidate();
            tglView1.Invalidate();
        }

        string SolutionPath = "solutions.bin";
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
            saveFileDialog1.InitialDirectory = Application.StartupPath;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                SaveConfig(saveFileDialog1.FileName);
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
                LoadConfig(openFileDialog1.FileName);
            }
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
                Root = null;
                DimsBox.Value = int.Parse(S.ReadLine());
                SlicesBox.Value = int.Parse(S.ReadLine());
                Root = new TShape();
                UpdateView();
                RubikCube.Code = S.ReadToEnd();
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
            TRubikCube.Size = (int)SlicesBox.Value;
            //tglView1.Context.SetDefineValue("SIZE", (int)SlicesBox.Value);
            //tglView1.Context.RebuildShaders();
            UpdateView();
        }

        void UpdateView()
        {
            if (Root != null)
            {
                Root = new TShape();
                RubikCube = new TRubikCube();
                RubikCube.Parent = Root;
                tglView1.Context.Root = Root;
                tglView1.Invalidate();
                StateBox.Invalidate();
                Moves.Clear();
                //OpenGL.BindBuffer(OpenGL.GL_SHADER_STORAGE_BUFFER, tglView1.Context.SsboPlanes[0]);
                //var buffer = new float[TAffine.Planes.Length * 2];
                //for (int i = 0; i < TAffine.Planes.Length; i++)
                //{
                //    buffer[2 * i] = TAffine.Planes[i][0];
                //    buffer[2 * i + 1] = TAffine.Planes[i][1];
                //}
                //OpenGL.BufferDatafv(OpenGL.GL_SHADER_STORAGE_BUFFER, buffer, OpenGL.GL_STATIC_DRAW);
            }
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

        private unsafe void TRubikForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Win32.wglDeleteContext(tglView1.Context.Handle);
        }
    }
}
