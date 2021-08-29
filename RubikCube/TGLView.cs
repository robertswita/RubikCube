using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGL
{
    public partial class TGLView : UserControl
    {
        public TGLContext Context = new TGLContext();
        public TGLView()
        {
            InitializeComponent();
            ResizeRedraw = true;
            Context.View = this;
            if (Context.Handle != IntPtr.Zero)
                SetStyle(ControlStyles.Opaque, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Context.DrawView();
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= (int)Win32.CS_OWNDC;
                return cp;
            }
        }

    }
}
