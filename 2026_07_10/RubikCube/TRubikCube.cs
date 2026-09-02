using GA;
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
        public static List<int>[,] SliceCubiesIndices;
        public float Score;

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
            TCubie.Cube = CreateHyperCube();
            //TMove.UpdateSizeMatrix();
            C = (Size - 1) / 2f;
            Scale(scale);
            for (int i = 0; i < Cubies.Length; i++)
            {
                var cubie = new TCubie();
                cubie.Transform = TAffine.CreateScale(TCubie.Scaling);
                cubie.GivensOrder = TVector.Uniform(TAffine.N);
                cubie.Index = i;
                cubie.StartIndex = i;
                cubie.Parent = this;
                Cubies[i] = cubie;
            }


            //SliceCubiesIndices = new List<int>[TAffine.N, Size];
            //for (int axis = 0; axis < TAffine.N; axis++)
            //    for (int slice = 0; slice < Size; slice++)
            //    {
            //        var indices = new List<int>();
            //        foreach (var cubie in Cubies)
            //        {
            //            var v = cubie.GetPos(axis);
            //            if (v == slice) indices.Add(cubie.Index);
            //        }
            //        SliceCubiesIndices[axis, slice] = indices;
            //    }
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
            //if (selection.Count != Cubies.Length / Size)
            //    ;
            return selection;
        }

        // Applies a move by rotating the Transform of every cubie in the slice. Transform is the
        // single source of truth for the cube state; State/Position (derived from it) stay current.
        public void Turn(TMove move)
        {
            var plane = TAffine.Planes[move.Plane];
            var rot = TCubie.SetAngle(move.Angle + 1);
            foreach (var cubie in Cubies)
                if (cubie.GetPos(move.Axis) == move.Slice)
                {
                    cubie.Transform.Rotate(plane[0], plane[1], rot.X, rot.Y);
                    cubie.ValidState = false;
                }
        }

        // Packs every cubie's orientation (from its Transform) into the shader's uint format: per row
        // r the field is (sign << BITS_FOR_COL) | col, where col is the single non-zero entry of that
        // row and sign is its sign. Identity row r -> field r, so a solved cubie packs to the identity
        // value and cubieL1 == 0. Axis numbering matches the CPU (row/col = CPU axes), consistent with
        // getStartCoordinate after the axis-order fix.
        public uint[] PackCubies()
        {
            int bitsForCol = TAffine.N <= 4 ? 2 : 3;
            int bitsPerRow = bitsForCol + 1;
            var packed = new uint[Cubies.Length];
            for (int id = 0; id < Cubies.Length; id++)
            {
                var t = Cubies[id].Transform.M;
                uint m = 0;
                for (int row = 0; row < TAffine.N; row++)
                {
                    int bestCol = 0;
                    float best = 0;
                    for (int col = 0; col < TAffine.N; col++)
                        if (Math.Abs(t[row, col]) > Math.Abs(best)) { best = t[row, col]; bestCol = col; }
                    uint sign = best < 0 ? 1u : 0u;
                    m |= ((sign << bitsForCol) | (uint)bestCol) << (row * bitsPerRow);
                }
                packed[id] = m;
            }
            return packed;
        }

        static int CubieL1(uint m, int bitsPerRow)
        {
            int dist = 0;
            for (int r = 0; r < TAffine.N; r++)
            {
                uint field = (m >> (r * bitsPerRow)) & ((1u << bitsPerRow) - 1u);
                dist += (int)(field > (uint)r ? field - (uint)r : (uint)r - field);
            }
            return dist;
        }

        static int ActiveAxes(uint m, int bitsPerRow)
        {
            int count = 0;
            for (int r = 0; r < TAffine.N; r++)
            {
                uint field = (m >> (r * bitsPerRow)) & ((1u << bitsPerRow) - 1u);
                if (field != (uint)r) count++;
            }
            return count;
        }

        // Mirrors Setup.glsl.c cubieState: moves-to-solve (active axes) dominant, L1 orientation tiebreak.
        static int CubieState(uint m, int bitsPerRow, int maxCubieL1)
        {
            return ActiveAxes(m, bitsPerRow) * (maxCubieL1 + 1) + CubieL1(m, bitsPerRow);
        }

        // Fitness of the current cube in the exact GPU metric (a specimen doing zero moves): the
        // normalized active-cluster error (moves-to-solve + L1, see cubieState) plus the count of
        // already-solved cubies that are no longer solved. Use this - not Evaluate() - as the
        // baseline/Score when driving the GPU GA, so it is comparable with best.Fitness. Mirrors Setup.glsl.c.
        public float EvaluateGpu()
        {
            int bitsForCol = TAffine.N <= 4 ? 2 : 3;
            int bitsPerRow = bitsForCol + 1;
            int maxCubieL1 = TAffine.N * ((1 << bitsForCol) + TAffine.N - 1);
            int maxCubieState = TAffine.N * (maxCubieL1 + 1) + maxCubieL1;
            var packed = PackCubies();
            int countActive = ActiveCluster.Count;
            float maxClusterState = (float)maxCubieState * countActive;
            float maxFA = maxClusterState * (countActive + 1);
            float fA = 0;
            int scrambled = 0;
            foreach (var cubie in ActiveCluster)
            {
                int d = CubieState(packed[cubie.StartIndex], bitsPerRow, maxCubieL1);
                if (d != 0) { fA += (maxClusterState + d) / maxFA; scrambled++; }
            }
            // Endgame: <= N unsolved active cubies -> drop the count reward, amplify magnitude (mirrors shader).
            if (scrambled > 0 && scrambled <= TAffine.N)
                fA *= (float)TAffine.N / scrambled;
            int solvedErrors = 0;
            foreach (var cubie in SolvedCubies)
                if (CubieL1(packed[cubie.StartIndex], bitsPerRow) != 0)
                    solvedErrors++;
            return solvedErrors + fA;
        }

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

        // Random active cubie + random axis-permutation, decomposed (EulerOrder / route) into the
        // (plane, moveAngle) solve steps, recording the cubie's coord vector before each step. The
        // axis is deferred - the cubie trajectory is axis-independent - so the caller realizes the
        // moves with any axis: random (GetReversedSeq) or enumerated (BuildSeedMoves). m = active axes.
        List<int[]> GetSolveSteps(out int[][] coordAtStep, out int m)
        {
            var nums = new int[TAffine.N];
            for (int i = 0; i < nums.Length; i++)
                nums[i] = i;
            var permuts = DoPermute(nums, 0, nums.Length - 1);
            var perm = permuts[TChromosome.Rnd.Next(permuts.Count)];
            var cube = new TRubikCube(this);

            var freeCubies = new List<TCubie>();
            foreach (var cubie in cube.ActiveCluster)
                if (cubie.State != 0)
                    freeCubies.Add(cubie);
            if (freeCubies.Count > 0)
                cube.ActiveCubie = freeCubies[TChromosome.Rnd.Next(freeCubies.Count)];

            m = cube.ActiveCubie.ActiveAxisCount();

            var M = cube.ActiveCubie.Transform.M;
            var A = (TMatrix)M.Clone();
            for (int y = 0; y < M.RowsCount; y++)
                for (int x = 0; x < M.ColsCount; x++)
                    A[y, x] = M[perm[y], perm[x]];
            cube.ActiveCubie.Transform.M = A;
            var eulerAngles = cube.ActiveCubie.Transform.GetEulerAngles(EulerOrder, IsEulerOrderReversed);
            cube.ActiveCubie.Transform.M = M;

            var steps = new List<int[]>();          // each: { plane, moveAngle }
            var coords = new List<int[]>();
            for (int idx = 0; idx < eulerAngles.Count; idx++)
            {
                var rot = eulerAngles[idx];
                var angle = TCubie.GetAngle(rot[0], rot[1]);
                if (angle == 0) continue;
                int axis1 = perm[(int)rot[2]], axis2 = perm[(int)rot[3]];
                if (axis1 > axis2) { (axis1, axis2) = (axis2, axis1); angle = 4 - angle; }
                int plane = axis2 * (axis2 - 1) / 2 + axis1;
                int moveAngle = 3 - angle;

                var coord = new int[TAffine.N];
                for (int a = 0; a < TAffine.N; a++)
                    coord[a] = cube.ActiveCubie.GetPos(a);
                coords.Add(coord);
                steps.Add(new[] { plane, moveAngle });

                int canon = 0;                       // any axis not in the plane, only to advance the sim
                while (canon == axis1 || canon == axis2) canon++;
                cube.Turn(new TMove { Plane = plane, Angle = moveAngle, Axis = canon, Slice = coord[canon] });
            }
            coordAtStep = coords.ToArray();
            return steps;
        }

        public List<int> ActSeq;
        public List<int> GetReversedSeq()
        {
            var steps = GetSolveSteps(out var coord, out _);
            ActSeq = new List<int>(steps.Count);
            for (int k = 0; k < steps.Count; k++)
            {
                int plane = steps[k][0], angle = steps[k][1];
                var pa = TAffine.Planes[plane];
                int axis = TChromosome.Rnd.Next(TAffine.N);
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                ActSeq.Add(new TMove { Plane = plane, Angle = angle, Axis = axis, Slice = coord[k][axis] }.Encode());
            }
            return ActSeq;
        }

        // Every axis realization (Axis not in plane, per step) of a step list. Full Cartesian product
        // when it stays under CAP, otherwise CAP random samples (the caller dedups). The axis choice
        // only changes which layer / collateral cubies move, not the target cubie's solve.
        static IEnumerable<int[]> AxisCombos(List<int[]> steps)
        {
            const int CAP = 64;
            var choices = new List<int>[steps.Count];
            long total = 1;
            for (int k = 0; k < steps.Count; k++)
            {
                var pa = TAffine.Planes[steps[k][0]];
                var list = new List<int>();
                for (int a = 0; a < TAffine.N; a++)
                    if (a != pa[0] && a != pa[1]) list.Add(a);
                choices[k] = list;
                total *= list.Count;
            }
            if (total <= CAP)
            {
                var idx = new int[steps.Count];
                while (true)
                {
                    var axis = new int[steps.Count];
                    for (int k = 0; k < steps.Count; k++) axis[k] = choices[k][idx[k]];
                    yield return axis;
                    int i = 0;
                    for (; i < idx.Length; i++) { if (++idx[i] < choices[i].Count) break; idx[i] = 0; }
                    if (i == idx.Length) yield break;
                }
            }
            for (int n = 0; n < CAP; n++)
            {
                var axis = new int[steps.Count];
                for (int k = 0; k < steps.Count; k++) axis[k] = choices[k][TChromosome.Rnd.Next(choices[k].Count)];
                yield return axis;
            }
        }

        // Packs up to numSeeds per-specimen seed sequences into a flat buffer (stride genes each, unused
        // slots = -1 sentinel) for the GPU Init shader. Reuses the CPU generator (GetSolveSteps) but
        // enumerates all axis realizations per draw. Targets populationCount * seedRatioPercent / 100
        // slots; numSeeds is however many distinct sequences it actually finds.
        public int[] BuildSeedMoves(int populationCount, int stride, int seedRatioPercent, out uint numSeeds)
        {
            int target = populationCount * seedRatioPercent / 100;
            var savedOrder = EulerOrder;
            var savedReversed = IsEulerOrderReversed;
            var slots = new List<int[]>();
            var seen = new HashSet<string>();
            int guard = 0, GUARD = 8 * target + 64;
            while (slots.Count < target && guard++ < GUARD)
            {
                if (!IsEulerOrderReversed) EulerOrder = GetOrder();   // both routes per drawn order
                IsEulerOrderReversed = !IsEulerOrderReversed;
                var steps = GetSolveSteps(out var coord, out _);
                if (steps.Count == 0) continue;                       // cubie already solved
                foreach (var combo in AxisCombos(steps))
                {
                    var moves = new int[steps.Count];
                    for (int k = 0; k < steps.Count; k++)
                        moves[k] = new TMove { Plane = steps[k][0], Angle = steps[k][1], Axis = combo[k], Slice = coord[k][combo[k]] }.Encode();
                    if (seen.Add(string.Join(",", moves)))
                    {
                        slots.Add(moves);
                        if (slots.Count >= target) break;
                    }
                }
            }
            EulerOrder = savedOrder;
            IsEulerOrderReversed = savedReversed;

            numSeeds = (uint)slots.Count;
            if (slots.Count == 0) return new int[1];                  // never a zero-size SSBO
            var buf = new int[slots.Count * stride];
            for (int s = 0; s < slots.Count; s++)
            {
                int k = 0;
                for (; k < slots[s].Length && k < stride; k++) buf[s * stride + k] = slots[s][k];
                for (; k < stride; k++) buf[s * stride + k] = -1;
            }
            return buf;
        }

    }
}
