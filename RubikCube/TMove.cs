using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !MAUI
using System.Windows.Forms.DataVisualization.Charting;
#endif
using TGL;

namespace RubikCube
{
    public class TMove
    {
        public int Axis;
        public int Slice;
        public int Plane; 
        public int Angle;
        public static TMatrix SizeMatrix;
        public static void UpdateSizeMatrix()
        {
            var sizes = new int[] { TAffine.N, TRubikCube.Size, TAffine.Planes.Length, 3 };
            SizeMatrix = new TMatrix(sizes[0] * sizes[1], sizes[2] * sizes[3]);
            SizeMatrix.DimSizes = sizes;
        }
        public int[] GetPlaneAxes()
        {
            return TAffine.Planes[Plane];
        }

        public static TMove Decode(int code)
        {
            var coords = SizeMatrix.Index2Coords(code);
            var move = new TMove();
            move.Axis = (int)coords.X;
            move.Slice = (int)coords.Y;
            move.Plane = (int)coords.Z;
            move.Angle = (int)coords.W;
            return move;
        }

        public int Encode()
        {
            //var tmp = ((TRubikCube.Size * Axis + Slice) * TAffine.Planes.Length + Plane) * 3 + Angle;
            return SizeMatrix.Coords2Index(new int[] { Axis, Slice, Plane, Angle });
        }

        public bool IsValid
        {
            get
            {
                if (Axis >= TAffine.N) return false;
                if (Plane >= TAffine.Planes.Length) return false;
                if (TAffine.Planes[Plane][0] == Axis || TAffine.Planes[Plane][1] == Axis)
                    ;
                return true;
            }
        }

    }
}
