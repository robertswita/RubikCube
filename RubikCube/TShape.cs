using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace TGL
{
    /// <summary>
    /// Shape class with N-D transformation support
    /// </summary>
    public class TShape
    {
        public bool Selected;
        public float Transparency = 1;
        public List<TVector> Vertices = new List<TVector>();
        public List<int> Faces = new List<int>();
        public List<TShape> Children = new List<TShape>();
        public List<Color> Colors = new List<Color>();

        TShape _Parent;
        public virtual TShape Parent
        {
            get { return _Parent; }
            set
            {
                if (value == _Parent) return;
                _Parent?.Children.Remove(this);
                _Parent = value;
                _Parent?.Children.Add(this);
            }
        }
        public TAffine Transform = new TAffine();
        public TAffine WorldTransform = new TAffine();
        //public TVector Origin
        //{
        //    get { return Transform.Cols[TAffine.N]; }
        //    set { Transform.Cols[TAffine.N] = value; }
        //}
        public void Scale(TVector s)
        {
            Transform = TAffine.CreateScale(s) * Transform;
        }
        public void Rotate(int axis, double angle)
        {
            if (angle != 0 && axis < TAffine.Planes.Length)
                Transform = TAffine.CreateRotation(axis, angle) * Transform;
        }
        //public void Translate(TVector t)
        //{
        //    //Transform = TAffine.CreateTranslation(t) * Transform;

        //}

        static float GetComponent(float hue)
        {
            if (hue < 0) hue += 360;
            if (hue > 360) hue -= 360;
            float c = 0;
            var section = hue / 60f;
            if (section < 1)
                c = section;
            else if (section < 3)
                c = 1;
            else if (section < 4)
                c = Vector3.Lerp(Vector3.One, Vector3.Zero, section - 3).X;
            return c;
        }

        static Color[] CreatePalette()
        {
            var pal = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                var rgb = new Vector3();
                var s = 8 * i / 256f;
                if (s < 1)
                    rgb.X = Vector3.Lerp(Vector3.Zero, Vector3.One, s).X;
                else if (s < 6)
                {
                    var hue = Vector3.Lerp(Vector3.Zero, Vector3.One * 300, (s - 1) / 5).X;
                    rgb.X = GetComponent(hue + 120);
                    rgb.Y = GetComponent(hue);
                    rgb.Z = GetComponent(hue - 120);
                }
                else if (s < 7)
                {
                    rgb = Vector3.Lerp(Vector3.One, Vector3.One / 2, s - 6);
                    rgb.Y = 1 - rgb.Y;
                }
                else
                    rgb = Vector3.Lerp(Vector3.One / 2, Vector3.One, s - 7);
                rgb *= 255;
                pal[i] = Color.FromArgb((int)rgb.X, (int)rgb.Y, (int)rgb.Z);
            }
            return pal;
        }


        public static TShape CreateHyperCube()
        {
            var obj = new TShape();
            var lbn = new TVector(TAffine.N) - 1;
            var rtf = new TVector(TAffine.N) + 1;
            for (int i = 0; i < 1 << TAffine.N; i++)
            {
                var p = lbn.Clone();
                for (int dim = 0; dim < TAffine.N; dim++)
                    if ((i & 1 << dim) != 0) p[dim] = rtf[dim];
                obj.Vertices.Add(p);
            }
            var colorCount = TAffine.Planes.Length * (1 << TAffine.N - 2);
            var pal = CreatePalette();
            var colorIdx = 0;
            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
            {
                var axis1 = 1 << TAffine.Planes[plane][0];
                var axis2 = 1 << TAffine.Planes[plane][1];
                var axes = axis1 | axis2;
                for (int i = 0; i < 1 << TAffine.N - 2; i++)
                {
                    var firstIdx = i & (axis1 - 1) | (i & ~(axis1 - 1)) << 1;
                    firstIdx = firstIdx & (axis2 - 1) | (firstIdx & ~(axis2 - 1)) << 1;
                    var quad = new int[4];
                    for (int j = 0; j < 4; j++)
                    {
                        var idx = firstIdx;
                        if ((j & 1) != 0) idx |= axis1;
                        if ((j >> 1) != 0) idx |= axis2;
                        quad[j] = idx;
                    }
                    obj.Faces.Add(quad[0]);
                    obj.Faces.Add(quad[1]);
                    obj.Faces.Add(quad[3]);
                    obj.Faces.Add(quad[2]);
                    obj.Colors.Add(pal[255 * colorIdx / colorCount]);
                    colorIdx++;
                }
            }
            return obj;
        }

    }
}
