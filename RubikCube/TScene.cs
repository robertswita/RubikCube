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
        public TObject3D Root = new TObject3D();
        public List<TCamera> Cameras = new List<TCamera>();
        public List<TLight> Lights = new List<TLight>();
        public string TexturesPath;
        public TScene()
        {
            Root.Scene = this;
        }
        // Funkcja GenList generuje identyfikator do listy wyświetlania unikatowy dla całej sceny.
        // Nie posługujemy się funkcją OpenGL.glGenLists ponieważ zwraca ona identyfikator 
        // unikalny tylko dla danego kontekstu
        uint FDisplayListCount;
        public uint GenList()
        {
            return ++FDisplayListCount;
        }
        public bool IsList(uint ID)
        {
            return (0 < ID && ID <= FDisplayListCount);
        }

    };
}

