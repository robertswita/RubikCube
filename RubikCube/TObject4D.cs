using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using TGL;

namespace RubikCube
{
    public struct TPoint3D
    {
        public double X;
        public double Y;
        public double Z;
        public TPoint3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    };
    internal class TObject4D
    {
        public bool Selected;
        public bool Transparent;
        public List<TPoint3D> Vertices = new List<TPoint3D>();
        public List<int> Faces = new List<int>();
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
        public double[] Transform = new double[16];
        public TPoint3D Origin;
        public TPoint3D AxisX { get { return new TPoint3D(Transform[0], Transform[1], Transform[2]); } }
        public TPoint3D AxisY { get { return new TPoint3D(Transform[4], Transform[5], Transform[6]); } }
        public TPoint3D AxisZ { get { return new TPoint3D(Transform[8], Transform[9], Transform[10]); } }
        public TPoint3D AxisW { get { return new TPoint3D(Transform[12], Transform[13], Transform[14]); } }

        public TObject4D()
        {
            LoadIdentity();
        }

        public void LoadIdentity()
        {
            Transform = new double[16];
            for (int i = 0; i < 4; i++)
                Transform[5 * i] = 1;
        }

        public void Scale(double[] scale)
        {
            var transform = Transform;
            Transform = new double[16];
            for (int i = 0; i < 4; i++)
                Transform[5 * i] = scale[i];
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
            var cosA = Math.Cos(angle);
            var sinA = Math.Sin(angle);
            var transform = Transform;
            LoadIdentity();
            Transform[5 * axis1] = cosA;
            Transform[5 * axis2] = cosA;
            Transform[4 * axis1 + axis2] = sinA;
            Transform[4 * axis2 + axis1] = -sinA;
            MultMatrix(transform);
        }
        public void Translate(double tX, double tY, double tZ)
        {
            OpenGL.glLoadIdentity();
            OpenGL.glTranslated(tX, tY, tZ);
            OpenGL.glMultMatrixd(Transform);
            OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
        }

        public double Dot(TPoint3D )
        public void MultMatrix(double[] m)
        {
            OpenGL.glLoadMatrixd(m);
            OpenGL.glMultMatrixd(Transform);
            OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
            Origin = 
        }

    }
}
