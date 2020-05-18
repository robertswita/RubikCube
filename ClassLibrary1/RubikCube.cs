using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace Rubik
{

    public class RubikCube : TObject3D
    {
        public static int N = 3;
        TCubik[,,] Cubiks = new TCubik[N, N, N];
        public static double M;
        public double Size = 0.9 / 2;
        public List<TCubik> Selection;
        public TObject3D Wall;

        public string Code
        {
            get
            {
                var code = "";
                foreach(var cubik in Cubiks)
                {
                    code += (char)(cubik.State +61);
                }
                return code;
            }
        }


        public RubikCube()
        {
            M = (N - 1) / 2.0;
            Scale(1.0 / N, 1.0 / N, 1.0 / N);
            for (int z = 0; z < N; z++)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        var cubik = new TCubik();
                        cubik.Translate(x - M, y - M, z - M);
                        cubik.Scale(Size, Size, Size);
                        cubik.Parent = this;
                        Cubiks[z, y, x] = cubik;
                        cubik.UpdateState();
                    }
        }

        public RubikCube(RubikCube cube): this()
        {
            for (int z = 0; z < N; z++)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        var cubik = Cubiks[z, y, x];
                        Array.Copy(cube.Cubiks[z, y, x].Transform, cubik.Transform, cubik.Transform.Length);
                        cubik.UpdateState();
                    }
        }


        public double Evaluate()
        {
            double result = 0;
            double idx = 0;
            var scores = new double[3];
            var basis = N + 1;

            for (int z = 0; z < N; z++)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        idx++;

                        var weight = 1 + idx / (N * N * N);
                        var cubik = Cubiks[z, y, x];
                        
                        
                        scores[0] += weight * getLineScore(0, y, z);
                        scores[1] += weight * getLineScore(1, z, x);
                        scores[2] += weight * getLineScore(2, x, y);
                        

                        //if (cubik.State != 0)
                        //{
                        //    result += weight;
                        //}

                        //var scores = new int[3];

                        //for(int i = 0; i< N; i++)
                        //{
                        //    if (Cubiks[z, y, i].State != 0)
                        //    {
                        //        scores[0]++;
                        //    }
                        //}

                        //for (int i = 0; i < N; i++)
                        //{
                        //    if (Cubiks[z, i, x].State != 0)
                        //    {
                        //        scores[1]++;
                        //    }
                        //}

                        //for (int i = 0; i < N; i++)
                        //{
                        //    if (Cubiks[i, y, x].State != 0)
                        //    {
                        //        scores[2]++;
                        //    }
                        //}

                        //Array.Sort(scores);
                        //var basis = N + 1;
                        //result += scores[0] + scores[1] / basis + scores[2] / basis / basis;

                    }

            Array.Sort(scores);
            result = scores[2] + scores[1] / basis + scores[0] / basis / basis;


            return result;
        }

        public double getLineScore(int axis, int segNo, int lineNo)
        {
            double hitScore = 0;
            double segScore = 0;
            double lineScore = 0;
            var v = new int[3];
            var basis = N + 1;
            v[(axis + 1) % 3] = segNo;
            
            var segCubik = Cubiks[v[2], v[1], v[0]];
            v[(axis + 2) % 3] = lineNo;

            var lineCubik = Cubiks[v[2], v[1], v[0]];

            for (var i=0; i < N; i++)
            {
                v[axis] = i;
                var cubik = Cubiks[v[2], v[1], v[0]];

                if(cubik.State != 0)
                {
                    hitScore++;
                }

                if(cubik.State != segCubik.State)
                {
                    segScore++;
                }

                if(cubik.State != lineCubik.State)
                {
                    lineScore++;
                }
            }

            return hitScore + segScore / basis + lineScore / basis / basis;
        }

        public void MakeMove(TMove move)
        {
            Select(move);
            var group = new TObject3D();
            var angle = (move.Angle + 1) * 90;

            if(move.Axis == 0)
            {
                group.RotateX(angle);
            }
            else if(move.Axis == 1)
            {
                group.RotateY(angle);
            } else
            {
                group.RotateZ(angle);
            }

            for (int i = 0; i < Selection.Count; i++)
            {
                var cubic = Selection[i];
                cubic.MultMatrix(group.Transform);
                Cubiks[cubic.Z, cubic.Y, cubic.X] = cubic;
                cubic.UpdateState();
            }
        }

        public void Select(TMove move)
        {
            Selection = new List<TCubik>();
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    var idx = new int [3];
                    idx[move.Axis % 3] = move.SegNo;
                    idx[(move.Axis + 1) % 3] = i;
                    idx[(move.Axis + 2) % 3] = j;
                    var cubik = Cubiks[idx[2], idx[1], idx[0]];
                    Selection.Add(cubik);
                }
            }

        }

        public void Group()
        {
            Wall = new TObject3D();
            for (int i = 0; i < Selection.Count; i++)
            {
                Selection[i].Parent = Wall;
            }
            Wall.Parent = this;
        }

        public void Ungroup()
        {
            for (int i = Wall.Children.Count - 1; i >= 0 ; i--)
            {
                Wall.Children[i].Parent = this;
            }
            Wall.Parent = null;
        }
    }
}
