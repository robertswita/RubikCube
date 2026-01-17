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
    public class TCamera : TObject3D
    {
        public bool IsPerspective;
        public double Fovy = 90;
        public TCube Zoom = new TCube();
        public TCube Clip = new TCube();
        public float Near = 0.1f;

        public TCamera()
        {
            Clip.Scale = new TVector(1, 1, 1);
            Zoom.Scale = new TVector(1, 1, 1);
        }
        // Nadpisujemy setter dla Parent aby wpisać kamerę do listy kamer w scenie (lub ją z listy usunąć)
        public override TObject3D Parent
        {
            set
            {
                Scene?.Cameras.Remove(this);
                base.Parent = value;
                Scene?.Cameras.Add(this);
            }
        }

        public TAffine Projection
        {
            get
            {
                WorldTransform = null;
                var proj = WorldTransform.Inv;
                var clip = new TCube();
                clip.Assign(Clip);
                if (IsPerspective)
                {
                    var permute = new TAffine();
                    permute.Cols.Swap(2, 3);
                    proj = permute * proj;
                    var aspect = Clip.Scale.Y / Clip.Scale.X;
                    var tgHalfFovy = Math.Tan(Math.PI * Fovy / 360);
                    clip.Scale.X = tgHalfFovy / aspect;
                    clip.Scale.Y = tgHalfFovy;
                    clip.Scale.Z = -0.5 / Near;
                    clip.Origin.Z = 0.5 / Near;
                }
                proj = Zoom.Transform * clip.Transform.Inv * proj;
                return proj;
            }
        }

    };

}
