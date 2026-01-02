using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    //public struct TPoint3D
    //{
    //    public double X;
    //    public double Y;
    //    public double Z;
    //    public TPoint3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    //};
    public class TObject4D
    {
        public bool Selected;
        public bool Transparent;
        public List<TVertex> Vertices = new List<TVertex>();
        public List<TFace> Faces = new List<TFace>();
        public List<TVector> TexVertices = new List<TVector>();
        public List<TMaterial> Materials = new List<TMaterial>();
        public List<TObject4D> Children = new List<TObject4D>();
        TObject4D _Parent;
        public virtual TObject4D Parent
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
        public TVector Origin = new TVector(4);
        public TVector AxisX { get { return Transform.Cols[0]; } }
        public TVector AxisY { get { return Transform.Cols[1]; } }
        public TVector AxisZ { get { return Transform.Cols[2]; } }
        public TVector AxisW { get { return Transform.Cols[3]; } }

        //public TObject4D()
        //{
        //    LoadIdentity();
        //}

        public void LoadIdentity()
        {
            Transform.LoadIdentity();
            //Transform = new double[16];
            //for (int i = 0; i < 4; i++)
            //    Transform[5 * i] = 1;
        }

        public void Scale(TVector s)
        {
            var transform = Transform;
            Transform = new TAffine();
            for (int i = 0; i < 4; i++)
                Transform[5 * i] = s[i];
            MultMatrix(transform);
        }
        public void RotateXY(double angle) { RotateAxes(0, 1, angle); }
        public void RotateXZ(double angle) { RotateAxes(0, 2, angle); }
        public void RotateXW(double angle) { RotateAxes(0, 3, angle); }
        public void RotateYZ(double angle) { RotateAxes(1, 2, angle); }
        public void RotateYW(double angle) { RotateAxes(1, 3, angle); }
        public void RotateZW(double angle) { RotateAxes(2, 3, angle); }

        public void RotateAxes(int axis1, int axis2, double angle)
        {
            angle *= Math.PI / 180;
            var cosA = (float)Math.Cos(angle);
            var sinA = (float)Math.Sin(angle);
            var transform = Transform;
            Transform.LoadIdentity();
            Transform[5 * axis1] = cosA;
            Transform[5 * axis2] = cosA;
            Transform[4 * axis1 + axis2] = sinA;
            Transform[4 * axis2 + axis1] = -sinA;
            MultMatrix(transform);
        }
        public void Translate(TVector t) { Origin += t; }

        public void MultMatrix(TAffine m)
        {
            OpenGL.glLoadMatrixf(m.Data);
            OpenGL.glMultMatrixf(Transform.Data);
            OpenGL.glGetFloatv(OpenGL.GL_MODELVIEW_MATRIX, Transform.Data);
            Origin = Transform * Origin;
        }

    }
}
