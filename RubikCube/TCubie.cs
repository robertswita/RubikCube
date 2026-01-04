using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TCubie : TObject4D
    {
        public int X { get { return (int)Math.Round(Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Origin.Z + TRubikCube.C); } }

        // 4D support: W coordinate from 4D transformation matrix
        public int W { get { return (int)Math.Round(Origin4D.W + TRubikCube.C); } }
        public double WCoord { get { return Origin4D.W; } set { Translate4D(0, 0, 0, value - Origin4D.W); } }

        public int ClusterCount;

        // 8 colors for 4D hypercube hyperfaces
        // Index: 0=X-, 1=X+, 2=Y-, 3=Y+, 4=Z-, 5=Z+, 6=W-, 7=W+
        public static readonly double[][] HyperFaceColors = new double[][]
        {
            new double[] { 1.0, 0.0, 0.0 },   // 0: X=0 (left) - Red
            new double[] { 1.0, 0.5, 0.0 },   // 1: X=N-1 (right) - Orange
            new double[] { 1.0, 1.0, 1.0 },   // 2: Y=0 (bottom) - White
            new double[] { 1.0, 1.0, 0.0 },   // 3: Y=N-1 (top) - Yellow
            new double[] { 0.0, 0.0, 1.0 },   // 4: Z=0 (back) - Blue
            new double[] { 0.0, 1.0, 0.0 },   // 5: Z=N-1 (front) - Green
            new double[] { 0.6, 0.0, 0.8 },   // 6: W=0 (ana) - Purple
            new double[] { 1.0, 0.0, 1.0 }    // 7: W=N-1 (kata) - Magenta
        };

        // Store color index for each face of the cube
        // ACTUAL face order from geometry: -Z, -X, -Y, +Z, +X, +Y (6 faces)
        public int[] FaceColors = new int[6] { -1, -1, -1, -1, -1, -1 };

        public TCubie()
        {
            var lbn = new TPoint3D(-1, -1, -1);
            var rtf = new TPoint3D(+1, +1, +1);
            for (int i = 0; i < 8; i++)
            {
                var p = lbn;
                if ((i & 1) == 1) p.X = rtf.X;
                if ((i & 2) != 0) p.Y = rtf.Y;
                if ((i & 4) != 0) p.Z = rtf.Z;
                Vertices.Add(p);
            }
            for (int dim = 0; dim < 3; dim++)
            {
                var axis1 = 1 << dim;
                var axis2 = 1 << (dim + 1) % 3;
                Faces.Add(axis1);
                Faces.Add(axis2);
                Faces.Add(0);
                Faces.Add(axis1 + axis2);
                Faces.Add(axis2);
                Faces.Add(axis1);
            }
            for (int i = Faces.Count - 1; i >= 0; i--)
                Faces.Add(7 - Faces[i]);
        }

        int GetAngle(double sinA, double cosA)
        {
            if (cosA > 0.1) return 0;
            if (sinA > 0.1) return 1;
            if (cosA < -0.1) return 2;
            if (sinA < -0.1) return 3;
            return 0;
        }

        public bool ValidState;
        int _State;
        public int State
        {
            get
            {
                if (!ValidState)
                {
                    var gamma = GetAngle(Transform[1], Transform[0]);
                    var beta = 0;
                    var alpha = 0;
                    if (gamma != 0)
                        alpha = GetAngle(Transform[6], Transform[10]);
                    else
                    {
                        beta = GetAngle(-Transform[2], Transform[0]);
                        alpha = GetAngle(-Transform[9], Transform[5]);
                    }
                    var moveCount = Math.Sign(alpha) + Math.Sign(beta) + Math.Sign(gamma);
                    _State = moveCount << 6 | gamma << 4 | beta << 2 | alpha;
                    if (alpha == 2 && gamma == 2) _State = 0x48;
                    ValidState = true;
                }
                return _State;
            }
            set
            {
                var alpha = value & 3;
                var beta = (value >> 2) & 3;
                var gamma = (value >> 4) & 3;
                var org = Origin;
                LoadIdentity();
                Scale(0.45, 0.45, 0.45);
                RotateX(90 * alpha);
                RotateY(90 * beta);
                RotateZ(90 * gamma);
                Translate(org.X, org.Y, org.Z);
                _State = value;
                ValidState = true;
                if (_State != 0)
                    Transparent = true;
            }
        }

        public int Orbit
        {
            get
            {
                // 4D Manhattan distance from center
                var dist = Math.Abs(Origin.X) + Math.Abs(Origin.Y) + Math.Abs(Origin.Z) + Math.Abs(Origin4D.W);
                return (int)Math.Round(2 * dist);
            }
        }

        public TCubie Copy()
        {
            var dest = new TCubie();
            Array.Copy(Transform, dest.Transform, Transform.Length);  // Copy 3D transform (for rendering)
            Array.Copy(Transform4D, dest.Transform4D, Transform4D.Length);  // Copy 4D transform
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest._State = _State;
            dest.ValidState = ValidState;
            dest.ClusterCount = ClusterCount;
            Array.Copy(FaceColors, dest.FaceColors, FaceColors.Length);  // Copy face colors
            return dest;
        }

        /// <summary>
        /// Full 4D orientation state tracking
        /// Since 4D rotations need 6 parameters (for 6 planes), we need more than 3 angles
        /// </summary>
        public struct State4D
        {
            public int PlaneXY;  // Rotation count in XY plane (0-3)
            public int PlaneXZ;  // Rotation count in XZ plane (0-3)
            public int PlaneXW;  // Rotation count in XW plane (0-3)
            public int PlaneYZ;  // Rotation count in YZ plane (0-3)
            public int PlaneYW;  // Rotation count in YW plane (0-3)
            public int PlaneZW;  // Rotation count in ZW plane (0-3)

            public bool IsSolved => PlaneXY == 0 && PlaneXZ == 0 && PlaneXW == 0 &&
                                   PlaneYZ == 0 && PlaneYW == 0 && PlaneZW == 0;
        }

        private State4D _state4D;
        public State4D State4DOrientation
        {
            get => _state4D;
            set
            {
                _state4D = value;
                ValidState4D = true;
            }
        }

        public bool ValidState4D = false;

        /// <summary>
        /// Apply 4D rotation and track state change
        /// </summary>
        public void ApplyPlaneRotation(int plane, int rotationCount)
        {
            ValidState4D = false; // Force recalculation

            switch (plane)
            {
                case 0: _state4D.PlaneXY = (_state4D.PlaneXY + rotationCount) % 4; break;
                case 1: _state4D.PlaneXZ = (_state4D.PlaneXZ + rotationCount) % 4; break;
                case 2: _state4D.PlaneXW = (_state4D.PlaneXW + rotationCount) % 4; break;
                case 3: _state4D.PlaneYZ = (_state4D.PlaneYZ + rotationCount) % 4; break;
                case 4: _state4D.PlaneYW = (_state4D.PlaneYW + rotationCount) % 4; break;
                case 5: _state4D.PlaneZW = (_state4D.PlaneZW + rotationCount) % 4; break;
            }

            ValidState4D = true;
        }

        /// <summary>
        /// Get 4D distance from solved state (better fitness function)
        /// </summary>
        public int Get4DStateDistance()
        {
            if (!ValidState4D) return int.MaxValue;

            return Math.Min(_state4D.PlaneXY, 4 - _state4D.PlaneXY) +
                   Math.Min(_state4D.PlaneXZ, 4 - _state4D.PlaneXZ) +
                   Math.Min(_state4D.PlaneXW, 4 - _state4D.PlaneXW) +
                   Math.Min(_state4D.PlaneYZ, 4 - _state4D.PlaneYZ) +
                   Math.Min(_state4D.PlaneYW, 4 - _state4D.PlaneYW) +
                   Math.Min(_state4D.PlaneZW, 4 - _state4D.PlaneZW);
        }
    }
}
