using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rubik;

namespace TGL
{
    public class TCubik : TObject3D
    {
        public int X
        {
            get
            {
                return (int) Math.Round(Origin.X + RubikCube.M);
            }
        }

        public int Y
        {
            get
            {
                return (int) Math.Round(Origin.Y + RubikCube.M);
            }
        }

        public int Z
        {
            get
            {
                return (int) Math.Round(Origin.Z + RubikCube.M);
            }
        }

        public int State;
        public void UpdateState()
        {
            //State = 0;
            //for (int i = 0; i < 2; i++)
            //{
            //    for (int j = 0; j < 3; j++)
            //    {
            //        var bit = Transform[4 * i + j];
            //        if (bit > 0.1)
            //        {
            //            State |= 1 << j;
            //            break;
            //        }
            //        if (bit < -0.1)
            //        {
            //            State |= 1 << j;
            //            State = ~State & 7;
            //            break;
            //        }
            //    }
            //    if (i < 1)
            //        State <<= 3;
            //}

            var gamma = GetAngle(Transform[0], Transform[1]);

            var alpha = 0;
            var beta = 0;

            if (gamma == 0)
            {
                alpha = GetAngle(Transform[5], -Transform[9]);
                beta = GetAngle(Transform[0], -Transform[2]);
            } else
            {
                alpha = GetAngle(Transform[10], Transform[6]);
            }

            State = gamma << 4 | beta << 2 | alpha; 
        }

        public int GetAngle(double cosA, double sinA)
        {
            if (cosA > 0.1) return 0;
            if (sinA > 0.1) return 1;
            if (cosA < -0.1) return 2;
            if (sinA < -0.1) return 3;

            return 0;
        }

        public TCubik(){
            var lbn = new TPoint3D(-1, -1, -1);
            var rtf = new TPoint3D(1, 1, 1);

            for (int i = 0; i < 8; i++)
            {
                var point = lbn;
                if ((i & 1) == 1)
                {
                    point.X = rtf.X;
                }
                if ((i & 2) == 2)
                {
                    point.Y = rtf.Y;
                }
                if((i & 4) == 4)
                {
                    point.Z = rtf.Z;
                }

                Vertices.Add(point);
            }

            for (int dim = 0; dim < 3; dim++)
            {
                var axis1 = 1<<dim;
                var axis2 = 1<<(dim + 1)%3;

                Faces.Add(axis1);
                Faces.Add(axis2);
                Faces.Add(0);

                Faces.Add(axis1 + axis2);
                Faces.Add(axis2);
                Faces.Add(axis1);
            }

            for (int i = Faces.Count - 1; i >= 0 ; i--)
            {
                Faces.Add(7-Faces[i]);
            }
        }
    }
}
