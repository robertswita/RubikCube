using System;
using System.Collections.Generic;
using System.Text;
using RubikCube;

namespace TGL
{
    class TObject3DComparer : IComparer<TObject3D>
    {
        //public TGLContext GLCanvas;
        public int Compare(TObject3D first, TObject3D second)
        {
            var projection = new double[16];
            var viewport = new int[4];
            ////double zX = 0, zY = 0, zZ = 0;
            var z1 = new TPoint3D();
            var z2 = new TPoint3D();
            OpenGL.glGetDoublev(OpenGL.GL_PROJECTION_MATRIX, projection);
            OpenGL.glGetIntegerv(OpenGL.GL_VIEWPORT, viewport);
            OpenGL.gluProject(first.Origin.X, first.Origin.Y, first.Origin.Z, first.Transform, projection, viewport, ref z1.X, ref z1.Y, ref z1.Z);
            OpenGL.gluProject(second.Origin.X, second.Origin.Y, second.Origin.Z, second.Transform, projection, viewport, ref z2.X, ref z2.Y, ref z2.Z);
            ////double z1 = GLCanvas.Project(first, first.Origin).Z;
            ////double z2 = GLCanvas.Project(second, second.Origin).Z;
            return z1.Z.CompareTo(z2.Z);
            //var cubie1 = first as TPiece;
            //var cubie2 = second as TPiece;
            //if (cubie1 != null && cubie2 != null)
            //    return cubie1.State.CompareTo(cubie2.State);
            //return 0;
        }
    }
}
