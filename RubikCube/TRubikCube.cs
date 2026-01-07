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
                    for (int x = 0; x < Size; x++)
                        for (int y = 0; y < Size; y++)
                            for (int z = 0; z < Size; z++)
                                for (int w = 0; w < Size; w++)
                                {
                                    var cubie = Cubies[w, z, y, x].Copy();
                                    for (int axis = 0; axis < 6; axis++)
                                    {
                                        var angle = cubie.State >> 2 * axis & 3;
                                        cubie.Rotate(axis, -90 * angle);
                                    }
                                    var idx = Size * (Size * (Size * cubie.X + cubie.Y) + cubie.Z) + cubie.W;
                                    _StateGrid[i, idx] = cubie.State + (1 << 6);
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
            for (int w = 0; w < Size; w++)
                for (int z = 0; z < Size; z++)
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            var cubie = new TCubie();
                            cubie.Scale(new TVector(cubieScale, cubieScale, cubieScale, cubieScale));
                            cubie.Origin = new TVector(x - C, y - C, z - C, w - C, 1);
                            //cubie.Translate(new TVector(x - C, y - C, z - C, w - C));
                            cubie.Parent = this;
                            Cubies[w, z, y, x] = cubie;
                        }
            //ActCubie = Cubies[(int)C, (int)C, (int)C];
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
            var slice = new TShape();
            int angle = 90 * (move.Angle + 1);
            slice.Rotate(move.Plane, angle);
            var selection = SelectSlice(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];
                cubie.Transform = slice.Transform * cubie.Transform;
                Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X] = cubie;
                cubie.ValidState = false;
                cubie.Transparency = 1;
                if (cubie.State != 0)
                    cubie.Transparency = 0.5f;
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

        //public int GetActCubie()
        //{
        //    ActiveCubie = null;
        //    var result = 0;
        //    var minDist = double.MaxValue;
        //    for (int restrict = (int)C; restrict >= 0; restrict--)
        //    {
        //        for (int side = 0; side < 2; side++)
        //        {
        //            //for (int i = 0; i < N; i++)
        //            //for (int n = 0; n < N; n++)
        //            for (int i = restrict; i < Size - restrict; i++)
        //                for (int n = restrict; n < Size - restrict; n++)
        //                    for (int axis = 0; axis < 3; axis++)
        //                    {
        //                        var v = new int[3];
        //                        v[axis] = side == 0 ? restrict : Size - 1 - restrict;
        //                        v[(axis + 1) % 3] = i;
        //                        v[(axis + 2) % 3] = n;
        //                        var cubie = Cubies[v[3], v[2], v[1], v[0]];
        //                        if (cubie.State != 0)
        //                        {
        //                            //var freeGenes = GetFreeGenes(v);
        //                            var dist = Math.Abs(C - v[2]) + Math.Abs(C - v[1]) + Math.Abs(C - v[0]);// / freeGenes.Count;
        //                            if (dist < minDist)
        //                            {
        //                                minDist = dist;
        //                                ActiveCubie = cubie;

        //                                //if (result < 0)
        //                                result = restrict;
        //                                //return result;
        //                            }
        //                        }
        //                    }
        //        }
        //        if (result > 0)
        //            break;
        //    }
        //    if (ActiveCubie == null)
        //        ActCluster = new List<TCubie>();
        //    else if (!ActCluster.Contains(ActiveCubie))
        //    {
        //        var cluster = GetCluster(ActiveCubie);
        //        foreach (var ccubie in cluster)
        //            ccubie.ClusterCount = cluster.Count;
        //        ActCluster.AddRange(cluster);
        //    }
        //    return result;
        //}

        //public int _GetActCubie()
        //{
        //    ActCubie = null;
        //    var result = 0;
        //    var minDist = double.MaxValue;
        //    for (int restrict = (int)C; restrict >= 0; restrict--)
        //    {
        //        for (int side = 0; side < 2; side++)
        //            for (int i = restrict; i < N - restrict; i++)
        //                //for (int i = 0; i < N; i++)
        //                //for (int n = 0; n < N; n++)
        //                for (int n = restrict; n < N - restrict; n++)
        //                    for (int axis = 0; axis < 3; axis++)
        //                    {
        //                        var v = new int[3];
        //                        v[axis] = side == 0 ? restrict : N - 1 - restrict;
        //                        v[(axis + 1) % 3] = i;
        //                        v[(axis + 2) % 3] = n;
        //                        var cubie = Cubies[v[2], v[1], v[0]];
        //                        if (cubie.State != 0)
        //                        {
        //                            //var freeGenes = GetFreeGenes(v);
        //                            var dist = Math.Abs(C - v[2]) + Math.Abs(C - v[1]) + Math.Abs(C - v[0]);// / freeGenes.Count;
        //                            if (dist < minDist)
        //                            {
        //                                minDist = dist;
        //                                ActCubie = cubie;

        //                                //if (result < 0)
        //                                result = restrict;
        //                                //return result;
        //                            }
        //                        }
        //                    }
        //        if (result > 0)
        //            break;
        //    }
        //    if (ActCubie != null && !ActCluster.Contains(ActCubie))
        //    {
        //        var cluster = GetCluster(ActCubie);
        //        foreach (var ccubie in cluster)
        //            ccubie.ClusterCount = cluster.Count;
        //        ActCluster.AddRange(cluster);
        //    }
        //    return result;
        //}

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
                    score += (1 + cubie.State / 4096f) * ActiveCluster.Count * 2;
            foreach (var cubie in ActiveCluster)
                if (cubie.State != 0)
                    score += 1 + cubie.State / 4096f;
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

        //public List<TCubie> GetCluster(TCubie cubie)
        //{
        //    var cluster = new List<TCubie>();
        //    var move = new TMove();
        //    var idx = new int[] { cubie.X, cubie.Y, cubie.Z };
        //    var cube = new TRubikCube();
        //    cubie = cube.Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X];
        //    for (int alpha = 0; alpha < 4; alpha++)
        //    {
        //        move.Plane = 0;
        //        move.Slice = cubie.X;
        //        cube.Turn(move);
        //        var neigh = Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X];
        //        if (!cluster.Contains(neigh))
        //            cluster.Add(neigh);
        //        for (int beta = 0; beta < 4; beta++)
        //        {
        //            move.Plane = 1;
        //            move.Slice = cubie.Y;
        //            cube.Turn(move);
        //            neigh = Cubies[cubie.Z, cubie.Y, cubie.X];
        //            if (!cluster.Contains(neigh))
        //                cluster.Add(neigh);
        //            for (int gamma = 0; gamma < 4; gamma++)
        //            {
        //                move.Plane = 2;
        //                move.Slice = cubie.Z;
        //                cube.Turn(move);
        //                neigh = Cubies[cubie.Z, cubie.Y, cubie.X];
        //                if (!cluster.Contains(neigh))
        //                    cluster.Add(neigh);
        //            }
        //        }
        //    }
        //    return cluster;
        //}
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
