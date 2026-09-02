using GA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TGL;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace RubikCube
{
    // The SEARCH over a cube. Split out of TRubikCube so the cube stays pure geometry (Cubies + Turn + the
    // Clusters structure) that the window holds for drawing, while the solver owns EVERYTHING about "what we
    // are solving": the GA decision loop + its per-solve state (accept baseline, stall, running best, gens,
    // compute time), the TARGET (active cubie/cluster, solved sets), and the whole seed generator. A cube has
    // no "active cubie". Created FRESH right before a solve (construction IS the reset); binds one cube via Cube.
    public class TRubikSolver
    {
        public readonly TRubikCube Cube;

        // Coherence LATCH THRESHOLD + endgame NORMALIZER base (uploaded to GPU uniform 7). Shared config, never
        // mutated (a search knob, not GPU state): the latch flips Coherent 0->1 when the residual reaches it, and
        // the coherence factor normalizes by Floor*N. Read by name (TRubikSolver.Floor) from Gpu and the batch report.
        public static uint Floor = 4;

        public float Score;            // fitness of the currently ACCEPTED config -- the accept-test baseline
        public uint Coherent;          // coherence LATCH (per-solve): 0 = count-peel descent, 1 = endgame coherence. Passed into every GPU call so the GA and ScoreCube use the same value. Reset to 0 per cluster in NextCluster.
        uint ScoreLadder;              // ladder value of the accepted config -- the coherence accept baseline (GPU-computed)
        //public int Scrambled;          // active-cluster residual count (read by the UI)
        public TRubikGenome Best;      // last accepted specimen (the displayed/applied state)
        public int IterationsCount;    // total GA generations across this solve (accumulated by ExecuteGA)
        int Stall;                     // consecutive non-improving GA runs -- the sideways/latch timer
        // THE TARGET -- what this solve is working on (was on the cube; a cube has no active cubie).
        public TCubie ActiveCubie;                             // the cubie the current GA run is trying to solve
        public TCluster ActiveCluster;                         // its cluster (a reference into Cube.Clusters)
        public List<TCubie> SolvedCubies = new List<TCubie>(); // clusters solved so far this solve
        public List<TCubie> SolvedInSlices = new List<TCubie>();// Solved cubies the active cluster's moves can TOUCH (the only ones the GPU break-check must replay); computed once per cluster
        // Accumulated GA COMPUTE time (GPU work + orchestration) across all GetNextMoves steps of this
        // solve -- the piece the generation count hides: coherence-on steps cost far more than count-peel
        // steps. Read by the form for display and by the batch for per-run timing.
        public readonly Stopwatch ComputeTime = new Stopwatch();
        // Trajectory logging (off by default � zero cost when nobody asks for it). When true, every accepted
        // step in GetNextMoves appends one line: gen / scrambled / structure / bestFit / score / moves / stepMs.
        public bool TrackTrajectory;
        public readonly List<string> Trajectory = new List<string>();
        double lastTrajectoryMs;   // ComputeTime.Elapsed at the last recorded step (for the per-step stepMs delta)

        public TRubikSolver(TRubikCube cube)
        {
            Cube = cube;
            ActiveCluster = Cube.Clusters[0];
        }

        // Packs cluster's cubie orientation (from its Transform) into the shader's uint format: per row
        // r the field is (col << 1) | sign, where col is the single non-zero entry of that
        // row and sign is its sign. Identity row r -> field r << 1, so a solved cubie packs to the identity
        // value and cubieL1 == 0. Axis numbering matches the CPU (row/col = CPU axes), consistent with
        // getStartCoordinate after the axis-order fix.
        public uint[] PackCluster()
        {
            var packed = new uint[ActiveCluster.Cubies.Count];
            for (int id = 0; id < ActiveCluster.Cubies.Count; id++)
                packed[id] = ActiveCluster.Cubies[id].Transform.OrthoPack();
            return packed;
        }

        public int TotalScrambled
        {
            get
            {
                var scrambled = 0;
                foreach (var cubie in Cube.Cubies)
                    if (cubie.State != 0) scrambled++;
                return scrambled;
            }
        }

        void LogStep()
        {
            double nowMs = ComputeTime.Elapsed.TotalMilliseconds;
            double stepMs = nowMs - lastTrajectoryMs;
            lastTrajectoryMs = nowMs;
            Trajectory.Add($"{IterationsCount}\t{ActiveCluster.ScrambledCount}\t{TRubikGenome.DescribeStructure(Best.Structure, TAffine.N)}\t{Best.Fitness:F4}\t{Score:F4}\t{lastMovesApplied}\t{stepMs:F1}");
        }

        // ONE GA decision step -- Runs in BOTH phases
        // (the count-peel descent AND the post-floor coherence climb). It:
        //   1. re-selects the target cubie -- UNSOLVED one of the active cluster -- which is what lets
        //      successive GA runs during a stall attack the SAME configuration from different cubies;
        //   2. advances to the next cluster when the current one is solved (descent phase, base floor);
        //   3. runs one GA and applies the STALL accept test (accept strictly-better always, equal only after
        //      StallLimit consecutive non-improving runs -- a gated sideways step off a plateau).
        // Returns the accepted moves (empty if none, or if the whole cube is solved -> Best == null).
        public List<TMove> GetNextMoves()
        {
            ComputeTime.Start();                       // accumulate GA compute time (paired Stop before EVERY return)
            var moves = new List<TMove>();
            // Log the OUTCOME of the previous accepted step: by now Cube already reflects it (Turn applied
            // immediately in batch mode; the animator finished turning it in interactive mode). Must run
            // BEFORE NextCluster() below � Scrambled still reads the PREVIOUS cluster here, and if that
            // cluster just completed (Score==0), CountScrambled() correctly returns 0 with no special case.
            if (TrackTrajectory && Best != null)
                LogStep();
            if (Score == 0)                            // current cluster solved -> advance
                NextCluster();
            if (ActiveCubie == null) // whole cube solved
            {
                Best = null;
                ComputeTime.Stop();
                return moves;
            }
            // COHERENCE LATCH (no walk). Two phases: during the DESCENT coherence is OFF, so the evaluator is the
            // bare count and the peel drives the residual down fast; once the active cluster's residual reaches the
            // floor (base 4), latch coherence ON for the ENDGAME and leave it on for the rest of the cluster. The
            // floor is now ONLY this latch threshold -- it never walks. Count-cancellation lives inside the metric
            // (the /scrambled in the coherence factor), so once latched the fitness is the bare coherence factor
            // for the whole endgame (0 < scrambled <= G) and the yardstick never jumps. The latch itself DOES
            // switch metrics (count -> factor), so Score (the accept-test reference) is re-measured once, at the
            // flip. Reset to descent (coherence OFF, floor 4) per cluster in NextCluster. Called after the moves
            // are applied, when the cube IS the state just reached.
            if (Coherent == 0 && ActiveCluster.ScrambledCount <= (int)Floor) // && Stall > (int)Floor)
            {
                Coherent = 1;
                Score = Gpu.ScoreCube(this);
                ScoreLadder = Gpu.LastScoreLadder;
                Stall = 0;
            }
            // advance target: DETERMINISTIC round-robin --  // the next UNSOLVED cubie of the cluster after the
            var cluster = ActiveCluster.Cubies;    // current one (wrapping). Cluster order is stable
            int cur = cluster.IndexOf(ActiveCubie);// (Cubies array order), so successive steps cycle
            for (int step = 1; step <= cluster.Count; step++)// through all cubies once per pass -- no coupon-
            {                                                // collector waste of the old random pick, and it
                var cand = cluster[(cur + step) % cluster.Count];  // pairs exactly with StallLimit = #unsolved.
                if (cand.State != 0) { ActiveCubie = cand; break; }
            }
            TRubikGenome.RubikCube = Cube;
            var candidate = Gpu.ExecuteGA(this, out int gens);   // fresh GA result; Best (the displayed/accepted state) advances ONLY on accept
            IterationsCount += gens;               // accumulate here (ExecuteGA reports gens instead of writing us)
            Stall++;
            // Accept: DESCENT compares the (fine) peel Fitness; COHERENT compares the STRUCTURE ladder so structurally-
            // equal configs TIE -> sideways fires, which is what escapes a peel LOCAL MINIMUM like the mono-twist (the move
            // mono[28] -> 2-block[20] raises the count but lowers the structure, so the ladder accepts it). Fitness < 1
            // rejects a Solved-breaker / no-op (Fitness is normalised < 1; >= 1 means a break or a stand-still). GA
            // SELECTION is separate: the sort breeds by Fitness, which in COHERENT is the PURE coherence factor.
            // COHERENT: STRICT ladder decrease. The ladder now folds merge-readiness (orientRaw) UNDER the structure,
            // so a structure-preserving rigid block spin TIES (same Ladder) and is rejected; only a move that improves
            // structure OR merge-readiness strictly lowers it and is taken. STALL is the safety valve: after the limit,
            // allow an equal-Ladder sideways so a flat merge-gradient corridor cannot hard-stall.
            bool accept = Coherent == 0
                ? candidate.Fitness < Score || candidate.Fitness == Score && Stall >= TGA<TRubikGenome>.StallLimit
                : candidate.Fitness < 1f && candidate.Fitness != Best.Fitness
                      && (candidate.Ladder <= ScoreLadder
                          || Stall >= TGA<TRubikGenome>.StallLimit && candidate.Ladder <= ScoreLadder);
            if (accept)
            //if (candidate.Fitness < Score || candidate.Fitness == Score && Stall >= TGA<TRubikGenome>.StallLimit)
            {
                Best = candidate;   // ACCEPTED -> the displayed/state Best advances (untouched during Stall, so the label no longer flickers to rejected candidates)
                Best.Correct();
                for (int i = 0; i < Best.BestMovesCount; i++)
                    moves.Add(TMove.Decode((int)Best.Genes[i]));
                Stall = 0;
                Score = Best.Fitness;
                if (Coherent != 0) ScoreLadder = Best.Ladder;   // advance the coherent structure baseline
                ActiveCluster.ScrambledCount = -1;
            }
            else if (Best == null) Best = candidate;   // first run before any accept: keep Best non-null (null == "whole cube solved" to Form1)
            ComputeTime.Stop();
            lastMovesApplied = moves.Count;
            return moves;
        }

        public void NextCluster()
        {
            Coherent = 0;        // new cluster -> descent
            Stall = 0;           // fresh stall counter -> the new cluster's peel starts un-stalled (latch is per-cluster)
            if (ActiveCubie != null)
                SolvedCubies.AddRange(ActiveCluster.Cubies);
            ActiveCubie = null;
            var minDist = int.MaxValue;
            foreach (var cubie in Cube.Cubies)
            {
                if (cubie.State == 0) continue;
                if (cubie.ClusterIndex < minDist)
                {
                    minDist = cubie.ClusterIndex;
                    ActiveCubie = cubie;
                }
            }
            if (ActiveCubie == null)
            {
                //ActiveCluster = null;
                SolvedCubies.Clear();
            }
            else
            {
                ActiveCluster = Cube.Clusters[ActiveCubie.ClusterIndex - 1];   // ClusterIndex is the 1-based rank; Clusters is in rank order                                                                                                                                                             
                ActiveCluster.ScrambledCount = -1;
                RebuildClusterMoves(); // The cluster's move set (lazy)
                ComputeSolvedInSlices(); // Solved-in-slices break-check set.
                Score = Gpu.ScoreCube(this);
            }
        }

        // The active cluster's move set (every move that turns a layer touching it) -- geometry, so it comes from
        // the cube, keyed on the target cubie. Cached; RebuildClusterMoves drops it when the target cluster changes.
        List<int> clusterMoves;
        public List<int> ClusterMoves => clusterMoves ??= Cube.GetClusterMoves(ActiveCubie);
        void RebuildClusterMoves() => clusterMoves = null;

        // The Solved cubies that the active cluster's free moves can actually TOUCH: those with >=1 coord in
        // validSlices (the cluster's coord values + mirrors = exactly the layers ClusterMoves turns). A solved cubie
        // outside every valid slice is never in a turned layer -> can never break, so the GPU break-check may SKIP it.
        void ComputeSolvedInSlices()
        {
            var pos = ActiveCubie.Position;
            var validSlices = new bool[TRubikCube.Size];
            for (int coord = 0; coord < TAffine.N; coord++)
            {
                validSlices[pos[coord]] = true;
                validSlices[TRubikCube.Size - 1 - pos[coord]] = true;
            }
            SolvedInSlices = new List<TCubie>();
            foreach (var s in SolvedCubies)
            {
                var sp = s.Position;
                for (int coord = 0; coord < TAffine.N; coord++)
                    if (validSlices[sp[coord]]) { SolvedInSlices.Add(s); break; }
            }
        }

        public class TSolveResult
        {
            public int Iterations;
            public bool Hung;
            public double ComputeMs;
            public List<string> Trajectory;
        }

        // Runs THIS solver's cube to completion headless (no animation), applying every accepted move
        // immediately via Turn. Drives the same GetNextMoves() decision core as the interactive Solve(), so
        // batch measurement can never drift from what the interactive path does. The caller owns both the
        // cube's initial state (scrambled or not) and the solver's construction. Hung=true and
        // Iterations==cap if the run fails to close within cap generations.
        public TSolveResult RunHeadless(int cap)
        {
            while (true)
            {
                var moves = GetNextMoves();
                if (Best == null)
                    return new TSolveResult { Iterations = IterationsCount, ComputeMs = ComputeTime.Elapsed.TotalMilliseconds, Trajectory = Trajectory };

                foreach (var m in moves) Cube.Turn(m);

                if (IterationsCount >= cap)
                    return new TSolveResult { Iterations = cap, Hung = true, ComputeMs = ComputeTime.Elapsed.TotalMilliseconds, Trajectory = Trajectory };
            }
        }

        // ============================ SEED GENERATOR (was on the cube) ============================
        // Heuristic decompositions of the ACTIVE cubie into move sequences -- search material, not geometry.

        //static void Swap(ref int a, ref int b)
        //{
        //    var temp = a;
        //    a = b;
        //    b = temp;
        //}

        //static List<int[]> DoPermute(int[] nums, int start, int end)
        //{
        //    var result = new List<int[]>();
        //    if (start == end)
        //    {
        //        var perm = new int[nums.Length];
        //        Array.Copy(nums, perm, nums.Length);
        //        result.Add(perm);
        //    }
        //    else
        //        for (var i = start; i <= end; i++)
        //        {
        //            Swap(ref nums[start], ref nums[i]);
        //            result.AddRange(DoPermute(nums, start + 1, end));
        //            Swap(ref nums[start], ref nums[i]);
        //        }
        //    return result;
        //}

        public List<int> GetOrder()
        {
            var mask = new bool[TAffine.N, TAffine.N];
            var planes = new List<int>();
            for (int i = 0; i < TAffine.Planes.Length; i++)
                planes.Add(i);
            var caseCount = 1;
            var order = new List<int>();
            while (planes.Count > 0)
            {
                var pool = new List<int>();
                for (int i = 0; i < planes.Count; i++)
                {
                    var planeIdx = TAffine.Planes[planes[i]];
                    var n = planeIdx[0];
                    var m = planeIdx[1];
                    if (n == 0 || mask[m, n - 1] && mask[n, n - 1])
                        pool.Add(planes[i]);
                }
                caseCount *= pool.Count;
                var plane = pool[TChromosome.Rnd.Next(pool.Count)];
                planes.Remove(plane);
                var maskIdx = TAffine.Planes[plane];
                mask[maskIdx[1], maskIdx[0]] = true;
                order.Add(plane);
            }
            return order;
        }

        //bool NonStandard(int[] order)
        //{
        //    if (order == null) return false;
        //    var mask = new bool[TAffine.N, TAffine.N];
        //    for (int i = 0; i < order.Length; i++)
        //    {
        //        var planeIdx = TAffine.Planes[order[i]];
        //        var n = planeIdx[0];
        //        var m = planeIdx[1];
        //        mask[m, n] = true;
        //        if (n > 0 && (!mask[m, n - 1] || !mask[n, n - 1]))
        //            return true;
        //    }
        //    return false;
        //}

        // Moves-to-solve of an orientation under (plane ORDER, mixed-Givens mode): reduce plane by plane in the
        // given order[] with a per-plane strategy packed 2 bits per plane in modeMask (base-4): 0=pod-L, 1=nad-L,
        // 2=pod-R, 3=nad-R. pod/nad zero the SUB- vs SUPER-diagonal; L/R = row op (left, Rotate, -sin) vs column
        // op (right, RotatePost, +sin) - a right op still yields a real cube move (L*M*R = I => M^-1 = R*L).
        // Natural order [0..P-1] + modeMask 0 = standard QR baseline. Non-destructive (works on a clone). The
        // full P! order space subsumes axis-permutation, so order (not perm) is the diversity axis (diagnostic).
        static int RotCountMode(TMatrix M, int[] order, int modeMask)
        {
            var A = (TMatrix)M.Clone();                        // working copy (non-destructive)
            int c = 0;
            for (int i = 0; i < order.Length; i++)
            {
                int p = order[i];
                int a1 = TAffine.Planes[p][0], a2 = TAffine.Planes[p][1];
                int strat = (modeMask >> (2 * p)) & 3;         // 0=pod-L 1=nad-L 2=pod-R 3=nad-R
                float a, b;
                switch (strat)
                {
                    case 0:  a = A[a1, a1]; b =  A[a2, a1]; break;   // pod-L: zero sub-diag [a2,a1] (row op)
                    case 1:  a = A[a2, a2]; b = -A[a1, a2]; break;   // nad-L: zero super-diag [a1,a2] (row op)
                    case 2:  a = A[a2, a2]; b = -A[a2, a1]; break;   // pod-R: zero sub-diag [a2,a1] (col op)
                    default: a = A[a1, a1]; b =  A[a1, a2]; break;   // nad-R: zero super-diag [a1,a2] (col op)
                }
                float r = (float)Math.Sqrt(a * a + b * b);
                if (r < 0.1f) continue;                                            // pivot ~ 0 -> identity rotation
                float cos = a / r, sin = b / r;
                if (strat < 2) A.Rotate(a1, a2, cos, -sin);        // left: row op, -sin
                else           A.RotatePost(a1, a2, cos, sin);     // right: col op, +sin
                if (TCubie.GetAngle(cos, sin) != 0) c++;
            }
            return c;
        }

        // mode moves-to-solve of orientation M after applying cube move (plane, angle). Clones the small n x n
        // matrix (not the whole cubie) and left-rotates it exactly as Turn rotates a cubie, so the greedy's
        // prediction matches the real move; the clone leaves M intact for the sibling candidates.
        static int RotCountModeAfter(TMatrix M, int plane, int angle, int[] order, int modeMask)
        {
            var A = (TMatrix)M.Clone();
            var pa = TAffine.Planes[plane];
            var rot = TCubie.SetAngle(angle);
            A.Rotate(pa[0], pa[1], rot.X, rot.Y);
            return RotCountMode(A, order, modeMask);
        }

        static int[] IdentityOrder()
        {
            var o = new int[TAffine.Planes.Length];
            for (int i = 0; i < o.Length; i++) o[i] = i;
            return o;
        }

        // A random plane-reduction order (any permutation of the P planes). Drawn per seed so the pool samples
        // the strategy x order space - the full generating axis (order subsumes perm, per the diagnostic). An
        // order that is invalid for a given (cubie, strategy) just makes the greedy stuck -> null -> redraw.
        static int[] RandomOrder()
        {
            var o = IdentityOrder();
            for (int i = o.Length - 1; i > 0; i--)
            {
                int j = TChromosome.Rnd.Next(i + 1);
                (o[i], o[j]) = (o[j], o[i]);
            }
            return o;
        }

        // Greedy solve of the host-designated active cubie under a mixed-Givens mode, via RANDOMIZED
        // BACKTRACKING on a lightweight copy of just that cubie (deep Transform, shared geometry). Every
        // descent takes a random cube move (plane + 90/180/270) that lowers the mode moves-to-solve by one;
        // on a dead end (the metric hits 0 with M != I, or no progress move exists) it backtracks and tries
        // another branch instead of giving up. This is the crucial fix over a one-shot descent: a mixed
        // mode's solving paths are NARROW, so a single random descent almost always dead-ends and the greedy
        // harvested ZERO macro-moves (every non-stuck draw was a minimal-length path). Backtracking finds a
        // full length-rc0 path iff one exists, so macro modes (rc0 > minimal) actually yield their longer
        // sequences. modeMask == 0 is the standard shortest-solve. Returns null only when the whole monotone
        // tree dead-ends (mode genuinely stuck for this cubie) - the caller redraws a fresh mode.
        public List<int> GetSolveSeq(int[] order, int modeMask)
        {
            var c = ActiveCubie.Copy();        // just the cubie - no full-cube clone
            var seq = new List<int>();
            int rc0 = RotCountMode(c.Transform.M, order, modeMask);
            return SolveModeDfs(c, order, modeMask, rc0, seq) ? seq : null;
        }

        // Depth-first search over the monotone tree (each step drops the mode moves-to-solve by exactly 1),
        // random branch order, first solution wins. seq accumulates the realized moves; on backtrack the last
        // move is popped. rc == current mode moves-to-solve == depth remaining down to the identity.
        bool SolveModeDfs(TCubie c, int[] order, int modeMask, int rc, List<int> seq)
        {
            if (c.State == 0) return true;                           // reached identity - seq solves the cubie
            var cand = new List<int[]>();
            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                for (int angle = 1; angle <= 3; angle++)            // quarter-turns: 1/2/3 = 90/180/270
                    if (RotCountModeAfter(c.Transform.M, plane, angle, order, modeMask) == rc - 1)
                        cand.Add(new[] { plane, angle });
            for (int i = cand.Count - 1; i > 0; i--)                // Fisher-Yates: progress moves in random order
            {
                int j = TChromosome.Rnd.Next(i + 1);
                (cand[i], cand[j]) = (cand[j], cand[i]);
            }
            foreach (var pick in cand)
            {
                var pa = TAffine.Planes[pick[0]];
                var child = c.Copy();
                child.Rotate(pa, TCubie.SetAngle(pick[1]));
                int axis = TChromosome.Rnd.Next(TAffine.N);          // any axis not in the plane (collateral only)
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = pick[0], Angle = pick[1], Axis = axis, Slice = child.GetPos(axis) }.Encode());
                if (SolveModeDfs(child, order, modeMask, rc - 1, seq)) return true;
                seq.RemoveAt(seq.Count - 1);                         // dead end - undo and try the next branch
            }
            return false;
        }

        // Single-descent (NO backtracking) mode solve: at each step take ONE random move that drops the mode
        // moves-to-solve by 1; on a dead end (no such move) FAIL (no undo). O(rc) per attempt vs the DFS's
        // branching - so cheap even when it fails, and the caller just redraws ("draw until valid"). Narrow
        // (macro) modes dead-end often, so the pool ends up mostly minimal - deliberate: the fast SEED path
        // trades macro-move material for speed. Mutates its own cubie copy in place (no backtrack -> no copies).
        public List<int> GetSolveSeqGreedy(int[] order, int modeMask)
        {
            var c = ActiveCubie.Copy();
            var seq = new List<int>();
            int rc = RotCountMode(c.Transform.M, order, modeMask);
            while (c.State != 0)
            {
                int bestPlane = -1, bestAngle = 0, hits = 0;
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)
                        if (RotCountModeAfter(c.Transform.M, plane, angle, order, modeMask) == rc - 1
                            && TChromosome.Rnd.Next(++hits) == 0)     // reservoir pick: one uniform winner, no list
                        { bestPlane = plane; bestAngle = angle; }
                if (hits == 0) return null;                           // dead end, no backtrack
                var pa = TAffine.Planes[bestPlane];
                c.Rotate(pa, TCubie.SetAngle(bestAngle));
                int axis = TChromosome.Rnd.Next(TAffine.N);
                while (axis == pa[0] || axis == pa[1]) axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = bestPlane, Angle = bestAngle, Axis = axis, Slice = c.GetPos(axis) }.Encode());
                rc--;
            }
            return seq;
        }

        // Working copy of `source` whose ORIENTATION is the relative rotation X = M.Dᵀ (renormalised to the cubie
        // scale) but whose POSITION is kept, State cache invalidated. Driving X -> I via world-moves brings `source`
        // to orientation D (w.X = I => w.M = D). D = null returns a plain copy (descent, X = M). Shared by the
        // retargeted decomposers so the coherence retarget lives in ONE place.
        TCubie RelativeSource(TCubie source, TMatrix D)
        {
            var c = source.Copy();
            if (D != null)
            {
                //float s = 0f;
                //for (int r = 0; r < TAffine.N; r++) s += source.Transform.M[r, 0] * source.Transform.M[r, 0];
                //s = (float)Math.Sqrt(s);
                c.Transform.M = (source.Transform.M * D.Transpose()) * (1f / TCubie.Scaling[0]);   // X = M.Dᵀ, rescaled back to s
                c.Rotate(TAffine.Planes[0], TCubie.SetAngle(0));   // no-op turn: invalidate the State cache after the M overwrite
            }
            return c;
        }

        // Retargeted FAST greedy: same single-descent mode solve as GetSolveSeqGreedy(order, modeMask), but toward a
        // TARGET orientation D (via the relative source) instead of identity -- so the RICH order/modeMask material
        // (macro-moves, many decomposition trees) is available for the coherence merge, not just the minimal
        // SUB/SUP core of GetSolveSeqDirect. D = null reduces to the plain identity solve. Returns null on a dead end.
        public List<int> GetSolveSeqGreedy(TCubie source, TMatrix D, int[] order, int modeMask)
        {
            var c = RelativeSource(source, D);
            var seq = new List<int>();
            int rc = RotCountMode(c.Transform.M, order, modeMask);
            while (c.State != 0)
            {
                int bestPlane = -1, bestAngle = 0, hits = 0;
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)
                        if (RotCountModeAfter(c.Transform.M, plane, angle, order, modeMask) == rc - 1
                            && TChromosome.Rnd.Next(++hits) == 0)     // reservoir pick: one uniform winner, no list
                        { bestPlane = plane; bestAngle = angle; }
                if (hits == 0) return null;                           // dead end, no backtrack
                var pa = TAffine.Planes[bestPlane];
                c.Rotate(pa, TCubie.SetAngle(bestAngle));
                int axis = TChromosome.Rnd.Next(TAffine.N);
                while (axis == pa[0] || axis == pa[1]) axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = bestPlane, Angle = bestAngle, Axis = axis, Slice = c.GetPos(axis) }.Encode());
                rc--;
            }
            return seq;
        }

        // Legacy standard-metric greedy: same shortest-solve as GetSolveSeq(0), but the moves-to-solve is
        // read through the cubie's State getter (which honours the CPU GA's EulerOrder / IsEulerOrderReversed
        // toggles), not the explicit mixed-Givens metric. Also the per-cluster RNG warm-up called by ActivateCluster.
        public List<int> GetSolveSeq()
        {
            var c = ActiveCubie.Copy();        // just the cubie - no full-cube clone
            var seq = new List<int>();
            while (c.State != 0)          // State getter refreshes c.RotationCount
            {
                int[] pa;
                TVector rot;
                var cand = new List<int[]>();
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)   // quarter-turns: 1/2/3 = 90/180/270
                    {
                        var rotCubie = c.Copy();
                        pa = TAffine.Planes[plane];
                        rot = TCubie.SetAngle(angle);
                        rotCubie.Rotate(pa, rot);
                        _ = rotCubie.State;
                        if (rotCubie.RotationCount == c.RotationCount - 1)
                            cand.Add(new[] { plane, angle });
                    }
                var pick = cand[TChromosome.Rnd.Next(cand.Count)];
                pa = TAffine.Planes[pick[0]];
                rot = TCubie.SetAngle(pick[1]);
                c.Rotate(pa, rot);
                int axis = TChromosome.Rnd.Next(TAffine.N);          // any axis not in the plane (collateral only)
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = pick[0], Angle = pick[1], Axis = axis, Slice = c.GetPos(axis) }.Encode());
            }
            return seq;
        }

        // M-DRIVEN decomposition (no order/mode draw, no rejection, no backtracking). Each step takes the SAME
        // proven progress primitive as GetSolveSeq() -- any quarter-turn that drops the cubie's moves-to-solve
        // (RotationCount) by one, which always exists for a non-identity orientation -- but CLASSIFIES it by
        // which diagonal it clears in its OWN plane (a1,a2): clearing the sub-diagonal (a2,a1) = SUB, the
        // super-diagonal (a1,a2) = SUP, a pure 180 diagonal sign flip = neither (sign bucket, only fills once the
        // permutation is seated). It then draws from feasible-SUB u feasible-SUP (u sign) with a bias toward SUB.
        // Because every step is a real progress move it NEVER dead-ends and NEVER needs a redraw, and the diagonal
        // sign flips close it out, so State reaches 0. Diversity is INTERNAL (which SUB/SUP move, which collateral
        // axis) -- no (order, mode) space, no stuck-mode waste. subBias in [0,1] = SUB preference when both are on
        // offer (0.5 = symmetric). NOTE: this yields MINIMAL (shortest) solves only -- macro-move depth (the old
        // mixed-Givens modes) is a later add-on (bounded detours), deliberately out of this light core.
        public List<int> GetSolveSeqDirect(double subBias = 0.75)
            => GetSolveSeqDirect(ActiveCubie, null, subBias);

        // Overload with an explicit SOURCE cubie and TARGET orientation D (an N x N orientation matrix; null =
        // identity = the descent case). For D != null it puts the RELATIVE rotation X = M.Dᵀ (renormalised to the
        // cubie scale) into the working copy and drives X -> I: the emitted world-moves w satisfy w.X = I => w.M = D,
        // so applied to `source` they bring its orientation to D, NOT to identity. Origin (position) is kept, so the
        // per-move slice stays correct (position evolves independently of orientation). This is the coherence
        // retarget -- align a cubie to a block orientation D instead of solving it away. Thresholds are
        // scale-RELATIVE (half the cubie scale), not a hard 0.5: the orientation matrix is scaled (~0.45), so a
        // hard 0.5 silently killed the SUB/SUP split (0.45 < 0.5) and every move fell into the uniform bucket.
        public List<int> GetSolveSeqDirect(TCubie source, TMatrix D, double subBias = 0.75)
        {
            var c = source.Copy();               // real orientation + real position
            float s = TCubie.Scaling[0];
            //float s = 0f;                         // scale of the (uniformly scaled) orientation, from column 0
            //for (int r = 0; r < TAffine.N; r++) s += source.Transform.M[r, 0] * source.Transform.M[r, 0];
            //s = (float)Math.Sqrt(s);
            float half = 0.5f * s;                // |entry| > half  <=>  a real (non-zero) matrix element
            if (D != null)
            {
                c.Transform.M = (source.Transform.M * D.Transpose()) * (1f / s);   // X = M.Dᵀ, rescaled back to s
                c.Rotate(TAffine.Planes[0], TCubie.SetAngle(0));   // we know it isn't in state D
            }
            var seq = new List<int>();
            while (c.State != 0)                  // State getter refreshes c.RotationCount
            {
                var sub = new List<int[]>();      // progress move that clears its plane's sub-diagonal (a2,a1)
                var sup = new List<int[]>();      // clears its plane's super-diagonal (a1,a2)
                var sign = new List<int[]>();     // pure 180 diagonal sign flip (permutation already seated there)
                var cand = new List<int[]>();
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                {
                    int a1 = TAffine.Planes[plane][0], a2 = TAffine.Planes[plane][1];   // a1 < a2
                    bool subBefore = Math.Abs(c.Transform.M[a2, a1]) > half;
                    bool supBefore = Math.Abs(c.Transform.M[a1, a2]) > half;
                    for (int angle = 1; angle <= 3; angle++)                            // quarter-turns 1/2/3
                    {
                        var t = c.Copy();
                        t.Rotate(TAffine.Planes[plane], TCubie.SetAngle(angle));
                        _ = t.State;
                        if (t.RotationCount != c.RotationCount - 1) continue;           // not a progress move
                        var mv = new[] { plane, angle };
                        if (subBefore && Math.Abs(t.Transform.M[a2, a1]) < half) sub.Add(mv);        // SUB
                        else if (supBefore && Math.Abs(t.Transform.M[a1, a2]) < half) sup.Add(mv);   // SUP
                        else sign.Add(mv);                                                            // sign fix
                        cand.Add(new[] { plane, angle });
                    }
                }
                // Draw from feasible-SUB u feasible-SUP u sign, biased toward SUB. The union == GetSolveSeq's
                // candidate set, so it is non-empty for any non-identity orientation (no stuck, no redraw).
                var other = new List<int[]>(); other.AddRange(sup); other.AddRange(sign);
                var from = (sub.Count > 0 && (other.Count == 0 || TChromosome.Rnd.NextDouble() < subBias)) ? sub : other;
                //var pick = from[TChromosome.Rnd.Next(from.Count)];
                var pick = cand[TChromosome.Rnd.Next(cand.Count)];
                var pa = TAffine.Planes[pick[0]];
                c.Rotate(pa, TCubie.SetAngle(pick[1]));
                int axis = TChromosome.Rnd.Next(TAffine.N);          // any axis not in the plane (collateral only)
                while (axis == pa[0] || axis == pa[1])
                    axis = (axis + 1) % TAffine.N;
                seq.Add(new TMove { Plane = pick[0], Angle = pick[1], Axis = axis, Slice = c.GetPos(axis) }.Encode());
            }
            return seq;
        }

        // Canonicalize a move sequence in place: compose consecutive moves in the SAME (axis, plane, slice) as
        // quarter-turns mod 4 (Rz90 Rz90 -> Rz180; Rz90 Rz270 -> removed). Same logic as TRubikGenome.Correct /
        // TGreedyDiversity.CorrectGenes. Run before the seed dedup so two Correct-equal draws fold to one slot
        // instead of being kept as distinct raw strings.
        static void CorrectSeq(List<int> g)
        {
            for (int idx = 0; idx < g.Count; idx++)
            {
                var move = TMove.Decode(g[idx]);
                for (int prev = idx - 1; prev >= 0; prev--)
                {
                    var pm = TMove.Decode(g[prev]);
                    if (pm.Axis != move.Axis || pm.Plane != move.Plane) break;
                    if (pm.Slice == move.Slice)
                    {
                        int angle = (move.Angle + pm.Angle) & 3;         // compose quarter-turns mod 4
                        if (angle > 0) { pm.Angle = angle; g[prev] = pm.Encode(); }
                        else { g.RemoveAt(prev); idx--; }
                        g.RemoveAt(idx); idx--;
                        break;
                    }
                }
            }
        }

        public static bool Diagnostic;       // set by Gpu.SeedStats: enables the census + report in BuildSeedMoves
        // A/B switch: true -> new M-driven SUB/SUP decomposition (GetSolveSeqDirect); false -> legacy order/mode
        // draw-and-reject (fast greedy / DFS). Old paths are kept intact below for comparison, never deleted.
        public static bool UseDirectSeed = true;
        // A/B switch for the coherence phase (only when UseDirectSeed): true -> retarget the seed to a BLOCK
        // orientation (build k+k / collapse the gateway) instead of solving the cubie to identity; false -> direct
        // seed always targets identity (the descent goal), which fights the coherence build. Descent is unaffected.
        public static bool CoherenceRetarget = true;
        public string LastSeedReport = "";   // filled by BuildSeedMoves (only when Diagnostic); shown by Seed Pool Stats
        private int lastMovesApplied;

        // Lowest-multiplicity orientation groups of the active cluster's NON-identity cubies. Computed ONCE per
        // BuildSeedMoves (the cluster is CONSTANT while seeds are drawn) -- doing it per draw, with a State/Euler
        // read per cubie, is what killed performance. Grouping is by OrthoPack (cheap, reads the matrix; no State),
        // and identity is detected by pack equality, not State==0. Histogram is PARTIAL info -- orientation only,
        // not layer -- so multiplicity ~ block size (upper bound). Returns the groups at the lowest count k_min:
        // >=2 of them -> a draw pairs two (build k+k, e.g. 1+1->2, 2+2->4); exactly 1 -> a draw solves it to
        // identity (removes an odd unpaired piece, 4+2+1 -> 4+2; or, when it is the last group, collapses the
        // gateway to solved). Empty list if nothing is scrambled. Per-draw sampling from this is O(1).
        List<List<TCubie>> LowestMultiplicityGroups()
        {
            uint idPack = new TAffine().OrthoPack();             // identity orientation pack (compute once)
            var groups = new Dictionary<uint, List<TCubie>>();
            foreach (var cu in ActiveCluster.Cubies)
            {
                uint key = cu.Transform.OrthoPack();
                if (key == idPack) continue;                     // identity = solved background, not a merge group
                if (!groups.TryGetValue(key, out var lst)) { lst = new List<TCubie>(); groups[key] = lst; }
                lst.Add(cu);
            }
            var atMin = new List<List<TCubie>>();
            if (groups.Count == 0) return atMin;                 // nothing scrambled
            int kMin = int.MaxValue;
            foreach (var g in groups.Values) if (g.Count < kMin) kMin = g.Count;
            foreach (var g in groups.Values) if (g.Count == kMin) atMin.Add(g);
            return atMin;
        }

        // GPU seed START: the packed orientation the GPU seating descent must reduce to identity, plus the cluster
        // index of the cubie whose position drives the collateral slice. DESCENT -> the active cubie itself (X = M,
        // target identity). COHERENCE retarget -> a lowest-multiplicity representative's RELATIVE X = M·Dᵀ toward a
        // partner block D (or identity if that level has a single group). ONE (rep, D) per seeding call: the whole
        // population then works the SAME merge (collateral carries the group), diversity from the descent randomness.
        // Packed orientation is unit (OrthoPack strips the ~0.45 scale), so M·Dᵀ packs cleanly with no rescale.
        public uint SeedStartM(out int repIndex, out uint realM, out uint collAxis)
        {
            TCubie source = ActiveCubie;
            TMatrix D = null;
            List<TCubie> group = null;
            if (CoherenceRetarget && Coherent != 0)
            {
                var atMin = LowestMultiplicityGroups();
                if (atMin.Count > 0)
                {
                    group = atMin[TChromosome.Rnd.Next(atMin.Count)];
                    source = group[TChromosome.Rnd.Next(group.Count)];
                    if (atMin.Count >= 2)                            // >=2 lowest groups -> pair (random partner)
                    {
                        List<TCubie> g2;
                        do { g2 = atMin[TChromosome.Rnd.Next(atMin.Count)]; } while (g2 == group);
                        D = g2[0].Transform.M;                       // D = partner group's orientation (build k+k)
                    }
                }
            }
            repIndex = ActiveCluster.Cubies.IndexOf(source);
            realM = source.Transform.OrthoPack();               // rep's REAL orientation (for the collateral slice)
            collAxis = CoLayerAxis(source, group);              // keep rep's block together across the seed
            return RelativeSource(source, D).Transform.OrthoPack();
        }

        // An axis on which every member of rep's group shares rep's coordinate -> turning THAT layer moves the whole
        // coherent block together, so the merge seed does not split the block it is trying to build (random collateral
        // could turn the pair's SPLIT axis -> 3+1 instead of 2+2->4). The move rotates in a plane not containing this
        // axis, so rep's (and the group's) coord on it is stable -> the block stays co-layered on it the whole seed.
        // 0xFFFFFFFF (no group / descent / not co-layered) -> the shader falls back to a random collateral axis.
        static uint CoLayerAxis(TCubie rep, List<TCubie> group)
        {
            if (group == null) return 0xFFFFFFFFu;
            for (int k = 0; k < TAffine.N; k++)
            {
                int c = rep.GetPos(k);
                bool allSame = true;
                foreach (var m in group) if (m.GetPos(k) != c) { allSame = false; break; }
                if (allSame) return (uint)k;
            }
            return 0xFFFFFFFFu;
        }

        // Packs up to numSeeds per-specimen seed sequences into a flat buffer (stride genes each, unused
        // slots = -1 sentinel) for the GPU Init shader. Each draw is a fresh greedy decomposition
        // (GetSolveSeq) under a RANDOMLY chosen mixed-Givens mode, realized with a random axis per move (the
        // axis only picks the collateral layer, not the target's solve, so it adds no decomposition trees).
        // mode 0 is the standard shortest-solve; mixed modes trace longer macro-move paths - the mode pool
        // (2^P) is the dominant diversity source. A stuck mode yields null; we just redraw. Targets
        // populationCount * seedRatioPercent / 100 slots; numSeeds is however many distinct sequences it finds.
        public int[] BuildSeedMoves(int populationCount, int stride, int seedRatioPercent, bool mixedModes, bool fast, out uint numSeeds)
        {
            int target = populationCount * seedRatioPercent / 100;
            int modeSpace = mixedModes ? 1 << (2 * TAffine.Planes.Length) : 1;   // 4^P strategies; SEED_MODE 0 -> mode 0 (baseline)
            var slots = new List<int[]>();
            var seen = new HashSet<string>();
            // The distinct-manoeuvre pool across modes is far richer than the single-tree shortest-solve pool,
            // but still finite. Stop once draws stop producing anything new (pool saturated) instead of
            // spinning a fixed guard; the stall budget scales with what we found so far so bigger pools get
            // patience. Both a stuck mode (null) and a duplicate count as a no-progress draw.
            int stall = 0;
            int draws = 0, stuck = 0, dupes = 0;                     // telemetry: why the pool ends up the size it is
            var lenHist = new SortedDictionary<int, int>();          // accepted-seed length distribution
            var modeContrib = new HashSet<int>();                    // distinct modes that landed >=1 accepted seed
            var ident = IdentityOrder();
            // Safe LOWER bound on moves-to-solve: each move is a Givens in one plane (2 axes), so it can fix at
            // most 2 axes -> solving d non-fixed axes needs >= ceil(d/2) moves. (Standard QR is only an UPPER
            // bound - e.g. diag(-1,1,-1) is 1 move via plane (0,2) but standard QR at natural order counts 2 -
            // so using it here would wrongly skip valid short solves. This bound never over-skips.)
            int min = (ActiveCubie.ActiveAxisCount() + 1) / 2;
            // A no-progress draw (stuck mode or duplicate). We call the pool saturated once a run of them
            // outlasts a budget that grows with the pool - a bigger pool earns more patience. One method,
            // so the three no-progress branches below read identically instead of repeating the expression.
            bool Saturated() => ++stall > 32 + 8 * slots.Count;
            if (UseDirectSeed)
            {
                // DIRECT M-driven path: each draw is a fresh SUB/SUP-weighted matrix decomposition
                // (GetSolveSeqDirect) -- never null, never stuck, so NO stuck/redraw budget. Dedup + saturation
                // still fold repeats and stop a saturated pool. No order/mode (diversity is internal to the
                // decomposition). The old fast/slow paths below stay for A/B (UseDirectSeed=false).
                // COHERENCE histogram is computed ONCE here (cluster is constant across draws), not per draw.
                var atMin = (CoherenceRetarget && Coherent != 0) ? LowestMultiplicityGroups() : null;
                while (slots.Count < target)
                {
                    List<int> seq;
                    if (atMin != null)
                    {
                        // COHERENCE: don't solve the cubie to identity (that unbuilds the block). Sample a
                        // (representative, target-orientation D) from the lowest-multiplicity groups and align the
                        // representative to D -- pair equal-size groups (k+k), or collapse the last group (D = null).
                        if (atMin.Count == 0) break;                     // nothing scrambled -> no seeds
                        var g1 = atMin[TChromosome.Rnd.Next(atMin.Count)];
                        var rep = g1[TChromosome.Rnd.Next(g1.Count)];
                        TMatrix D = null;                                // single lowest group -> solve to identity
                        if (atMin.Count >= 2)                            // >=2 lowest groups -> pair (random partner)
                        {
                            List<TCubie> g2;
                            do { g2 = atMin[TChromosome.Rnd.Next(atMin.Count)]; } while (g2 == g1);
                            D = g2[0].Transform.M;                       // D = partner group's orientation (build k+k)
                        }
                        seq = GetSolveSeqDirect(rep, D);
                    }
                    else
                        seq = GetSolveSeqDirect();                       // descent (or retarget off): active cubie -> identity
                    draws++;
                    if (seq.Count == 0) break;                            // cubie already solved -> no seeds
                    CorrectSeq(seq);                                      // canonicalize before dedup
                    var moves = seq.ToArray();
                    if (seen.Add(string.Join(",", moves)))
                    {
                        slots.Add(moves); stall = 0;
                        lenHist.TryGetValue(moves.Length, out int hc); lenHist[moves.Length] = hc + 1;
                    }
                    // CUMULATIVE dupe cap (not the reset-on-find Saturated): coherence has a SMALL distinct pool,
                    // so target is unreachable and the reset budget churned ~100x dupes for ~1x distinct. Stop once
                    // total dupes pass ~2x the pool found -- the tail slots are filled by repetition below anyway.
                    else if (++dupes > 64 + 2 * slots.Count) break;
                }
            }
            else if (fast)
            {
                // FAST path: one single random descent per draw (GetSolveSeqGreedy, NO backtracking), thrown in
                // "as they come" with NO dedup. A descent that dead-ends on a narrow mode returns null and is just
                // redrawn ("draw until valid") - cheap because each attempt is O(rc), not the DFS's branching. Cap
                // the draws so a cubie whose modes all dead-end on a single descent can't spin forever; whatever we
                // gathered is topped up to target by repetition below. Duplicates are FINE here (more of the same
                // easy manoeuvre is legitimate seed material), so no seen-set, no CorrectSeq, no saturation budget.
                // COHERENCE histogram computed ONCE (cluster is constant across draws); null in descent.
                var atMinF = (CoherenceRetarget && Coherent != 0) ? LowestMultiplicityGroups() : null;
                int maxDraws = 32 * target + 64;
                for (int draw = 0; slots.Count < target && draw < maxDraws; draw++)
                {
                    int mode = mixedModes ? TChromosome.Rnd.Next(modeSpace) : 0;
                    var order = mixedModes ? RandomOrder() : ident;
                    List<int> seq;
                    if (atMinF != null)                              // COHERENCE: retarget the RICH greedy to D
                    {
                        if (atMinF.Count == 0) break;                // nothing scrambled -> no seeds
                        var g1 = atMinF[TChromosome.Rnd.Next(atMinF.Count)];
                        var rep = g1[TChromosome.Rnd.Next(g1.Count)];
                        TMatrix D = null;                            // single lowest group -> solve to identity
                        if (atMinF.Count >= 2)                       // >=2 lowest groups -> pair (random partner)
                        {
                            List<TCubie> g2;
                            do { g2 = atMinF[TChromosome.Rnd.Next(atMinF.Count)]; } while (g2 == g1);
                            D = g2[0].Transform.M;                   // D = partner group's orientation (build k+k)
                        }
                        seq = GetSolveSeqGreedy(rep, D, order, mode);
                    }
                    else
                        seq = GetSolveSeqGreedy(order, mode);        // single descent; null on any dead end
                    if (seq == null) { draws++; stuck++; continue; }  // narrow mode dead-ended -> redraw
                    if (seq.Count == 0) break;                        // cubie already solved -> no seeds
                    CorrectSeq(seq);
                    //slots.Add(seq.ToArray());
                    var moves = seq.ToArray();
                    if (seen.Add(string.Join(",", moves)))
                    {
                        slots.Add(moves); stall = 0;
                        lenHist.TryGetValue(moves.Length, out int hc); lenHist[moves.Length] = hc + 1;
                        modeContrib.Add(mode);
                    }
                    // CUMULATIVE dupe cap (not the reset-on-find Saturated): coherence has a SMALL distinct pool,
                    // so target is unreachable and the reset budget churned ~100x dupes for ~1x distinct. Stop once
                    // total dupes pass ~2x the pool found -- the tail slots are filled by repetition below anyway.
                    else if (++dupes > 64 + 2 * slots.Count) break;
                    //lenHist.TryGetValue(seq.Count, out int hc); lenHist[seq.Count] = hc + 1;
                    //modeContrib.Add(mode);
                }
            }
            else
            while (slots.Count < target)
            {
                int mode = TChromosome.Rnd.Next(modeSpace);
                var order = mixedModes ? RandomOrder() : ident;      // random plane-reduction order (baseline: natural)
                draws++;
                if (RotCountMode(ActiveCubie.Transform.M, order, mode) < min)   // undercounting -> provably stuck, skip the DFS
                { stuck++; if (Saturated()) break; continue; }
                var seq = GetSolveSeq(order, mode);                  // greedy under (random order, random mode)
                if (seq == null) { stuck++; if (Saturated()) break; continue; }  // rc0>=min but still stuck
                if (seq.Count == 0) break;                            // cubie already solved -> no seeds
                CorrectSeq(seq);                                      // canonicalize before dedup: fold Correct-equal draws
                var moves = seq.ToArray();
                if (seen.Add(string.Join(",", moves)))
                {
                    slots.Add(moves); stall = 0;
                    lenHist.TryGetValue(moves.Length, out int hc); lenHist[moves.Length] = hc + 1;
                    modeContrib.Add(mode);
                }
                else
                {
                    dupes++;
                    if (Saturated()) break;                           // long run with no new manoeuvre: exhausted
                }
            }

            // Census + report are DIAGNOSTIC-only (Seed Pool Stats): the census alone is modeSpace (up to 4^P =
            // 4096) extra RotCountMode calls per cubie - pure waste in production. Skip unless Diagnostic is set.
            if (Diagnostic)
            {
                // mode census: rc0 (mode moves-to-solve) of THIS cubie under each mode (at natural order). Path
                // length == rc0, so this is the cubie's macro-move potential. All rc0 == minimal => degenerate
                // (shallow) cubie; a spread => deep orientation that mixed modes inflate into macro-moves.
                var rc0Hist = new SortedDictionary<int, int>();
                for (int mode = 0; mode < modeSpace; mode++)
                {
                    int r = RotCountMode(ActiveCubie.Transform.M, ident, mode);
                    rc0Hist.TryGetValue(r, out int hc); rc0Hist[r] = hc + 1;
                }

                var rep = new System.Text.StringBuilder();
                rep.AppendLine($"[BuildSeedMoves]  N={TAffine.N}  SEED_MODE={(mixedModes ? 1 : 0)}  order={(mixedModes ? "random" : "natural")}  stride={stride}  target={target}  ({seedRatioPercent}% of population)");
                rep.AppendLine($"distinct seeds = {slots.Count}     draws={draws}  stuck={stuck}  dupes={dupes}");
                if (slots.Count > 0 && slots.Count < target)
                    rep.AppendLine($"pool < target -> {target} slots filled by repeating the {slots.Count} distinct seeds");
                rep.AppendLine($"distinct modes contributing = {modeContrib.Count} / {modeSpace}");
                rep.Append("mode rc0 census (moves:modes):");
                foreach (var kv in rc0Hist) rep.Append($"  {kv.Key}:{kv.Value}");
                rep.AppendLine();
                rep.Append("accepted length histogram (moves:count):");
                foreach (var kv in lenHist) rep.Append($"  {kv.Key}:{kv.Value}");
                LastSeedReport = rep.ToString();
                System.Diagnostics.Debug.WriteLine(LastSeedReport);
            }

            // Seed the FULL target, not just as many specimens as the pool has distinct manoeuvres. A shallow
            // cubie with few decompositions no longer leaves the population under-seeded: every distinct seed is
            // placed once (slots 0..pool), then the remaining slots are topped up by sampling the pool with
            // repetition. numSeeds = target so Init pre-seeds exactly that many specimens.
            if (slots.Count == 0) { numSeeds = 0; return new int[1]; }   // nothing to seed (solved / stuck); never a zero-size SSBO
            numSeeds = (uint)target;
            var buf = new int[target * stride];
            for (int s = 0; s < target; s++)
            {
                int[] seed = s < slots.Count ? slots[s] : slots[TChromosome.Rnd.Next(slots.Count)];
                int k = 0;
                for (; k < seed.Length && k < stride; k++) buf[s * stride + k] = seed[k];
                for (; k < stride; k++) buf[s * stride + k] = -1;
            }
            return buf;
        }
    }
}
