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
using Rubik;

namespace Cyberiada3D
{
    public partial class Form1 : Form
    {
        public RubikCube RubikCube;
        public List<TMove> Moves = new List<TMove>();
        public static Random Rnd = new Random();
        public double HighScore = double.MaxValue;

        public Form1()
        {
            InitializeComponent();
        }

        Point StartPos;
        private void tglView1_MouseDown(object sender, MouseEventArgs e)
        {
            StartPos = e.Location;
            
        }

        private void tglView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var rot = new TPoint3D();
                rot.X = 180.0 * (e.Y - StartPos.Y) / Height;
                rot.Y = 180.0 * (e.X - StartPos.X) / Width;
                tglView1.Context.Root.RotateY(-rot.Y);
                tglView1.Context.Root.RotateX(-rot.X);
                StartPos = e.Location;
                tglView1.Invalidate();
            }
        }

        private void tglView1_MouseUp(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right)
            //{
            //    var r = Rectangle.FromLTRB(StartPos.X, StartPos.Y, e.X, e.Y);
            //    tglView1.Context.ZoomIn(r);
            //}
        }

        private void tglView1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            RubikCube = new RubikCube();
            RubikCube.Parent = tglView1.Context.Root;
        }

        int MoveNo;
        int FrameNo;
        int FrameCount = 15;

        private void timer1_Tick(object sender, EventArgs e)
        {
           if(MoveNo < Moves.Count)
            {
                var move = Moves[MoveNo];
                if(FrameNo < FrameCount)
                {
                    if(FrameNo == 0)
                    {
                        RubikCube.Select(move);
                        RubikCube.Group();                 
                    }
                    double angle = (move.Angle + 1) * 90;
                    if (angle > 180)
                        angle -= 360;
                    var ratio = (double) FrameNo / (FrameCount - 1);
                    angle *= ratio;
                    RubikCube.Wall.LoadIdentity();
                    if (move.Axis == 0)
                    {
                        RubikCube.Wall.RotateX(angle);
                    }
                    else if (move.Axis == 1)
                    {
                        RubikCube.Wall.RotateY(angle);
                    }
                    else
                    {
                        RubikCube.Wall.RotateZ(angle);
                    }
                    FrameNo++;
                    tglView1.Invalidate();
                }
                else
                {
                    FrameNo = 0;
                    MoveNo++;
                    RubikCube.Ungroup();
                    RubikCube.MakeMove(move);
                } 
            } else
            {
                MoveTimer.Stop();
                Moves.Clear();
                MoveNo = 0;
                if (HighScore > 0)
                {
                    Solve();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 25; i++)
            {
                var code = Rnd.Next(9 * RubikCube.N);
                var move = new TMove();
                move.Decode(code);
                Moves.Add(move);
            }
            MoveTimer.Start();
        }

        private void SegCountBox_ValueChanged(object sender, EventArgs e)
        {
            RubikCube.N = (int)SegCountBox.Value;
            RubikCube.Parent = null;
            RubikCube = new RubikCube();
            RubikCube.Parent = tglView1.Context.Root;
            tglView1.Invalidate();
        }

        public double OnEvaluate(TChromosome specimen)
        {
            var cube = new RubikCube(RubikCube);
            var bestFitness = double.MaxValue;
            for (int len = 0; len < specimen.Genes.Length; len++)
            {
                var move = new TMove();
                move.Decode((int)specimen.Genes[len]);
                cube.MakeMove(move);
                var fitness = cube.Evaluate();
                if (fitness < bestFitness)
                {
                    bestFitness = fitness;
                    (specimen as RubikGenome).MovesCount = len + 1;
                }
            }
            specimen.UnFitness = bestFitness;
            return bestFitness;
        }

        public void OnProgress(TChromosome best)
        {
            DataChart.Series[0].Points.AddY(best.UnFitness);
            DataChart.Update();
            ErrorLbl.Text = "Error: " + HighScore.ToString();
            ErrorLbl.Refresh();
        }

        void Solve()
        {
            TChromosome.GenesLength = 30;
            TChromosome.MinGenes = new double[TChromosome.GenesLength];
            TChromosome.MaxGenes = new double[TChromosome.GenesLength];
            for (int i = 0; i < TChromosome.MaxGenes.Length; i++)
                TChromosome.MaxGenes[i] = 9 * RubikCube.N;
            TGA<RubikGenome>.GenerationsCount = 100;
            var ga = new TGA<RubikGenome>();
            ga.Evaluate = OnEvaluate;
            ga.Progress = OnProgress;
            ga.Execute();
            HighScore = ga.Best.UnFitness;
            if (HighScore > 0)
            {
                Moves.Clear();
                for (int i = 0; i < ga.Best.MovesCount; i++)
                {
                    var move = new TMove();
                    move.Decode((int)ga.Best.Genes[i]);
                    Moves.Add(move);
                }
                MoveTimer.Start();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Solve();
        }
    }
}
