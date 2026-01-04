using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using TGL;

namespace RubikCube
{
    public class TRubikCube : TObject3D
    {
        public static int N = 2;  // Changed to 2 for 2x2x2x2 hypercube
        public static double C;
        public TCubie[,,,] Cubies = new TCubie[N, N, N, N];  // 4D array
        public List<TMove> Moves = new List<TMove>();
        public List<TCubie> ActCluster = new List<TCubie>();
        public TCubie ActCubie;
        int[,] _StateGrid;
        public int[,] StateGrid
        {
            get
            {
                if (_StateGrid == null)
                {
                    int totalCubies = N * N * N * N;
                    _StateGrid = new int[totalCubies, totalCubies];
                    for (int w = 0; w < N; w++)
                        for (int x = 0; x < N; x++)
                            for (int y = 0; y < N; y++)
                                for (int z = 0; z < N; z++)
                                {
                                    var cubie = Cubies[w, z, y, x];
                                    var transform = (double[])cubie.Transform.Clone();
                                    var alpha = cubie.State & 3;
                                    var beta = (cubie.State >> 2) & 3;
                                    var gamma = (cubie.State >> 4) & 3;
                                    cubie.RotateZ(-90 * gamma);
                                    cubie.RotateY(-90 * beta);
                                    cubie.RotateX(-90 * alpha);
                                    var i = x * N * N * N + y * N * N + z * N + w;
                                    var idx = cubie.X * N * N * N + cubie.Y * N * N + cubie.Z * N + cubie.W;
                                    cubie.Transform = transform;
                                    _StateGrid[i, idx] = cubie.State + (1 << 6);
                                }
                }
                return _StateGrid;
            }
        }

        public TRubikCube()
        {
            C = (N - 1) / 2.0;
            Scale(1.0 / N, 1.0 / N, 1.0 / N);
            var cubieScale = 0.9 / 2;
            for (int w = 0; w < N; w++)
                for (int z = 0; z < N; z++)
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
                        {
                            var cubie = new TCubie();
                            cubie.Scale(cubieScale, cubieScale, cubieScale);
                            cubie.Translate(x - C, y - C, z - C);
                            cubie.WCoord = w - C;  // Set 4D W coordinate
                            InitializeCubieFaceColors(cubie, x, y, z, w);  // Set colors based on 4D position
                            cubie.Parent = this;
                            Cubies[w, z, y, x] = cubie;
                        }
            //ActCubie = Cubies[(int)C, (int)C, (int)C, (int)C];
        }

        /// <summary>
        /// Initialize face colors for a cubie based on its position in the 4D hypercube
        /// </summary>
        private void InitializeCubieFaceColors(TCubie cubie, int x, int y, int z, int w)
        {
            // Face order in FaceColors: -X, +X, -Y, +Y, -Z, +Z
            // HyperFaceColors index: 0=X-, 1=X+, 2=Y-, 3=Y+, 4=Z-, 5=Z+, 6=W-, 7=W+

            // X faces
            if (x == 0)
                cubie.FaceColors[0] = 0;  // -X face gets X=0 color (Red)
            if (x == N - 1)
                cubie.FaceColors[1] = 1;  // +X face gets X=N-1 color (Orange)

            // Y faces
            if (y == 0)
                cubie.FaceColors[2] = 2;  // -Y face gets Y=0 color (White)
            if (y == N - 1)
                cubie.FaceColors[3] = 3;  // +Y face gets Y=N-1 color (Yellow)

            // Z faces
            if (z == 0)
                cubie.FaceColors[4] = 4;  // -Z face gets Z=0 color (Blue)
            if (z == N - 1)
                cubie.FaceColors[5] = 5;  // +Z face gets Z=N-1 color (Green)

            // Note: W dimension colors will be shown when viewing different slices
            // Interior cubies get -1 (no color) on faces not on the hypercube boundary
        }

        public TRubikCube(TRubikCube src)
        {
            for (int w = 0; w < N; w++)
                for (int z = 0; z < N; z++)
                    for (int y = 0; y < N; y++)
                        for (int x = 0; x < N; x++)
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
            // Redirect to 4D version for all moves
            // This ensures backward compatibility while supporting 4D
            return SelectSlice4D(move);
        }

