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
    public class TCubie : TObject4D
    {
        public int X { get { return (int)Math.Round(Origin.X + TRubikCube.C); } }
        public int Y { get { return (int)Math.Round(Origin.Y + TRubikCube.C); } }
        public int Z { get { return (int)Math.Round(Origin.Z + TRubikCube.C); } }
        public int W { get { return (int)Math.Round(Origin.W + TRubikCube.C); } }
        public int ClusterCount;
        public static Color[] Colors = new Color[] { 
            Color.Red, Color.Green, Color.Blue,
            Color.Cyan, Color.Magenta, Color.Yellow,
            Color.LightPink, Color.LightGreen, Color.LightBlue,
            Color.Orange, Color.DarkKhaki, Color.DarkMagenta
        };

        public TCubie()
        {
            var lbn = new TVector(-1, -1, -1, -1);
            var rtf = new TVector(+1, +1, +1, +1);
            for (int i = 0; i < 16; i++)
            {
                var p = new TVertex();
                p.Assign(lbn.Clone());
                if ((i & 1) == 1) p.X = rtf.X;
                if ((i & 2) != 0) p.Y = rtf.Y;
                if ((i & 4) != 0) p.Z = rtf.Z;
                if ((i & 8) != 0) p.W = rtf.W;
                Vertices.Add(p);
            }
            var faceIndices = new List<int>();
            for (int i = 0; i < 3; i++)
                for (int j = i + 1; j < 4; j++)
                {
                    var axis1 = 1 << i;
                    var axis2 = 1 << j;
                    faceIndices.Add(axis1);
                    faceIndices.Add(axis2);
                    faceIndices.Add(0);
                    faceIndices.Add(axis1 + axis2);
                    faceIndices.Add(axis2);
                    faceIndices.Add(axis1);
                }
            for (int i = faceIndices.Count - 1; i >= 0; i--)
                faceIndices.Add(15 - faceIndices[i]);
            for (int i = 0; i < 12; i++)
            {
                var mat = new TMaterial();
                //var rgb = 0xff << (i % 3) * 8;
                //if (i > 2)
                //    rgb = ~rgb;
                //mat.Diffuse.Color = System.Drawing.Color.FromArgb(rgb);
                mat.Diffuse.Color = Colors[i];
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
                    var angles = Transform.GetEulerAngles();
                    for (int i = 0; i < 6; i++)
                    {
                        var angle = GetAngle(angles[i].X, angles[i].Y);
                        _State |= angle << 2 * i;
                    }
                    // take into account moveCount!
                    ValidState = true;
                }
                return _State;
            }
            set
            {
                //var org = Origin;
                LoadIdentity();
                Scale(new TVector(0.45f, 0.45f, 0.45f));
                var shift = 0;
                for (int i = 0; i < 3; i++)
                    for (int j = i + 1; j < 4; j++)
                    {
                        var angle = (value >> shift) & 3;
                        RotateAxes(i, j, angle * 90);
                        shift += 2;
                    }

                //Rotation.X = 90 * alpha;
                //Rotation.Y = 90 * beta;
                //Rotation.Z = 90 * gamma;
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
            Array.Copy(Transform.Data, dest.Transform.Data, Transform.Data.Length);
            dest.Vertices = Vertices;
            dest.Faces = Faces;
            dest.TexVertices = TexVertices;
            dest.Materials = Materials;
            dest._State = _State;
            dest.ValidState = ValidState;
            dest.ClusterCount = ClusterCount;
            return dest;
        }
    }
}
