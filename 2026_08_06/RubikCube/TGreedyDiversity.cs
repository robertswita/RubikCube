using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TGL;

namespace RubikCube
{
    // Diagnostic (not in the production path). Question: the greedy solves a cubie by repeatedly taking a
    // quarter-turn that lowers RotationCount, and RotationCount = number of non-identity Givens rotations in
    // the STANDARD-order QR of the orientation matrix. Permuting the axes before that QR (A[y,x] = M[perm[y],
    // perm[x]] = PᵀMP, then standard decomposition) changes which moves count as progress, so the greedy can
    // reach different shortest sequences. This measures how many distinct solving sequences the greedy can
    // produce under the identity permutation vs. the union over all N! axis permutations, over EVERY cubie
    // orientation (the rotation group: 24 for N=3, 192 for N=4). Nothing here touches TCubie.State - RotCount
    // is computed locally, so the comparison is self-contained and independent of the production metric.
    //
    // Sequences are compared as MANOEUVRES, not raw (plane, angle) tokens: each is realised into genes with
    // the first valid axis (the axis is irrelevant to the target solve; a fixed choice keeps sequences
    // comparable) and slice 0, then reduced by the composition of TRubikGenome.Correct (consecutive same
    // axis+plane+slice quarter-turns fold mod 4; slice along a collateral axis is invariant under same-plane
    // moves, so 0 is faithful). This collapses effect-equivalent forms - 2x90 folds to one 180 - so only
    // genuinely different net manoeuvres are counted, not move-count-padded duplicates.
    static class TGreedyDiversity
    {
        static int[][] perms;                    // all N! axis permutations (index 0 = identity)
        static List<int>[] orders;               // all valid QR plane orders (index 0 = standard order)
        static uint identityPack;                // OrthoPack of the identity orientation (greedy solve-check)
        static Dictionary<long, int> rotMemo;    // RotCount keyed by orientation x (perm, order, reversed, mode)

        static double Pct(long baseVal, long total) => baseVal == 0 ? 0 : 100.0 * (total - baseVal) / baseVal;

