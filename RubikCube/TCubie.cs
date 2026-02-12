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
        public static TShape Cube = null!;
        public static TMatrix SizeMatrix = null!;
        public static int MaxScore;
        public static TVector Scaling = null!;
        public List<TVector> EulerAngles = null!;
        //public int X { get { return (int)Math.Round(Transform.Origin.X + TRubikCube.C); } }
        //public int Y { get { return (int)Math.Round(Transform.Origin.Y + TRubikCube.C); } }
        //public int Z { get { return (int)Math.Round(Transform.Origin.Z + TRubikCube.C); } }
        //public int W { get { return (int)Math.Round(Transform.Origin.W + TRubikCube.C); } }
        //public double Error;
        public int StartIndex;
        public int RotationCount;

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
                        if (angle > 0) RotationCount++;
                    }
                    ValidState = true;
                }
                return state;
            }
            set
            {
                var org = Transform.Origin;
                Transform = TAffine.CreateScale(Scaling);
                Index = StartIndex;
                for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                {
                    var shift = (TAffine.Planes.Length - 1 - i) << 1;
                    Transform.Rotate(i, 90 * (value >> shift & 3));
                }
                Transform.Origin = org;
                ValidState = false;
                var state_ = State;
                if (state_ != value) { }
                state = value;
                ValidState = true;
                if (state != 0)
                    Transparency = 0.5f;
            }
        }

        public double Score
        {
            get { return (double)State / MaxScore; }
        }

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

        //int index;
        public int Index
        {
            get
            {
                return SizeMatrix.Coords2Index(Position);
                //var index = (int)pos[pos.Size - 1];
                //for (int i = pos.Size - 2; i >= 0; i--)
                //    index = index * TRubikCube.Size + (int)pos[i];
                //return index;
            }
            set 
            {
                //var stride = (int)Math.Pow(TRubikCube.Size, TAffine.N);
                //var subs = new TVector(TAffine.N);
                //for (int i = TAffine.N - 1; i >= 0; i--)
                //{
                //    stride /= TRubikCube.Size;
                //    var sub = value / stride;
                //    value -= sub * stride;
                //    subs[i] = sub;
                //}
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
