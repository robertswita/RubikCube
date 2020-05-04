using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{

    public class RubikCube : TObject3D
    {
        public static int N = 3;
        TCubik[,,] Cubiks = new TCubik[N, N, N];
        public static double M;
        public double Size = 0.9 / 2;
        public List<TCubik> Selection;
        public TObject3D Wall;

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
            foreach (var cubik in Cubiks)
            {
                idx++;

                var weight = idx / (N * N * N);

                if (cubik.State != 0)
                {
                    result += weight;
                }

            }
            return result;
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
