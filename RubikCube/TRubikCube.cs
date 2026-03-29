using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using TGL;
using GA;

namespace RubikCube
{
    public class TRubikCube : TShape
    {
        public static int Size = 3;
        public static float C;
        public TCubie[] Cubies;
        public TCubie ActiveCubie;
        public List<TCubie> SolvedCubies = new List<TCubie>();
        //public List<int> Seq, RevSeq, ActSeq;
        //public TVector ActivePos;
        TMatrix[,] stateGrid;
        public static List<int> EulerOrder;
        public static bool IsEulerOrderReversed;
        //public int[,] StateGrid2
        //{
        //    get
        //    {
        //        if (stateGrid == null)
        //        {
        //            var gridSize = Cubies.Length;
        //            stateGrid = new int[gridSize, gridSize];
        //            for (int pos = 0; pos < Cubies.Length; pos++)
        //            {
        //                var cubie = Cubies[pos];
        //                stateGrid[cubie.Index, pos] = cubie.State | 1 << 31;
        //            }
        //        }
        //        return stateGrid;
        //    }
        //}

        public TMatrix[,] StateGrid
        {
            get
            {
                if (stateGrid == null)
                {
                    var gridSize = Cubies.Length;
                    stateGrid = new TMatrix[gridSize, gridSize];
                    for (int pos = 0; pos < Cubies.Length; pos++)
                    {
                        var cubie = Cubies[pos];
                        stateGrid[cubie.Index, pos] = cubie.Transform.M;
                    }
                }
                return stateGrid;
            }
        }

        public TRubikCube()
        {
            var size = 1;
            var scale = new TVector(TAffine.N);
            TCubie.Scaling = new TVector(TAffine.N);
            var dimSizes = new int[TAffine.N];
            var s = 0.9f / (TAffine.N - 1);// / (TAffine.N - 2);
            for (int dim = 0; dim < TAffine.N; dim++)
            {
                size *= Size;
                dimSizes[dim] = Size;
                scale[dim] = 1f / Size;
                TCubie.Scaling[dim] = s;
            }
            Cubies = new TCubie[size];
            TCubie.SizeMatrix = new TMatrix(size, 1);
            TCubie.SizeMatrix.DimSizes = dimSizes;
            TCubie.MaxScore = 1 << 2 * TAffine.Planes.Length;
            TCubie.Cube = CreateHyperCube();
            TMove.UpdateSizeMatrix();
            C = (Size - 1) / 2f;
            Scale(scale);
            for (int i = 0; i < Cubies.Length; i++)
            {
                var cubie = new TCubie();
                cubie.Transform = TAffine.CreateScale(TCubie.Scaling);
                cubie.Index = i;
                cubie.StartIndex = i;
                cubie.Parent = this;
                Cubies[i] = cubie;
            }
        }