        // 4D slice selection
        public List<TCubie> SelectSlice4D(TMove move)
        {
            var selection = new List<TCubie>();

            // Determine which axes are involved in the rotation plane
            // Planes: 0=XY, 1=XZ, 2=XW, 3=YZ, 4=YW, 5=ZW
            int[] planeAxes = GetPlaneAxes(move.Plane);
            int axis1 = planeAxes[0];  // First rotating axis
            int axis2 = planeAxes[1];  // Second rotating axis

            // Determine which of the two remaining axes is fixed
            int[] remainingAxes = GetRemainingAxes(move.Plane);
            int fixedAxisIdx = remainingAxes[move.FixedAxis];

            // Select all hypercubies in the slice
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    for (int k = 0; k < N; k++)
                    {
                        var v = new int[4];
                        v[axis1] = i;
                        v[axis2] = j;
                        v[fixedAxisIdx] = move.Slice;
                        v[remainingAxes[1 - move.FixedAxis]] = k;
                        selection.Add(Cubies[v[3], v[2], v[1], v[0]]);
                    }
            return selection;
        }

        // Get the two axes that define a rotation plane
        private int[] GetPlaneAxes(int plane)
        {
            // Planes: 0=XY, 1=XZ, 2=XW, 3=YZ, 4=YW, 5=ZW
            // Axes: 0=X, 1=Y, 2=Z, 3=W
            switch (plane)
            {
                case 0: return new int[] { 0, 1 }; // XY
                case 1: return new int[] { 0, 2 }; // XZ
                case 2: return new int[] { 0, 3 }; // XW
                case 3: return new int[] { 1, 2 }; // YZ
                case 4: return new int[] { 1, 3 }; // YW
                case 5: return new int[] { 2, 3 }; // ZW
                default: return new int[] { 0, 1 };
            }
        }

        // Get the two axes NOT involved in the rotation plane
        public int[] GetRemainingAxes(int plane)
        {
            switch (plane)
            {
                case 0: return new int[] { 2, 3 }; // XY -> remaining: Z, W
                case 1: return new int[] { 1, 3 }; // XZ -> remaining: Y, W
                case 2: return new int[] { 1, 2 }; // XW -> remaining: Y, Z
                case 3: return new int[] { 0, 3 }; // YZ -> remaining: X, W
                case 4: return new int[] { 0, 2 }; // YW -> remaining: X, Z
                case 5: return new int[] { 0, 1 }; // ZW -> remaining: X, Y
                default: return new int[] { 2, 3 };
            }
        }

        public void Turn(TMove move)
        {
            // For 4D: Use 3D rotations on the 3D projection
            // The W coordinate is updated separately based on the plane rotation
            var slice = new TObject3D();
            int angle = 90 * (move.Angle + 1);

            // Map plane rotations to 3D rotations
            // Planes: 0=XY, 1=XZ, 2=XW, 3=YZ, 4=YW, 5=ZW
            switch (move.Plane)
            {
                case 0: // XY plane - rotate around Z axis
                    slice.RotateZ(angle);
                    break;
                case 1: // XZ plane - rotate around Y axis
                    slice.RotateY(angle);
                    break;
                case 2: // XW plane - rotate X and W
                    slice.RotateX(angle); // Placeholder - needs 4D handling
                    break;
                case 3: // YZ plane - rotate around X axis
                    slice.RotateX(angle);
                    break;
                case 4: // YW plane - rotate Y and W
                    slice.RotateY(angle); // Placeholder - needs 4D handling
                    break;
                case 5: // ZW plane - rotate Z and W
                    slice.RotateZ(angle); // Placeholder - needs 4D handling
                    break;
            }

            var selection = SelectSlice4D(move);
            for (int i = 0; i < selection.Count; i++)
            {
                var cubie = selection[i];

                // Apply 4D rotation to W coordinate for planes involving W
                if (move.Plane >= 2) // XW, YW, ZW involve W dimension
                {
                    Apply4DRotation(cubie, move.Plane, angle);
                }

                cubie.MultMatrix(slice.Transform);
                Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X] = cubie;
                cubie.ValidState = false;
                cubie.Transparent = false;
                if (cubie.State != 0)
                    cubie.Transparent = true;
                cubie.Parent = this;
            }
            _StateGrid = null;
        }

