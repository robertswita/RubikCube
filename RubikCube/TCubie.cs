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

        public TCubie()
        {
            Vertices = Cube.Vertices;
            Faces = Cube.Faces;
            Colors = Cube.Colors;
        }

        public int GetAngle(double cosA, double sinA)
        {
            if (cosA > 0.1) return 0;
            if (sinA > 0.1) return 1;
            if (cosA < -0.1) return 2;
            if (sinA < -0.1) return 3;
            return 0;
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
                    EulerAngles = Transform.GetEulerAngles();
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
                if (state != 0)
                    Transparency = 0.5f;
            }
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
            return dest;
        }

        public int Index
        {
            get
            {
                return SizeMatrix.Coords2Index(Position);
            }
            set 
            {
                var pos = SizeMatrix.Index2Coords(value);
                Transform.Origin = pos - TRubikCube.C;
            }
        }

        public int[] Position
        {
            get 
            {
                var pos = Transform.Origin + TRubikCube.C;
                var position = new int[pos.Size];
                for (int i = 0; i < pos.Size; i++)
                    position[i] = (int)Math.Round(pos[i]);
                return position;
            }
        }
    }
}