        public TRubikCube(TRubikCube src)
        {
            Cubies = new TCubie[src.Cubies.Length];
            for (int pos = 0; pos < Cubies.Length; pos++)
            {
                var cubie = src.Cubies[pos].Copy();
                cubie.Parent = this;
                Cubies[pos] = cubie;
            }
            if (src.ActiveCubie != null)
            {
                ActiveCubie = Cubies[src.ActiveCubie.StartIndex];
                foreach (var cubie in src.SolvedCubies)
                {
                    SolvedCubies.Add(Cubies[cubie.StartIndex]);
                }
                var actCluster = src.ActiveCluster;
                activeCluster = new List<TCubie>();
                for (int i = 0; i < actCluster.Count; i++)
                {
                    var cubie = actCluster[i];
                    activeCluster.Add(Cubies[cubie.StartIndex]);
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
            //var planeAxes = TAffine.Planes[move.Plane];
            //for (int segNo = 0; segNo < Size; segNo++)
            //    for (int i = 0; i < Size; i++)
            //        for (int j = 0; j < Size; j++)
            //        {
            //            var v = new int[] { segNo, segNo, segNo, segNo };
            //            v[planeAxes[0]] = i;
            //            v[planeAxes[1]] = j;
            //            v[move.Axis] = move.Slice;
            //            selection.Add(Cubies[v[3], v[2], v[1], v[0]]);
            //        }
            foreach (var cubie in Cubies)
            {
                var v = Math.Round(cubie.Transform.Origin[move.Axis] + C);
                if (v == move.Slice)
                    selection.Add(cubie);
            }
            //if (selection.Count != Cubies.Length / Size)
            //    ;
            return selection;
        }

        public void Turn(TMove move)
        {
            int angle = 90 * (move.Angle + 1);
            var selection = SelectSlice(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];
                cubie.Rotate(move.Plane, angle);
                cubie.ValidState = false;
                cubie.Transparency = cubie.State != 0 ? 0.1f : 1;
                cubie.Parent = this;
            }
            stateGrid = null;
        }

        public void ReTurn(TMove move)
        {
            move.Angle = 2 - move.Angle;
            Turn(move);
            move.Angle = 2 - move.Angle;
        }

        public float Evaluate()
        {
            float score = 0;
            var scrambled = new List<TCubie>();
            //var rotCount = 0;
            //var maxClusterState = (double)(1 << 2 * TAffine.Planes.Length);// * ActiveCluster.Count;// * TAffine.N;
            //foreach (var cubie in ActiveCluster)
            //    if (cubie.State != 0)
            //    {
            //        score += 0.5 * maxClusterState + cubie.State + (cubie.RotationCount << TAffine.Planes.Length);
            //        //scrambled++;
            //        //rotCount += cubie.RotationCount;
            //    }
            //////score *= 1 + ((ActiveCluster.Count - scrambled) & 1);
            //score /= TAffine.N * maxClusterState * ActiveCluster.Count;
            //score *= rotCount / (scrambled + 1);
            //var scrambled = new List<TCubie>();
            //var rotCount = 0;
            var maxClusterState = (float)(1 << 2 * TAffine.Planes.Length) * ActiveCluster.Count * TAffine.N;
            //var maxClusterState = (double)(1 << 2 * TAffine.Planes.Length) * ActiveCluster.Count;
            //var hists = new int[TRubikCube.Size, TAffine.N];
            foreach (var cubie in ActiveCluster)
                if (cubie.State != 0)
                {
                    score += maxClusterState + cubie.State + (cubie.RotationCount << TAffine.Planes.Length);
                    scrambled.Add(cubie);
                    //scrambled++;
                    //var pos = cubie.Position;
                    //for (int dim = 0; dim < TAffine.N; dim++)
                    //    hists[pos[dim], dim]++;
                    //rotCount += cubie.RotationCount;
                }
            //var maxHist = 0;
            //for (int j = 0; j < TAffine.N; j++)
            //{
            //    var max = 0;
            //    for (int i = 0; i < TRubikCube.Size; i++)
            //        if (hists[i, j] > max)
            //            max = hists[i, j];
            //    if (max > maxHist)
            //        maxHist = max;
            //}

            //score *= states.Count;
            //score += rotCount << TAffine.Planes.Length;
            //score *= 1 + ((ActiveCluster.Count - scrambled) & 1);
            score /= maxClusterState * (ActiveCluster.Count + 1);
            //if (scrambled.Count < 4 && scrambled.Count > 0)
            //{
            //    var states = new List<int>();
            //    foreach (var cubie in scrambled)
            //        if (!states.Contains(cubie.State))
            //            states.Add(cubie.State);
            //    score *= 5 - scrambled.Count;
            //    //score *= 4.0 / scrambled.Count;
            //    score *= (double)(states.Count + 1) / (scrambled.Count + 1);
            //    //score++;
            //}
            if (scrambled.Count == 1)
            {
                score *= 2;
                //score *= (double)(states.Count + 1) / (scrambled + 1);
                //score++;
            }
            //if (scrambled == 4 && states.Count == 1 && maxHist == 4)
            //    score /= 4;


            //for (int i = 0; i < scrambled.Count; i++)
            //{
            //    var scrambie = scrambled[i];

            //}
            //var maxClusterState = ActiveCluster.Count;
            //foreach (var cubie in ActiveCluster)
            //    if (cubie.State != 0)
            //    {
            //        score += (maxClusterState + cubie.State);
            //        scrambled++;
            //    }
            ////score *= 1 + ((ActiveCluster.Count - scrambled) & 1);
            //score /= maxClusterState * (ActiveCluster.Count + 1);


            //var maxClusterState = (1 << 2 * TAffine.Planes.Length) * ActiveCluster.Count;
            //foreach (var cubie in ActiveCluster)
            //    if (cubie.State != 0)
            //        score += maxClusterState * cubie.RotationCount * 3 / 2 + cubie.State;
            //score /= maxClusterState * (ActiveCluster.Count * TAffine.N * 3 / 2 + 1);

            //foreach (var cubie in ActiveCluster)
            //    score += cubie.State * cubie.RotationCount;
            //score /= maxClusterState;
            foreach (var cubie in SolvedCubies)
                if (cubie.State != 0)
                    score++;
            return 100 * score;
        }

        public List<int> GetAllMoves()
        {
            var freeGenes = new List<int>();
            var pos = ActiveCubie.Position;
            for (int axis = 0; axis < TAffine.N; axis++)
                for (int coord = 0; coord < TAffine.N; coord++)
                    for (int side = 0; side < 2; side++)
                        for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                        {
                            var move = new TMove();
                            move.Plane = plane;
                            var planeAxes = move.GetPlaneAxes();
                            if (planeAxes[0] == axis || planeAxes[1] == axis)
                                continue;
                            move.Axis = axis;
                            if (side == 0)
                                move.Slice = pos[coord];
                            else
                                move.Slice = Size - 1 - pos[coord];
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


        public List<int> GetFreeMoves()
        {
            var freeGenes = new List<int>();
            var pos = ActiveCubie.Position;
            for (int axis = 0; axis < TAffine.N; axis++)
                for (int coord = 0; coord < TAffine.N; coord++)
                    for (int side = 0; side < 2; side++)
                        for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                        //for (int plane = 0; plane < TAffine.N - 1; plane++)
                        {
                            var move = new TMove();
                            move.Plane = plane;
                            var planeAxes = move.GetPlaneAxes();
                            if (planeAxes[0] == axis || planeAxes[1] == axis)
                                continue;
                            //if (planeAxes[0] != 0 && planeAxes[1] != 0)
                            //    continue;
                            move.Axis = axis;
                            if (side == 0)
                                move.Slice = pos[coord];
                            else
                                move.Slice = Size - 1 - pos[coord];
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

        public List<int> GetFreeMoves2()
        {
            var freeGenes = new List<int>();
            foreach (var cubie in ActiveCluster)
            {
                if (cubie.State != 0)
                {
                    //for (int i = 0; i < TAffine.Planes.Length; i++)
                    //{
                    //var angle = cubie.State >> 2 * i & 3;
                    //if (angle > 0)
                    //{
                    //for (int coord = 0; coord < TAffine.N; coord++)
                        for (int j = 0; j < TAffine.Planes.Length; j++)
                                    //for (int side = 0; side < 2; side++)
                                    for (int axis = 0; axis < TAffine.N; axis++)
                                    {
                                        var move = new TMove();
                                        move.Plane = j;
                                        var planes = move.GetPlaneAxes();
                                        if (axis == planes[0] || axis == planes[1])
                                            continue;
                                        move.Axis = axis;
                                        var pos = (int)Math.Round(cubie.Transform.Origin[axis] + TRubikCube.C);
                                        //if (side == 0)
                                            move.Slice = pos;
                                        //else
                                        //    move.Slice = Size - 1 - pos;
                                        //move.Angle = 3 - angle;
                                        var gene = move.Encode();
                                        if (freeGenes.IndexOf(gene) < 0)
                                        {
                                                //freeGenes.Add(gene);
                                                //if (move.Angle != 1)
                                                //{
                                                //    move.Angle = 2 - move.Angle;
                                                //    freeGenes.Add(move.Encode());
                                                //}
                                                freeGenes.Add(gene + 0);
                                                freeGenes.Add(gene + 1);
                                                freeGenes.Add(gene + 2);
                                            }
                                    }
                        }

                    //}
                    //var pos = ActiveCubie.Position;
                    //for (int axis = 0; axis < TAffine.N; axis++)
                    //    for (int coord = 0; coord < TAffine.N; coord++)
                    //        for (int side = 0; side < 2; side++)
                    //            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    //            //for (int plane = 0; plane < TAffine.N - 1; plane++)
                    //            {
                    //                var move = new TMove();
                    //                move.Plane = plane;
                    //                var planeAxes = move.GetPlaneAxes();
                    //                if (planeAxes[0] == axis || planeAxes[1] == axis)
                    //                    continue;
                    //                move.Axis = axis;
                    //                if (side == 0)
                    //                    move.Slice = pos[coord];
                    //                else
                    //                    move.Slice = Size - 1 - pos[coord];
                    //                var gene = move.Encode();
                    //                if (freeGenes.IndexOf(gene) < 0)
                    //                {
                    //                    freeGenes.Add(gene + 0);
                    //                    freeGenes.Add(gene + 1);
                    //                    freeGenes.Add(gene + 2);
                    //                }
                    //            }
                //}
            }
            return freeGenes;
        }


        private List<TCubie> activeCluster;
        public List<TCubie> ActiveCluster
        {
            get
            {
                if (activeCluster == null)
                {
                    activeCluster = new List<TCubie>();
                    //for (int state = 0; state < 1 << 2 * TAffine.Planes.Length; state++)
                    //{
                    //    var cubie = ActiveCubie.Copy();
                    //    for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                    //        cubie.Rotate(i, 90 * (state >> 2 * i & 3));
                    //    var clusterCubie = Cubies[cubie.Index];
                    //    if (!activeCluster.Contains(clusterCubie))
                    //        activeCluster.Add(clusterCubie);
                    //}
                    foreach (var cubie in Cubies)
                        if (cubie.ClusterIndex == ActiveCubie.ClusterIndex)
                            activeCluster.Add(cubie);
                }
                return activeCluster;
            }
        }

        public void NextCluster()
        {
            if (ActiveCubie != null)
                SolvedCubies.AddRange(ActiveCluster);
            ActiveCubie = null;
            var minDist = int.MaxValue;
            foreach (var cubie in Cubies)
            {
                if (cubie.State == 0) continue;
                if (cubie.ClusterIndex < minDist)
                {
                    minDist = cubie.ClusterIndex;
                    ActiveCubie = cubie;
                }
            }
            activeCluster = null;
            if (ActiveCubie == null)
                SolvedCubies.Clear();
        }

        public int ScrambledCount()
        {
            var scrambled = 0;
            for (int i = 0; i < Cubies.Length; i++)
                if (Cubies[i].State != 0) scrambled++;
            return scrambled;
        }

        //public List<int> GetReversedSeq2()
        //{
        //    Seq = new List<int>();
        //    RevSeq = new List<int>();
        //    ActSeq = ActiveCubie.IsReversedSeq ? RevSeq : Seq;
        //    var cube = new TRubikCube(this);
        //    //foreach (var cubie in cube.ActiveCluster)
        //    //for (int n = 0; n < 1; n++)
        //    //{
        //    //var cubie = cube.ActiveCluster[n];
        //    var p = ActiveCubie.Transform.Origin.Clone();
        //    //if (!ActiveCubie.IsReversedSeq)
        //    {
        //        for (int i = 0; i < TAffine.Planes.Length; i++)
        //        {
        //            var angle = ActiveCubie.GetAngle(ActiveCubie.EulerAngles[i][0], ActiveCubie.EulerAngles[i][1]);// cubie.State >> shift & 3;
        //                                                                                                           //shift -= 2;
        //            if (angle > 0)
        //            {
        //                var move = new TMove();
        //                move.Plane = i;
        //                var planes = move.GetPlaneAxes();
        //                while (move.Axis == planes[0] || move.Axis == planes[1])
        //                    move.Axis++;
        //                //move.Slice = (int)Math.Round(cubie.Transform.Origin[move.Axis] + TRubikCube.C);
        //                move.Slice = (int)Math.Round(p[move.Axis] + TRubikCube.C);
        //                move.Angle = 3 - angle;
        //                Seq.Add(move.Encode());
        //                cube.Turn(move);
        //                p = TAffine.CreateRotation(i, (move.Angle + 1) * 90) * p;
        //            }
        //        }
        //        //ActSeq = Seq;
        //    }
        //    //else
        //    {
        //        cube = new TRubikCube(this);
        //        var revEulerAngles = ActiveCubie.Transform.GetReversedEulerAngles();
        //        p = ActiveCubie.Transform.Origin.Clone();
        //        for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
        //        {
        //            var angle = ActiveCubie.GetAngle(revEulerAngles[i][0], revEulerAngles[i][1]);
        //            if (angle > 0)
        //            {
        //                var move = new TMove();
        //                move.Plane = i;
        //                var planes = move.GetPlaneAxes();
        //                while (move.Axis == planes[0] || move.Axis == planes[1])
        //                    move.Axis++;
        //                //move.Slice = (int)Math.Round(cubie.Transform.Origin[move.Axis] + TRubikCube.C);
        //                move.Slice = (int)Math.Round(p[move.Axis] + C);
        //                move.Angle = 3 - angle;
        //                RevSeq.Add(move.Encode());
        //                cube.Turn(move);
        //                p = TAffine.CreateRotation(i, (move.Angle + 1) * 90) * p;
        //            }
        //        }
        //        //ActSeq = RevSeq;
        //    }
        //    ActivePos = p;
        //    //var revSeqSolved = 0;
        //    //foreach (var cubie in cube.ActiveCluster)
        //    //    if (cubie.State == 0) revSeqSolved++;
        //    //ActSeq = seqSolved >= revSeqSolved ? Seq : RevSeq;

        //    ActiveCubie.IsReversedSeq = !ActiveCubie.IsReversedSeq;
        //    return ActSeq;
        //}

        //static void Swap(ref int a, ref int b)
        //{
        //    var temp = a;
        //    a = b;
        //    b = temp;
        //}

        //static List<int[]> DoPermute(int[] nums, int start, int end)
        //{
        //    var result = new List<int[]>();
        //    if (start == end)
        //    {
        //        var perm = new int[nums.Length];
        //        Array.Copy(nums, perm, nums.Length);
        //        result.Add(perm);
        //    }
        //    else
        //        for (var i = start; i <= end; i++)
        //        {
        //            Swap(ref nums[start], ref nums[i]);
        //            result.AddRange(DoPermute(nums, start + 1, end));
        //            Swap(ref nums[start], ref nums[i]);
        //        }
        //    return result;
        //}

        public List<int> GetOrder()
        {
            var mask = new bool[TAffine.N, TAffine.N];
            var planes = new List<int>();
            for (int i = 0; i < TAffine.Planes.Length; i++)
                planes.Add(i);
            var order = new List<int>();
            while (planes.Count > 0)
            {
                var pool = new List<int>();
                for (int i = 0; i < planes.Count; i++)
                {
                    var planeIdx = TAffine.Planes[planes[i]];
                    var n = planeIdx[0];
                    var m = planeIdx[1];
                    if (n == 0 || mask[m, n - 1] && mask[n, n - 1])
                        pool.Add(planes[i]);
                }
                var plane = pool[TChromosome.Rnd.Next(pool.Count)];
                planes.Remove(plane);
                var maskIdx = TAffine.Planes[plane];
                mask[maskIdx[1], maskIdx[0]] = true;
                order.Add(plane);
            }
            return order;
        }

        public List<int> ActSeq;
        public List<int> GetReversedSeq()
        {
            ActSeq = new List<int>();
            //var eulerAngles = ActiveCubie.Transform.GetEulerAngles(EulerOrder, IsEulerOrderReversed);
            var eulerAngles = ActiveCubie.Transform.GetEulerAngles(null);
            var cube = new TRubikCube(this);
            var p = ActiveCubie.Transform.Origin.Clone();
            for (int idx = 0; idx < eulerAngles.Count; idx++)
            {
                var rot = eulerAngles[idx];
                var angle = ActiveCubie.GetAngle(rot[0], rot[1]);
                if (angle > 0)
                {
                    var axis1 = (int)rot[2];
                    var axis2 = (int)rot[3];
                    var planeIdx = axis2 * (axis2 - 1) / 2 + axis1;
                    var move = new TMove();
                    move.Plane = planeIdx;
                    var planes = move.GetPlaneAxes();
                    while (move.Axis == planes[0] || move.Axis == planes[1])
                        move.Axis++;
                    //move.Slice = (int)Math.Round(cubie.Transform.Origin[move.Axis] + TRubikCube.C);
                    move.Slice = (int)Math.Round(p[move.Axis] + TRubikCube.C);
                    move.Angle = 3 - angle;
                    ActSeq.Add(move.Encode());
                    cube.Turn(move);
                    p = TAffine.CreateRotation(planeIdx, (move.Angle + 1) * 90) * p;
                }
            }
            if (cube.ActiveCubie.State != 0)
                ;
            return ActSeq;
        }

    }
}
