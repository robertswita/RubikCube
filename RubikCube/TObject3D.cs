/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej Inteligencji
***********************************************************/
using System;
using System.Collections.Generic;

namespace TGL
{
public struct TPoint3D
{
    public double X;
    public double Y;
    public double Z;
    public TPoint3D(double x, double y, double z) { X = x; Y = y; Z = z; }
};

public class TObject3D
{
    public bool Selected;
    public bool Transparent;
    public List<TPoint3D> Vertices = new List<TPoint3D>();
    public List<int> Faces = new List<int>();
    public List<TObject3D> Children = new List<TObject3D>();
    TObject3D _Parent;
    public virtual TObject3D Parent
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
    public TPoint3D AxisX { get { return new TPoint3D(Transform[0], Transform[1], Transform[2]); } }
    public TPoint3D AxisY { get { return new TPoint3D(Transform[4], Transform[5], Transform[6]); } }
    public TPoint3D AxisZ { get { return new TPoint3D(Transform[8], Transform[9], Transform[10]); } }
    public TPoint3D Origin { get { return new TPoint3D(Transform[12], Transform[13], Transform[14]); } }

    public TObject3D()
    {
        LoadIdentity();
    }

    public void LoadIdentity()
    {
        Transform = new double[16];
        for (int i = 0; i < 4; i++)
            Transform[5 * i] = 1;
    }

    public void Scale(double sX, double sY, double sZ)
    {
        OpenGL.glLoadIdentity();
        OpenGL.glScaled(sX, sY, sZ);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }
    public void RotateX(double alpha)
    {
        OpenGL.glLoadIdentity();
        OpenGL.glRotated(alpha, 1, 0, 0);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }
    public void RotateY(double beta)
    {
        OpenGL.glLoadIdentity();
        OpenGL.glRotated(beta, 0, 1, 0);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }
    public void RotateZ(double gamma)
    {
        OpenGL.glLoadIdentity();
        OpenGL.glRotated(gamma, 0, 0, 1);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }
    public void Translate(double tX, double tY, double tZ)
    {
        OpenGL.glLoadIdentity();
        OpenGL.glTranslated(tX, tY, tZ);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }
    public void MultMatrix(double[] m)
    {
        OpenGL.glLoadMatrixd(m);
        OpenGL.glMultMatrixd(Transform);
        OpenGL.glGetDoublev(OpenGL.GL_MODELVIEW_MATRIX, Transform);
    }

};

}