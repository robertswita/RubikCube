using System;
using System.Windows.Forms;
using OpenTK.WinForms;
using OpenTK.Graphics.OpenGL4;

namespace TGL
{
    public partial class TGLView : GLControl
    {
        public TGLContext Context = new TGLContext();

        public TGLView()
        {
            InitializeComponent();
            ResizeRedraw = true;
            Context.View = this;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            MakeCurrent();
            Context.DrawView();
            SwapBuffers();
        }

        public void Recreate()
        {
            // Not needed with GLControl
        }
    }
}
