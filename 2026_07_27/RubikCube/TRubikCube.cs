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
        public TCubie ActiveCubie;
        public List<TCubie> SolvedCubies = new List<TCubie>();
        public static List<int> EulerOrder;
        public static bool IsEulerOrderReversed;
        public float Score;
        public int Scrambled;
        public TRubikGenome Best;
        public int IterationsCount;
        int Stall;

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
            //TCubie.Cube = CreateHyperCube();
            //TMove.UpdateSizeMatrix();
            C = (Size - 1) / 2f;
            Scale(scale);
            for (int i = 0; i < Cubies.Length; i++)
            {
                var cubie = new TCubie();
                cubie.Transform = TAffine.CreateScale(TCubie.Scaling);
                cubie.GivensOrder = TVector.Uniform(TAffine.N);
                cubie.Index = i;                 // setter stamps the sparse orbit-based ClusterIndex
                cubie.StartIndex = i;
                cubie.Parent = this;
                Cubies[i] = cubie;
            }
            RenumberClusters();
        }

        // The Index setter stamps each cubie with a sparse ORBIT index (linear index of its sorted |coords|
        // from the centre - a distance class), so values are non-contiguous and run into the thousands. Remap
        // them to a dense, sequential cluster rank 1,2,3,... in ascending orbit order: now ClusterIndex reads
        // as a real "k-th cluster" and ClustersCount is the number of clusters. NextCluster (min ClusterIndex)
        // and ActiveCluster (equality) depend only on order and equality - both preserved by this monotonic
        // remap - so the solve order is unchanged.
        private void RenumberClusters()
        {
            var byOrbit = new List<TCubie>(Cubies);
            byOrbit.Sort((a, b) => a.ClusterIndex.CompareTo(b.ClusterIndex));
            int rank = 0, prevOrbit = int.MinValue;
            foreach (var cubie in byOrbit)
            {
                int orbit = cubie.ClusterIndex;                  // still the orbit index (not yet remapped)
                if (orbit != prevOrbit) { rank++; prevOrbit = orbit; }   // 1, 2, 3, ...
                cubie.ClusterIndex = rank;
            }
            ClustersCount = rank;
        }

        public TRubikCube(TRubikCube src)
        {
            ClustersCount = src.ClustersCount;
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
                    SolvedCubies.Add(Cubies[cubie.StartIndex]);
                var actCluster = src.ActiveCluster;
                activeCluster = new List<TCubie>();
                for (int i = 0; i < actCluster.Count; i++)
                    activeCluster.Add(Cubies[actCluster[i].StartIndex]);
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
                var v = cubie.GetPos(move.Axis);
                if (v == move.Slice)
                    selection.Add(cubie);
            }
            return selection;
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

        // Packs every cubie's orientation (from its Transform) into the shader's uint format: per row
        // r the field is (sign << BITS_FOR_COL) | col, where col is the single non-zero entry of that
        // row and sign is its sign. Identity row r -> field r, so a solved cubie packs to the identity
        // value and cubieL1 == 0. Axis numbering matches the CPU (row/col = CPU axes), consistent with
        // getStartCoordinate after the axis-order fix.
        public uint[] PackCubies()
        {
            var packed = new uint[Cubies.Length];
            for (int id = 0; id < Cubies.Length; id++)
                packed[id] = Cubies[id].Transform.OrthoPack();
            return packed;
        }

        // Cube scoring lives on the GPU now (Gpu.ScoreCube evaluates a zero specimen with the same
        // evaluator the GA uses - one scorer, no separate kernel). The old host mirror (EvaluateGpu +
        // CubieL1/ActiveAxes/CubieState) was removed so the two can no longer drift.

        //bool IsEvaluating;
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
                                freeGenes.Add(gene + 1);   // 90
                                freeGenes.Add(gene + 2);   // 180
                                freeGenes.Add(gene + 3);   // 270  (angle 0 = identity, excluded)
                            }
                        }
            return freeGenes;
        }

        List<int> freeMoves;
        public List<int> FreeMoves => freeMoves ??= GetFreeMoves();
        void RebuildFreeMoves() => freeMoves = null;

        List<int> GetFreeMoves()
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
                                freeGenes.Add(gene + 1);   // 90
                                freeGenes.Add(gene + 2);   // 180
                                freeGenes.Add(gene + 3);   // 270  (angle 0 = identity, excluded)
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
                                freeGenes.Add(gene + 1);   // 90
                                freeGenes.Add(gene + 2);   // 180
                                freeGenes.Add(gene + 3);   // 270  (angle 0 = identity, excluded)
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


        // Number of distinct clusters. After RenumberClusters, cubie ClusterIndex runs 1..ClustersCount in
        // solve order. Set once during cube construction; used for the UI "Cluster k / K" label.
        public int ClustersCount;

        private List<TCubie> activeCluster;
        public List<TCubie> ActiveCluster
        {
            get
            {
                if (activeCluster == null)
                {
                    activeCluster = new List<TCubie>();
                    if (ActiveCubie != null)
                        foreach (var cubie in Cubies)
                            if (cubie.ClusterIndex == ActiveCubie.ClusterIndex)
                                activeCluster.Add(cubie);
                }
                return activeCluster;
            }
        }

        public void Reset()
        {
            IterationsCount = 0;
            Score = 0;
            ActiveCubie = null;
        }

        // ONE GA decision step -- Runs in BOTH phases
        // (the count-peel descent AND the post-floor coherence climb). It:
        //   1. re-selects the target cubie -- UNSOLVED one of the active cluster -- which is what lets
        //      successive GA runs during a stall attack the SAME configuration from different cubies;
        //   2. advances to the next cluster when the current one is solved (descent phase, base floor);
        //   3. runs one GA and applies the STALL accept test (accept strictly-better always, equal only after
        //      StallLimit consecutive non-improving runs -- a gated sideways step off a plateau).
        // Returns the accepted moves (empty if none, or if the whole cube is solved -> Best == null).
        public List<TMove> GetNextMoves()
        {
            var moves = new List<TMove>();
            if (Score == 0)                            // current cluster solved -> advance
                NextCluster();
            if (ActiveCubie == null) // whole cube solved
            {
                Best = null;
                return moves;
            }
            Scrambled = 0;
            foreach (var c in ActiveCluster) if (c.State != 0) Scrambled++;
            // COHERENCE LATCH (no walk). Two phases: during the DESCENT coherence is OFF, so the evaluator is the
            // bare count and the peel drives the residual down fast; once the active cluster's residual reaches the
            // floor (base 4), latch coherence ON for the ENDGAME and leave it on for the rest of the cluster. The
            // floor is now ONLY this latch threshold -- it never walks. Count-cancellation lives inside the metric
            // (the /scrambled in the coherence factor), so once latched the fitness is the bare coherence factor
            // for the whole endgame (0 < scrambled <= G) and the yardstick never jumps. The latch itself DOES
            // switch metrics (count -> factor), so RubikCube.Score (the accept-test reference) is re-measured once,
            // at the flip. Reset to descent (coherence OFF, floor 4) per cluster in Solve(). Called after the moves
            // are applied, when the cube IS the state just reached.
            if (Gpu.Coherent == 0 && Scrambled <= Gpu.Floor)     // reached the floor -> latch the endgame ONCE
            {
                Gpu.Coherent = 1;
                Score = Gpu.ScoreCube(this); // metric switched count -> factor: rebase the accept test
            }
            // advance target: DETERMINISTIC round-robin --  // the next UNSOLVED cubie of the cluster after the
            var cluster = ActiveCluster;           // current one (wrapping). Cluster order is stable
            int cur = cluster.IndexOf(ActiveCubie);// (Cubies array order), so successive steps cycle
            for (int step = 1; step <= cluster.Count; step++)// through all cubies once per pass -- no coupon-
            {                                                // collector waste of the old random pick, and it
                var cand = cluster[(cur + step) % cluster.Count];  // pairs exactly with StallLimit = #unsolved.
                if (cand.State != 0) { ActiveCubie = cand; break; }
            }
            TRubikGenome.RubikCube = this;
            Gpu.swExecGA.Start();
            Best = Gpu.ExecuteGA();
            Gpu.swExecGA.Stop();
            if (VerifyReplay && Best.ResultPacked != null) VerifyWinnerReplay(Best);   // A1: GPU state == CPU replay (remove in A2)
            // Accept EQUAL fitness too (<=), not only strictly-better: a sideways move on the plateau, but only
            // after a full round-robin pass with no improvement. Safe because the no-op penalty (2*CUBIES_COUNT)
            // makes standing still score ABOVE Score, so Fitness == Score is always a REAL move to a different
            // equal-fitness state. The stall budget is the number of UNSOLVED cubies: paired with the round-robin
            // target advance, that is exactly "try every cubie once; if none improved, step sideways" -- a
            // principled limit, not the old fixed 20 (STALL_LIMIT define is no longer read here).
            Stall++;
            if (Best.Fitness <= Score)
                //|| (Best.Fitness == Score && Stall >= Scrambled))   // one full round-robin pass = #unsolved = Scrambled
            {
                Stall = 0;
                Best.Correct();
                for (int i = 0; i < Best.BestMovesCount; i++)
                    moves.Add(TMove.Decode((int)Best.Genes[i]));
                Score = Best.Fitness;
            }
            IterationsCount += Gpu.GenerationsCount;
            return moves;
        }

        // A1 CHECKPOINT (temporary): prove the GPU-replayed winner state matches a CPU replay before A2 makes the
        // batch depend on it. Clones the CURRENT cube (the state uploaded as Cubies for this GA run), replays the
        // winner's RAW prefix, and compares OrthoPack per cubie against best.ResultPacked. Runs BEFORE Correct(),
        // so it uses the same raw move codes the GPU replayed. Slower than either path (it does both) -- for
        // verification only; A2 deletes this and the CPU replay it mirrors.
        public static bool VerifyReplay = false;   // A1 verified; off so the profiling batch measures real cost, not the check
        void VerifyWinnerReplay(TRubikGenome best)
        {
            var clone = new TRubikCube(this);
            for (int i = 0; i < best.BestMovesCount; i++)
                clone.Turn(TMove.Decode((int)best.Genes[i]));
            for (int id = 0; id < Cubies.Length; id++)
                if (clone.Cubies[id].Transform.OrthoPack() != best.ResultPacked[id])
                    throw new Exception($"ReplayWinner mismatch: cubie {id}, GPU {best.ResultPacked[id]} vs CPU {clone.Cubies[id].Transform.OrthoPack()}");
        }

        public void NextCluster()
        {
            Gpu.Coherent = 0;    // new cluster -> descent
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
            else
            {
                RebuildFreeMoves();
                GetSolveSeq();
                Score = Gpu.ScoreCube(this);
            }
        }

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

        static void Swap(ref int a, ref int b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        static List<int[]> DoPermute(int[] nums, int start, int end)
        {
            var result = new List<int[]>();
            if (start == end)
            {
                var perm = new int[nums.Length];
                Array.Copy(nums, perm, nums.Length);
                result.Add(perm);
            }
            else
                for (var i = start; i <= end; i++)
                {
                    Swap(ref nums[start], ref nums[i]);
                    result.AddRange(DoPermute(nums, start + 1, end));
                    Swap(ref nums[start], ref nums[i]);
                }
            return result;
        }

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

        public List<int> GetOrder()
        {
            var mask = new bool[TAffine.N, TAffine.N];
            var planes = new List<int>();
            for (int i = 0; i < TAffine.Planes.Length; i++)
                planes.Add(i);
            var caseCount = 1;
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
                caseCount *= pool.Count;
                var plane = pool[TChromosome.Rnd.Next(pool.Count)];
                planes.Remove(plane);
                var maskIdx = TAffine.Planes[plane];
                mask[maskIdx[1], maskIdx[0]] = true;
                order.Add(plane);
            }
            return order;
        }

        bool NonStandard(int[] order)
        {
            if (order == null) return false;
            var mask = new bool[TAffine.N, TAffine.N];
            for (int i = 0; i < order.Length; i++)
            {
                var planeIdx = TAffine.Planes[order[i]];
                var n = planeIdx[0];
                var m = planeIdx[1];
                mask[m, n] = true;
                if (n > 0 && (!mask[m, n - 1] || !mask[n, n - 1]))
                    return true;
            }
            return false;
        }

        // Moves-to-solve of an orientation under (plane ORDER, mixed-Givens mode): reduce plane by plane in the
        // given order[] with a per-plane strategy packed 2 bits per plane in modeMask (base-4): 0=pod-L, 1=nad-L,
        // 2=pod-R, 3=nad-R. pod/nad zero the SUB- vs SUPER-diagonal; L/R = row op (left, Rotate, -sin) vs column
        // op (right, RotatePost, +sin) - a right op still yields a real cube move (L*M*R = I => M^-1 = R*L).
        // Natural order [0..P-1] + modeMask 0 = standard QR baseline. Non-destructive (works on a clone). The
        // full P! order space subsumes axis-permutation, so order (not perm) is the diversity axis (diagnostic).
        static int RotCountMode(TMatrix M, int[] order, int modeMask)
        {
            var A = (TMatrix)M.Clone();                        // working copy (non-destructive)
            int c = 0;
            for (int i = 0; i < order.Length; i++)
            {
                int p = order[i];
                int a1 = TAffine.Planes[p][0], a2 = TAffine.Planes[p][1];
                int strat = (modeMask >> (2 * p)) & 3;         // 0=pod-L 1=nad-L 2=pod-R 3=nad-R
                float a, b;
                switch (strat)
                {
                    case 0:  a = A[a1, a1]; b =  A[a2, a1]; break;   // pod-L: zero sub-diag [a2,a1] (row op)
                    case 1:  a = A[a2, a2]; b = -A[a1, a2]; break;   // nad-L: zero super-diag [a1,a2] (row op)
                    case 2:  a = A[a2, a2]; b = -A[a2, a1]; break;   // pod-R: zero sub-diag [a2,a1] (col op)
                    default: a = A[a1, a1]; b =  A[a1, a2]; break;   // nad-R: zero super-diag [a1,a2] (col op)
                }
                float r = (float)Math.Sqrt(a * a + b * b);
                if (r < 0.1f) continue;                                            // pivot ~ 0 -> identity rotation
                float cos = a / r, sin = b / r;
                if (strat < 2) A.Rotate(a1, a2, cos, -sin);        // left: row op, -sin
                else           A.RotatePost(a1, a2, cos, sin);     // right: col op, +sin
                if (TCubie.GetAngle(cos, sin) != 0) c++;
            }
            return c;
        }

        // mode moves-to-solve of orientation M after applying cube move (plane, angle). Clones the small n x n
        // matrix (not the whole cubie) and left-rotates it exactly as Turn rotates a cubie, so the greedy's
        // prediction matches the real move; the clone leaves M intact for the sibling candidates.
        static int RotCountModeAfter(TMatrix M, int plane, int angle, int[] order, int modeMask)
        {
            var A = (TMatrix)M.Clone();
            var pa = TAffine.Planes[plane];
            var rot = TCubie.SetAngle(angle);
            A.Rotate(pa[0], pa[1], rot.X, rot.Y);
            return RotCountMode(A, order, modeMask);
        }

        static int[] IdentityOrder()
        {
            var o = new int[TAffine.Planes.Length];
            for (int i = 0; i < o.Length; i++) o[i] = i;
            return o;
        }

        // A random plane-reduction order (any permutation of the P planes). Drawn per seed so the pool samples
        // the strategy x order space - the full generating axis (order subsumes perm, per the diagnostic). An
        // order that is invalid for a given (cubie, strategy) just makes the greedy stuck -> null -> redraw.
        static int[] RandomOrder()
        {
            var o = IdentityOrder();
            for (int i = o.Length - 1; i > 0; i--)
            {
                int j = TChromosome.Rnd.Next(i + 1);
                (o[i], o[j]) = (o[j], o[i]);
            }
            return o;
        }

        // Greedy solve of the host-designated active cubie under a mixed-Givens mode, via RANDOMIZED
        // BACKTRACKING on a lightweight copy of just that cubie (deep Transform, shared geometry). Every
        // descent takes a random cube move (plane + 90/180/270) that lowers the mode moves-to-solve by one;
        // on a dead end (the metric hits 0 with M != I, or no progress move exists) it backtracks and tries
        // another branch instead of giving up. This is the crucial fix over a one-shot descent: a mixed
        // mode's solving paths are NARROW, so a single random descent almost always dead-ends and the greedy
        // harvested ZERO macro-moves (every non-stuck draw was a minimal-length path). Backtracking finds a
        // full length-rc0 path iff one exists, so macro modes (rc0 > minimal) actually yield their longer
        // sequences. modeMask == 0 is the standard shortest-solve. Returns null only when the whole monotone
        // tree dead-ends (mode genuinely stuck for this cubie) - the caller redraws a fresh mode.
        public List<int> GetSolveSeq(int[] order, int modeMask)
        {
            var c = ActiveCubie.Copy();        // just the cubie - no full-cube clone
            var seq = new List<int>();
            int rc0 = RotCountMode(c.Transform.M, order, modeMask);
            return SolveModeDfs(c, order, modeMask, rc0, seq) ? seq : null;
        }

        // Depth-first search over the monotone tree (each step drops the mode moves-to-solve by exactly 1),
        // random branch order, first solution wins. seq accumulates the realized moves; on backtrack the last
        // move is popped. rc == current mode moves-to-solve == depth remaining down to the identity.
        bool SolveModeDfs(TCubie c, int[] order, int modeMask, int rc, List<int> seq)
        {
            if (c.State == 0) return true;                           // reached identity - seq solves the cubie
            var cand = new List<int[]>();
            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                for (int angle = 1; angle <= 3; angle++)            // quarter-turns: 1/2/3 = 90/180/270
                    if (RotCountModeAfter(c.Transform.M, plane, angle, order, modeMask) == rc - 1)
                        cand.Add(new[] { plane, angle });
            for (int i = cand.Count - 1; i > 0; i--)                // Fisher-Yates: progress moves in random order
            {
                int j = TChromosome.Rnd.Next(i + 1);
                (cand[i], cand[j]) = (cand[j], cand[i]);
            }
            foreach (var pick in cand)
            {
                var pa = TAffine.Planes[pick[0]];
                var child = c.Copy();
                child.Rotate(pa, TCubie.SetAngle(pick[1]));
                int axis = TChromosome.Rnd.Next(TAffine.N);          // any axis not in the plane (collateral only)
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = pick[0], Angle = pick[1], Axis = axis, Slice = child.GetPos(axis) }.Encode());
                if (SolveModeDfs(child, order, modeMask, rc - 1, seq)) return true;
                seq.RemoveAt(seq.Count - 1);                         // dead end - undo and try the next branch
            }
            return false;
        }

        // Single-descent (NO backtracking) mode solve: at each step take ONE random move that drops the mode
        // moves-to-solve by 1; on a dead end (no such move) FAIL (no undo). O(rc) per attempt vs the DFS's
        // branching - so cheap even when it fails, and the caller just redraws ("draw until valid"). Narrow
        // (macro) modes dead-end often, so the pool ends up mostly minimal - deliberate: the fast SEED path
        // trades macro-move material for speed. Mutates its own cubie copy in place (no backtrack -> no copies).
        public List<int> GetSolveSeqGreedy(int[] order, int modeMask)
        {
            var c = ActiveCubie.Copy();
            var seq = new List<int>();
            int rc = RotCountMode(c.Transform.M, order, modeMask);
            while (c.State != 0)
            {
                int bestPlane = -1, bestAngle = 0, hits = 0;
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)
                        if (RotCountModeAfter(c.Transform.M, plane, angle, order, modeMask) == rc - 1
                            && TChromosome.Rnd.Next(++hits) == 0)     // reservoir pick: one uniform winner, no list
                        { bestPlane = plane; bestAngle = angle; }
                if (hits == 0) return null;                           // dead end, no backtrack
                var pa = TAffine.Planes[bestPlane];
                c.Rotate(pa, TCubie.SetAngle(bestAngle));
                int axis = TChromosome.Rnd.Next(TAffine.N);
                while (axis == pa[0] || axis == pa[1]) axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = bestPlane, Angle = bestAngle, Axis = axis, Slice = c.GetPos(axis) }.Encode());
                rc--;
            }
            return seq;
        }

        // Legacy standard-metric greedy: same shortest-solve as GetSolveSeq(0), but the moves-to-solve is
        // read through the cubie's State getter (which honours the CPU GA's EulerOrder / IsEulerOrderReversed
        // toggles), not the explicit mixed-Givens metric. Kept for the CPU path (TRubikGenome.Genesis and the
        // warm-up call in ActivateCubie); the GPU seed path uses the mode overload instead.
        public List<int> GetSolveSeq()
        {
            var c = ActiveCubie.Copy();        // just the cubie - no full-cube clone
            var seq = new List<int>();
            while (c.State != 0)          // State getter refreshes c.RotationCount
            {
                int[] pa;
                TVector rot;
                var cand = new List<int[]>();
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)   // quarter-turns: 1/2/3 = 90/180/270
                    {
                        var rotCubie = c.Copy();
                        pa = TAffine.Planes[plane];
                        rot = TCubie.SetAngle(angle);
                        rotCubie.Rotate(pa, rot);
                        _ = rotCubie.State;
                        if (rotCubie.RotationCount == c.RotationCount - 1)
                            cand.Add(new[] { plane, angle });
                    }
                var pick = cand[TChromosome.Rnd.Next(cand.Count)];
                pa = TAffine.Planes[pick[0]];
                rot = TCubie.SetAngle(pick[1]);
                c.Rotate(pa, rot);
                int axis = TChromosome.Rnd.Next(TAffine.N);          // any axis not in the plane (collateral only)
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = pick[0], Angle = pick[1], Axis = axis, Slice = c.GetPos(axis) }.Encode());
            }
            return seq;
        }

        // Packs up to numSeeds per-specimen seed sequences into a flat buffer (stride genes each, unused
        // slots = -1 sentinel) for the GPU Init shader. Each draw is a fresh greedy decomposition
        // (GetSolveSeq) under a RANDOMLY chosen mixed-Givens mode, realized with a random axis per move (the
        // axis only picks the collateral layer, not the target's solve, so it adds no decomposition trees).
        // mode 0 is the standard shortest-solve; mixed modes trace longer macro-move paths - the mode pool
        // (2^P) is the dominant diversity source. A stuck mode yields null; we just redraw. Targets
        // populationCount * seedRatioPercent / 100 slots; numSeeds is however many distinct sequences it finds.
        // Canonicalize a move sequence in place: compose consecutive moves in the SAME (axis, plane, slice) as
        // quarter-turns mod 4 (Rz90 Rz90 -> Rz180; Rz90 Rz270 -> removed). Same logic as TRubikGenome.Correct /
        // TGreedyDiversity.CorrectGenes. Run before the seed dedup so two Correct-equal draws fold to one slot
        // instead of being kept as distinct raw strings.
        static void CorrectSeq(List<int> g)
        {
            for (int idx = 0; idx < g.Count; idx++)
            {
                var move = TMove.Decode(g[idx]);
                for (int prev = idx - 1; prev >= 0; prev--)
                {
                    var pm = TMove.Decode(g[prev]);
                    if (pm.Axis != move.Axis || pm.Plane != move.Plane) break;
                    if (pm.Slice == move.Slice)
                    {
                        int angle = (move.Angle + pm.Angle) & 3;         // compose quarter-turns mod 4
                        if (angle > 0) { pm.Angle = angle; g[prev] = pm.Encode(); }
                        else { g.RemoveAt(prev); idx--; }
                        g.RemoveAt(idx); idx--;
                        break;
                    }
                }
            }
        }

        public static bool Diagnostic;       // set by Gpu.SeedStats: enables the census + report in BuildSeedMoves
        public string LastSeedReport = "";   // filled by BuildSeedMoves (only when Diagnostic); shown by Seed Pool Stats

        public int[] BuildSeedMoves(int populationCount, int stride, int seedRatioPercent, bool mixedModes, bool fast, out uint numSeeds)
        {
            int target = populationCount * seedRatioPercent / 100;
            int modeSpace = mixedModes ? 1 << (2 * TAffine.Planes.Length) : 1;   // 4^P strategies; SEED_MODE 0 -> mode 0 (baseline)
            var slots = new List<int[]>();
            var seen = new HashSet<string>();
            // The distinct-manoeuvre pool across modes is far richer than the single-tree shortest-solve pool,
            // but still finite. Stop once draws stop producing anything new (pool saturated) instead of
            // spinning a fixed guard; the stall budget scales with what we found so far so bigger pools get
            // patience. Both a stuck mode (null) and a duplicate count as a no-progress draw.
            int stall = 0;
            int draws = 0, stuck = 0, dupes = 0;                     // telemetry: why the pool ends up the size it is
            var lenHist = new SortedDictionary<int, int>();          // accepted-seed length distribution
            var modeContrib = new HashSet<int>();                    // distinct modes that landed >=1 accepted seed
            var ident = IdentityOrder();
            // Safe LOWER bound on moves-to-solve: each move is a Givens in one plane (2 axes), so it can fix at
            // most 2 axes -> solving d non-fixed axes needs >= ceil(d/2) moves. (Standard QR is only an UPPER
            // bound - e.g. diag(-1,1,-1) is 1 move via plane (0,2) but standard QR at natural order counts 2 -
            // so using it here would wrongly skip valid short solves. This bound never over-skips.)
            int min = (ActiveCubie.ActiveAxisCount() + 1) / 2;
            // A no-progress draw (stuck mode or duplicate). We call the pool saturated once a run of them
            // outlasts a budget that grows with the pool - a bigger pool earns more patience. One method,
            // so the three no-progress branches below read identically instead of repeating the expression.
            bool Saturated() => ++stall > 32 + 8 * slots.Count;
            if (fast)
            {
                // FAST path: one single random descent per draw (GetSolveSeqGreedy, NO backtracking), thrown in
                // "as they come" with NO dedup. A descent that dead-ends on a narrow mode returns null and is just
                // redrawn ("draw until valid") - cheap because each attempt is O(rc), not the DFS's branching. Cap
                // the draws so a cubie whose modes all dead-end on a single descent can't spin forever; whatever we
                // gathered is topped up to target by repetition below. Duplicates are FINE here (more of the same
                // easy manoeuvre is legitimate seed material), so no seen-set, no CorrectSeq, no saturation budget.
                int maxDraws = 32 * target + 64;
                for (int draw = 0; slots.Count < target && draw < maxDraws; draw++)
                {
                    int mode = mixedModes ? TChromosome.Rnd.Next(modeSpace) : 0;
                    var order = mixedModes ? RandomOrder() : ident;
                    var seq = GetSolveSeqGreedy(order, mode);        // single descent; null on any dead end
                    if (seq == null) { draws++; stuck++; continue; }  // narrow mode dead-ended -> redraw
                    if (seq.Count == 0) break;                        // cubie already solved -> no seeds
                    slots.Add(seq.ToArray());
                    lenHist.TryGetValue(seq.Count, out int hc); lenHist[seq.Count] = hc + 1;
                    modeContrib.Add(mode);
                }
            }
            else
            while (slots.Count < target)
            {
                int mode = TChromosome.Rnd.Next(modeSpace);
                var order = mixedModes ? RandomOrder() : ident;      // random plane-reduction order (baseline: natural)
                draws++;
                if (RotCountMode(ActiveCubie.Transform.M, order, mode) < min)   // undercounting -> provably stuck, skip the DFS
                { stuck++; if (Saturated()) break; continue; }
                var seq = GetSolveSeq(order, mode);                  // greedy under (random order, random mode)
                if (seq == null) { stuck++; if (Saturated()) break; continue; }  // rc0>=min but still stuck
                if (seq.Count == 0) break;                            // cubie already solved -> no seeds
                CorrectSeq(seq);                                      // canonicalize before dedup: fold Correct-equal draws
                var moves = seq.ToArray();
                if (seen.Add(string.Join(",", moves)))
                {
                    slots.Add(moves); stall = 0;
                    lenHist.TryGetValue(moves.Length, out int hc); lenHist[moves.Length] = hc + 1;
                    modeContrib.Add(mode);
                }
                else
                {
                    dupes++;
                    if (Saturated()) break;                           // long run with no new manoeuvre: exhausted
                }
            }

            // Census + report are DIAGNOSTIC-only (Seed Pool Stats): the census alone is modeSpace (up to 4^P =
            // 4096) extra RotCountMode calls per cubie - pure waste in production. Skip unless Diagnostic is set.
            if (Diagnostic)
            {
                // mode census: rc0 (mode moves-to-solve) of THIS cubie under each mode (at natural order). Path
                // length == rc0, so this is the cubie's macro-move potential. All rc0 == minimal => degenerate
                // (shallow) cubie; a spread => deep orientation that mixed modes inflate into macro-moves.
                var rc0Hist = new SortedDictionary<int, int>();
                for (int mode = 0; mode < modeSpace; mode++)
                {
                    int r = RotCountMode(ActiveCubie.Transform.M, ident, mode);
                    rc0Hist.TryGetValue(r, out int hc); rc0Hist[r] = hc + 1;
                }

                var rep = new System.Text.StringBuilder();
                rep.AppendLine($"[BuildSeedMoves]  N={TAffine.N}  SEED_MODE={(mixedModes ? 1 : 0)}  order={(mixedModes ? "random" : "natural")}  stride={stride}  target={target}  ({seedRatioPercent}% of population)");
                rep.AppendLine($"distinct seeds = {slots.Count}     draws={draws}  stuck={stuck}  dupes={dupes}");
                if (slots.Count > 0 && slots.Count < target)
                    rep.AppendLine($"pool < target -> {target} slots filled by repeating the {slots.Count} distinct seeds");
                rep.AppendLine($"distinct modes contributing = {modeContrib.Count} / {modeSpace}");
                rep.Append("mode rc0 census (moves:modes):");
                foreach (var kv in rc0Hist) rep.Append($"  {kv.Key}:{kv.Value}");
                rep.AppendLine();
                rep.Append("accepted length histogram (moves:count):");
                foreach (var kv in lenHist) rep.Append($"  {kv.Key}:{kv.Value}");
                LastSeedReport = rep.ToString();
                System.Diagnostics.Debug.WriteLine(LastSeedReport);
            }

            // Seed the FULL target, not just as many specimens as the pool has distinct manoeuvres. A shallow
            // cubie with few decompositions no longer leaves the population under-seeded: every distinct seed is
            // placed once (slots 0..pool), then the remaining slots are topped up by sampling the pool with
            // repetition. numSeeds = target so Init pre-seeds exactly that many specimens.
            if (slots.Count == 0) { numSeeds = 0; return new int[1]; }   // nothing to seed (solved / stuck); never a zero-size SSBO
            numSeeds = (uint)target;
            var buf = new int[target * stride];
            for (int s = 0; s < target; s++)
            {
                int[] seed = s < slots.Count ? slots[s] : slots[TChromosome.Rnd.Next(slots.Count)];
                int k = 0;
                for (; k < seed.Length && k < stride; k++) buf[s * stride + k] = seed[k];
                for (; k < stride; k++) buf[s * stride + k] = -1;
            }
            return buf;
        }

    }
}
