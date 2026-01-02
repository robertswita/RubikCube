using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGL;

namespace RubikCube
{
    public class TMove
    {
        public int Slice;
        public int Plane; 
        public int Angle;
        static int[,] AxesIndices = new int[,] { { 0, 1 }, { 0, 2 }, { 0, 3 }, { 1, 2 }, { 1, 3 }, { 2, 3 } };
        public int[] GetAxes() { return new int[] { AxesIndices[Plane, 0], AxesIndices[Plane, 1] }; }

//public void Assign(TMove move)
//{
//    Slice = move.Slice;
//    Axis = move.Axis;
//    Angle = move.Angle;
//}

// N = number of axes (4 for XYZW)
        (int i, int j) FromIndex(int index, int N = 4)
        {
            int i = 0;
            int rowCount = N - 1 - i; // elements in row 0 of upper triangle

            int k = index; // we’ll consume this as we move down rows

            while (k >= rowCount)
            {
                k -= rowCount;
                i++;
                rowCount = N - 1 - i;
            }

            int j = i + 1 + k;
            return (i, j);
        }

        public static TMove Decode(int code)
        {
            var move = new TMove();
            move.Slice = code / 18;
            code = code % 18;
            move.Plane = code / 3;
            move.Angle = code % 3;
            return move;
        }

        public int Encode()
        {
            return 18 * Slice + 3 * Plane + Angle;
        }

    }
}
