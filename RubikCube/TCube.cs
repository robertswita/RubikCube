using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGL
{
    [Serializable]
    public class TCube
    {
        public TCube()
        {
            FOrigin = new TVector();
            FScale = new TVector();
        }
        TVector FOrigin;
        public TVector Origin
        {
            get { return FOrigin; }
            set { FOrigin.Assign(value); }
        }
        TVector FScale;
        public TVector Scale
        {
            get { return FScale; }
            set { FScale.Assign(value); }
        }
        public void Assign(TCube cube)
        {
            FOrigin.Assign(cube.Origin);
            FScale.Assign(cube.Scale);
        }
        public bool Empty() { return (Scale.X == 0) || (Scale.Y == 0) || (Scale.Z == 0); }
        //public double Width { get { return 2 * Scale.X; } set { Scale.X = value / 2; } }
        //public double Height { get { return 2 * Scale.Y; } set { Scale.Y = value / 2; } }
        //public double Depth { get { return 2 * Scale.Z; } set { Scale.Z = value / 2; } }
        public TVector LBN
        {
            get { return (Origin - Scale); }
            set
            {
                FOrigin = (RTF + value) / 2;
                FScale = Origin - value;
            }
        }
        public TVector RTF
        {
            get { return (Origin + Scale); }
            set
            {
                FOrigin = (LBN + value) / 2;
                FScale = value - Origin;
            }
        }
        public void Union(TVector v)
        {
            TVector lbn = LBN;
            TVector rtf = RTF;
            for (int i = 0; i < lbn.Size; i++)
            {
                if (v[i] < lbn[i]) lbn[i] = v[i];
                if (v[i] > rtf[i]) rtf[i] = v[i];
            }
            LBN = lbn;
            RTF = rtf;
        }
        public void Mult(TCube cube)
        {
            FScale *= cube.Scale;
            FOrigin *= cube.Scale;
            FOrigin += cube.Origin;
        }
        public void UnMult(TCube cube)
        {
            FOrigin -= cube.Origin;
            FOrigin /= cube.Scale;
            FScale /= cube.Scale;
        }
        public TAffine Transform
        {
            get
            {
                return TAffine.Translate(Origin) * TAffine.Scale(Scale);
            }
        }
    }
}
