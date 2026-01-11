using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TCubie : TShape
    {
        static TShape Cube = CreateTesseract();
        public int X { get { return (int)Math.Round(Transform.Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Transform.Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Transform.Origin.Z + TRubikCube.C); } }
        public int W { get { return (int)Math.Round(Transform.Origin.W + TRubikCube.C); } }
        public double Error;
        public int OriginalPos;

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
        int _State;
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
                    _State = 0;
                    var shift = 0;
                    var angles = Transform.GetEulerAngles();
                    var moveCount = 0;
                    for (int i = 0; i < angles.Count; i++)
                    //for (int i = angles.Count - 1; i >= 0; i--)
                    {
                        var angle = GetAngle(angles[i].X, angles[i].Y);
                        _State |= angle << shift;
                        shift += 2;
                        if (angle > 0) moveCount++;
                    }
                    _State |= moveCount << 2 * angles.Count;
                    //var xform = TAffine.CreateScale(new TVector(0.45f, 0.45f, 0.45f, 0.45f));
                    //for (int i = 0; i < TAffine.Planes.Length; i++)
                    //    xform = TAffine.CreateRotation(i, 90 * (_State >> 2 * i & 3)) * xform;
                    //Error = (xform.M - Transform.M).Norm;
                    //if (Error > 0.01)
                    //    ;
                    ValidState = true;
                }
                return _State;
            }
            set
            {
                //var alpha = value & 3;
                //var beta = (value >> 2) & 3;
                //var gamma = (value >> 4) & 3;
                var org = Transform.Origin;
                Transform = TAffine.CreateScale(new TVector(0.45f, 0.45f, 0.45f, 0.45f));
                for (int i = TAffine.Planes.Length - 1; i >= 0; i--)
                    Rotate(i, 90 * (value >> 2 * i & 3));
                Transform.Origin = org;
                //Translate(org);
                ValidState = false;
                var state = State;
                _State = value;
                ValidState = true;
                if (_State != 0)
                    Transparency = 0.5f;
            }
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
            dest._State = _State;
            dest.ValidState = ValidState;
            dest.OriginalPos = OriginalPos;
            return dest;
        }

    }
}
