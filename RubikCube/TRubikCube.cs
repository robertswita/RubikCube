using GA;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TRubikCube : TShape
    {
        public static int Size = 3;
        public static float C;
        public TCubie[] Cubies;
        public static List<int> EulerOrder;
        public static bool IsEulerOrderReversed;
        // Cluster STRUCTURE (orbits) of THIS cube -- pure geometry. Per-instance now (each holds its own cubie
        // refs), built by the two ctors. The SEARCH target (which cluster/cubie is active, what's solved) lives
        // on TRubikSolver, not here -- a cube has no "active cubie".
        public List<TCluster> Clusters = new List<TCluster>();

        //sbyte[] Transforms;
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
        TMatrix[,] stateGrid;
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
            set { stateGrid = value; }
        }

        public TRubikCube()
        {
            var scale = new TVector(TAffine.N);
            TCubie.Scaling = new TVector(TAffine.N);
            var dimSizes = new int[TAffine.N];
            var s = 0.9f / (TAffine.N - 1);// / (TAffine.N - 2);
            for (int dim = 0; dim < TAffine.N; dim++)
            {
                dimSizes[dim] = Size;
                scale[dim] = 1f / Size;
                TCubie.Scaling[dim] = s;
            }
            TCubie.DimsSizes = new TDims(dimSizes);
            Cubies = new TCubie[TCubie.DimsSizes.TotalLinearSize];
            TCubie.MaxScore = 1 << 2 * TAffine.Planes.Length;
            //TCubie.Cube = CreateHyperCube();
            //TMove.UpdateSizeMatrix();
            C = (Size - 1) / 2f;
            Scale(scale);
            for (int i = 0; i < Cubies.Length; i++)
            {
                var cubie = new TCubie();
                cubie.Transform = TAffine.CreateScale(TCubie.Scaling);
                cubie.GivensOrder = TVector.Uniform(TAffine.N);               
                cubie.StartIndex = i; // setter stamps the sparse orbit-based ClusterIndex
                cubie.Parent = this;
                Cubies[i] = cubie;
            }
            CreateClusters();
        }

        // The Index setter stamps each cubie with a sparse ORBIT index (linear index of its sorted |coords|
        // from the centre - a distance class), so values are non-contiguous and run into the thousands. Remap
        // them to a dense, sequential cluster rank 1,2,3,... in ascending orbit order: now ClusterIndex reads
        // as a real "k-th cluster" and Clusters.Count is the number of clusters. NextCluster (min ClusterIndex)
        // and ActiveCluster (equality) depend only on order and equality - both preserved by this monotonic
        // remap - so the solve order is unchanged.
        private void CreateClusters()
        {
            Clusters.Clear();
            var byOrbit = new List<TCubie>(Cubies);
            byOrbit.Sort((a, b) => a.ClusterIndex.CompareTo(b.ClusterIndex));
            int orbit = int.MinValue;
            TCluster cluster = null;
            foreach (var cubie in byOrbit)
            {
                if (cubie.ClusterIndex != orbit)
                {
                    cluster = new TCluster();
                    Clusters.Add(cluster);
                    orbit = cubie.ClusterIndex;
                }
                cubie.ClusterIndex = Clusters.Count;
                cluster.Cubies.Add(cubie);   // this cube's cubie objects; Clusters[k-1] is the rank-k cluster
            }
            TCluster.MaxSize = Clusters.Max(c => c.Cubies.Count);
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
            // Mirror the cluster STRUCTURE by StartIndex: membership is geometry (identical for this N/SIZE), but
            // the refs must point at THIS copy's cubies (Cubies is indexed by StartIndex). No SEARCH target is
            // copied here -- that belongs to a TRubikSolver (a solver copy ctor when Solutions needs it).
            foreach (var srcCluster in src.Clusters)
            {
                var cluster = new TCluster();
                foreach (var cubie in srcCluster.Cubies)
                    cluster.Cubies.Add(Cubies[cubie.StartIndex]);
                Clusters.Add(cluster);
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

        // Applies a move by rotating the Transform of every cubie in the slice. Transform is the
        // single source of truth for the cube state; State/Position (derived from it) stay current.
        public void Turn(TMove move)
        {
            var plane = TAffine.Planes[move.Plane];
            var rot = TCubie.SetAngle(move.Angle);
            foreach (var cubie in Cubies)
                if (cubie.GetPos(move.Axis) == move.Slice)
                    cubie.Rotate(plane, rot);
        }

        // Cube scoring lives on the GPU now (Gpu.ScoreCube evaluates a zero specimen with the same
        // evaluator the GA uses - one scorer, no separate kernel). The old host mirror (EvaluateGpu +
        // CubieL1/ActiveAxes/CubieState) was removed so the two can no longer drift.

        //bool IsEvaluating;
        // DEAD CPU evaluators (Evaluate/Evaluate2) -- referenced only by the dead CPU-GA path in TRubikGenome
        // (now commented out). They read the SEARCH target (ActiveCluster/SolvedCubies) which no longer lives on
        // the cube, so they are #if false'd rather than deleted (kept for reference, per the seed-path review).
#if false
        public float Evaluate2()
        {
            float score = 0;
            var scrambled = new List<TCubie>();
            var maxClusterState = (float)(1 << 2 * TAffine.Planes.Length) * ActiveCluster.Count * TAffine.N;
            foreach (var cubie in ActiveCluster)
                if (cubie.State != 0)
                {
                    score += maxClusterState + cubie.State + (cubie.RotationCount << TAffine.Planes.Length);
                    scrambled.Add(cubie);
                }
            score /= maxClusterState * (ActiveCluster.Count + 1);
            foreach (var cubie in SolvedCubies)
                if (cubie.State != 0)
                    score++;
            score *= 100;
            //if (scrambled.Count > 0 && !IsEvaluating)
            //{
            //    IsEvaluating = true;
            //    var activeCubie = scrambled[0];
            //    var eulerAngles = activeCubie.EulerAngles;
            //    var seq = new List<TMove>();
            //    for (int idx = 0; idx < eulerAngles.Count; idx++)
            //    {
            //        var rot = eulerAngles[idx];
            //        var angle = TCubie.GetAngle(rot[0], rot[1]);
            //        if (angle > 0)
            //        {
            //            var axis1 = (int)rot[2];
            //            var axis2 = (int)rot[3];
            //            var planeIdx = axis2 * (axis2 - 1) / 2 + axis1;
            //            var move = new TMove();
            //            move.Plane = planeIdx;
            //            move.Axis = TChromosome.Rnd.Next(TAffine.N);
            //            while (move.Axis == axis1 || move.Axis == axis2)
            //                move.Axis = (move.Axis + 1) % TAffine.N;
            //            move.Slice = activeCubie.GetPos(move.Axis);
            //            //move.Slice = (int)Math.Round(p[move.Axis] + TRubikCube.C);
            //            move.Angle = 3 - angle;
            //            seq.Add(move);
            //            Turn(move);
            //            //p = TAffine.CreateRotation(planeIdx, (move.Angle + 1) * 90) * p;
            //        }
            //    }
            //    var predict = Evaluate();
            //    for (int idx = seq.Count-1; idx >= 0; idx--)
            //    {
            //        var move = seq[idx];
            //        move.Angle = 2 - move.Angle;
            //        Turn(move);
            //    }
            //    if (predict == 0)
            //        predict = 1;
            //    if (predict < score)
            //        score = predict;
            //    IsEvaluating = false;
            //}
            return score;
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
            var minScr = TAffine.N;
            if (scrambled.Count <= minScr && scrambled.Count > 0)
            {
                //score *= minScr + 1 - scrambled.Count;
                //var states = new List<int>();
                //foreach (var cubie in scrambled)
                //{
                //    if (!states.Contains(cubie.State))
                //        states.Add(cubie.State);
                //}
                //score *= (float)states.Count / scrambled.Count;
                //score *= (float)states.Count / minScr;
                //score *= 2;
                score *= (float)minScr / scrambled.Count;
                //if (scrambled.Count == minScr && states.Count == 1)
                //    score = 0.01f;
                //score += minScr - scrambled.Count;
            }
            //if (scrambled.Count <= minScr && scrambled.Count > 0)
            //{
            //    var states = new List<int>();
            //    //foreach (var cubie in scrambled)

            //    var hists = new int[TRubikCube.Size, TAffine.N];
            //    foreach (var cubie in scrambled)
            //    {
            //        if (!states.Contains(cubie.State))
            //            states.Add(cubie.State);
            //        var pos = cubie.Position;
            //        //var pos = TCubie.SizeMatrix.Index2Coords(cubie.StartIndex);
            //        for (int dim = 0; dim < TAffine.N; dim++)
            //            hists[(int)pos[dim], dim]++;
            //    }
            //    var maxHist = 0;
            //    var maxDim = 0;
            //    var histPos = new int[TAffine.N];
            //    for (int dim = 0; dim < TAffine.N; dim++)
            //    {
            //        var max = 0;
            //        for (int i = 0; i < TRubikCube.Size; i++)
            //            if (hists[i, dim] > max)
            //            {
            //                max = hists[i, dim];
            //                histPos[dim] = i;
            //            }
            //        if (max > maxHist)
            //        {
            //            maxHist = max;
            //            maxDim = dim;
            //        }
            //    }
            //    var histStates = new List<int>();
            //    foreach (var cubie in scrambled)
            //    {
            //        var pos = cubie.Position;
            //        if (pos[maxDim] == histPos[maxDim] && !histStates.Contains(cubie.State))
            //            histStates.Add(cubie.State);
            //    }
            //    //score *= (float)minScr / maxHist;
            //    //score *= (float)minScr / maxHist;
            //    //score *= minScr + 1 - scrambled.Count;
            //    //score *= (float)(states.Count) / (scrambled.Count);
            //    //score *= (float)(histStates.Count) / (scrambled.Count);
            //    //score++;
            //    //score *= (float)states.Count / maxHist;
            //    var bitCount = 0;
            //    for (int i = 1; i < minScr; i <<= 1)
            //        if ((maxHist & i) != 0)
            //            bitCount++;
            //    if (states.Count == 1 && bitCount == 1 && maxHist == scrambled.Count)
            //        score /= maxHist;
            //}

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
#endif

        // Geometry: every move that turns a layer TOUCHING the given cubie's cluster (the orbit's coord values +
        // mirrors span the cluster's slices). Parameterized by a cubie so callers pass their own (solver target,
        // shuffle, cluster menus, greedy-diversity) -- the cube itself has no "active cubie".
        public List<int> GetClusterMoves(TCubie cubie)
        {
            var pos = cubie.Position;

            // slice'y dotykane przez KT�RYKOLWIEK kubik aktywnego klastra: klaster to orbita
            // (permutacje osi + odbicia) aktywnego kubika, wi�c jego warto�ci wsp�rz�dnych + lustra
            // daj� ca�y zakres slice'�w klastra.
            // Podw�jny zapis do tej samej kom�rki jest bezpieczny (m.in. �rodek przy nieparzystym SIZE).
            var validSlices = new bool[Size];
            for (int coord = 0; coord < TAffine.N; coord++)
            {
                validSlices[pos[coord]] = true;
                validSlices[Size - 1 - pos[coord]] = true;
            }
            var clusterMoves = new List<int>();
            var move = new TMove();
            for (move.Slice = 0; move.Slice < Size; move.Slice++)
            {
                if (!validSlices[move.Slice]) continue;
                for (move.Plane = 0; move.Plane < TAffine.Planes.Length; move.Plane++)
                {
                    var planeAxes = TAffine.Planes[move.Plane];
                    for (move.Axis = 0; move.Axis < TAffine.N; move.Axis++)
                    {
                        if (move.Axis == planeAxes[0] || move.Axis == planeAxes[1]) continue;
                        for (move.Angle = 1; move.Angle < 4; move.Angle++)
                            clusterMoves.Add(move.Encode());
                    }
                }
            }
            return clusterMoves;
        }

        // Scrambles the cube with a reliable number of random cluster-moves — enough that the result is
        // indistinguishable from a uniformly random reachable state. The move count is an internal detail;
        // callers just ask for "a scramble", not a specific dose.
        public void Scramble()
        {
            const int movesPerCubie = 200;   // empirically enough for a well-mixed cube; tune here only
            var rnd = TChromosome.Rnd;
            for (int i = 0; i < movesPerCubie * Cubies.Length; i++)
            {
                var cubie = Cubies[rnd.Next(Cubies.Length)];
                var all = GetClusterMoves(cubie);
                Turn(TMove.Decode(all[rnd.Next(all.Count)]));
            }
        }

        // The SEARCH state and machinery moved to TRubikSolver: the active-cluster/target (ActiveCubie,
        // ActiveCluster, SolvedCubies, SolvedInSlices), the ClusterMoves cache + ComputeSolvedInSlices +
        // ActivateCluster, and the whole seed generator (BuildSeedMoves, GetSolveSeq*, RotCountMode*, GetOrder...).
        // The cube keeps only geometry: Cubies, Turn, Clusters structure, and GetClusterMoves(cubie) above.

        //public int ScrambledCount()
        //{
        //    var scrambled = 0;
        //    for (int i = 0; i < Cubies.Length; i++)
        //        if (Cubies[i].State != 0) scrambled++;
        //    return scrambled;
        //}

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

        //public List<int> GetOrder()
        //{
        //    var nums = new int[TAffine.N];
        //    for (int i = 0; i < nums.Length; i++)
        //        nums[i] = i;
        //    var permuts = DoPermute(nums, 0, nums.Length - 1);
        //    return new List<int>(permuts[TChromosome.Rnd.Next(permuts.Count)]);
        //}

        //public List<int> GetOrder2()
        //{
        //    var order = new List<int>();
        //    var axes = new List<int>();
        //    for (int i = 0; i < TAffine.N; i++)
        //        axes.Add(i);
        //    var connAxes = new List<int>();
        //    //var mask = new bool[TAffine.N, TAffine.N];
        //    while (axes.Count > 0)
        //    {
        //        var axis = axes[TChromosome.Rnd.Next(axes.Count)];
        //        if (connAxes.Count > 0)
        //        {
        //            var connAxis = connAxes[TChromosome.Rnd.Next(connAxes.Count)];
        //            var axis1 = axis;
        //            var axis2 = connAxis;
        //            if (axis1 > axis2)
        //            {
        //                axis1 = connAxis;
        //                axis2 = axis;
        //            }
        //            order.Add(axis2 * (axis2 - 1) / 2 + axis1);
        //        }
        //        axes.Remove(axis);
        //        connAxes.Add(axis);
        //        //var maskIdx = TAffine.Planes[plane];
        //        //mask[axis, connAxis] = true;
        //    }
        //    return order;
        //}

    }
}
