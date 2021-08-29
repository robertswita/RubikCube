using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubikCube
{
    public class TMove
    {
        public int Slice;
        public int Axis; 
        public int Angle;

        //public void Assign(TMove move)
        //{
        //    Slice = move.Slice;
        //    Axis = move.Axis;
        //    Angle = move.Angle;
        //}

        public static TMove Decode(int code)
        {
            var move = new TMove();
            move.Slice = code / 9;
            code = code % 9;
            move.Axis = code / 3;
            move.Angle = code % 3;
            return move;
        }

        public int Encode()
        {
            return 9 * Slice + 3 * Axis + Angle;
        }

    }
}
