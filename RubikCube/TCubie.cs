using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;
using static System.Windows.Forms.AxHost;

namespace RubikCube
{
    public class TCubie : TShape
    {
        public static TShape Cube;
        public static TMatrix SizeMatrix;
        public static int MaxScore;
        //public double Error;
        public int StartIndex;
        int rotationCount;
        public int RotationCount
        {
            get
            {
                _ = State;
                return rotationCount;
            }
            set => rotationCount = value;
        }
        public static TVector Scaling;
        public bool IsReversedSeq;
        public int ClusterIndex;
        public TVector GivensOrder;

        public TCubie()
        {
            Vertices = Cube.Vertices;
            Faces = Cube.Faces;
            Materials = Cube.Materials;
        }

        public static int GetAngle(double cosA, double sinA)
        {
            if (cosA > 0.5) return 0;
            if (sinA > 0.5) return 1;
            if (cosA < -0.5) return 2;
            if (sinA < -0.5) return 3;
            return 0;
        }
        public static TVector SetAngle(int angle)
        {
            float cosA = 0;
            float sinA = 0;
            switch (angle)
            {
                case 0: cosA = 1; break;
                case 1: sinA = 1; break;
                case 2: cosA = -1; break;
                case 3: sinA = -1; break;
            }
            return new TVector(cosA, sinA);
        }

        // Per-face alpha for an unsolved cubie that read well at N=3 (faceCount 6). The shader gets the
        // final alpha per instance and stays oblivious to the geometry - the layer compensation lives here.
        public static float BaseUnsolvedAlpha = 0.1f;


        // The hypercube mesh has Cube.Materials.Count faces (= P * 2^(N-2): 6 at N=3, 24 at N=4),
        // all stacking in the WBOIT revealage (product of 1-alpha over the layers). A fixed
        // per-face alpha makes high-N cubies pile up to near-opaque. Scale alpha so the NET
        // revealage stays what N=3 had: revealage (1-a)^faceCount constant => a = 1 -
        // (1-BaseUnsolvedAlpha)^(6 / faceCount). Any true layer count L proportional to faceCount
        // cancels, so this holds regardless of L.
        //base.Transparency = 1 - (float)Math.Pow(1 - BaseUnsolvedAlpha, 6.0 / Materials.Count);
        public override float Transparency =>
            State == 0 ? 1f : 1f - (float)Math.Pow(1 - BaseUnsolvedAlpha, 6.0 / Cube.Materials.Count);

        private bool ValidState;
        int state;
        public int State
        {
            get
            {
                if (!ValidState)
                {
                    EulerAngles = Transform.GetEulerAngles(null);
                    state = 0;
                    rotationCount = 0;
                    var shift = (EulerAngles.Count - 1) << 1;
                    for (int i = 0; i < EulerAngles.Count; i++)
                    {
                        var angle = GetAngle(EulerAngles[i].X, EulerAngles[i].Y);
                        state |= angle << shift;
                        shift -= 2;
                        if (angle > 0)
                            rotationCount++;
                    }
                    //var RotationCount2 = TAffine.N;
                    //for (int i = 0; i < TAffine.N; i++)
                    //    if (Math.Abs(Transform.M[i, i]) > 0.5)
                    //        RotationCount2--;
                    //RotationCount = RotationCount * TAffine.N + RotationCount2;

                    //state |= RotationCount << 2 * EulerAngles.Count;

                    //var xform = TAffine.CreateScale(new TVector(0.45f, 0.45f, 0.45f, 0.45f));
                    //for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                    //    xform = TAffine.CreateRotation(i, 90 * (state >> 2 * i & 3)) * xform;
                    //var Error = (xform.M - Transform.M).Norm;
                    //if (Error > 0.01)
                    //    ;                    

                    ValidState = true;
                }
                return state;
            }
            set
            {
                Transform = TAffine.CreateScale(Scaling);
                Index = StartIndex;
                for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                {
                    var shift = (TAffine.Planes.Length - 1 - i) << 1;
                    Transform.Rotate(i, 90 * (value >> shift & 3));
                }
                state = value;
                ValidState = true;
            }
        }

        public void Rotate(int[] plane, TVector rot)
        {
            Transform.Rotate(plane[0], plane[1], rot.X, rot.Y);
            ValidState = false;
        }

        // Number of axes the cubie's orientation does not leave fixed: axis k is fixed <=> M[k,k] is
        // the positive scale (~+0.45). Cubies are uniformly scaled, so the threshold sits below it.
        public int ActiveAxisCount()
        {
            int m = 0;
            for (int k = 0; k < TAffine.N; k++)
                if (Transform.M[k, k] < 0.1f) m++;
            return m;
        }

        //public double Score
        //{
        //    get { return (double)State / MaxScore; }
        //}

        //public TVector GetStartPos()
        //{
        //    var cubie = Copy();
        //    for (int axis = 0; axis < TAffine.Planes.Length; axis++)
        //        cubie.Rotate(axis, -90 * (cubie.State >> 2 * axis & 3));
        //    return cubie.Transform.Origin;
        //}

        public TCubie Copy()
        {
            var dest = new TCubie();
            dest.Transform = Transform.Clone();
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest.state = state;
            dest.ValidState = ValidState;
            dest.StartIndex = StartIndex;
            dest.ClusterIndex = ClusterIndex;
            dest.RotationCount = RotationCount;
            dest.EulerAngles = EulerAngles;
            return dest;
        }

        public int Index
        {
            get { return SizeMatrix.Coords2Index(Position); }
            set
            {
                var pos = SizeMatrix.Index2Coords(value);
                Transform.Origin = pos - TRubikCube.C;
                var position = new int[pos.Size];
                for (int dim = 0; dim < position.Length; dim++)
                    position[dim] = (int)Math.Round(Math.Abs(Transform.Origin[dim]) + TRubikCube.C);
                Array.Sort(position);
                Array.Reverse(position);
                ClusterIndex = SizeMatrix.Coords2Index(position);
            }
        }

        public int GetPos(int coord) { return (int)Math.Round(Transform.Origin[coord] + TRubikCube.C); }
        public int[] Position
        {
            get 
            {            
                var position = new int[Transform.Origin.Size];
                for (int i = 0; i < position.Length; i++)
                    position[i] = GetPos(i);
                return position;
            }
        }
    }
}
