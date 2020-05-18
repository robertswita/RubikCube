using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rubik;

namespace TGL
{
    public class TMove
    {
        public int Axis;
        public int SegNo;
        public int Angle;
        public int Encode()
        {
            var code = 3 * RubikCube.N * Axis + 3 * SegNo + Angle;
            return code;
        }

        public void Decode(int code)
        {
            var size = 3 * RubikCube.N;
            Axis = code / size;
            code %= size;
            SegNo =  code / 3;
            Angle = code % 3;
        }
    }
}
