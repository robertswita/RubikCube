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
        public Rectangle Viewport;
        public TShape Root = new TShape();
        public TAffine Transform = new TAffine();
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
                //SetupCamera();
                DrawScene();
                Win32.SwapBuffers(HDC);
            }
        }

        //void SetupCamera()
        //{
        //    // Set up projection matrix
        //    OpenGL.glMatrixMode(OpenGL.GL_PROJECTION);
        //    OpenGL.glLoadIdentity();

        //    // Use perspective projection for better 3D viewing
        //    double aspect = Viewport.Width / (double)Viewport.Height;
        //    OpenGL.gluPerspective(45.0, aspect, 0.1, 100.0);

        //    // Position camera to view the scene
        //    OpenGL.gluLookAt(
        //        0, 0, 8,      // Camera position (looking from positive Z)
        //        0, 0, 0,      // Look at origin
        //        0, 1, 0       // Up vector
        //    );

        //    // Switch back to modelview matrix for object transformations
        //    OpenGL.glMatrixMode(OpenGL.GL_MODELVIEW);
        //}

        void DrawScene()
        {
            Transform = new TAffine();
            DrawObject(Root);
        }

        TObject3DComparer ZOrderComparer = new TObject3DComparer();
        protected void DrawObject(TShape obj)
        {
            var transform = Transform.Clone();
            Transform = Transform * obj.Transform;
            obj.WorldTransform = Transform.Clone();

            var childrenList = new List<TShape>(obj.Children);
            if (obj.WorldTransform.Origin.Size > 2)
                childrenList.Sort(ZOrderComparer);
            for (int i = 0; i < obj.Children.Count; i++)
                DrawObject(childrenList[i]);
            OpenGL.glBegin(OpenGL.GL_QUADS);
            for (int i = 0; i < obj.Faces.Count; i++)
            {
                var v = obj.Vertices[obj.Faces[i]];
                if (i % 4 == 0)
                {
                    var color = obj.Colors[i / 4];
                    OpenGL.glColor4ub(color.R, color.G, color.B, (byte)(255 * obj.Transparency));
                }
                v = Transform * v;
                if (v.Size == 2)
                    OpenGL.glVertex2f(v.X, v.Y);
                else
                    OpenGL.glVertex3f(v.X, v.Y, v.Z);
            }
            OpenGL.glEnd();
            Transform = transform;
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
                //OpenGL.glDisable(OpenGL.GL_CULL_FACE);  // Disable face culling to show all faces
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
