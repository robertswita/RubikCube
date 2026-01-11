using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using TGL;

namespace RubikCube
{
    public class TRubikCube: TShape
    {
        //public static int N = 3;
        public static int Size = 3;
        public static float C;
        //public TCubie[,,] Cubies = new TCubie[Size, Size, Size];
        public TCubie[,,,] Cubies = new TCubie[Size, Size, Size, Size];
        //public TCubie[] Cubies
        public List<TMove> Moves = new List<TMove>();
        //public List<TCubie> ActCluster = new List<TCubie>();
        public TCubie ActiveCubie;
        private List<TCubie> SolvedCubies = new List<TCubie>();
        //public TObject3D Create3DProjection(int axis)
        //{

        //}
        int[,] _StateGrid;
        public int[,] StateGrid
        {
            get
            {
                if (_StateGrid == null)
                {
                    var gridSize = Size * Size * Size * Size;
                    _StateGrid = new int[gridSize, gridSize];
                    var i = 0;
                    for (int w = 0; w < Size; w++)
                        for (int z = 0; z < Size; z++)
                            for (int y = 0; y < Size; y++)
                                for (int x = 0; x < Size; x++)
                                {
                                    var cubie = Cubies[w, z, y, x].Copy();
                                    for (int axis = 0; axis < 6; axis++)
                                    //for (int axis = 5; axis >= 0; axis--)
                                    {
                                        var angle = cubie.State >> 2 * axis & 3;
                                        cubie.Rotate(axis, -90 * angle);
                                    }
                                    var idx = Size * (Size * (Size * cubie.W + cubie.Z) + cubie.Y) + cubie.X;
                                    if (idx != cubie.OriginalPos)
                                        ;
                                    _StateGrid[i, idx] = cubie.State | 1 << 31;
                                    i++;
                                }
                }
                return _StateGrid;
            }
        }

        public TRubikCube()
        {
            C = (Size - 1) / 2f;
            Scale(new TVector(1f / Size, 1f / Size, 1f / Size, 1f / Size));
            var cubieScale = 0.9f / 2;
            var pos = 0;
            for (int w = 0; w < Size; w++)
                for (int z = 0; z < Size; z++)
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            var cubie = new TCubie();
                            cubie.Transform = TAffine.CreateScale(new TVector(cubieScale, cubieScale, cubieScale, cubieScale));
                            cubie.Transform.Origin = new TVector(x - C, y - C, z - C, w - C);
                            cubie.Parent = this;
                            Cubies[w, z, y, x] = cubie;
                            cubie.OriginalPos = pos;
                            pos++;
                        }
        }

