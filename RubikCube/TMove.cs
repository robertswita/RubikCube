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
        public static TDims DimsSizes = new TDims(1 << 2, 1 << 5, 1 << 3, 1 << 22);

        public int[] GetPlaneAxes()
        {
            return TAffine.Planes[Plane];
        }

        public static TMove Decode(int code)
        {
            var move = new TMove();
            var coords = DimsSizes.BitIndexToCoords(code);
            move.Angle = coords[0];
            move.Plane = coords[1];
            move.Axis = coords[2];
            move.Slice = coords[3];
            return move;
        }

        public int Encode()
        {
            return DimsSizes.CoordsToBitIndex(new int[] { Angle, Plane, Axis, Slice });
        }

        // Inverse move: keep axis/plane/slice, negate the angle in quarter-turns (1<->3, 2 and 0 stay).
        public static int GetRevCode(int code)
        {
            int oldAngle = code & 3;
            int newAngle = (4 - oldAngle) & 3;
            return (code & ~3) | newAngle;
        }

        //public bool IsValid
        //{
        //    get
        //    {
        //        if (Axis >= TAffine.N) return false;
        //        if (Plane >= TAffine.Planes.Length) return false;
        //        if (TAffine.Planes[Plane][0] == Axis || TAffine.Planes[Plane][1] == Axis)
        //            ;
        //        return true;
        //    }
        //}

    }
}
