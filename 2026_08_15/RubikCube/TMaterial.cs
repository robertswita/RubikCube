/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej Inteligencji
***********************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TGL
{
    [Serializable]
    public class TMaterial
    {
        public class TTexture
        {
            public Color Color;
            public int DisplayList;
            public string Path;
            public Bitmap Map
            {
                get
                {
                    Bitmap bmp = null;
                    if (File.Exists(Path))
                        try
                        {
                            bmp = new Bitmap(Path);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    if (bmp == null)
                    {
                        bmp = new Bitmap(1, 1);
                        bmp.SetPixel(0, 0, Color);
                    }
                    return bmp;
                }
            }
        }
        public string Name = "material";
        public TTexture Diffuse = new TTexture();
        public TTexture Specular = new TTexture();
        public TTexture Normal = new TTexture();
        public float Shininess = 10;
        public TTexture[] Textures;

        public TMaterial()
        {
            Specular.Color = Color.White;
            Normal.Color = Color.FromArgb(127, 127, 255);
            Textures = new TTexture[] { Diffuse, Specular, Normal };
        }
    };

}
