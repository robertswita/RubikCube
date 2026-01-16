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
        Dictionary<string, TRubikGenome> Solutions = new Dictionary<string,TRubikGenome>();
        int MoveNo;
        double HighScore;
        public TRubikCube RubikCube;// = new TRubikCube();
        //public TScene Scene = new TScene();
        //public TCamera Camera;
        TGA<TRubikGenome> Ga;
        TShape Root = new TShape();
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
            Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 2), (float)e.Delta / 60);
            Root.Rotate(Math.Min(TAffine.Planes.Length - 1, 3), (float)e.Delta / 60);
            tglView1.Invalidate();
        }

        private void TRubikForm_Load(object sender, EventArgs e)
        {
            tglView1.Context.Root = Root;
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;
            LoadSolutions();
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
                tglView1.Invalidate();
            }
            else if (MoveNo > 0)
            {
                MoveNo = 0;
                Moves.Clear();
                label2.Text = HighScore.ToString();
                label4.Text = RubikCube.Code.Count(x => x != '\0').ToString();
                GACount++;
                label6.Text = GACount.ToString();
                MovesLbl.Text = MovesCount.ToString();
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
                label1.Text = Time.ToString();
                MoveTimer.Stop();
                if (Ga != null)
                    Solve();
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
            //specimen.Conjugate();
            //specimen.Mutate(RubikCube.ActCubie);
            specimen.Fitness = double.MaxValue;
            var cube = new TRubikCube(RubikCube);
            //string startCode = cube.Code;
            for (int i = 0; i < specimen.Genes.Length; i++)
            {
                //if (!TRubikGenome.FreeMoves.Contains((int)specimen.Genes[i]))
                //    ;
                var move = TMove.Decode((int)specimen.Genes[i]);
                // Final optimalization
                if (i == 0)
                {
                    var actCubie = RubikCube.ActiveCubie;
                    move.Slice = actCubie.Position[move.Axis];
                    specimen.Genes[0] = move.Encode();
                }
                cube.Turn(move);
                //var cubeCopy = new TRubikCube(cube);
                //for (int j = i - 1; j >= 0; j--)
                //    cube.ReTurn(TMove.Decode((int)specimen.Genes[j]));
                double fitness = cube.Evaluate();
                if (fitness < specimen.Fitness)// && cube.Code != startCode)
                {
                    specimen.Fitness = fitness;
                    specimen.MovesCount = i + 1;
                    //if (fitness == 0) break;
                }
                //cube = cubeCopy;
            }
            return specimen.Fitness;
        }

        bool TrySolutions = true;
        Stopwatch Watch;
        void Solve()
        {
            if (HighScore == 0)
            {
                RubikCube.NextCluster();
                if (RubikCube.ActiveCubie != null)
                {
                    TRubikGenome.FreeMoves = RubikCube.GetFreeMoves();
                    HighScore = RubikCube.Evaluate();
                }
            }
            if (RubikCube.ActiveCluster != null)
            {
                Watch = Stopwatch.StartNew();
                IterElapsed = TimeSpan.Zero;
                chart1.Series[0].Points.Clear();

                TChromosome.GenesLength = 50;
                Ga = new TGA<TRubikGenome>();
                Ga.GenerationsCount = 50;
                Ga.PopulationCount = 200;
                Ga.WinnerRatio = 0.1;
                Ga.MutationRatio = 5;
                Ga.SelectionType = TGA<TRubikGenome>.TSelectionType.Unique;
                Ga.Evaluate = OnEvaluate;
                Ga.Progress = OnProgress;
                TRubikGenome.FreeMoves = RubikCube.GetFreeMoves();
                Ga.HighScore = HighScore;
                Ga.Execute();
                if (Ga.HighScore == 0 && RubikCube.ActiveCluster.Count > 1)
                {
                    SaveSolution(Ga.Best);
                }
                if (Ga.HighScore < HighScore)
                {
                    HighScore = Ga.HighScore;
                    for (int i = 0; i < Ga.Best.MovesCount; i++)
                        Moves.Add(TMove.Decode((int)Ga.Best.Genes[i]));
                    TrySolutions = true;
                }
                if (TrySolutions)
                {
                    foreach (var solution in Solutions)
                    {
                        var tryMoves = DecodeSolution(solution.Value);
                        for (int j = -1; j < TRubikGenome.FreeMoves.Count; j++)
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
                MovesCount += Moves.Count;
                Time += Watch.Elapsed;
                MoveTimer.Start();
            }
            else
            {
                Ga = null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            MovesCount = 0;
            Time = TimeSpan.Zero;
            GACount = 0;
            IsPaused = false;
            //RubikCube.GetActCubie();
            //HighScore = RubikCube.Evaluate();
            HighScore = 0;
            Solve();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (MoveTimer.Enabled) return;
            IsPaused = true;
            var size = TRubikCube.Size;
            var rnd = TChromosome.Rnd;
            for (int i = 0; i < 30; i++)
            {
                RubikCube.ActiveCubie = RubikCube.Cubies[rnd.Next(RubikCube.Cubies.Length)];
                TRubikGenome.FreeMoves = RubikCube.GetFreeMoves();
                var code = TRubikGenome.FreeMoves[rnd.Next(TRubikGenome.FreeMoves.Count)];
                var move = TMove.Decode(code);
                Moves.Add(move);
                RubikCube.ActiveCubie.State = RubikCube.ActiveCubie.State;
            }
            MoveTimer.Start();
        }

        string SolutionPath = "solutions.bin";
        List<TMove> DecodeSolution(TRubikGenome solution)
        {
            var pos = RubikCube.ActiveCubie.Position;
            var map = new List<int>();
            var result = new List<TMove>();
            var sliceCountInCluster = TAffine.N;
            for (var i = 0; i < solution.Genes.Length; i++)
            {
                var move = TMove.Decode((int)solution.Genes[i]);
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
                result.Add(move);
            }
            return result;
        }

        void LoadSolutions()
        {
            Solutions = new Dictionary<string, TRubikGenome>();
            try
            {
                var file = new FileStream(SolutionPath, FileMode.OpenOrCreate);
                using (var reader = new BinaryReader(file))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var key = reader.ReadString();
                        var movesCount = reader.ReadInt32();
                        var solution = new TRubikGenome();
                        solution.Genes = new double[movesCount];
                        for (int i = 0; i < movesCount; i++)
                        {
                            var move = new TMove();
                            move.Axis = reader.ReadByte();
                            move.Slice = reader.ReadByte();
                            move.Plane = reader.ReadByte();
                            move.Angle = reader.ReadByte();
                            solution.Genes[i] = move.Encode();
                        }
                        Solutions.Add(key, solution);
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
                    //var genes = new List<int>(solution.MovesCount);
                    for (int i = 0; i < solution.MovesCount; i++)
                    {
                        //genes.Add((int)solution.Genes[i]);
                        //writer.Write((int)solution.Genes[i]);
                        var move = TMove.Decode((int)solution.Genes[i]);
                        writer.Write((byte)move.Axis);
                        writer.Write((byte)move.Slice);
                        writer.Write((byte)move.Plane);
                        writer.Write((byte)move.Angle);
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
                tglView1.Invalidate();
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
            UpdateView();
        }

        void UpdateView()
        {
            Root = new TShape();
            RubikCube = new TRubikCube();
            RubikCube.Parent = Root;
            tglView1.Context.Root = Root;
            tglView1.Invalidate();
            StateBox.Invalidate();
            Moves.Clear();
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
            UpdateView();
        }
    }
}
