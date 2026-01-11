using System;
using System.Collections.Generic;
using System.Text;
using RubikCube;

namespace TGL
{
    class TObject3DComparer : IComparer<TShape>
    {
        //public TGLContext GLCanvas;
        public int Compare(TShape first, TShape second)
        {
            var projection = new double[16];
            var viewport = new int[4];
            ////double zX = 0, zY = 0, zZ = 0;
            var z1 = new TVector();
            var z2 = new TVector();
            //OpenGL.glGetDoublev(OpenGL.GL_PROJECTION_MATRIX, projection);
            //OpenGL.glGetIntegerv(OpenGL.GL_VIEWPORT, viewport);
            //OpenGL.gluProject(first.Origin.X, first.Origin.Y, first.Origin.Z, first.Transform, projection, viewport, ref z1.X, ref z1.Y, ref z1.Z);
            //OpenGL.gluProject(second.Origin.X, second.Origin.Y, second.Origin.Z, second.Transform, projection, viewport, ref z2.X, ref z2.Y, ref z2.Z);
            return z1.Z.CompareTo(z2.Z);
        }
    }
}
