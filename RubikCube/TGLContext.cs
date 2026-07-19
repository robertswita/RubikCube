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
        public TScene Scene;
        public TAffine Transform = new TAffine();

        public IntPtr Handle => HRC;

        public void Create()
        {
            HDC = View.CreateGraphics().GetHdc();
            var pfd = Win32.PIXELFORMATDESCRIPTOR.CreateDefault();
            var idx = Win32.ChoosePixelFormat(HDC, &pfd);
            Win32.SetPixelFormat(HDC, idx, &pfd);
            HRC = Win32.wglCreateContext(HDC);
            Win32.wglMakeCurrent(HDC, HRC);
            Gpu.Init();   // buduj wspólny pipeline GPU-GA, póki kontekst jest bieżący
        }

        public void Release()
        {
            Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Win32.wglDeleteContext(HRC);
        }

        internal void DrawView()
        {
            //if (Handle != IntPtr.Zero)
            {
                Viewport = View.ClientRectangle;
                Win32.wglMakeCurrent(HDC, HRC);
                Init();
                //SetupCamera();
                // Gather the scene: per-instance world transform + alpha (unsolved cubies are translucent).
                // Gpu.RenderScene owns the WBOIT passes (opaque -> transparent -> composite) and the clears.
                var instances = new List<TAffine>();
                var alphas = new List<float>();
                if (Scene != null)
                {
                    GatherInstances(Scene.Root, new TAffine(), instances, alphas);
                    Gpu.UpdateLights(Scene.Lights);   // upload the scene lights (UBO) before drawing
                    Gpu.RenderScene(instances, alphas, View.BackColor, Viewport.Width, Viewport.Height);
                }
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

        // Gathers the accumulated ND world transform of every face-bearing node (all cubies share one
        // base mesh). TGLContext owns the scene graph; Gpu owns the render pipeline (Gpu.DrawCube).
        void GatherInstances(TShape obj, TAffine parent, List<TAffine> list, List<float> alphas)
        {
            var world = parent * obj.Transform;
            if (obj.Faces.Count > 0) { list.Add(world); alphas.Add(obj.Transparency); }
            foreach (var child in obj.Children)
                GatherInstances(child, world, list, alphas);
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
                //OpenGL.Enable(OpenGL.GL_CULL_FACE);  // Disable face culling to show all faces
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