        public static string Run()
        {
            int n = TAffine.N;
            perms = Permutations(n);
            orders = AllPlaneOrders();                             // ALL P! orders; solve-check keeps the valid ones
            rotMemo = new Dictionary<long, int>();
            identityPack = new TAffine().OrthoPack();
            var orientations = AllOrientations();
            int modeCount = 1 << TAffine.Planes.Length;

            int stratCount = modeCount * modeCount;                // 4^P: pod/nad x L/R per plane

            // Cheap "is perm necessary?" mode: strategy x order over ALL P! orders (perm=id) vs strategy x perm.
            // If the FULL order-cross still misses something perm reaches, perm is necessary. N>=4 samples
            // orientations and (optionally) strategies - orders stay FULL (720 for N=4; that's the whole point).
            // Fresh random draw each run.
            bool sampled = n >= 4;
            int sampleOrients = 5, sampleStrats = stratCount;
            int[] strat;
            if (sampled)
            {
                var rnd = new Random();
                orientations = SamplePick(orientations, sampleOrients, rnd);
                if (sampleStrats < stratCount)
                {
                    var sset = new HashSet<int> { 0 };             // always include baseline strategy 0
                    while (sset.Count < sampleStrats) sset.Add(rnd.Next(stratCount));
                    strat = new int[sset.Count]; sset.CopyTo(strat);
                }
                else { strat = new int[stratCount]; for (int i = 0; i < stratCount; i++) strat[i] = i; }
            }
            else
            {
                strat = new int[stratCount];
                for (int i = 0; i < stratCount; i++) strat[i] = i;
            }

            long sumId = 0, sumModeLR = 0, sumCrossPerm = 0, sumCrossOrder = 0, sumOrderOrPerm = 0;
            int P = TAffine.Planes.Length;
            var histId = new int[P + 1];                           // baseline manoeuvres by length
            var histModeLR = new int[P + 1];                       // strategy alone (4^P, perm0 order0), EXTRA vs base
            var histCrossPerm = new int[P + 1];                    // strategy x perm, EXTRA vs base
            var histCrossOrder = new int[P + 1];                   // strategy x order (FULL P!), EXTRA vs base

            foreach (var o in orientations)
            {
                if (sampled) rotMemo.Clear();                       // bound memory: variants don't carry across orientations here

                var idSet = AllGreedy(o, 0, 0, false, 0);           // baseline (standard QR)

                var modeLRU = new HashSet<string>(idSet);           // strategy alone: sampled 4^P at perm0, order0
                foreach (int m in strat) if (m != 0) modeLRU.UnionWith(AllGreedy(o, 0, 0, false, m));

                var crossPerm = new HashSet<string>(modeLRU);       // JOINT strategy x perm (order 0)
                for (int pi = 1; pi < perms.Length; pi++)
                    foreach (int m in strat)
                        crossPerm.UnionWith(AllGreedy(o, pi, 0, false, m));

                var crossOrder = new HashSet<string>(modeLRU);      // JOINT strategy x order (perm 0)
                for (int qi = 1; qi < orders.Length; qi++)
                    foreach (int m in strat)
                        crossOrder.UnionWith(AllGreedy(o, 0, qi, false, m));

                var orderOrPerm = new HashSet<string>(crossOrder);  // does perm reach anything the FULL order-cross misses?
                orderOrPerm.UnionWith(crossPerm);

                sumId += idSet.Count; sumModeLR += modeLRU.Count;
                sumCrossPerm += crossPerm.Count; sumCrossOrder += crossOrder.Count;
                sumOrderOrPerm += orderOrPerm.Count;

                foreach (var s in idSet) histId[Len(s)]++;
                foreach (var s in modeLRU)    if (!idSet.Contains(s)) histModeLR[Len(s)]++;
                foreach (var s in crossPerm)  if (!idSet.Contains(s)) histCrossPerm[Len(s)]++;
                foreach (var s in crossOrder) if (!idSet.Contains(s)) histCrossOrder[Len(s)]++;
            }

            int count = orientations.Count;
            var sb = new StringBuilder();
            sb.AppendLine($"Greedy decomposition diversity  (N = {n}, Size = {TRubikCube.Size})");
            sb.AppendLine($"  cubie orientations (rotation group) : {count}");
            sb.AppendLine($"  axis permutations (N!)              : {perms.Length}");
            sb.AppendLine($"  plane orders (of P! = {Factorial(TAffine.Planes.Length)}) : {orders.Length}");
            sb.AppendLine($"  reduction modes (2^P, pod/nad-L)    : {modeCount}");
            sb.AppendLine($"  reduction strategies (4^P, +L/R)    : {stratCount}");
            if (sampled)
                sb.AppendLine($"  *** SAMPLED: {count} orientations x {strat.Length} strategies, FULL {orders.Length} orders (re-run for another draw) ***");
            sb.AppendLine();
            sb.AppendLine($"  baseline (standard QR)              : {sumId}   (avg {sumId / (double)count:0.00} / orientation)");
            sb.AppendLine();
            sb.AppendLine($"  strategy alone (4^P, perm0 order0)  : {sumModeLR}   (+{Pct(sumId, sumModeLR):0.0}%)");
            sb.AppendLine();
            sb.AppendLine("  JOINT cross (strategy x axis together) - synergy = gain OVER strategy alone:");
            sb.AppendLine($"  strategy x perm (order 0)           : {sumCrossPerm}   (+{Pct(sumId, sumCrossPerm):0.0}% vs base, +{Pct(sumModeLR, sumCrossPerm):0.0}% vs strategy)");
            sb.AppendLine($"  strategy x order (FULL {orders.Length,3} orders)  : {sumCrossOrder}   (+{Pct(sumId, sumCrossOrder):0.0}% vs base, +{Pct(sumModeLR, sumCrossOrder):0.0}% vs strategy)");
            sb.AppendLine();
            sb.AppendLine("  IS PERM NECESSARY? (full order-cross U perm  vs  full order-cross alone):");
            sb.AppendLine($"  (order U perm) = {sumOrderOrPerm}   ({(sumOrderOrPerm == sumCrossOrder ? "SAME -> full order already contains perm -> perm NOT necessary" : $"+{sumOrderOrPerm - sumCrossOrder} -> perm reaches what full order misses -> perm NECESSARY")})");
            sb.AppendLine();
            sb.AppendLine("  manoeuvres by length  (baseline count, then EXTRA vs baseline):");
            sb.AppendLine("    length  baseline   strat  xPerm  xOrder");
            for (int L = 0; L < histId.Length; L++)
                if (histId[L] > 0 || histModeLR[L] > 0 || histCrossPerm[L] > 0 || histCrossOrder[L] > 0)
                    sb.AppendLine($"    {L,6}  {histId[L],8}  {histModeLR[L],6}  {histCrossPerm[L],6}  {histCrossOrder[L],7}");
            return sb.ToString();
        }

