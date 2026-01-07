using System;
using System.Collections.Generic;
using System.Drawing;

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
        public TVector Origin
        {
            get { return Transform.Cols[TAffine.N]; }
            set { Transform.Cols[TAffine.N] = value; }
        }
        public void Scale(TVector s)
        {
            Transform = TAffine.CreateScale(s) * Transform;
        }
        public void Rotate(int axis, double angle)
        {
            if (angle != 0)
                Transform = TAffine.CreateRotation(axis, angle) * Transform;
        }
        public void Translate(TVector t)
        {
            Transform = TAffine.CreateTranslation(t) * Transform;
        }

        public static TShape CreateTesseract()
        {
            var obj = new TShape();
            var lbn = new TVector(-1, -1, -1, -1);
            var rtf = new TVector(+1, +1, +1, +1);
            for (int i = 0; i < 16; i++)
            {
                var p = lbn.Clone();
                if ((i & 1) != 0) p.X = rtf.X;
                if ((i & 2) != 0) p.Y = rtf.Y;
                if ((i & 4) != 0) p.Z = rtf.Z;
                if ((i & 8) != 0) p.W = rtf.W;
                obj.Vertices.Add(p);
            }
            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
            {
                var axis1 = 1 << TAffine.Planes[plane][0];
                var axis2 = 1 << TAffine.Planes[plane][1];
                var axes = axis1 | axis2;
                var dim = 0;
                while ((axes & 1 << dim) != 0) dim++;
                var axis3 = 1 << dim;
                dim++;
                while ((axes & 1 << dim) != 0) dim++;
                var axis4 = 1 << dim;
                for (int i = 0; i < 4; i++)
                {
                    var firstIdx = 0;
                    if ((i & 1) != 0) firstIdx |= axis2;
                    if ((i >> 1) != 0) firstIdx |= axis1;
                    var quad = new int[4];
                    for (int j = 0; j < 4; j++)
                    {
                        var idx = firstIdx;
                        if ((j & 1) != 0) idx |= axis3;
                        if ((j >> 1) != 0) idx |= axis4;
                        //obj.Faces.Add(idx);
                        quad[j] = idx;
                    }
                    obj.Faces.Add(quad[0]);
                    obj.Faces.Add(quad[1]);
                    obj.Faces.Add(quad[3]);
                    obj.Faces.Add(quad[2]);
                }
            }
            obj.Colors.AddRange(new Color[] {
                Color.Red, Color.Green, Color.Blue,
                Color.Cyan, Color.Magenta, Color.Yellow,
                Color.Orange, Color.White, Color.Pink,
                Color.Beige, Color.Gray, Color.Olive,
                Color.DarkRed, Color.DarkGreen, Color.DarkBlue,
                Color.LightCyan, Color.Maroon, Color.LightYellow,
                Color.SeaGreen, Color.SkyBlue, Color.Silver,
                Color.Sienna, Color.Tomato, Color.Aqua,
            });
            return obj;
        }
    }
}
