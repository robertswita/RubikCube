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
        public static int Size = 3;
        public static float C;
        public TCubie[] Cubies;
        public TCubie ActiveCubie;
        private List<TCubie> SolvedCubies = new List<TCubie>();
        int[,] stateGrid;
        public int[,] StateGrid
        {
            get
            {
                if (stateGrid == null)
                {
                    var gridSize = Cubies.Length;
                    stateGrid = new int[gridSize, gridSize];
                    for (int pos = 0; pos < Cubies.Length; pos++)
                    {
                        var cubie = Cubies[pos];
                        stateGrid[pos, cubie.Index] = cubie.State | 1 << 31;
                    }
                }
                return stateGrid;
            }
        }

        public TRubikCube()
        {
            var size = 1;
            var scale = new TVector(TAffine.N);
            var cubieScale = new TVector(TAffine.N);
            var dimSizes = new int[TAffine.N];
            for (int dim = 0; dim < TAffine.N; dim++)
            {
                size *= Size;
                dimSizes[dim] = Size;
                scale[dim] = 1f / Size;
                cubieScale[dim] = 0.45f;
            }
            Cubies = new TCubie[size];
            TCubie.SizeMatrix = new TMatrix(size, 1);
            TCubie.SizeMatrix.DimSizes = dimSizes;
            TCubie.MaxScore = 1 << 2 * TAffine.Planes.Length;
            TCubie.Cube = CreateHyperCube();
            TMove.UpdateSizeMatrix();
            C = (Size - 1) / 2f;
            Scale(scale);
            for (int pos = 0; pos < Cubies.Length; pos++)
            {
                var cubie = new TCubie();
                cubie.Transform = TAffine.CreateScale(cubieScale);
                cubie.Index = pos;
                cubie.StartIndex = pos;
                cubie.Parent = this;
                Cubies[pos] = cubie;
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
            return selection;
        }

        public void Turn(TMove move)
        {
            //if (!TRubikGenome.FreeMoves.Contains(move.Encode()))
            //    ;
            int angle = 90 * (move.Angle + 1);
            var rotation = TAffine.CreateRotation(move.Plane, angle);
            var selection = SelectSlice(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];
                cubie.Transform = rotation * cubie.Transform;
                //Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X] = cubie;
                cubie.ValidState = false;
                cubie.Transparency = cubie.State != 0 ? 0.1f : 1;
                cubie.Parent = this;
                //var startPos = cubie.GetStartPos();
                //var idx = Size * (Size * (Size * startPos.W + startPos.Z) + startPos.Y) + startPos.X;
                //if (idx != cubie.OriginalPos)
                //    ;
            }
            stateGrid = null;
        }

        public void ReTurn(TMove move)
        {
            move.Angle = 2 - move.Angle;
            Turn(move);
            move.Angle = 2 - move.Angle;
        }

        public double Evaluate()
        {
            var score = 0d;
            double maxClusterState = (1 << 2 * TAffine.Planes.Length) * ActiveCluster.Count;
            foreach (var cubie in SolvedCubies)
                if (cubie.State != 0)
                    score += ActiveCluster.Count + 1;
                    //score += maxState + cubie.State;
                    //score += (1 + cubie.Score) * ActiveCluster.Count;
            foreach (var cubie in ActiveCluster)
                if (cubie.State != 0)
                    score += 1 + cubie.State / maxClusterState;
                    //score += maxState + cubie.State;
            return 100 * score / (ActiveCluster.Count + 1);
        }

        public List<int> GetFreeMoves()
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
                                move.Slice = (int)pos[coord];
                            else
                                move.Slice = Size - 1 - (int)pos[coord];
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
                    for (int state = 0; state < 1 << 2 * TAffine.Planes.Length; state++)
                    {
                        var cubie = ActiveCubie.Copy();
                        for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                            cubie.Rotate(i, 90 * (state >> 2 * i & 3));
                        var clusterCubie = Cubies[cubie.Index];
                        if (!activeCluster.Contains(clusterCubie))
                            activeCluster.Add(clusterCubie);
                    }
                }
                return activeCluster;
            }
        }

        public void NextCluster()
        {
            if (activeCluster != null)
                SolvedCubies.AddRange(ActiveCluster);
            var minDist = float.MaxValue;
            ActiveCubie = null;
            foreach (var cubie in Cubies)
            {
                if (cubie.State == 0) continue;
                var dist = 0f;
                for (int dim = 0; dim < TAffine.N; dim++)
                    dist += Math.Abs(cubie.Transform.Origin[dim]);
                if (dist < minDist)
                {
                    minDist = dist;
                    ActiveCubie = cubie;
                }
            }
            activeCluster = null;
            if (ActiveCubie == null)
                SolvedCubies.Clear();
        }

    }
}