        // Helper method to apply 4D rotation to W coordinate
        private void Apply4DRotation(TCubie cubie, int plane, double angleDeg)
        {
            double angleRad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            double x = cubie.Origin.X;
            double y = cubie.Origin.Y;
            double z = cubie.Origin.Z;
            double w = cubie.WCoord;

            switch (plane)
            {
                case 2: // XW - rotate X and W
                    cubie.WCoord = -sin * x + cos * w;
                    break;
                case 4: // YW - rotate Y and W
                    cubie.WCoord = -sin * y + cos * w;
                    break;
                case 5: // ZW - rotate Z and W
                    cubie.WCoord = -sin * z + cos * w;
                    break;
            }
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
                    for (int i = restrict; i < N - restrict; i++)
                        for (int j = restrict; j < N - restrict; j++)
                            for (int k = restrict; k < N - restrict; k++)
                                for (int axis = 0; axis < 4; axis++)  // Now 4 axes for 4D
                                {
                                    var v = new int[4];
                                    v[axis] = side == 0 ? restrict : N - 1 - restrict;
                                    v[(axis + 1) % 4] = i;
                                    v[(axis + 2) % 4] = j;
                                    v[(axis + 3) % 4] = k;
                                    var cubie = Cubies[v[3], v[2], v[1], v[0]];
                                    if (cubie.State != 0)
                                    {
                                        // 4D Manhattan distance from center
                                        var dist = Math.Abs(C - v[0]) + Math.Abs(C - v[1]) + Math.Abs(C - v[2]) + Math.Abs(C - v[3]);
                                        if (dist < minDist)
                                        {
                                            minDist = dist;
                                            ActCubie = cubie;
                                            result = restrict;
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
            var idx = new int[] { ActCubie.X, ActCubie.Y, ActCubie.Z, ActCubie.W };

            // Iterate over all 6 rotation planes
            for (int plane = 0; plane < 6; plane++)
                for (int fixedAxis = 0; fixedAxis < 2; fixedAxis++)
                    for (int i = 0; i < 4; i++)
                        for (int side = 0; side < 2; side++)
                        {
                            var move = new TMove();
                            move.Plane = plane;
                            move.FixedAxis = fixedAxis;
                            if (side == 0)
                                move.Slice = idx[i];
                            else
                                move.Slice = N - 1 - idx[i];
                            var gene = move.Encode();
                            if (freeGenes.IndexOf(gene) < 0)
                            {
                                freeGenes.Add(gene + 0);  // 90°
                                freeGenes.Add(gene + 1);  // 180°
                                freeGenes.Add(gene + 2);  // 270°
                            }
                        }
            return freeGenes;
        }

        public List<TCubie> GetCluster(TCubie cubie)
        {
            var cluster = new List<TCubie>();
            var cube = new TRubikCube();
            var testCubie = cube.Cubies[cubie.W, cubie.Z, cubie.Y, cubie.X];

            // Explore all 6 rotation planes to find cluster
            for (int plane = 0; plane < 6; plane++)
            {
                for (int angle = 0; angle < 4; angle++)  // 4 rotations: 0°, 90°, 180°, 270°
                {
                    var move = new TMove();
                    move.Plane = plane;
                    move.FixedAxis = 0;  // Try first fixed axis option
                    move.Slice = GetSliceForCubie(cubie, plane, 0);
                    move.Angle = 0;  // 90° rotation
                    cube.Turn(move);

                    var neigh = Cubies[testCubie.W, testCubie.Z, testCubie.Y, testCubie.X];
                    if (!cluster.Contains(neigh))
                        cluster.Add(neigh);
                }
            }
            return cluster;
        }

        // Helper to get appropriate slice for a cubie based on plane
        private int GetSliceForCubie(TCubie cubie, int plane, int fixedAxis)
        {
            int[] remainingAxes = GetRemainingAxes(plane);
            int[] coords = new int[] { cubie.X, cubie.Y, cubie.Z, cubie.W };
            return coords[remainingAxes[fixedAxis]];
        }

        // Slice extraction methods for 4D visualization
        public TCubie[,,] GetSliceXYZ(int wSlice)
        {
            var slice = new TCubie[N, N, N];
            for (int z = 0; z < N; z++)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        slice[z, y, x] = Cubies[wSlice, z, y, x];
            return slice;
        }

        public TCubie[,,] GetSliceXYW(int zSlice)
        {
            var slice = new TCubie[N, N, N];
            for (int w = 0; w < N; w++)
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                        slice[w, y, x] = Cubies[w, zSlice, y, x];
            return slice;
        }

        public TCubie[,,] GetSliceXZW(int ySlice)
        {
            var slice = new TCubie[N, N, N];
            for (int w = 0; w < N; w++)
                for (int z = 0; z < N; z++)
                    for (int x = 0; x < N; x++)
                        slice[w, z, x] = Cubies[w, z, ySlice, x];
            return slice;
        }

        public TCubie[,,] GetSliceYZW(int xSlice)
        {
            var slice = new TCubie[N, N, N];
            for (int w = 0; w < N; w++)
                for (int z = 0; z < N; z++)
                    for (int y = 0; y < N; y++)
                        slice[w, z, y] = Cubies[w, z, y, xSlice];
            return slice;
        }
    }
}
