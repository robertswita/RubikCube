using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TFace
    {
        public List<TVertex> Vertices = new List<TVertex>();
        public List<TVector> TexVertices = new List<TVector>();
        public TMaterial Material;
        public bool Smooth = true;
        public TVector Normal
        {
            get
            {
                var v01 = Vertices[1] - Vertices[0];
                var v02 = Vertices[2] - Vertices[0];
                //return TMatrix.Cross(v01, v02);
                return v01.Cross(v02);
            }
        }

        TMatrix _TB;
        public TMatrix TB
        {
            get
            {
                if (_TB == null)
                {
                    var E = new TMatrix(3, 2);
                    E.Cols[0] = Vertices[1] - Vertices[0];
                    E.Cols[1] = Vertices[2] - Vertices[0];
                    if (TexVertices.Count == 3)
                    {
                        var UV = new TMatrix(2, 2);
                        var UV01 = TexVertices[1] - TexVertices[0];
                        var UV02 = TexVertices[2] - TexVertices[0];
                        UV.Cols[0] = new TVector(UV01[0], UV01[1]);
                        UV.Cols[1] = new TVector(UV02[0], UV02[1]);
                        _TB = E * UV.Inv;
                    }
                    else
                        _TB = (TMatrix)E.Clone();
                }
                return _TB;
            }
        }

        public void AddVertex(TVertex v)
        {
            Vertices.Add(v);
            v.Faces.Add(this);
        }

        public TFace Adjacent(int idx)
        {
            TVertex vA = Vertices[idx];
            TVertex vB = Vertices[(idx + 1) % 3];
            for (int i = 0; i < vA.Faces.Count; i++)
            {
                var face = vA.Faces[i];
                if (face == this) continue;
                for (int j = 0; j < face.Vertices.Count; j++)
                    if (face.Vertices[j] == vB)
                        return face;
            }
            return null;
        }

        public bool Contain(TVector p)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                TVector AB = Vertices[(i + 1) % 3] - Vertices[i];
                TVector PA = Vertices[i] - p;
                if (PA.X * AB.Y < PA.Y * AB.X)
                    return false;
            }
            return true;
        }


    }
}
