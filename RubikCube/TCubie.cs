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
            var lbn = new TVector(-1, -1, -1);
            var rtf = new TVector(+1, +1, +1);
            for (int i = 0; i < 8; i++)
            {
                var p = new TVertex();
                p.Assign(lbn.Clone());
                if ((i & 1) == 1) p.X = rtf.X;
                if ((i & 2) != 0) p.Y = rtf.Y;
                if ((i & 4) != 0) p.Z = rtf.Z;
                Vertices.Add(p);
            }
            var faceIndices = new List<int>();
            for (int dim = 0; dim < 3; dim++)
            {
                var axis1 = 1 << dim;
                var axis2 = 1 << (dim + 1) % 3;
                faceIndices.Add(axis1);
                faceIndices.Add(axis2);
                faceIndices.Add(0);
                faceIndices.Add(axis1 + axis2);
                faceIndices.Add(axis2);
                faceIndices.Add(axis1);
            }
            for (int i = faceIndices.Count - 1; i >= 0; i--)
                faceIndices.Add(7 - faceIndices[i]);
            for (int i = 0; i < 6; i++)
            {
                var mat = new TMaterial();
                var rgb = 0xff << (i % 3) * 8;
                if (i > 2)
                    rgb = ~rgb;
                mat.Diffuse.Color = System.Drawing.Color.FromArgb(rgb);
                Materials.Add(mat);
            }
            for (int i = 0; i < faceIndices.Count; i += 3)
            {
                var face = new TFace();
                face.AddVertex(Vertices[faceIndices[i]]);
                face.AddVertex(Vertices[faceIndices[i + 1]]);
                face.AddVertex(Vertices[faceIndices[i + 2]]);
                var matIdx = i / 6;
                face.Material = Materials[matIdx];
                Faces.Add(face);
            }
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
                //var org = Origin;
                //LoadIdentity();
                Scale = new TVector(0.45f, 0.45f, 0.45f);
                Rotation.X = 90 * alpha;
                Rotation.Y = 90 * beta;
                Rotation.Z = 90 * gamma;
                //Origin = new TVector(org.X, org.Y, org.Z);
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
            //Array.Copy(Transform, dest.Transform, Transform.Length);
            dest.Scale = Scale.Clone();
            dest.Rotation = Rotation.Clone();
            dest.Origin = Origin.Clone();
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest._State = _State;
            dest.ValidState = ValidState;
            dest.ClusterCount = ClusterCount;
            return dest;
        }
    }
}