        // Empirical, per-axis check. For each orientation and each axis, every Correct-distinct manoeuvre is
        // applied to a real cube (target set to the orientation, moves realised via cube.Turn with the real
        // slice = target's current layer, then undone in reverse to restore). Reports, per method, the
        // Correct-string count vs the count of distinct REAL whole-cube effects - so effect-duplicates that
        // Correct misses (non-adjacent equivalence) are folded out. Solve rate confirms each actually solves.
        public static string Verify()
        {
            int n = TAffine.N;
            perms = Permutations(n);
            orders = AllOrders();
            rotMemo = new Dictionary<long, int>();
            identityPack = new TAffine().OrthoPack();
            int modeCount = 1 << TAffine.Planes.Length;
            int stratCount = modeCount * modeCount;                   // 4^P: pod/nad x L/R per plane
            var orientations = AllOrientations();

            var cube = new TRubikCube();
            // Generic target = a cubie with a TRIVIAL rotation stabilizer: no non-identity proper rotation fixes it,
            // so it can be forced into EVERY orientation AND no two manoeuvres collapse to the same whole-cube effect
            // by a position symmetry -- exactly what this verification needs. Detected by ORBIT size == orientation
            // count (as in FindOrientationCluster), which is exact and SIGN-AWARE: it accepts the even-cube clusters
            // (4^3, 6^4) that a "distinct distance magnitudes" test wrongly rejects (a repeated magnitude with
            // opposite signs is broken only by a reflection, so the rotation stabilizer stays trivial). So the true
            // minimum is even Size = 2(N-1) -- 6 for 4D, not the 7 the magnitude bound would demand.
            int orientCount = orientations.Count;
            var clusterSize = new Dictionary<int, int>();
            foreach (var cubie in cube.Cubies)
            {
                clusterSize.TryGetValue(cubie.ClusterIndex, out var c);
                clusterSize[cubie.ClusterIndex] = c + 1;
            }
            int targetIndex = -1;
            foreach (var cubie in cube.Cubies)
                if (clusterSize[cubie.ClusterIndex] == orientCount) { targetIndex = cubie.StartIndex; break; }
            if (targetIndex < 0)
                return $"No full-orientation cluster (size {orientCount}) at Size = {TRubikCube.Size}. For N = {n} set Size >= {2 * (n - 1)} (even, e.g. {2 * (n - 1)}; odd needs {2 * n - 1}), then retry.";
            var tgt = cube.Cubies[targetIndex];
            var targetCoords = string.Join(",", tgt.Position);

            long total = 0, solved = 0;
            long strId = 0, strPerm = 0, strOrder = 0, strMode = 0, strModeLR = 0, strAll = 0;
            long effId = 0, effPerm = 0, effOrder = 0, effMode = 0, effModeLR = 0, effAll = 0;

            foreach (var o in orientations)
            {
                var idSet = AllGreedy(o, 0, 0, false, 0);
                var permU = new HashSet<string>(idSet);
                for (int p = 1; p < perms.Length; p++) permU.UnionWith(AllGreedy(o, p, 0, false, 0));
                var orderU = new HashSet<string>(idSet);
                for (int q = 1; q < orders.Length; q++) orderU.UnionWith(AllGreedy(o, 0, q, false, 0));
                var modeU = new HashSet<string>(idSet);
                for (int m = 1; m < modeCount; m++) modeU.UnionWith(AllGreedy(o, 0, 0, false, SpreadLeft(m)));
                var modeLRU = new HashSet<string>(idSet);
                for (int m = 1; m < stratCount; m++) modeLRU.UnionWith(AllGreedy(o, 0, 0, false, m));
                var allU = new HashSet<string>(permU);
                allU.UnionWith(orderU); allU.UnionWith(modeLRU);

                foreach (var cb in cube.Cubies) cb.State = 0;              // reset the cube to solved
                tgt.Transform.M.Assign(o.M);                              // set the target orientation to o
                tgt.Rotate(TAffine.Planes[0], TCubie.SetAngle(0));        // no-op rotate: invalidate State, keep M/pos

                var manEffect = new Dictionary<string, string>();         // manoeuvre -> real whole-cube effect
                foreach (var man in allU)
                {
                    if (man.Length == 0) continue;
                    total++;
                    var applied = new List<TMove>();
                    foreach (var code in man.Split(' '))
                    {
                        var mv = TMove.Decode(int.Parse(code));
                        mv.Slice = tgt.GetPos(mv.Axis);                   // real slice: the target's current layer
                        cube.Turn(mv);
                        applied.Add(mv);
                    }
                    if (tgt.State == 0) solved++;
                    manEffect[man] = CubeHash(cube);
                    for (int i = applied.Count - 1; i >= 0; i--)          // restore: apply the manoeuvre in reverse
                    {
                        var a = applied[i];
                        cube.Turn(new TMove { Plane = a.Plane, Angle = (4 - a.Angle) & 3, Axis = a.Axis, Slice = a.Slice });
                    }
                }

                strId += NonEmpty(idSet);      effId += DistinctEff(idSet, manEffect);
                strPerm += NonEmpty(permU);    effPerm += DistinctEff(permU, manEffect);
                strOrder += NonEmpty(orderU);  effOrder += DistinctEff(orderU, manEffect);
                strMode += NonEmpty(modeU);    effMode += DistinctEff(modeU, manEffect);
                strModeLR += NonEmpty(modeLRU); effModeLR += DistinctEff(modeLRU, manEffect);
                strAll += NonEmpty(allU);      effAll += DistinctEff(allU, manEffect);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Manoeuvre verification  (N = {n}, Size = {TRubikCube.Size})");
            sb.AppendLine($"  target cubie (generic)              : StartIndex {targetIndex}, coords ({targetCoords})");
            sb.AppendLine($"  orientations                        : {orientations.Count}");
            sb.AppendLine($"  verified to SOLVE the target        : {solved} / {total}   ({(total == 0 ? 0 : 100.0 * solved / total):0.00}%)");
            sb.AppendLine();
            sb.AppendLine("  distinct manoeuvres per method  (Correct-string  ->  real whole-cube effect):");
            sb.AppendLine($"  baseline (standard QR)              : {strId,7}  ->  {effId,7}");
            sb.AppendLine($"  + axis permutations (perm)          : {strPerm,7}  ->  {effPerm,7}");
            sb.AppendLine($"  + plane order (order)               : {strOrder,7}  ->  {effOrder,7}");
            sb.AppendLine($"  + mixed reduction (mode, 2^P L)     : {strMode,7}  ->  {effMode,7}");
            sb.AppendLine($"  + adding right ops (mode, 4^P L/R)  : {strModeLR,7}  ->  {effModeLR,7}");
            sb.AppendLine($"  combined                            : {strAll,7}  ->  {effAll,7}");
            return sb.ToString();
        }

        static long NonEmpty(HashSet<string> s)
        {
            long c = 0;
            foreach (var m in s) if (m.Length > 0) c++;
            return c;
        }

        static int DistinctEff(HashSet<string> mans, Dictionary<string, string> eff)
        {
            var d = new HashSet<string>();
            foreach (var m in mans) if (m.Length > 0) d.Add(eff[m]);
            return d.Count;
        }

        static string CubeHash(TRubikCube cube)   // whole-cube state: per cubie (current position, orientation)
        {
            var sb = new StringBuilder();
            foreach (var c in cube.Cubies)
                sb.Append(c.Index).Append(':').Append(c.Transform.OrthoPack()).Append(';');
            return sb.ToString();
        }

        // Finds the cubie cluster whose orbit size equals the number of orientations 2^(N-1)*N! -- a cubie with a
        // TRIVIAL rotation stabilizer, so every cubie there reaches ALL orientations. It is detected by orbit size
        // (== orientCount), which is exact and sign-aware, so it works at the TRUE minimal size, NOT the older
        // "all distances distinct" bound: distinct distance MAGNITUDES are sufficient but not necessary. In an EVEN
        // cube a repeated magnitude carried with OPPOSITE signs (e.g. coords just under and just over the centre)
        // can only be swapped by a REFLECTION (det -1), which is not a proper rotation, so the rotation stabilizer
        // stays trivial anyway. Smallest cube with such a cluster is therefore even Size = 2(N-1) -- 4^3 and 6^4 --
        // below the distinct-magnitude bound 2N-1 (5^3 / 7^4). (Odd cubes still need 2N-1.)
        public static string FindOrientationCluster()
        {
            int n = TAffine.N;
            int orientCount = AllOrientations().Count;

            var cube = new TRubikCube();
            var sizeByCluster = new Dictionary<int, int>();
            var exampleByCluster = new Dictionary<int, TCubie>();
            foreach (var cubie in cube.Cubies)
            {
                int ci = cubie.ClusterIndex;
                if (!sizeByCluster.ContainsKey(ci)) { sizeByCluster[ci] = 0; exampleByCluster[ci] = cubie; }
                sizeByCluster[ci]++;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Orientation-cluster finder  (N = {n}, Size = {TRubikCube.Size})");
            sb.AppendLine($"  cubie orientations (rotation group) : {orientCount}");
            sb.AppendLine($"  cubies total                        : {cube.Cubies.Length}");
            sb.AppendLine($"  distinct clusters                   : {sizeByCluster.Count}");
            sb.AppendLine();

            int matches = 0;
            foreach (var kv in sizeByCluster) if (kv.Value == orientCount) matches++;
            if (matches == 0)
                sb.AppendLine($"  no cluster of size {orientCount} at Size = {TRubikCube.Size}  (for N = {n} set Size >= {2 * (n - 1)} even, e.g. {2 * (n - 1)}; odd needs {2 * n - 1}).");
            else
            {
                sb.AppendLine($"  clusters of size {orientCount} (every cubie there reaches all orientations):");
                foreach (var kv in sizeByCluster)
                    if (kv.Value == orientCount)
                    {
                        var c = exampleByCluster[kv.Key];
                        sb.AppendLine($"    clusterIndex {kv.Key}   example StartIndex {c.StartIndex}   coords ({string.Join(",", c.Position)})");
                    }
            }
            sb.AppendLine();
            sb.AppendLine("  cluster-size histogram (size : how many clusters):");
            var bySize = new SortedDictionary<int, int>();
            foreach (var v in sizeByCluster.Values) { bySize.TryGetValue(v, out var cnt); bySize[v] = cnt + 1; }
            foreach (var kv in bySize)
                sb.AppendLine($"    {kv.Key,6} : {kv.Value}");
            return sb.ToString();
        }

        // VERIFY the cluster metric empirically. The metric keys a cluster by the sorted |coord| multiset; the TRUE
        // cluster is the orbit of a position under the actual move group. This BFSes the real orbit (apply every
        // move, follow ONE tracked position) and partitions each metric-cluster into true orbits. A metric-cluster
        // that splits into >1 orbit is WRONGLY MERGED. Theory (determinant invariant): every move is det +1 on the
        // signed-permutation of a position, so a class with ALL-DISTINCT nonzero magnitudes contains both dets and
        // splits into >=2 orbits -> merged; a class with any REPEATED magnitude is a single orbit -> fine. Distinct
        // magnitudes need Size >= 2N (even) / 2N+1 (odd), so the bug is DORMANT below that and REAL at/above it.
        // Run on a small safe cube (all "1 orbit") and a large one (e.g. 6^3 / 7^3, expect the {distinct} classes
        // to show "2 orbits  <-- MERGED"). O(orbit * moves * replay) -- a one-off diagnostic, fine up to ~7^3/8^4.
        public static string VerifyClusterOrbits()
        {
            int n = TAffine.N, size = TRubikCube.Size;
            var baseCube = new TRubikCube();
            var byCluster = new Dictionary<int, List<int>>();            // metric clusterIndex -> cubie array slots
            for (int slot = 0; slot < baseCube.Cubies.Length; slot++)
            {
                int ci = baseCube.Cubies[slot].ClusterIndex;
                if (!byCluster.TryGetValue(ci, out var l)) { l = new List<int>(); byCluster[ci] = l; }
                l.Add(slot);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Cluster-orbit verification  (N = {n}, Size = {size})");
            sb.AppendLine("  metric cluster = sorted |coord| multiset;  true orbit = BFS under real Turn moves.");
            sb.AppendLine("  a metric cluster that splits into >1 orbit is WRONGLY MERGED by the metric.");
            sb.AppendLine();

            int mergedClusters = 0, mergedCubies = 0;
            foreach (var kv in byCluster.OrderBy(k => k.Key))
            {
                var memberKey = new Dictionary<string, int>();          // solved-position key -> slot
                foreach (var slot in kv.Value) memberKey[PosKey(baseCube.Cubies[slot].Position)] = slot;
                var uncovered = new HashSet<string>(memberKey.Keys);
                var orbitSizes = new List<int>();
                while (uncovered.Count > 0)
                {
                    int startSlot = memberKey[uncovered.First()];
                    var orbit = OrbitOf(startSlot);                     // set of position keys reachable by moves
                    int inThisCluster = 0;
                    foreach (var pk in orbit) if (uncovered.Remove(pk)) inThisCluster++;
                    orbitSizes.Add(inThisCluster);
                }
                bool merged = orbitSizes.Count > 1;
                if (merged) { mergedClusters++; mergedCubies += kv.Value.Count; }
                // only print the interesting ones in full; summarise sizes
                sb.AppendLine($"  cluster {kv.Key,4}: metric size {kv.Value.Count,4} -> {orbitSizes.Count} orbit(s) " +
                              $"{{{string.Join(",", orbitSizes)}}}" + (merged ? "   <-- MERGED" : ""));
            }
            sb.AppendLine();
            sb.AppendLine(mergedClusters == 0
                ? "  RESULT: every metric cluster is a single true orbit -> metric CORRECT at this size."
                : $"  RESULT: {mergedClusters} cluster(s) merged (covering {mergedCubies} cubies) -> metric WRONG at this size.");
            return sb.ToString();
        }

        static string PosKey(int[] pos) => string.Join(",", pos);

        // The true orbit of the cubie in array slot `targetSlot`: the set of positions it can reach under the move
        // group. BFS where each node is a position; expand by replaying the node's move-sequence from a solved cube,
        // then applying every move GetAllMoves lists for the target at that position. Positions are stable identities
        // (Turn rotates Transforms in place, never reorders Cubies), so Cubies[targetSlot] is always the same cubie.
        static HashSet<string> OrbitOf(int targetSlot)
        {
            var visited = new HashSet<string>();
            var parent = new Dictionary<string, List<int>>();          // position key -> moves from solved
            var start = new TRubikCube();
            string sk = PosKey(start.Cubies[targetSlot].Position);
            visited.Add(sk); parent[sk] = new List<int>();
            var queue = new Queue<string>();
            queue.Enqueue(sk);
            while (queue.Count > 0)
            {
                var pk = queue.Dequeue();
                var seq = parent[pk];
                var cube = new TRubikCube();
                foreach (var code in seq) cube.Turn(TMove.Decode(code));
                cube.ActiveCubie = cube.Cubies[targetSlot];
                foreach (var code in cube.GetClusterMoves())
                {
                    var cube2 = new TRubikCube();
                    foreach (var c in seq) cube2.Turn(TMove.Decode(c));
                    cube2.Turn(TMove.Decode(code));
                    var np = PosKey(cube2.Cubies[targetSlot].Position);
                    if (visited.Add(np))
                    {
                        var next = new List<int>(seq) { code };
                        parent[np] = next;
                        queue.Enqueue(np);
                    }
                }
            }
            return visited;
        }

        // All reachable cubie orientations, generated by closing the identity under every quarter-turn.
        // Convention-safe: the same Rotate that Turn uses, keyed by the packed signed-permutation (OrthoPack).
        static List<TAffine> AllOrientations()
        {
            var seen = new Dictionary<uint, TAffine>();
            var queue = new Queue<TAffine>();
            var id = new TAffine();
            seen[id.OrthoPack()] = id;
            queue.Enqueue(id);
            while (queue.Count > 0)
            {
                var o = queue.Dequeue();
                for (int plane = 0; plane < TAffine.Planes.Length; plane++)
                    for (int angle = 1; angle <= 3; angle++)
                    {
                        var c = ApplyMove(o, plane, angle);
                        var k = c.OrthoPack();
                        if (!seen.ContainsKey(k)) { seen[k] = c; queue.Enqueue(c); }
                    }
            }
            return new List<TAffine>(seen.Values);
        }

        // ALL P! plane orders (not just the left-valid ones). The greedy's solve-check keeps only orders that
        // actually reduce to identity under the given strategy, so this covers left-valid, right-valid AND
        // mixed-valid orders correctly - removing the left-bias of AllOrders (its zero-propagation rule is
        // derived for row/left ops only). results[0] = natural order [0,1,...,P-1] (the standard baseline).
        static List<int>[] AllPlaneOrders()
        {
            int P = TAffine.Planes.Length;
            var a = new List<int>();
            for (int i = 0; i < P; i++) a.Add(i);
            var results = new List<List<int>>();
            PermutePlanes(a, 0, results);
            return results.ToArray();
        }

        static void PermutePlanes(List<int> a, int k, List<List<int>> results)
        {
            if (k == a.Count) { results.Add(new List<int>(a)); return; }
            for (int i = k; i < a.Count; i++)
            {
                (a[k], a[i]) = (a[i], a[k]);
                PermutePlanes(a, k + 1, results);
                (a[k], a[i]) = (a[i], a[k]);
            }
        }

        static List<TAffine> SamplePick(List<TAffine> src, int k, Random rnd)
        {
            var idx = new List<int>();
            for (int i = 0; i < src.Count; i++) idx.Add(i);
            for (int i = idx.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (idx[i], idx[j]) = (idx[j], idx[i]); }
            var pick = new List<TAffine>();
            for (int i = 0; i < k && i < idx.Count; i++) pick.Add(src[idx[i]]);
            return pick;
        }

        static long Factorial(int m) { long f = 1; for (int i = 2; i <= m; i++) f *= i; return f; }

        static List<int>[] SampleOrders(List<int>[] src, int k, Random rnd)
        {
            var idx = new List<int>();
            for (int i = 1; i < src.Length; i++) idx.Add(i);       // keep index 0 (natural order), shuffle the rest
            for (int i = idx.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (idx[i], idx[j]) = (idx[j], idx[i]); }
            var sel = new List<List<int>> { src[0] };
            for (int i = 0; i < k - 1 && i < idx.Count; i++) sel.Add(src[idx[i]]);
            return sel.ToArray();
        }

        // All valid QR plane orders (zero-propagation rule, same set GetOrder samples). orders[0] = standard.
        static List<int>[] AllOrders()
        {
            var results = new List<List<int>>();
            OrderDfs(new List<int>(), new bool[TAffine.N, TAffine.N], results);
            return results.ToArray();
        }

        static void OrderDfs(List<int> order, bool[,] mask, List<List<int>> results)
        {
            int planeCount = TAffine.Planes.Length;
            if (order.Count == planeCount) { results.Add(new List<int>(order)); return; }
            for (int p = 0; p < planeCount; p++)
            {
                if (order.Contains(p)) continue;
                int a = TAffine.Planes[p][0], b = TAffine.Planes[p][1];
                if (!(a == 0 || (mask[b, a - 1] && mask[a, a - 1]))) continue;
                mask[b, a] = true; order.Add(p);
                OrderDfs(order, mask, results);
                order.RemoveAt(order.Count - 1); mask[b, a] = false;
            }
        }

        // When true, Dfs skips a candidate move in the SAME plane as the previous one (Rz90 Rz90 = Rz180 is a
        // composition). Test switch for "does forbidding same-plane-consecutive lose distinct manoeuvres, or is
        // the composed move found anyway?" - Correct() already folds these for the count, so equal counts => lossless.
        static bool PruneSamePlane = false;

        // Every shortest manoeuvre the greedy can produce under one metric variant (perm, order, reversed, mode):
        // branch on ALL quarter-turns dropping RotCount by one. Slice is inert, so genes use slice 0 (no frame).
        // A mixed mode can hit rc==0 short of identity, so Dfs keeps only paths that actually solve.
        static HashSet<string> AllGreedy(TAffine o, int permIdx, int orderIdx, bool reversed, int modeMask)
        {
            var work = new TAffine();
            work.M.Assign(o.M);
            var results = new HashSet<string>();
            Dfs(work, permIdx, orderIdx, reversed, modeMask, RotCount(o, permIdx, orderIdx, reversed, modeMask), new List<int>(), results, -1);
            return results;
        }

        static void Dfs(TAffine o, int permIdx, int orderIdx, bool reversed, int modeMask, int rc, List<int> genes, HashSet<string> results, int prevPlane)
        {
            if (rc == 0)
            {
                if (o.OrthoPack() == identityPack)   // solve-check: mixed modes can reach rc==0 with M != I
                {
                    var g = new List<int>(genes);
                    CorrectGenes(g);
                    results.Add(string.Join(" ", g));
                }
                return;
            }
            for (int plane = 0; plane < TAffine.Planes.Length; plane++)
            {
                if (PruneSamePlane && plane == prevPlane) continue;   // same-plane-consecutive = a composition
                for (int angle = 1; angle <= 3; angle++)
                {
                    var c = ApplyMove(o, plane, angle);
                    if (RotCount(c, permIdx, orderIdx, reversed, modeMask) == rc - 1)
                    {
                        genes.Add(new TMove { Plane = plane, Angle = angle, Axis = FirstAxis(plane), Slice = 0 }.Encode());
                        Dfs(c, permIdx, orderIdx, reversed, modeMask, rc - 1, genes, results, plane);
                        genes.RemoveAt(genes.Count - 1);
                    }
                }
            }
        }

        // Moves-to-solve of orientation o under a metric variant: permute axes by perms[permIdx] (PᵀMP), optionally
        // transpose (reversed), then reduce plane-by-plane in orders[orderIdx] with a per-plane strategy packed 2
        // bits per plane in modeMask (base-4): 0=pod-L, 1=nad-L, 2=pod-R, 3=nad-R. pod/nad = zero the SUB- vs
        // SUPER-diagonal; L/R = row op (left mult, Rotate, -sin) vs column op (right mult, RotatePost, +sin). A
        // right op in the reduction is still realizable: L*M*R = I => M^-1 = R*L, a product of Givens applied as
        // real (left-mult) cube moves. Count non-identity rotations. modeMask=0,order=0,reversed=false = QR baseline.
        static int RotCount(TAffine o, int permIdx, int orderIdx, bool reversed, int modeMask)
        {
            int planeCount = TAffine.Planes.Length;
            int modeSpace = 1 << (2 * planeCount);              // 4^P: two bits (pod/nad, L/R) per plane
            long variant = ((permIdx * (long)orders.Length + orderIdx) * 2 + (reversed ? 1 : 0)) * modeSpace + modeMask;
            long span = perms.Length * (long)orders.Length * 2 * modeSpace;
            long key = (long)o.OrthoPack() * span + variant;
            if (rotMemo.TryGetValue(key, out var cached)) return cached;

            int n = TAffine.N;
            var perm = perms[permIdx];
            var A = new TMatrix(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    A[y, x] = o.M[perm[y], perm[x]];
            if (reversed) A = A.Transpose();

            var order = orders[orderIdx];
            int c = 0;
            for (int i = 0; i < planeCount; i++)
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
                else           A.RotatePost(a1, a2, cos, sin);     // right: col op, +sin (user's convention)
                if (TCubie.GetAngle(cos, sin) != 0) c++;
            }
            rotMemo[key] = c;
            return c;
        }

        // Pack a P-bit pod/nad pattern into the base-4 modeMask with every plane forced LEFT (strat 0 or 1) - used
        // to reproduce the original left-only 2^P mode sweep inside the new 4^P strategy space.
        static int SpreadLeft(int podNad)
        {
            int code = 0;
            for (int p = 0; p < TAffine.Planes.Length; p++)
                if ((podNad & (1 << p)) != 0) code |= 1 << (2 * p);
            return code;
        }

        static TAffine ApplyMove(TAffine o, int plane, int angle)
        {
            var c = o.Clone();
            var pa = TAffine.Planes[plane];
            var rot = TCubie.SetAngle(angle);
            c.Rotate(pa[0], pa[1], rot.X, rot.Y);
            return c;
        }

        static int FirstAxis(int plane)   // lowest axis not in the plane (collateral layer selector)
        {
            var pa = TAffine.Planes[plane];
            int a = 0;
            while (a == pa[0] || a == pa[1]) a++;
            return a;
        }

        // TRubikGenome.Correct's composition on a variable-length gene list: fold each move into the nearest
        // preceding run of the same axis+plane (quarter-turns add mod 4; a 0 result cancels both).
        static void CorrectGenes(List<int> g)
        {
            for (int idx = 0; idx < g.Count; idx++)
            {
                var move = TMove.Decode(g[idx]);
                for (int prev = idx - 1; prev >= 0; prev--)
                {
                    var pm = TMove.Decode(g[prev]);
                    if (pm.Axis != move.Axis) break;
                    if (pm.Plane != move.Plane) break;
                    if (pm.Slice == move.Slice)
                    {
                        int angle = (move.Angle + pm.Angle) & 3;
                        if (angle > 0) { pm.Angle = angle; g[prev] = pm.Encode(); }
                        else { g.RemoveAt(prev); idx--; }
                        g.RemoveAt(idx); idx--;
                        break;
                    }
                }
            }
        }

        static int Len(string seq)   // number of moves in a space-joined sequence ("" = solved, length 0)
        {
            if (seq.Length == 0) return 0;
            int c = 1;
            foreach (var ch in seq) if (ch == ' ') c++;
            return c;
        }

        static string ExampleExtra(TAffine o, HashSet<string> idSet, HashSet<string> union)
        {
            foreach (var s in union)
                if (!idSet.Contains(s))
                {
                    if (s.Length == 0) return "(identity)";
                    var sb = new StringBuilder();
                    foreach (var code in s.Split(' '))
                    {
                        var m = TMove.Decode(int.Parse(code));
                        sb.Append($"p{m.Plane}a{m.Angle}s{m.Slice} ");
                    }
                    return sb.ToString().Trim();
                }
            return null;
        }

        static int[][] Permutations(int n)
        {
            var nums = new int[n];
            for (int i = 0; i < n; i++) nums[i] = i;
            var res = new List<int[]>();
            Permute(nums, 0, res);
            return res.ToArray();
        }

        static void Permute(int[] a, int k, List<int[]> res)
        {
            if (k == a.Length) { res.Add((int[])a.Clone()); return; }
            for (int i = k; i < a.Length; i++)
            {
                (a[k], a[i]) = (a[i], a[k]);
                Permute(a, k + 1, res);
                (a[k], a[i]) = (a[i], a[k]);
            }
        }
    }
}
