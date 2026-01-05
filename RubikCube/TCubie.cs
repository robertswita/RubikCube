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
        public int X { get { return (int)Math.Round(Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Origin.Z + TRubikCube.C); } }
        public int W { get { return (int)Math.Round(Origin.W + TRubikCube.C); } }

        public TCubie()
        {
            Vertices = Cube.Vertices;
            Faces = Cube.Faces;
            Colors = Cube.Colors;
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
                Transform.LoadIdentity();
                Scale(new TVector(0.45f, 0.45f, 0.45f));
                Rotate(0, 90 * alpha);
                Rotate(1, 90 * beta);
                Rotate(2, 90 * gamma);
                Translate(org);
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
            Array.Copy(Transform.Data, dest.Transform.Data, Transform.Data.Length);
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest._State = _State;
            dest.ValidState = ValidState;
            return dest;
        }

    }
}
