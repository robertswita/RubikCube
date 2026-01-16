using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var move = new TMove();
            var coords = SizeMatrix.Index2Coords(code);
            move.Axis = (int)coords.X;
            move.Slice = (int)coords.Y;
            move.Plane = (int)coords.Z;
            move.Angle = (int)coords.W;
            //move.Slice = code / 72;
            //code -= move.Slice * 72;
            //move.Axis = code / 18;
            //code -= move.Axis * 18;
            //move.Plane = code / 3;
            //code -= move.Plane * 3;
            //move.Angle = code;
            return move;
        }

        public int Encode()
        {
            //var tmp = ((TRubikCube.Size * Axis + Slice) * TAffine.Planes.Length + Plane) * 3 + Angle;
            //var tmp = ((TAffine.N * Slice + Axis) * TAffine.Planes.Length + Plane) * 3 + Angle;
            return SizeMatrix.Coords2Index(new TVector(Axis, Slice, Plane, Angle));
            //return ((TRubikCube.Size * Axis + Slice) * TAffine.Planes.Length + Plane) * 3 + Angle;
            //return Angle + 3 * (Plane + TAffine.Planes.Length * (Axis + 4 * Slice));
            //72 * Slice + 18 * Axis + 3 * Plane + Angle;
        }

    }
}
