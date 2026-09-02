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
    public class TScene
    {
        TShape root;
        // Assigning a Root re-establishes its scene back-link (TShape.Scene reads Root.scene), so a
        // freshly created root swapped in (e.g. on a dimension change) stays connected to this scene.
        public TShape Root
        {
            get { return root; }
            set { root = value; if (root != null) root.Scene = this; }
        }
        //public List<TCamera> Cameras = new List<TCamera>();
        public List<TLight> Lights = new List<TLight>();
        public string TexturesPath;
        public TScene()
        {
            Root = new TShape();
        }

    };
}

