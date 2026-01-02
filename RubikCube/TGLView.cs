using System;
using System.Windows.Forms;

namespace TGL
{
    public partial class TGLView : UserControl
    {
        // W konstruktorze kontrolki tworzymy klasę kontekstu TGLContext i przypisujemy jej uchwyt zwrotny do kontrolki. 
        // Polecenie new alokuje obiekt na stosie zarządzanym przez Garbage Collector. 
        // Takiego obiektu nie musimy sami dealokować.
        public TGLContext Context = new TGLContext();
        public TGLView()
        {
            InitializeComponent();
            //ResizeRedraw = true;
            SetStyle(ControlStyles.Opaque, true);
            Context.View = this;
        }
        // Kontrolka rysuje swoje wnętrze w odpowiedzi na komunikat okna WM_PAINT. 
        // Nadpiszemy handler tego komunikatu - OnPaint i wywołamy w nim metodę DrawScene kontekstu:
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Context.Handle != IntPtr.Zero)
                Context.DrawView();
        }

        protected override void OnResize(EventArgs e)
        {
            if (Context.Handle != IntPtr.Zero)
                Context.Resize();
        }

        public void Recreate()
        {
            RecreateHandle();
        }

        // Kontekst DC kontrolki będzie kontekstem prywatnym, tworzonym razem z oknem kontrolki
        // i zwalnianym dopiero przy niszczeniu okna. Trzeba ustalić styl klasy okna na CS_OWNDC.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= (int)Win32.CS_OWNDC;
                return cp;
            }
        }
    };

}
