namespace TGL
{
    public class TLight : TObject3D
    {
        public TVector Ambient = new TVector(0.3f, 0.3f, 0.3f);
        public TVector Diffuse = new TVector(1, 1, 1);
        public TVector Specular = new TVector(0.05f, 0.05f, 0.05f);
        public TVector AttCoeff = new TVector(1, 0.14f, 0.07f);
        public bool IsDirectional;
        public bool IsEnabled = true;
        public override TObject3D Parent
        {
            set
            {
                Scene?.Lights.Remove(this);
                base.Parent = value;
                Scene?.Lights.Add(this);
            }
        }
    }
}
