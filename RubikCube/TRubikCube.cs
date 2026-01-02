using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using TGL;

namespace RubikCube
{
    public class TRubikCube: TObject4D
    {
        public static int N = 3;
        public static int Size = 3;
        public static float C;
        //public TCubie[,,] Cubies = new TCubie[Size, Size, Size];
        public TCubie[,,,] Cubies = new TCubie[Size, Size, Size, Size];
        //public TCubie[] Cubies
        public List<TMove> Moves = new List<TMove>();
        public List<TCubie> ActCluster = new List<TCubie>();
        public TCubie ActCubie;
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
                    _StateGrid = new int[Size * Size * Size, Size * Size * Size];
                    for (int x = 0; x < Size; x++)
                        for (int y = 0; y < Size; y++)
                            for (int z = 0; z < Size; z++)
                                for (int w = 0; w < Size; w++)
                                {
                                    var cubie = Cubies[w, z, y, x].Copy();
                                //var transform = (double[])cubie.Transform.Clone();
                                var alpha = cubie.State & 3;
                                var beta = (cubie.State >> 2) & 3;
                                var gamma = (cubie.State >> 4) & 3;
                                //cubie.RotateZ(-90 * gamma);
                                //cubie.RotateY(-90 * beta);
                                //cubie.RotateX(-90 * alpha);
                                cubie.Roll(-90 * gamma);
                                cubie.Yaw(-90 * beta);
                                cubie.Pitch(-90 * alpha);
                                var i = x * Size * Size + y * Size + z;
                                var idx = cubie.X * Size * Size + cubie.Y * Size + cubie.Z;
                                //cubie.Transform = transform;
                                _StateGrid[i, idx] = cubie.State + (1 << 6);
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
                            cubie.Origin = new TVector(x - C, y - C, z - C, w - C);
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
            if (src.ActCubie != null)
            {
                ActCubie = Cubies[src.ActCubie.W, src.ActCubie.Z, src.ActCubie.Y, src.ActCubie.X];
                ActCluster = new List<TCubie>();
                for (int i = 0; i < src.ActCluster.Count; i++)
                {
                    var cubie = src.ActCluster[i];
                    ActCluster.Add(Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X]);
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
            var axes = move.GetAxes();
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                {
                    var v = new int[4];
                    for (int k = 0; k < 4; k++)
                    v[move.Plane] = move.Slice;

                    v[axes[0]] = i;
                    v[axes[1]] = j;

                    selection.Add(Cubies[v[2], v[1], v[0]]);
                }
            return selection;
        }

        public void Turn(TMove move)
        {
            var slice = new TObject3D();
            int angle = 90 * (move.Angle + 1);
            if (move.Plane == 0)
                slice.Pitch(angle);
            else if (move.Plane == 1)
                slice.Yaw(angle);
            else
                slice.Roll(angle);
            var selection = SelectSlice(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];
                cubie.Transform = slice.Transform * cubie.Transform;
                //cubie.MultMatrix(slice.Transform);
                Cubies[cubie.Z, cubie.Y, cubie.X] = cubie;
                cubie.ValidState = false;
                cubie.Transparent = false;
                if (cubie.State != 0)
                    cubie.Transparent = true;
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

        public int GetActCubie()
        {
            ActCubie = null;
            var result = 0;
            var minDist = double.MaxValue;
            for (int restrict = (int)C; restrict >= 0; restrict--)
            {
                for (int side = 0; side < 2; side++)
                {
                    //for (int i = 0; i < N; i++)
                    //for (int n = 0; n < N; n++)
                    for (int i = restrict; i < Size - restrict; i++)
                        for (int n = restrict; n < Size - restrict; n++)
                            for (int axis = 0; axis < 3; axis++)
                            {
                                var v = new int[3];
                                v[axis] = side == 0 ? restrict : Size - 1 - restrict;
                                v[(axis + 1) % 3] = i;
                                v[(axis + 2) % 3] = n;
                                var cubie = Cubies[v[2], v[1], v[0]];
                                if (cubie.State != 0)
                                {
                                    //var freeGenes = GetFreeGenes(v);
                                    var dist = Math.Abs(C - v[2]) + Math.Abs(C - v[1]) + Math.Abs(C - v[0]);// / freeGenes.Count;
                                    if (dist < minDist)
                                    {
                                        minDist = dist;
                                        ActCubie = cubie;

                                        //if (result < 0)
                                        result = restrict;
                                        //return result;
                                    }
                                }
                            }
                }
                if (result > 0)
                    break;
            }
            if (ActCubie == null)
                ActCluster = new List<TCubie>();
            else if (!ActCluster.Contains(ActCubie))
            {
                var cluster = GetCluster(ActCubie);
                foreach (var ccubie in cluster)
                    ccubie.ClusterCount = cluster.Count;
                ActCluster.AddRange(cluster);
            }
            return result;
        }

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
            double result = 0;
            if (ActCubie == null) return 0;
            var count = ActCubie.ClusterCount;
            //var orbit = ActCluster.Last().Orbit;
            for (int i = 0; i < ActCluster.Count; i++)
            {
                var cubie = ActCluster[i];
                if (i == ActCluster.Count - count)
                    result *= 2 * count;
                //if (cubie.Orbit == orbit)
                //{
                //    result *= 2 * (ActCluster.Count - i);
                //    orbit = -1;
                //}
                if (cubie.State != 0)
                {
                    //var dist = Math.Abs(C - cubie.Z) + Math.Abs(C - cubie.Y) + Math.Abs(C - cubie.X);
                    result += (1 + cubie.State / 256.0);// * (1 + dist / 3 / C);
                }
            }
            return result;
        }

        public List<int> GetFreeGenes()
        {
            var freeGenes = new List<int>();
            var idx = new int[] { ActCubie.X, ActCubie.Y, ActCubie.Z };
            //var max = 0;
            //for (int j = 1; j < 3; j++)
            //    if (Math.Abs(idx[j] - C) > Math.Abs(idx[max] - C))
            //        max = j;
            //var tmp = idx[0];
            //idx[0] = idx[max];
            //idx[max] = tmp;
            for (int axis = 0; axis < 3; axis++)
                for (int i = 0; i < 3; i++)
                    for (int side = 0; side < 2; side++)
                    {
                        var move = new TMove();
                        move.Plane = axis;
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

        public List<TCubie> GetCluster(TCubie cubie)
        {
            var cluster = new List<TCubie>();
            var move = new TMove();
            var idx = new int[] { cubie.X, cubie.Y, cubie.Z };
            var cube = new TRubikCube();
            cubie = cube.Cubies[cubie.Z, cubie.Y, cubie.X];
            for (int alpha = 0; alpha < 4; alpha++)
            {
                move.Plane = 0;
                move.Slice = cubie.X;
                cube.Turn(move);
                var neigh = Cubies[cubie.Z, cubie.Y, cubie.X];
                if (!cluster.Contains(neigh))
                    cluster.Add(neigh);
                for (int beta = 0; beta < 4; beta++)
                {
                    move.Plane = 1;
                    move.Slice = cubie.Y;
                    cube.Turn(move);
                    neigh = Cubies[cubie.Z, cubie.Y, cubie.X];
                    if (!cluster.Contains(neigh))
                        cluster.Add(neigh);
                    for (int gamma = 0; gamma < 4; gamma++)
                    {
                        move.Plane = 2;
                        move.Slice = cubie.Z;
                        cube.Turn(move);
                        neigh = Cubies[cubie.Z, cubie.Y, cubie.X];
                        if (!cluster.Contains(neigh))
                            cluster.Add(neigh);
                    }
                }
            }
            return cluster;
        }
    }
}