        public TRubikCube(TRubikCube src)
        {
            for (int w = 0; w < Size; w++)
                for (int z = 0; z < Size; z++)
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            var cubie = src.Cubies[w, z, y, x].Copy();
                            cubie.Parent = this;
                            Cubies[w, z, y, x] = cubie;
                        }
            if (src.ActiveCubie != null)
            {
                ActiveCubie = Cubies[src.ActiveCubie.W, src.ActiveCubie.Z, src.ActiveCubie.Y, src.ActiveCubie.X];
                foreach (var cubie in src.SolvedCubies)
                {
                    SolvedCubies.Add(Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X]);
                }
                var actCluster = src.ActiveCluster;
                activeCluster = new List<TCubie>();
                for (int i = 0; i < actCluster.Count; i++)
                {
                    var cubie = actCluster[i];
                    activeCluster.Add(Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X]);
                }
            }
        }

        public string Code
        {
            get
            {
                string code = "";
                foreach (var cubie in Cubies)
                    code += (char)cubie.State;
                return code;
            }
            set
            {
                int i = 0;
                foreach (var cubie in Cubies)
                    cubie.State = value[i++];
            }
        }

        public List<TCubie> SelectSlice(TMove move)
        {
            var selection = new List<TCubie>();
            var planeAxes = TAffine.Planes[move.Plane];
            //var slices = new List<int>() { 0, 1, 2, 3 };
            //for (int dim = 0; dim < axes.Length; dim++)
            //    slices.Remove(axes[dim]);
            //slices.Remove(move.Axis);
            for (int segNo = 0; segNo < Size; segNo++)
                for (int i = 0; i < Size; i++)
                    for (int j = 0; j < Size; j++)
                    {
                        var v = new int[] { segNo, segNo, segNo, segNo };
                        v[planeAxes[0]] = i;
                        v[planeAxes[1]] = j;
                        v[move.Axis] = move.Slice;
                        selection.Add(Cubies[v[3], v[2], v[1], v[0]]);
                    }
            return selection;
        }

        public void Turn(TMove move)
        {
            int angle = 90 * (move.Angle + 1);
            var rotation = TAffine.CreateRotation(move.Plane, angle);
            var selection = SelectSlice(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];
                cubie.Transform = rotation * cubie.Transform;
                Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X] = cubie;
                cubie.ValidState = false;
                cubie.Transparency = cubie.State != 0 ? 0.1f : 1;
                cubie.Parent = this;
            }
            _StateGrid = null;
        }
        public void ReTurn(TMove move)
        {
            move.Angle = 2 - move.Angle;
            Turn(move);
            move.Angle = 2 - move.Angle;
        }

        public double Evaluate()
        {
            //double result = 0;
            //if (ActCubie == null) return 0;
            //var count = ActCubie.ClusterCount;
            //for (int i = 0; i < ActCluster.Count; i++)
            //{
            //    var cubie = ActCluster[i];
            //    if (i == ActCluster.Count - count)
            //        result *= 2 * count;
            //    if (cubie.State != 0)
            //    {
            //        result += (1 + cubie.State / 256.0);// * (1 + dist / 3 / C);
            //    }
            //}
            //return result;
            var score = 0d;
            foreach (var cubie in SolvedCubies)
                if (cubie.State != 0)
                    score += (1 + cubie.State / 4096f / 8) * ActiveCluster.Count * 2;
            foreach (var cubie in ActiveCluster)
                if (cubie.State != 0)
                    score += 1 + cubie.State / 4096f / 8;
            return score;
        }

        public List<int> GetFreeMoves()
        {
            var freeGenes = new List<int>();
            var idx = new int[] { ActiveCubie.X, ActiveCubie.Y, ActiveCubie.Z, ActiveCubie.W };
            for (int axis = 0; axis < 4; axis++)
                for (int i = 0; i < 4; i++)
                    for (int plane = 0; plane < 6; plane++)
                        for (int side = 0; side < 2; side++)
                        {
                            var move = new TMove();
                            move.Plane = plane;
                            var planeAxes = move.GetPlaneAxes();
                            if (planeAxes[0] == axis || planeAxes[1] == axis)
                                continue;
                            move.Axis = axis;
                            if (side == 0)
                                move.Slice = idx[i];
                            else
                                move.Slice = Size - 1 - idx[i];
                            var gene = move.Encode();
                            if (freeGenes.IndexOf(gene) < 0)
                            {
                                freeGenes.Add(gene + 0);
                                freeGenes.Add(gene + 1);
                                freeGenes.Add(gene + 2);
                            }
                        }
            return freeGenes;
        }

        private List<TCubie> activeCluster;
        public List<TCubie> ActiveCluster
        {
            get
            {
                if (ActiveCubie == null) return null;
                if (activeCluster == null)
                {
                    activeCluster = new List<TCubie>();
                    for (int angleZW = 0; angleZW < 4; angleZW++)
                    {
                        for (int angleYW = 0; angleYW < 4; angleYW++)
                        {
                            for (int angleYZ = 0; angleYZ < 4; angleYZ++)
                            {
                                for (int angleXW = 0; angleXW < 4; angleXW++)
                                {
                                    for (int angleXZ = 0; angleXZ < 4; angleXZ++)
                                    {
                                        for (int angleXY = 0; angleXY < 4; angleXY++)
                                        {
                                            var cubie = Cubies[ActiveCubie.W, ActiveCubie.Z, ActiveCubie.Y, ActiveCubie.X];
                                            if (!activeCluster.Contains(cubie))
                                                activeCluster.Add(cubie);
                                            ActiveCubie.Rotate(0, 90);
                                        }
                                        ActiveCubie.Rotate(1, 90);
                                    }
                                    ActiveCubie.Rotate(2, 90);
                                }
                                ActiveCubie.Rotate(3, 90);
                            }
                            ActiveCubie.Rotate(4, 90);
                        }
                        ActiveCubie.Rotate(5, 90);
                    }
                }
                return activeCluster;
            }
        }

        public void NextCluster()
        {
            if (activeCluster != null)
                SolvedCubies.AddRange(ActiveCluster);
            var c = (int)C;
            var minDist = int.MaxValue;
            ActiveCubie = null;
            foreach (var cubie in Cubies)
            {
                if (cubie.State == 0) continue;
                var dist = Math.Abs(cubie.X - c) + Math.Abs(cubie.Y - c) + Math.Abs(cubie.Z - c) + Math.Abs(cubie.W - c);
                if (dist < minDist)
                {
                    minDist = dist;
                    ActiveCubie = cubie;
                }
            }
            activeCluster = null;
        }

    }
}
