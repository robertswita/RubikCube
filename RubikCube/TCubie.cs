using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TCubie : TObject3D
    {
        public int X { get { return (int)Math.Round(Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Origin.Z + TRubikCube.C); } }
        public int ClusterCount;

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
                var dist = Math.Abs(Origin.X) + Math.Abs(Origin.Y) + Math.Abs(Origin.Z);
                return (int)Math.Round(2 * dist);
            }
        }

        public TCubie Copy()
        {
            var dest = new TCubie();
            Array.Copy(Transform, dest.Transform, Transform.Length);
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest._State = _State;
            dest.ValidState = ValidState;
            dest.ClusterCount = ClusterCount;
            return dest;
        }
    }
}
