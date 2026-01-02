using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGL
{
    public class TVertex: TVector
    {
        //public TVector Coord = new TVector();
        public TVector TexCoord = new TVector();
        public List<TObject3D> Bones = new List<TObject3D>();
        public List<float> Weights = new List<float>();
        public int Index;
        TVector _Normal;
        public TVector Normal
        {
            get
            {
                if (_Normal == null)
                {
                    _Normal = new TVector();
                    foreach (var face in Faces)
                    {
                        _Normal += face.Normal;
                    }
                }
                return _Normal;
            }
            set { _Normal = value; }
        }
        //TMatrix _TB;
        //public TMatrix TB
        //{
        //    get
        //    {
        //        if (_TB == null)
        //        {
        //            _TB = new TMatrix(3, 2);
        //            for(int i = 0; i < Faces.Count; i++)
        //            {
        //                var face = Faces[i];
        //                //var angle = _TB.Cols[0].Dot(face.TB.Cols[0]);
        //                //if (i == 0 || angle > 0)
        //                    _TB += face.TB;
        //                //else
        //                //    continue;
        //            }
        //            //var T = _TB.Cols[0];
        //            //T.Norm = 1;
        //            //_TB.Cols[0] = T;
        //            //var B = _TB.Cols[1];
        //            //B.Norm = 1;
        //            //_TB.Cols[1] = B;
        //        }
        //        return _TB;
        //    }
        //}
        public List<TFace> Faces = new List<TFace>();
    }
}
