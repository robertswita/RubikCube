---
name: cluster-orbit-structure
description: "Cube cluster/orbit structure - **COHERENCE DECOMPOSITION IS MICRO-ONLY + SIZE-GENERALISED (2026-07-31).** The coherence eval lives ONLY in
EvaluateMicro (Gpu.cs: `micro = Cubies.Length <= 82`); bigger cubes (6^3 = 216) use EvaluateMacro, which has NO
coherence -> `Best.Structure = 0` there is expected, not a bug. The Micro decomposition was generalised from BINARY
(bit box, 2-way split, 2^k complete -- correct only for SIZE=2) to ANY SIZE: box fixes a full coord VALUE (via a
ref-cubie), split is 2N-ary by the DISTINCT values present, and COMPLETENESS = `cnt == perms * 2^nz` where
perms = (#varying)!/prod(magnitude-multiplicity!), nz = #varying nonzero axes (magnitude = |2*coord-(SIZE-1)|).
This counts the sub-ORBIT ({a,a,a,a}->16 vs {a,a,b,b}->96 via perms), NOT a grid. Validated on 4^3 (SIZE=4).
The PARITY /2 (det+1) for all-distinct-nonzero clusters is DEFERRED but PROVEN UNNECESSARY for realistic Micro
cubes: all-distinct needs floor(SIZE/2) >= N, but Micro (<=82, N>=3) caps SIZE (N=3->SIZE<=4, N=4->SIZE<=3), so
NO all-distinct cluster ever occurs there -> every Micro cluster is oddStab (has a repeat or a zero) -> the core is
EXACT. Still base-2 downstream (carry ceil(occ/2), gateway G=2^(N-1), even-guard) -- may need the 2N-ary carry
(÷branching) if 4^3 doesn't close well; unmeasured. Old note (cluster orbit theory) follows.

full-orientation cluster exists at even Size=2(N-1) (4^3, 6^4), and the |coord|-multiset cluster metric is correct for every cube in use (only all-distinct-magnitude classes on Size>=2N could merge chiral orbits, unverified & dormant)."
metadata: 
  node_type: memory
  type: reference
  originSessionId: 95217005-1638-45fd-b8b9-7a2254e5a463
  modified: 2026-07-23T07:53:06.714Z
---

**UPDATE 2026-08-01 — CHIRALITY BIT IS IN; CLUSTERS NO LONGER MERGE.** `TCubie.Index` now stamps
`ClusterIndex = 2*key + Chirality(position)` (TCubie.cs:185-194), so the |coord|-multiset key is extended by the
signed-permutation determinant sign for the all-distinct-nonzero case. `ClusterIndex` therefore SPLITS the two chiral
orbits instead of merging them: every ActiveCluster is now a SINGLE rotation orbit, size <= `N!*2^(N-1)` (never the
`N!*2^N` merged class). Below Size 2N the bit is always 0 (no class is chiral) so ranks are unchanged. Consequence for
sizing: the coherence scratch is bounded by the max cluster, a function of N only (SIZE-independent). The host injects
`MAX_CLUSTER = TRubikCube.MaxClusterSize` (max over ClusterIndex groups, RenumberClusters -> Clusters.Max(Size)) as a
compile-time #define, and EvaluateMicro sizes `scr`/`grp`/`stateDone` + the DFS guard by it instead of CUBIES_COUNT
(=SIZE^N) -- so the decomposition is Macro-safe (scratch stays tiny even when SIZE^N is huge). The DFS stack
`stMask/stRef[2*N*N]` stays N-sized: branching is <= 2N (a cluster has <= N distinct magnitudes -> <= 2N signed values
per axis), NOT SIZE, so a 2N-ary depth-N tree fits 2*N*N regardless of SIZE. NOTE: the SHADER decomposition's parity
/2 (block completeness for all-distinct-nonzero sub-blocks) is still deferred -- irrelevant for cubes in use (no
all-distinct cluster below Size 2N). The old note below (written BEFORE the bit landed) called the merge "dormant &
unverified"; that is now superseded -- the bit is live.

Two facts about cubie CLUSTERS (position orbits) and ORIENTATION, established 2026-07-23. Kept because the code
comments stated the first wrongly and I briefly mis-called the second a bug.

**1. A FULL-ORIENTATION cluster (orbit size = 2^(N-1)*N! = every cubie there reaches ALL orientations, trivial
rotation stabilizer) exists at even Size = 2(N-1), NOT the 2N-1 the old comments claimed.** So 4^3 and 6^4 have one
(4D drops from 7^4=2401 cubies to 6^4=1296). Reason: a trivial rotation stabilizer needs that no proper rotation
(signed permutation, det +1) fixes the cubie. Distinct distance MAGNITUDES are sufficient but NOT necessary -- in an
EVEN cube a repeated magnitude carried with OPPOSITE signs (a coord just under and just over the centre) can only be
swapped by a REFLECTION (det -1), which is not a rotation, so the stabilizer stays trivial anyway. Odd cubes still
need 2N-1. Detected by ORBIT SIZE == orientation count, which is exact and sign-aware -- FindOrientationCluster
already did this right; only its comment/hint were wrong (fixed). Verify's target-picking used the too-strict
distinct-magnitude test and so bailed on 6^4 ("set Size >= 7") -- changed to the orbit-size test so it runs on 6^4.
Verify is the PHYSICAL side (forces the target cubie into each orientation, applies REAL turns, checks solve + records
whole-cube effect) so it NEEDS a cube containing such a cluster; Run/Greedy-Diversity is the THEORETICAL side (counts
greedy sequences over the abstract rotation group, no physical cube).

**2. The cluster metric keys on the sorted multiset of |coord| (distance-from-centre magnitudes) --
`ClusterIndex = Coords2Index(sort_desc(|Origin[dim]|))` in TCubie.Index. It is CORRECT for every cube the project
uses (2^4, 2^5, 3^4, 4^3) -- I over-claimed "wrong for even cubes"; the even-cube full-cluster observation does NOT
impugn it (that is about the ORIENTATION stabilizer, orthogonal to the POSITION-orbit metric; the metric reports those
clusters at size == orientCount, i.e. it AGREES).** Moves preserve the magnitude multiset (a plane-(i,j) turn only
swaps |x_i|<->|x_j|), so an orbit is always CONTAINED in one magnitude class -- the metric NEVER wrongly splits an
orbit, it can only MERGE. It merges iff a class has more positions than the max orbit: a class with any REPEATED
magnitude has |class| = N!*2^(N-1) = |orbit| (fine); a class with ALL-DISTINCT nonzero magnitudes has |class| =
N!*2^N = 2x the rotation-orbit, so it MAY be two chiral orbits merged into one cluster. That needs Size >= 2N (even) /
2N+1 (odd) -- 6^3, 8^4, ... -- so it is DORMANT (no cube in use reaches it), and even then only bites if the move
group is chirality-restricted (det +1), which is UNVERIFIED. VERIFY before touching: BFS under Turn from one
distinct-magnitude cubie (e.g. (c+1,c+2,c+3) in 7^3) -- reach 48 => one orbit, metric OK; reach 24 => two orbits,
metric merges. ONLY IF it merges: the cheap fix is NOT the stabilizer but appending ONE bit to the key for the
all-distinct case -- the sign of the signed-permutation determinant (which axis got which magnitude, times the signs)
= det +/-1 = the chirality that separates the two orbits, O(N). But adding that bit when the group does NOT split
would WRONGLY split one orbit into two, so the BFS check must come first. Related: [[coherence-eval-solution]].
