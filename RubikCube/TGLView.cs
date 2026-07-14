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
            ResizeRedraw = true;
            SetStyle(ControlStyles.Opaque, true);
            Context.View = this;
        }
        // Kontrolka rysuje swoje wnętrze w odpowiedzi na komunikat okna WM_PAINT. 
        // Nadpiszemy handler tego komunikatu - OnPaint i wywołamy w nim metodę DrawScene kontekstu:
        // Kontekst GL tworzymy dokładnie wtedy, gdy powstaje uchwyt okna - symetrycznie do OnHandleDestroyed,
        // zamiast leniwie w OnPaint. W trybie projektanta pomijamy: designer tworzy uchwyt kontrolki, a
        // kontekst GL + Gpu.Init w jego procesie wywaliłyby ładowanie formatki.
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            //if (!DesignMode)
                Context.Create();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //if (DesignMode)
            //{
            //    e.Graphics.Clear(BackColor);
            //    return;
            //}
            Context.DrawView();   // kontekst istnieje od OnHandleCreated; DrawView i tak strzeże HRC
        }

        // Kontekst GL jest przywiązany do prywatnego DC tej kontrolki (CS_OWNDC), więc zwalniamy go
        // dokładnie wtedy, gdy ginie uchwyt okna - przy zamknięciu formy i przy RecreateHandle.
        // Release() zeruje HRC, więc następny paint leniwie odbuduje świeży kontekst na nowym DC.
        // Dzięki temu całe GL zostaje w TGLContext, a formatka nie dotyka funkcji GL/Win32.
        protected override void OnHandleDestroyed(EventArgs e)
        {
            Context.Release();
            base.OnHandleDestroyed(e);
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
