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
        public int X { get { return (int)Math.Round(Transform.Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Transform.Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Transform.Origin.Z + TRubikCube.C); } }
        public int W { get { return (int)Math.Round(Transform.Origin.W + TRubikCube.C); } }
        //public double Error;
        public int StartIndex;

        public TCubie()
        {
            Vertices = Cube.Vertices;
            Faces = Cube.Faces;
            Colors = Cube.Colors;
        }

        int GetAngle(double cosA, double sinA)
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
                    //var gamma = GetAngle(Transform[1], Transform[0]);
                    //var beta = 0;
                    //var alpha = 0;
                    //if (gamma != 0)
                    //    alpha = GetAngle(Transform[6], Transform[10]);
                    //else
                    //{
                    //    beta = GetAngle(-Transform[2], Transform[0]);
                    //    alpha = GetAngle(-Transform[9], Transform[5]);
                    //}
                    //var moveCount = Math.Sign(alpha) + Math.Sign(beta) + Math.Sign(gamma);
                    //_State = moveCount << 6 | gamma << 4 | beta << 2 | alpha;
                    //if (alpha == 2 && gamma == 2) _State = 0x48;
                    state = 0;
                    var shift = 0;
                    var angles = Transform.GetEulerAngles();
                    var moveCount = 0;
                    for (int i = 0; i < angles.Count; i++)
                    //for (int i = angles.Count - 1; i >= 0; i--)
                    {
                        var angle = GetAngle(angles[i].X, angles[i].Y);
                        state |= angle << shift;
                        shift += 2;
                        if (angle > 0) moveCount++;
                    }
                    state |= moveCount << 2 * angles.Count;
                    //var xform = TAffine.CreateScale(new TVector(0.45f, 0.45f, 0.45f, 0.45f));
                    //for (int i = 0; i < TAffine.Planes.Length; i++)
                    //    xform = TAffine.CreateRotation(i, 90 * (_State >> 2 * i & 3)) * xform;
                    //Error = (xform.M - Transform.M).Norm;
                    //if (Error > 0.01)
                    //    ;
                    ValidState = true;
                }
                return state;
            }
            set
            {
                var org = Transform.Origin;
                Transform = TAffine.CreateScale(new TVector(0.45f, 0.45f, 0.45f, 0.45f));
                for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                    Rotate(i, 90 * (value >> 2 * i & 3));
                Transform.Origin = org;
                ValidState = false;
                var state = State;
                if (state != value)
                    ;
                this.state = value;
                ValidState = true;
                if (this.state != 0)
                    Transparency = 0.5f;
            }
        }

        public TVector GetStartPos()
        {
            var cubie = Copy();
            for (int axis = 0; axis < TAffine.Planes.Length; axis++)
                cubie.Rotate(axis, -90 * (cubie.State >> 2 * axis & 3));
            return cubie.Transform.Origin;
        }

        //public int Orbit
        //{
        //    get
        //    {
        //        // 4D Manhattan distance from center
        //        var dist = Math.Abs(Origin.X) + Math.Abs(Origin.Y) + Math.Abs(Origin.Z) + Math.Abs(Origin.W);
        //        return (int)Math.Round(2 * dist);
        //    }
        //}

        public TCubie Copy()
        {
            var dest = new TCubie();
            //Array.Copy(Transform.M.Data, dest.Transform.M.Data, Transform.M.Data.Length);
            //Array.Copy(Transform.Origin.Data, dest.Transform.Origin.Data, Transform.Origin.Data.Length);
            dest.Transform = Transform.Clone();
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest.state = state;
            dest.ValidState = ValidState;
            dest.StartIndex = StartIndex;
            return dest;
        }

        //int index;
        public int Index
        {
            get
            {
                var pos = Transform.Origin + TRubikCube.C;
                return SizeMatrix.Coords2Index(pos);
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

        public TVector GetPosition()
        {
            return Transform.Origin + TRubikCube.C;
        }
    }
}
