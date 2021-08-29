using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RubikCube
{
    public partial class TSpaceForm : Form
    {
        public List<List<TMove>> Solutions;
        public TSpaceForm()
        {
            InitializeComponent();
        }

        private void TSpaceForm_Load(object sender, EventArgs e)
        {
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
        }

        private void TSpaceForm_Resize(object sender, EventArgs e)
        {
            Pen pen = new Pen(Color.Red);
            var bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            var gc = Graphics.FromImage(bmp);
            for (int i = 0; i < Solutions.Count; i++)
            {
                if (i != 0) pen.Color = Color.Blue;
                var solution = Solutions[i];
                Point actPos = new Point(Width / 2, Height / 2);
                var pts = new List<Point>();
                pts.Add(actPos);
                for (int j = 0; j < solution.Count; j++)
                {
                    var move = solution[j];
                    var scale = 20 * (move.Angle + 1);
                    Size v = new Size((move.Slice - 1) * scale, (move.Axis - 1) * scale);
                    actPos += v;
                    pts.Add(actPos);
                }
                gc.DrawLines(pen, pts.ToArray());
            }
            pictureBox1.Image = bmp;
        }
    }
}
