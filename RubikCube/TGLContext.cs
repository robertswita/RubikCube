/**********************************************************
Autor: Robert Świta
Politechnika Koszalińska
Katedra Systemów Multimedialnych i Sztucznej inteligencji
***********************************************************/
using GA;
using RubikCube;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace TGL
{
    public unsafe class TGLContext
    {
        public TGLView View;
        IntPtr HDC;
        IntPtr HRC;
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
                    var pfd = Win32.PIXELFORMATDESCRIPTOR.CreateDefault();
                    var idx = Win32.ChoosePixelFormat(HDC, &pfd);
                    Win32.SetPixelFormat(HDC, idx, &pfd);
                    HRC = Win32.wglCreateContext(HDC);
                    Win32.wglMakeCurrent(HDC, HRC);
                    Gpu.Init();   // build the shared GPU-GA pipeline while this context is current
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
                OpenGL.ClearColor(backColor.R / 255f, backColor.G / 255f, backColor.B / 255f, 1);
                OpenGL.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                OpenGL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
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
            OpenGL.Begin(OpenGL.GL_QUADS);
            for (int i = 0; i < obj.Faces.Count; i++)
            {
                var v = obj.Vertices[obj.Faces[i]];
                if (i % 4 == 0)
                {
                    var color = obj.Colors[i / 4];
                    OpenGL.Color4ub(color.R, color.G, color.B, (byte)(255 * obj.Transparency));
                }
                v = Transform * v;
                if (v.Size == 2)
                    OpenGL.Vertex2f(v.X, v.Y);
                else
                    OpenGL.Vertex3f(v.X, v.Y, v.Z);
            }
            OpenGL.End();
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
                OpenGL.Enable(OpenGL.GL_DEPTH_TEST);
                //OpenGL.glDisable(OpenGL.GL_CULL_FACE);  // Disable face culling to show all faces
                //OpenGL.glPolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                //OpenGL.glEnable(OpenGL.GL_TEXTURE_2D);
                //OpenGL.glEnable(OpenGL.GL_LIGHTING);
                //OpenGL.glEnable(OpenGL.GL_COLOR_MATERIAL);
                //OpenGL.glEnable(OpenGL.GL_NORMALIZE);
                if (IsTransparencyOn)
                {
                    OpenGL.Enable(OpenGL.GL_BLEND);
                    OpenGL.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                }
                else
                    OpenGL.Disable(OpenGL.GL_BLEND);
                IsInited = true;
            }
        }

    };

}
