using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TCubie : TShape
    {
        public static TShape Cube;
        public static TMatrix SizeMatrix;
        public static int MaxScore;
        //public double Error;
        public int StartIndex;
        public int RotationCount;
        public static TVector Scaling;
        public bool IsReversedSeq;
        public int ClusterIndex;
        public TVector GivensOrder;

        public TCubie()
        {
            Vertices = Cube.Vertices;
            Faces = Cube.Faces;
            Colors = Cube.Colors;
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

        public override float Transparency { 
            get { 
                base.Transparency = State != 0 ? 0.1f : 1;
                return base.Transparency;
            } 
        }

        public bool ValidState;
        int state;
        public int State
        {
            get
            {
                if (!ValidState)
                {
                    state = 0;
                    //EulerAngles = Transform.GetEulerAngles(TRubikCube.EulerOrder, TRubikCube.IsEulerOrderReversed);
                    EulerAngles = Transform.GetEulerAngles(null);
                    RotationCount = 0;
                    var shift = (EulerAngles.Count - 1) << 1;
                    for (int i = 0; i < EulerAngles.Count; i++)
                    {
                        var angle = GetAngle(EulerAngles[i].X, EulerAngles[i].Y);
                        state |= angle << shift;
                        shift -= 2;
                        if (angle > 0)
                        {
                            RotationCount++;
                        }
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
                    //for (int i = 0; i < TAffine.N; i++)
                    //    if (Math.Abs(Transform.M[i, i] - Scaling[i]) > 1E-3)
                    //    {
                    //        state = 1;
                    //        break;
                    //    }
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
                ValidState = false;
                var state_ = State;
                if (state_ != value)
                    ;
                state = value;
                ValidState = true;
                //if (state != 0)
                //    Transparency = 0.5f;
            }
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
