using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubikCube
{
    // One orbit of cubies (a "cluster") in a SPECIFIC cube instance. Membership is geometry (the orbit is
    // fixed for a given N/SIZE), but Cubies are THIS cube's TCubie objects -- meaningful in the debugger
    // (StartIndex/State/position), not opaque indices. Built per cube: the scratch ctor groups by orbit,
    // the copy ctor mirrors by StartIndex. Size == Cubies.Count (kept as a field for the MaxClusterSize scan).
    public class TCluster
    {
        public static int MaxSize = 12;
        public List<TCubie> Cubies = new List<TCubie>();
        // Number of this cluster's cubies not yet solved, counted fresh against the cube's CURRENT state.
        // Always recomputed (cheap loop, even for the largest clusters) — never cached, so callers never see
        // a stale pre-move count.
        int scrambledCount;
        public int ScrambledCount
        {
            get
            {
                if (scrambledCount < 0)
                {
                    scrambledCount = 0;
                    foreach (var c in Cubies) if (c.State != 0) scrambledCount++;
                }
                return scrambledCount;
            }
            set => scrambledCount = value;
        }
    }
}
