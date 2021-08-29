﻿/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej inteligencji
***********************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace TGL
{
    public class TGLContext
    {
        public TGLView View;
        IntPtr HDC;
        IntPtr HRC;
        Win32.PIXELFORMATDESCRIPTOR pfd;
        List<object> NameStack;
        public Rectangle Viewport;
        public TObject3D Root = new TObject3D();
        public IntPtr Handle
        {
            get
            {
                if (HRC == IntPtr.Zero)
                {
                    HDC = View.CreateGraphics().GetHdc();
                    pfd = new Win32.PIXELFORMATDESCRIPTOR();
                    var idx = Win32.ChoosePixelFormat(HDC, pfd);
                    Win32.SetPixelFormat(HDC, idx, pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);
                }
                return HRC;
            }
        }

        internal void DrawView()
        {
            if (Handle != IntPtr.Zero)
            {
                Viewport = View.ClientRectangle;
                var backColor = View.BackColor;
                Win32.wglMakeCurrent(HDC, HRC);
                OpenGL.glClearColor(backColor.R / 255f, backColor.G / 255f, backColor.B / 255f, 1);
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                OpenGL.glViewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
                Init();
                DrawScene();
                Win32.SwapBuffers(HDC);
            }
        }

        void DrawScene()
        {
            OpenGL.glLoadIdentity();
            DrawObject(Root);
        }

        //TObject3DComparer ZOrderComparer = new TObject3DComparer();
        protected void DrawObject(TObject3D obj)
        {
            OpenGL.glPushMatrix();
            OpenGL.glMultMatrixd(obj.Transform);
            //var childrenList = new List<TObject3D>(obj.Children);
            //childrenList.Sort(ZOrderComparer);
            for (int i = 0; i < obj.Children.Count; i++)
                DrawObject(obj.Children[i]);
            //var alpha = obj.Transform[0] > 0.1 && obj.Transform[5] > 0.1 && obj.Transform[10] > 0.1 ? 1 : 0.1;
            var alpha = obj.Transparent ? 0.1: 1;
            if (obj.Selected)
                alpha = 0.5;
            if (alpha < 0.5 && IsTransparencyOn)
                OpenGL.glDisable(OpenGL.GL_DEPTH_TEST);
            else
                OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
            OpenGL.glBegin(OpenGL.GL_TRIANGLES);
            for (int i = 0; i < obj.Faces.Count; i++)
            {
                var vertex = obj.Vertices[obj.Faces[i]];
                if (i % 6 == 0)
                    OpenGL.glColor4d(vertex.X, vertex.Y, vertex.Z, alpha);
                OpenGL.glVertex3d(vertex.X, vertex.Y, vertex.Z);
            }
            OpenGL.glEnd();
            OpenGL.glPopMatrix();
        }

        //void gluPickMatrix(double x, double y, double w, double h)
        //{
        //    /* Translate and scale the picked region to the entire window */
        //    //OpenGL.glTranslated((Viewport.Width - 2 * x) / w, (Viewport.Height - 2 * y) / h, 0);
        //    //OpenGL.glScaled(Viewport.Width / w, Viewport.Height / h, 1.0);
        //    OpenGL.glScaled(Viewport.Width / w, Viewport.Height / h, 1.0);
        //    OpenGL.glTranslated(1 - 2 * x / Viewport.Width, 1 - 2 * y / Viewport.Height, 0);
        //}

        bool _IsTransparencyOn;
        public bool IsTransparencyOn
        {
            get { return _IsTransparencyOn; }
            set
            {
                _IsTransparencyOn = value;
                IsInited = false;
                View.Invalidate();
            }
        }
        public bool IsInited;
        void Init()
        {
            if (!IsInited)
            {
                OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                OpenGL.glEnable(OpenGL.GL_CULL_FACE);
                //OpenGL.glPolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                //OpenGL.glEnable(OpenGL.GL_TEXTURE_2D);
                //OpenGL.glEnable(OpenGL.GL_LIGHTING);
                //OpenGL.glEnable(OpenGL.GL_COLOR_MATERIAL);
                //OpenGL.glEnable(OpenGL.GL_NORMALIZE);
                if (IsTransparencyOn)
                {
                    OpenGL.glEnable(OpenGL.GL_BLEND);
                    OpenGL.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                }
                else
                    OpenGL.glDisable(OpenGL.GL_BLEND);
                IsInited = true;
            }
        }

    };

}
