using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubikCube
{
    public class TMove
    {
        public int Slice;      // Which slice to rotate (0 to N-1)
        public int Plane;      // Which plane to rotate in: 0=XY, 1=XZ, 2=XW, 3=YZ, 4=YW, 5=ZW
        public int FixedAxis;  // Which of the 2 non-rotating axes is held constant (0 or 1)
        public int Angle;      // Rotation angle: 0=90°, 1=180°, 2=270°

        // Backward compatibility field (deprecated for 4D)
        public int Axis
        {
            get { return Plane; }  // Map to Plane for backward compatibility
            set { Plane = value; }
        }

        public static TMove Decode(int code)
        {
            var move = new TMove();
            move.Plane = code / 18;
            code = code % 18;
            move.FixedAxis = code / 9;
            code = code % 9;
            move.Slice = code / 3;
            move.Angle = code % 3;

            // Validate and clamp to current cube size
            if (move.Slice >= TRubikCube.N)
                move.Slice = TRubikCube.N - 1;  // Clamp to valid range
            if (move.Plane >= 6)
                move.Plane = 0;  // Should never happen, but be safe

            return move;
        }

        public int Encode()
        {
            return 18 * Plane + 9 * FixedAxis + 3 * Slice + Angle;
        }

        /// <summary>
        /// Get human-readable name for the plane
        /// </summary>
        public string PlaneName
        {
            get
            {
                string[] names = { "XY", "XZ", "XW", "YZ", "YW", "ZW" };
                return Plane >= 0 && Plane < 6 ? names[Plane] : "Unknown";
            }
        }

        public override string ToString()
        {
            return $"{PlaneName} plane, FixedAxis={FixedAxis}, Slice={Slice}, Angle={90 * (Angle + 1)}°";
        }
    }
}
