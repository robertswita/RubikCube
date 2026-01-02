/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej Inteligencji
***********************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace TGL
{
    [Serializable]
    public class TMap
    {
        public int DisplayList;
        public List<TFace> Faces = new List<TFace>();
        public TMaterial Material;
    };
}
