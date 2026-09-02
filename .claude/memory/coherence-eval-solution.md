---
name: coherence-eval-solution
description: "Coherence evaluator arc (2026). CURRENT BEST = 2*P*S ladder, GATEWAY (G+0, single G-block = one move from solved) closing target, pow2 pre-guard + full-block(agreeAxes=0) exclusion. NO E (that chased G+G, wrong). 3^3: 5 batches ~168 mean, worst tail 527, var <20k."
metadata:
  node_type: memory
  type: project
  originSessionId: 95217005-1638-45fd-b8b9-7a2254e5a463
  modified: 2026-08-02T06:24:02.846Z
---

***** PARTLY SUPERSEDED 2026-08-01 -- read this first: *****
- Metric moved to **2 * P2 * S** (P2 = DEPTH-weighted parent count, not plain P), and is being A/B'd against the
  ADDITIVE **2*(P2 + N*S)** (MEASURE=P2+NS). Additive ties Floor(1+1+1+1) with 2+1+1, opening the floor->2+2 growth path
  the multiplicative P2*S walls (2+1+1 surcharge). Which wins on the tail is still open.
- **Decomposition UNIFIED**: every residual is decomposed for its REAL structure (no pow2/even pre-guard, no
  all-singletons proxy/lie). A multiplicative "reward as discount <1" variant was tried and REVERTED -- it can only
  lower the base, so it CAN'T deter the mono-twist (a lone singleton stays cheap -> peel isolates it); the additive
  ladder scores the singleton EXPENSIVELY (its real 2*P2*S), which deters isolation. Additive is structure-dominant.
- **Latch is no longer Floor-alone**: coherence engages on STALL -- `Scrambled < Floor && Stall > Floor` (see
  [[unified-evaluator-direction]]). Floor still lives in NORM only.
- The 3^3 numbers below are void (pre-unification). The everything below is the 2026-07-31 arc, kept for the reasoning
  (gateway G+0, S load-bearing, E is backwards) which still holds. Old text follows.

***** CURRENT BEST (2026-07-31): 2*P*S ladder, GATEWAY (G+0) closing target, POW2 pre-guard + full-block exclusion. *****
Metric numerator = **2 * P * S** (P = plain parent count, S = #distinct block sizes), no E, no coupling; /(NORM*(countActive+1)).
- **THE KEY INSIGHT (empirical, 5 batches): the closing target is the GATEWAY G+0 (a SINGLE coherent G-block, G=2^(N-1),
  agreeAxes=1), NOT G+G.** A single G-block is the 2^(N-1) coherent LAYER = the gateway. "One move from solved" holds
  literally only on 2^N (single cluster); on MULTI-cluster (3^3) closing G+0 WITHOUT disturbing already-solved clusters
  is a COMMUTATOR -- so "one move" = ONE commutator. Still the closest close (one commutator vs several for G+G).
  G+G (two G-blocks, scrambled=2G) is PAST the gateway (~several commutators) -- over-built. So the earlier
  "target = G+G, add E to make k+k < k+0" was BACKWARDS: E pushed the search past the gateway. **Dropping E (k+0 = k+k
  tie) lets it rest on the gateway and close.** DON'T reintroduce E to chase G+G.
- **DISTINGUISH the gateway from the trap:** gateway = G+0, agreeAxes=1, scrambled=G (GOOD, one move from solved);
  FULL BLOCK = 2G, agreeAxes=0 ("solved up to a GLOBAL FRAME ROTATION", TRAP). They are different single blocks; we
  once conflated them ("big single block = bad"), which produced the wrong G+G target. The agreeAxes=0 full block
  leaves P=0 -> ladder 0 (looks SOLVED!), so it MUST be excluded.
- **POW2 pre-guard** (`(scrambled & (scrambled-1))==0`): score only power-of-2 counts; non-powers -> scattered all-
  singletons proxy (else branch). At scrambled==2G, require `aMax==1` (exactly G+G) else WALL as scattered -- this is
  what kills the full-block agreeAxes=0 trap.
- **S is load-bearing** (penalises mixed like 4+2: without it 4+2 == 2+2). **E is NOT** (its only job -- k+k<k+0 -- is
  the wrong direction here). Coupling (Cnorm) PARKED.
- **NORM = 2*(FLOOR-1) + 2*(N - findMSB(FLOOR))** (floor value of 2*P at all-singletons FLOOR). fitness<1 is the real
  requirement (countActive+1 gives headroom), so a stale/off NORM is harmless, only affects floor-pinning.
- **3^3 RESULTS (5 batches, 50 runs, N=3 Floor=4):** means 124-208 (~168), WORST tail 527, var all <20k -- ~4x faster,
  ~7x shorter tail, ~70x tighter variance than the coupling metric on 3^3. Best result the project has had; the corner
  cluster (last, the mono-twist/G-block orbit) now closes cleanly. GPU-computes `ladder` -> Specimen.Ladder; host reads
  Best.Ladder (C# LadderValue deleted). Accept = `Best.Fitness<1 && Best.Ladder<=ScoreLadder` (ladder ties -> sideways).

***** PRIOR (2026-07-30): P2-LADDER + FIXED-COUPLING under a POWER-OF-2 FILTER, GPU-computed ladder. *****
Two-phase: DESCENT (COHERENT=0, bare count peel) until the residual reaches FLOOR (a power of 2, uniform loc 7), then
LATCH the ENDGAME. Endgame metric numerator = **2*(P2 + N*S) + Cnorm**, /((FLOOR+2N-1-findMSB(FLOOR))*(countActive+1)):
- **P2** = depth-weighted parent count (carry par=ceil(occ/2) per depth d, P2+=par*d), **S** = #distinct block sizes.
- **POWER-OF-2 FILTER** (cost): the O(pieces²)+stack decomposition runs ONLY when `scrambled` is a power of 2 (only a
  2^k count can be ONE complete coherent block). Non-powers skip it and are scored as all-singletons on the SAME scale
  (`2*(P2_allsingle+N)`) + stamped an all-singletons histogram (else structure=0 → false "solved" in the accept).
- **Cnorm** = FIXED-coordinate coupling penalty in [0,1): blocks sharing a fixed layer-coord lie in a common layer
  (one move rotates them together). Gives the SINGLETON floor a gradient (spread cheap, clustered dear) — the
  reward-UNfixed alternative can't, singletons have no unfixed axes. Computed by a COUNTING identity, not a pair loop:
  `C = Σ over (axis,value) of choose(k,2)`, k = #blocks fixing that axis to that value → O(pieces·N) not O(pieces²·N).
- **FINE-FITNESS vs STRUCTURE-ACCEPT split:** `ladder = 2*(P2+N*S)` (NO Cnorm) is the ACCEPT metric — GPU-computes it,
  writes it into `Specimen.Ladder`, host reads `Best.Ladder`/`Gpu.LastScoreLadder` (C# `LadderValue` DELETED, so a
  metric change is one shader line, no C# edit). Cnorm rides in the GA-SORT fitness only, NOT the ladder, so
  structurally-equal configs still TIE in the ladder → sideways fire. Accept also gates `Best.Fitness < 1` (kills
  Solved-cluster breaks; fitness normalised < 1).
- **BEST BY TAIL/VARIANCE** ([[measure-tail-not-mean]]): 4 batches N=4 Floor=4, ALL tight — means 1317–2126, worst run
  6193 across 40, vs gole P2 tails 12148/10241 and 2P2 (no coupling) swinging 851↔3275 / tail 21081, var to 36.9M.
  Batch-to-batch STABILITY itself is the quality signal (user's: weak metric = unstable, good = tight+short-tail).
- **SIGN-FLIP CONTROL confirms C is CAUSAL + directional (not noise):** inverting the term to `1−Cnorm` (REWARD coupling
  instead of penalise) reproducibly FATTENS the tail — 2/2 inverted batches had a run ≥13k (47774, 13286) and var ≥14M,
  vs coupling's 0/40 runs >6193 and var ≤2.4M. Even the milder inverted batch beats no coupling batch. Notably the flip
  hurts ONLY the tail (median stays ~normal) → C's job is TAIL control (steering off entanglement traps), not the median.
- **COST STILL UNMEASURED:** `ms/1k-gen` is worthless for this (swings 13.6k↔26k on IDENTICAL 2P2 config — it mixes
  cheap descent gens with dear endgame gens by outcome composition). Need an ISOLATED measure: fixed endgame state,
  fixed gen count, no early-exit, stopwatch. Bucketing didn't visibly move ms/1k-gen (too noisy) — pieces is small in
  the endgame so the coupling loop may never have been the dominant cost (the decomposition is). PROTOCOL to compare
  metrics: tail-MASS `P(gens > threshold)` over 50+ pooled runs (Bernoulli, cheap to estimate), not the mean.
Everything below (walking-floor + sqrt, FragSymmAll, collateral, hole-penalty, ...) is SUPERSEDED history.

***** [SUPERSEDED] earlier best (2026): WALKING FLOOR (starting at 4) + sqrt(s*G) DENOMINATOR. ***** Metric body UNCHANGED from
FragSymmAll below (weakest-piece cost * S + collateral); only two things differ, and both were needed:
(1) **Denominator `N * sqrt(scrambled * G)`** instead of `N * G`. Law behind it: with `f = (cost+collat)/(N*s^c)` the
fitness goes like `s^(1-c) * sum`. A level-up (k+0 -> 2k+0, and identically k+k -> 2k+2k) doubles s while the sum falls
by at worst 13/8, so a STRICT descent needs `2^(1-c) < 1.625`, i.e. **c > 0.30**; a healthy peel among scattered
residuals needs **c < 1**. `c = 0` (the old N*G) breaks the first -- on 2^5 `4+0` (0.400) is WORSE than `2+0` (0.325),
the block chain climbs. `c = 1` breaks the second (count cancels, cubies come off in pairs). `c = 1/2` satisfies both
with margin and is the geometric mean of the two denominators we kept oscillating between.
(2) **FLOOR is a UNIFORM (Setup location 7), not a #define**, and WALKS: `max(4, min(2*d_max, G))` where d_max = the
largest coherent block of the CURRENT cube, read from `Best.Structure` (the evaluator already reports the histogram --
the host never repeats the decomposition). Must follow the CUBE, not the candidate: keyed to the candidate's own d_max
every merge raises its own floor and gets pushed back up. d_max only grows along the climb, so NO latch is needed.
**The lower bound of 4 is worth 5x on the tail**: from 2 -> median 3603 / tail 19308 (falls into the 1+1/mono basin and
brute-forces the last pair); from 4 -> median 1195 / tail 3965. TRAP that cost a run: `LargestPiece` returns **1**, not
0, for a scattered residual (0 means "never decomposed", i.e. above the gateway), so the bound must wrap the WHOLE
expression -- patching the `== 0` branch does nothing. INVARIANT: the GA and `EvalZeroSpecimen` must use the SAME floor
(both set uniform 7), and when the floor moves `RubikCube.Score` must be re-measured -- moving it rescales every fitness.
**2^4 RESULTS (10 runs): min 335, median 1195, TAIL 3965, mean 1393, std 937, var 878k -- BEST EVER on tail, mean and
variance** (previous bests 4928 / 1725 / 2.14M); median 1195 is just behind FragSymmAll's 984.
**THE IDENTITY THAT GOVERNS ALL OF THIS:** adding a symmetric partner raises cost by exactly aMax, while the collateral
block it swallows, `[d,2d)`, has `agreeAxes = N - log2(d) = aMax` -- the SAME number. So `k+0` and `k+k` always have
EQUAL sums, and solving a whole block is exactly free in the structural term. Consequence: any metric that wants k+k
STRICTLY better than k+0 must invert the count, which kills the peel. Tried three times (2*collat, /(N*s), FLOOR=G) --
all measured WORSE (2204 / 2398 / 3292). The mono-twist is hard because it is a SINGLE CUBIE with no handles, not
because it is "1+0": **k=1 is a special case, not the first term of a series.** Don't generalise it again.
**MEASUREMENT CAVEAT:** the same code measured 2-3x worse today than two days earlier (baseline FLOOR=4: median 984
then, 3452 now) with identical defines, STALL and specimen stride -- cause unfound. **Compare only within one session.**

***** PREVIOUS BEST (2026): "FragSymmAll" = WEAKEST-PIECE + SELF-SCALING SYMMETRY, no gates. ***** Assess EVERY
residual (0<scrambled<=gateway) -- no isPow2 gate, no allSame gate, no singleton exclusion (there was never a reason).
RULE: a config is only as good as its SMALLEST piece -- a big piece among smaller ones is a PREMATURE COMMITMENT
(solving a single BREAKS the pair), so cost is computed as if EVERY cubie sat at the weakest level:
`cost = (scrambled / smallest) * aMax * S`, where aMax = max agreeAxes over pieces, smallest = 2^(N-aMax), and
S = **number of DISTINCT piece sizes** (self-scaling SYMMETRY: uniform 1, two levels 2, three 3). Collateral =
`[scrambled, gateway)` lowest-set-bit walk, LINEAR (`+= agreeAxes`). Discount = `(cost+collat)/(gateway*N)`, NOT
capped -- a heavily mixed tree may exceed 1 (a real surcharge; those are liabilities the search should avoid, while
SCATTERED configs stay a discount ~0.56 and remain usable stepping stones). Linear beat squared (squares complicated
without paying). Enforces the user's hierarchy: 2+1+1 > 1+1+1+1, 4+2+2 > 2+2+2+2, 4+2+2 < 1x8, 4+2 < 4+2+2. Odd
scrambled needs no special case: an odd sum of powers of 2 always contains a singleton, so aMax=N automatically, and
the collateral's first block is a size-1 partner. **2^4 RESULTS (10 runs): median 984, TAIL 4928, mean 1731, var
2.14M, 10/10 -- BEST ON ALL THREE AXES**, beating FragSymm (1148/6615/3.2M) and C (2029/6183/2.9M). Path observed:
`1+1+1+1 -> 2+0 -> 2+2 -> 4+0 -> 4+4 -> 8+0 -> ... -> gateway` = literally building the binary tree bottom-up, the
same tree the metric prices it with. Very stable, no hangs, constant activity (never freezes). NEXT: 2^5.

***** MILESTONE (2026): COLLATERAL + SYMMETRY GATE — a ~10x win on 2^4. *** ** The endgame-coherence direction WORKS
after the SYMMETRY GATE.** The collateral metric alone (assessing every config) INVERTED the hierarchy: it scored a
2+1+1 (cost 45) BELOW a 1+1+1+1 (cost 68), i.e. rewarded a premature lone pair over full freedom -- but a lone pair
is a LIABILITY (solving a singleton BREAKS the pair; move = one full 8-cubie layer turn = the gateway, and commutators
change a SUBSET while preserving the rest, so free singletons reach 2+2 easier than a committed 2+1+1). Rewarding
asymmetry created traps -> stalls. FIX (user): assess ONLY SYMMETRIC configs -- binary tree symmetric ⟺ scrambled is
a power of 2 AND all coherent pieces the SAME size (agreeAxes) AND size>=2 (no singletons); asymmetric (2+1+1,
1+1+1+1, 4+2+2, 2+2+2+2+2) is LEFT AT BASE (flat) so the search wanders through them freely. RESULT on 2^4, 9 runs:
median 1137, max 6615, 9/9 solved, ZERO catastrophes -- vs the FragBug+Floor baseline (median 12414, max 30165): ~11x
median, ~4.5x tail; the WORST new run beats the baseline MEDIAN. Path: 1+1+1+1 -> 2+2 -> 4+0 -> gateway-8 -> solved =
a clean SYMMETRIC ladder of growing coherent blocks, each completed by solved collateral boxes. So the working
metric = COLLATERAL (structure/entanglement) + SYMMETRY GATE (only clean symmetric trees) + FLOOR (magnitude/closing).
Impl is in EvaluateMicro: decomposition tracks firstAgree/allSame; gate `(scrambled&(scrambled-1))==0 && allSame &&
firstAgree<N`; collateral = lowest-set-bit walk over [scrambled,gateway); discount `*= (cost+collat)/(gateway*N^2)`.
NEXT: test on 2^5 (coherence's real target). Deferred idea: PENALISE asymmetry (keep 1+1+1+1 > 2+1+1 strictly).
OFFICIAL 2^4 COMPARISON (10 runs each, GA count -- lower better; judge by TAIL/VARIANCE): FragSymm median 1148 / max
6615 / mean 1725 / var 3.2M -- BEST. C (flat-discount, the prior champion) median 2029 / max 6183 / mean 2914 / var
2.9M -- runner-up (tiny bit lower variance but shifted ~1.8x HIGHER). D3 (cliff base) median 1582 / max 12813 / var
13.5M. D median 5843 / max 12046 / var 12.9M. D2 median 4278 / max 22842 / var 46.5M. Takeaway: FragSymm and C are the
ONLY two with a bounded tail (~6k); D/D2/D3 are 2x fatter tail, 4-15x variance. FragSymm DETHRONES C -- beats it on
median/mean, ties the tail, same variance class. (Buggy FragBug+Floor baseline was median 12414 / max 30165 / var
~90M, so FragSymm ~11x median, ~28x variance.)

**COLLATERAL-TREE metric (2026, user's design — the reopened endgame direction, currently WIRED, awaiting 2^4 runs):**
The endgame-coherence direction was REOPENED: the bug was never coherence itself, it was that the metric IGNORED
solved cubies. A "nice" coherent-2 in the endgame is really entangled with the SOLVED cubies its solving-turn would
disturb -- so a coherent-6 that scored 13 (4+2) is really (4+2)+2 = 22. SYMMETRY gives the rule: assess the residual
against the GATEWAY slice 2^(N-1) (the first-turn target) as a binary tree; unsolved coherent blocks are leaves
(cost agreeAxes^2 = f^2 for a 2^(N-f) block); the COLLATERAL (solved cubies filling the slice) are also leaves.
KEY SIMPLIFICATION (validated): unsolved blocks tile [0,scrambled) exactly, so collateral = the interval
[scrambled, gateway), whose maximal aligned blocks depend ONLY on scrambled (a lowest-set-bit walk -- NO iterating
solved cubies). So `cost = Σ agreeAxes²(unsolved blocks) + Σ agreeAxes²(collateral interval)`, applied as a DISCOUNT
`*= cost / (gateway * N^2)` in (0,1]. Properties (all validated in scratchpad/collateral_cost.py): coherent-8=1,
coherent-4=8, coherent-2=22, 4+2=22, 2+2+1=59, mono-twist=45. This (a) prices entangled endgames honestly (no more
freeze on a "nice" 6), (b) makes a MONO-TWIST the most expensive COST -> the collateral subsumes the anti-OVER-DESCENT part of the floor,
(c)
is a uniform discount with a CONSTANT denominator (gateway*N^2, per user "can't set the gateway arbitrarily") so a
fully-scattered full gateway = 1 = base -> CONTINUOUS with the bare-count peel above the gateway (no boundary barrier
-- the lesson respected). Structure: coherence-collateral for 0<scrambled<=gateway, bare peel above, static COHERENCE=1 (latch reverted).
FIRST 2^4 RESULT: it SOLVED (45581 GA) and the collateral KILLED THE FREEZE on entangled "nice" configs (diagnosis
confirmed) -- BUT slow, because `fitness = base * discount` with base ~ scrambled makes the DEEP endgame nearly flat
(mono 1*45 ~ coherent-2 2*22) -> sideways wander. So the FLOOR is still needed (re-added): `*= N/scrambled` for
scrambled<=N cancels the scrambled -> `fitness ~ magnitude * cost`, a real closing gradient (mono 45 clearly >
coherent-2 22). So the split is: COLLATERAL = structure (entanglement, anti-over-descent), FLOOR = magnitude/closing.
Boundary smooth (*N/N=1 at N). Awaiting multi-run 2^4 tail vs baseline (median 12.4k, tail 30k). Fallback: baseline below.

**RESTING POINT (2026, end of the endgame-coherence arc): COHERENCE EARNS ITS KEEP ONLY ON THE DESCENT; the 2^4
ENDGAME IS AN IRREDUCIBLE COMMUTATOR SEARCH.** Every attempt to make coherence help AT/BELOW the floor FAILED, both
polarities proven: as a PENALTY (raw agreeAxes² surcharge) the search FROZE on the first scattered-4 (fragmented shoved
above the floor, avoided); as a REWARD (/N² discount) it CLIMBED (4->9->13, un-solving to chase coherent structure)
and stuck on a "nice 6" that is NOT easy -- a coherent-looking 6 is a hard commutator. This is the arc's OLDEST lesson
re-confirmed the hard way: COHERENCE LIES ABOUT MOVE-COUNT, so it cannot shortcut the endgame. User: "zaszliśmy z
analogią za daleko." FINAL config = the proven baseline: coherence gated `scrambled > N` (credits structure on the
DESCENT, reaches the floor), FLAT floor `*= N/scrambled` at <= N, static `COHERENCE=1`, multiplier `cost/scrambled`.
On 2^4 that is **10/10 solved, tail ~30k, no hangs** -- the ceiling. The endgame closes by FLAT-floor + STALL walk +
GA move-search finding the commutator, NOT by any coherence gradient. Reverted the CPU latch (Gpu.SetCoherence now
dormant/unused). NEXT directions if revisited: (a) accept 2^4=10/10 and test whether descent-coherence actually helps
2^5 (coherence's real job - bigger gateway); (b) attack the endgame via SEARCH (commutator seeds), NOT the metric.

**COHERENCE MUST BE A REWARD (discount <=1), NOT A PENALTY (surcharge) (2026, user) -- TRIED, it CLIMBS (see RESTING
POINT above); kept for the record but the endgame-coherence direction is CLOSED:** The exact-fragmentation
rewrite made `local_fA_sum *= cost/scrambled` with cost = SUM agreeAxes² -- a SURCHARGE: a scattered-4 got x16, so it
was shoved ABOVE the floor, the search AVOIDED it and FROZE on the first scattered-4 it hit (couldn't use scattered
configs as stepping stones, couldn't reach the easy-to-find 2+1+1). The original coherence was a REFUND; the rewrite
inverted it. FIX: normalise by the ALL-SINGLETONS cost -> `local_fA_sum *= cost / (scrambled * N*N)`, a DISCOUNT in
(0,1]: fully-scattered (every cubie a singleton, cost = scrambled*N²) = EXACTLY 1 (no effect, stays at the floor
baseline so ALL scattered configs are EQUAL -> free wander + stepping stones); any coherent structure < 1 (refund
BELOW the floor: 2+1+1 ~0.64, 2+2 ~0.28, coherent-4 ~0.06 on 2^4). Keep the exact decomposition + agreeAxes² (it
shapes the ladder); the ONLY change is the extra /N² that flips surcharge->refund. Watch: the <=1 refund also rewards
structure ABOVE the floor, so the search MAY climb to a coherent-8 (a legit 1-move gateway on 2^4, not a dead-end, but
watch it doesn't climb instead of closing). This pairs with the CPU coherence LATCH (below) -- coherence off on the
descent, on in the endgame.

**THE FLOOR'S FLATNESS IS PROTECTIVE — do NOT put a coherence gradient at the floor (2026, empirical):** After
restoring the floor, "fixing" the cosmetic issue that a 2+1+1 and a scattered-4 score identically at the floor (by
running coherence for ALL scrambled, gradient at the floor too) REGRESSED HARD: 2^4 HUNG at 9 (above the floor!),
error flat, 12 min, vs the baseline's 10/10. Cause: a coherence gradient at the floor rewards BIG coherent structures,
so it pulls the search to UN-SOLVE / climb back UP to build one (the un-solve-to-consolidate pathology, cf. 2^5
14->16). The floor being FLAT on structure (coherence gated `scrambled > N`, OFF at <= N) is PROTECTIVE: no gradient,
no climb incentive; the STALL walk + orientation magnitude still stumble onto a coherent-4 and close (user's endgame
model 2+1+1 -> 2+2 -> coherent-4 -> solved happens by WANDERING, not by a floor gradient). So "2+1+1 == scattered-4 at
the floor" is BY DESIGN, not a bug. Correct gate: coherence `scrambled > N` only. Baseline (D3 + this) = 10/10 solved
on 2^4, tail ~30k, NO hangs. Open NEXT lever (user's hunch, unverified): coherence may HINDER the DESCENT too (above
the floor) -> A/B coherence OFF above floor (pure count-peel to reach the floor) and see if the tail tightens; the
direction is LESS coherence, not more. (Coherence may be mainly a 2^5 tool - bigger gateway, harder to stumble on -
while on 2^4 the floor+STALL suffice.)

**THE FLOOR IS LOAD-BEARING — removing it caused over-descent (2026, user's diagnosis, the real fix):** After the
exact metric reached genuinely-coherent plateaus, tuning the metric SHAPE proved INERT — linear / sqrt(concave) /
agreeAxes²(superlinear) ALL stalled at the SAME last cycle (2+1 = a coherent pair + a singleton defect; D4 base sank
even to a lone mono-twist). Metric shape is NOT the lever for the last cycle. The real cause: the exact-decomposition
rewrite THREW OUT THE FLOOR (`scrambled <= N -> cost floored / *= N/scrambled`). Without it the coherence SUM keeps
SHRINKING with count (fewer cubies = fewer pieces = cheaper), so the search OVER-DESCENDS past the solvable floor into
the unsolvable last cycle. FIX (restored): gate exact coherence to `scrambled > N` (drives the descent); for
`scrambled <= N` apply the FLOOR `local_fA_sum *= float(N)/float(scrambled)` -> fitness ~ N*avg_magnitude (CONSTANT in
count) so ALL floor states compete EQUALLY on CLOSING (the commutator / orientation magnitude), NOT on count. The
floor STOPS the over-descent and points the search at closing the last cycle instead of sinking below it. (Retargeting
the GA at the whole small residual — "approach A" — does NOT fix this; without the floor it still over-descends.)
Metric-shape note: keep agreeAxes² on the descent (ranks deep defects correctly); the floor governs the endgame, so
linear-vs-squared is secondary there. NOTE the concave sqrt was a MISREAD of "non-linear" (it compressed the whole
cost and lost gradient -> stalled EARLIER at 7); the user meant per-PIECE contribution superlinear in depth
(agreeAxes²), a different thing.

**RESOLUTION (2026, the arc's endpoint — EXACT-FRAGMENTATION metric + the IRREDUCIBLE lie):** The honest limit of a
coherence metric is **cost = SUM over COMPLETE coherent pieces of their DEPTH** (fragmentation-dominant). Implemented
in EvaluateMicro: each STATE-group is decomposed EXACTLY into maximal complete shared-layer sub-cubes via an explicit
STACK (fix shared axes = agreeAxes; if the box isn't filled, split a still-varying axis, recurse; GLSL has no
recursion). A complete 2^(N-f) piece with f fixed axes = depth f (gateway f=1); a lone DEFECT = a singleton = depth N
(crushes the structure). No normalisation, no G-cap (exact SUM meets the bare count for fragmented residuals on its
own — no boundary to patch). KEY: `fragmentation ≈ #independent pieces ≈ MOVE-COUNT` (user's insight — "fragmentacja
jest najważniejszym parametrem, nie liczba kubików w grupie"), so this is the honest measure the size / continuous-
hole-fraction versions LIED about (they handed a scattered same-state blob a fake near-gateway discount and hung on
it). It WORKS: on 2^4 the cheap-fragmented traps are gone; the search reaches genuinely coherent structures. **BUT the
lie is now proven IRREDUCIBLE, and this is the real endpoint:** a CLEAN coherent structure is a GENUINE local minimum,
because the endgame COMMUTATOR must FIRST break coherence (raise cost) before it solves — so NO coherence-as-distance
metric descends through it monotonically; the first escape move always looks worse. Not a metric bug — inherent.
Subtle and important: the honest metric DEEPENS the well — the lying metric gave cheap fragmented sideways states the
search could wander out through; the exact metric makes every sideways move expensive, so the coherent minimum is a
deep well with no cheap exits → **the kick (worse-accept / STALL) becomes REQUIRED, not optional.** Clean division of
labour, and it IS progress: **metric = honest (done), reaches the true structure; SEARCH = must escape a genuine
coherent minimum via a worse-accepting commutator.** So STALL/kick is no longer a patch on a broken measure — it is
the CORRECT tool for the now-isolated search problem (regime-STALL: descend the honest gradient, kick at a coherent
plateau). NEXT: wire the coherent-plateau kick. Evidence: 2^4 froze at error 30.33 / 6-unsolved coherent, 929→3879
iters flat. CODE: exact-decomposition block is live in EvaluateMicro (`if (scrambled > 0u)`, stack `stMask/stVal[2*N]`,
guard `4*CUBIES_COUNT`).

**PRINCIPLE (hard-won meta-lesson of the WHOLE coherence arc, user's words): COHERENCE IS A LYING PROXY FOR
MOVE-COUNT — it must MODULATE a real base, never REPLACE it.** Coherence measures STRUCTURE (shared layers,
gathered orientations); it is NECESSARY but NOT SUFFICIENT for closeness-to-solved. Every solve's penultimate
states are coherent, so a coherent structure MAY be near — but the CONVERSE FAILS: a coherent-*looking* state can
be a deep commutator needle. Every PURE-coherence metric hangs EXACTLY there. Proven twice this session: (1) variant
E (continuous partial credit) — dead end; (2) the HOLE-PENALTY sum (per state-group `c = agreeAxes + LAMBDA·(1 −
gsize/2^(N-a))`, normalised by max(N,LAMBDA), SUMMED over groups, all residuals ≤ G; it made the METRIC climb
beautifully — un-solves to build the gateway, 2^5 14→16, solved 2^4 fast ~2316 GA with a "kaleidoscope of
coherence") — BUT it HANGS >100k at the gateway (8/16 on 2^4), a fat tail / catastrophe = UNACCEPTABLE. The hang is
where coherence LIED (low coherence-cost, search can't close). STALL/kick only helps the SEARCH escape the lie's
traps — it does NOT make the metric honest (patch, not cure; user "not my favourite"). METHOD note: refining the
analogy DEEPER (bits → holes → sum) never fixed it — the gap is in the PREMISE (coherence = moves), not the depth.
"Searching too deep for analogies ends exactly like this." DON'T re-drill this analogy. **CONCLUSION: coherence as a
REFUND on a move-count base (the discrete 1/logs ladder on the CLIFF/count base — ZERO catastrophes) is the reliable
form** — the base anchors distance, coherence corrects the count-deception. The pure-coherence branch (E, hole-
penalty) is closed. CODE STATE: the hole-penalty is currently in EvaluateMicro (left for a possible STALL-restored
test); the discrete 1/logs ladder is the reliable fallback, fully reconstructable on request.

**LATEST (this session — the coherence-LADDER branch, brought to a CORRECT but BOUNDED state; 2^5 close remains
SEARCH-limited, not metric-limited).** The EvaluateMicro coherence block was reworked into a clean recursive ladder,
then the branch was judged done (substantively right, no bigger leap visible). Final state:
(1) **Cost = N − log2(scrambled) + (1/log2(scrambled))·log2(g)**, g = #equal groups = scrambled/firstSize →
`int ls=findMSB(scrambled), lf=findMSB(firstSize); cost = float(int(N)-ls) + float(ls-lf)/float(ls);`. The **`1/logs`
coefficient** is the key: max fragmentation penalty `(1/logs)(logs−1)=1−1/logs < 1`, so EACH scrambled-count gets its
own DISJOINT band `[N−logs, N−logs+1)` — NO collisions (the earlier `0.5·log2(g)` collided subsubcoh-16 = coh-8 = 2,
which was the "too little gradation"), everything auto-BELOW-floor, gateway (g=1, firstSize 2^(N-1)) = 1 for every N.
Philosophy: **climb to the biggest coherent band first (16 beats 8 beats 4), de-fragment within a band second**. Safe:
firstSize>1 ⇒ logs≥1, no div-by-0. (2) **Floor T = N** (was constant 4; 4 collided with coherent-2 = N−1 on 2^5 and
broke worse for N≥6). N = the mono-twist depth, so every coherent structure sits strictly below it; on 2^4 N=4 so
unchanged there. (3) Non-coherent default: `scrambled ≤ T` → floor T; above → bare count. (4) Trigger EXTENDED to the
gateway `G = 2^(N-1)`; only power-of-2 `scrambled` runs the O(k^2) decomposition; buffer `scr[1<<(N-1)]`. (5)
Coherent-2^N degeneracy (agreeAxes 0 → cost 0) = "solved UP TO A GLOBAL FRAME ROTATION" (real in symmetry-currency,
NOT in slice-move currency — that rotation ≈ SIZE·Givens moves); excluded by the G-cap, also unreachable in the peel.
**FLOOR fitness = T·M + T·avg(d)/max_fA** (count cancels via `×cost/scrambled`, so 4-vs-5 is decided by the SECOND
term = AVERAGE orientation magnitude, not the count). GetActiveCubieError is **orientation-L1-from-Identity ONLY**
(position does NOT enter — a home-but-twisted cubie is NOT better-scored; my "drift-to-twist-home" claim was WRONG).
**REJECTED this session — a "second term pointing toward coherence" (agreeAxes-deficit floor tiebreak):** it would
need the O(k^2) decomposition on NON-powers too (ill-defined — a non-power can't be coherent without first CHANGING
the count, which the floor deliberately ignores), AND it **re-opens variant E** — a partial-coherence deficit favors
asymmetric "one-half-gathered" traps that the symmetric gate exists to exclude; scaling it small only makes the bias
weak, not zero. So the floor tiebreak STAYS neutral avg(d); coherence FORMATION is a SEARCH problem (seed + STALL),
the metric only REWARDS the result. **Seeding: SEED_FAST reverted to 0 (DFS backtracking).** The fast single-descent
path (SEED_FAST=1) is cheaper CPU but too SHALLOW — the endgame rung-jumps (merge subcoherent → coherent → gateway)
are MACRO-moves / commutators that minimal seeds don't produce; DFS mixed-Givens gives that material (slow, invisible
progress until a breakthrough = the Las Vegas tail). On 2^5 the metric now visibly CLIMBS the ladder (scattered-4 err
15 → coherent-16-region err ~7.6), proving the ladder works — but the final gather/close stays search-bound. Verdict:
this branch is correct and bounded; 2^5 is genuinely very hard for a pure uniform GA.

**VARIANT E TRIED AND KILLED (this session, empirically confirmed) — REVERTED to the 1/logs ladder above, which is
the CURRENT state of EvaluateMicro.** E = pure CONTINUOUS coherence replacing the whole eval (no floor, no power-of-2
gate, computed for ALL residuals): per state-group `cost = (N − log2 s) + MU·deficit`, `deficit = (N−log2 s) −
agreeAxes`, `fitness = Σ groups`, MU tunable. **It is a structural dead end, not a tuning problem.** A big same-state
but SCATTERED group (agreeAxes≈0) costs `depth·(1+MU)` with `depth = N−log2 s` SMALL for big s, so it is far CHEAPER
than its cubies as singletons — E rewards the MAXIMUM orientation-alignment cheat (seen: 9-10 cubies one state). No
sane MU fixes it (a 9-group on 2^4 needs MU≈42, which crushes small groups). ROOT: **continuous PARTIAL credit for
incomplete gathering is inherently gameable; the discrete ALL-OR-NOTHING symmetric+agreeAxes gate is LOAD-BEARING** —
exactly what blocks this. Observed on 2^4: E PLATEAUED (error 2.1995404 flat from iter 3407 → 10857+) on a **14-cubie
(non-power-of-2) grouping that D3/the discrete measure classifies as NON-coherent** (14 > T and not a power of 2). So
E over-values states the disciplined measure rejects. Sub-findings: (a) E broke `error==0 ⟺ solved` (E=0/negative for
coherent-not-identity → the "Solved X/Y" count printed garbage; solved-detection must use `scrambled==0`, NOT the
fitness — restoring the 1/logs ladder makes `error==0 ⟺ solved` hold again). (b) [CORRECTED — earlier "E ill-defined on 3^3"
was WRONG] the coherence operates ONLY on the ACTIVE CLUSTER's cubies (not all 27), and coherent intersections are
power-of-2 in every rich cluster regardless of SIZE (extreme coords binary, middle coords fixed ×1), so 3^3 behaves the
SAME as 2^N — a VALID poligon, not broken. The discrete ladder's `scrambled ≤ G = 2^(N-1)` + power-of-2 gate rules out
negative depth by construction. E's 3^3 weakness was the SAME partial-credit cheat, not a SIZE artifact (a negative
depth would need > 2^N cubies aligned into ONE state within the active cluster, which didn't materialise). (c) E's
smooth landscape wants GENTLE STALL (sideways-only); the kick thrashes it — but sideways-only only DELAYS the
pseudo-coherent plateau, doesn't avoid it. Lesson:
**"continuous coherence" was the wrong ask — coherence must be all-or-nothing per group.** Everything below documents
PRIOR arcs — read as history.

**HEADLINE (2026, the decisive test): the floor+ladder+smooth-base metric OVERFIT 2^4. On 3^4 (the realistic
multi-cluster deep test) the ordering REVERSED: C > D3 > D4.** C = the SIMPLE variant (cliff base + FLAT
coherent discount, no floor, no depth-ladder, pair-histogram detector) generalises best. D4 (smooth base) was
WORST -- didn't close at all: the CLIFF base is load-bearing for the multi-cluster greedy PEEL (count-first
aggression drives "solve one cubie whole"; the smooth magnitude gradient is too weak and dithers). D3 (cliff +
floor + depth-ladder) HANGS on the last 4 cubies of the last cluster where C does not: the FLOOR flattens every
below-T state to cost T, so the final commutator (which threads through 3/2/1-unsolved intermediates) loses its
gradient -- the very floor that blocked the mono-twist descent on 2^4 kills the last mile on deep clusters. So
the floor and the depth-ladder were tuned to the WRONG target (2^4's coherent-8 gateway) and do not generalise.
**Recommendation: revert to C** (cliff + flat coherent discount). Keep what survived the real test, drop floor,
depth-ladder, and smooth base. The 3^4 test earned its keep -- 2^4 alone could never have shown this (single
corner cluster, no deep multi-cluster endgame). Everything below documents the (overfit) floor/ladder/smooth
build and how it was derived; read it as history + mechanism, NOT as the current best.

**[superseded] The coherence-eval saga RESOLVED into a working metric (2026).** After many falsified variants (see
[[ga-mechanism]]: orientation-only, 1-layer, 2-layer, flat-discount, depth-scaling-without-floor all failed),
this combination works on 2^4 and is the best runtime distribution we have had. All in EvaluateMicro (Micro =
<=64 cubies, i.e. 2^4/2^5; Macro stays baseline). Built collaboratively, driven by the user's structural insight.

**The structural insight (the user's, load-bearing):** in a 2^N cube every move rotates a full SLICE = 2^(N-1)
cubies, so it flips exactly 2^(N-1) cubies' solved-status. Therefore the state one move before solved has
EXACTLY 2^(N-1) unsolved cubies forming one coherent layer -- the **coherent 2^(N-1) layer is the unique,
MANDATORY gateway** to solved (every solve's penultimate state is one). So driving the unsolved COUNT below
2^(N-1) is not merely deceptive, it is a **cul-de-sac** the search must reverse out of; the count field's
attractor (the mono-twist, 1 unsolved) is the hardest state. Confirmed on screen: the solver visibly descends
from a coherent-8 at the very end on 2^4.

**The metric = three parts:**
1. **Smooth (cliff-free) base.** `GetActiveCubieError` returns `d/maxClusterState` (magnitude only), NOT the
   old cliff form `(maxClusterState+d)/max_fA` (which added a big per-cubie premium -> fA was essentially a
   COUNT). Magnitude base gives the floor plateau a sub-gradient toward less twist = toward solved. (Setup.glsl.c;
   the two forms are commented A/B.) This is the "kill the cliff / sum-of-states" idea.
2. **Hard FLOOR at T = 2^(N-2).** For a non-coherent residual with `scrambled <= T`: `fA *= T/scrambled`. Floors
   every below-T state so the deceptive descent below the reachable target is blocked (mono-twist and scattered-
   few all sit AT the floor -- no cheaper neighbour to flee toward, which is what fixed the earlier depth-scaling
   tail). T is the **2-move** target (a complete two-layer intersection, 2^(N-2) cubies): far more NUMEROUS hence
   reachable than the 1-move 2^(N-1) layer, which a short GA cannot assemble. NOTE: the old removed `scrambled<=N`
   penalty was accidentally right ONLY at N=4 (2^(N-2)=N iff N=4); the principled threshold is 2^(N-2).
3. **Coherent depth-LADDER below the floor.** A COMPLETE same-state intersection is charged its DEPTH (moves-to-
   solve) instead of the floor: `fA *= depth/scrambled`, `depth = N - findMSB(scrambled)`. On 2^4: coherent-8 ->
   1, coherent-4 -> 2 (the target), coherent-2 -> 3; all dip below the floor = the non-deceptive gradient the flat
   floor alone lacked. Mono-twist (scrambled==1) is EXCLUDED (`scrambled > 1`) -> falls to the floor, not a
   1-cubie "coherent" reward.

**Detector = agreeAxes (replaces the old axis-PAIR histogram, which hard-coded f=2 and missed the layer/8-rung):**
count the axes on which ALL scrambled cubies share a coordinate. Gate = power-of-2 size AND one state AND
`agreeAxes == N - log2(scrambled)` (a distinct set of that many cubies cannot agree on MORE axes, so == means
they exactly FILL the subcube = a genuine complete intersection). O(k*N), exact (== on packed state, no hashing).
Non-gameable because it needs a shared LAYER, not just a shared orientation -- the 3/32 cheat (spin scattered
cubies into a common orientation IN PLACE) does not raise agreeAxes; only GATHERING them onto a shared
intersection does. Membership test = `cubieL1(cur)==0` (solved), consistent with the `scrambled` counter.

**[SUPERSEDED 2026 -> [[measure-tail-not-mean]]: STALL stays as a TIMER but its escape becomes a SIDEWAYS move
GATED by the limit: `Best.Fitness < Score || (Stall >= StallLimit && Best.Fitness <= Score)`. Always-on `<=`
thrashed (lateral every run, no search time); the strict `<` hung on the plateau. The gated form walks 3^4's
stuck plateau to the exit. The "harder clusters still need STALL's worse-accept" below is void. History follows.]**

**STALL is removable when the eval's gradient alone clears the final needle; harder clusters still need it
(2026).** On 2^4 (single corner cluster) and 4^3 (Slices=4, solved in 234 GA) STALL_LIMIT huge causes no hang.
3^4 (Slices=3) with STALL OFF got STUCK at 4 unsolved on the second-to-last cluster (a k=3 orbit, 32 cubies).
**This is NOT a metric-coverage failure (user corrected my wrong "odd SIZE misfires" claim).** Intersection
cubie-counts are power-of-2 in every RICH cluster regardless of SIZE (the extreme coords are BINARY, 0 or
SIZE-1, and a cluster's MIDDLE coords are each a single FIXED value per cubie -> contribute x1, never x3; the
NUMBER OF LAYERS is irrelevant to the cubie count, so an odd SIZE does NOT break the 2-based structure). Minor
caveat (user): DEGENERATE clusters -- coords coincident across several axes, so FEWER cubies -- can have a
slightly different intersection architecture, but those are small and peel early. The one that matters here,
3^4's second-to-last, is the RICHEST cluster in that cube (a k=3 orbit, 32 cubies; not a full 192 but the
biggest), its cubies are "deep" (high orientation complexity) -> a genuinely hard endgame, fully 2-based. Proof it worked:
it stopped at exactly 4 = 2^2 -- a clean power-of-2 residual the gate can handle. So the metric DID drive the
cluster down to a clean small residual; the stick is the FINAL commutator/needle the pure gradient misses, and
STALL was OFF (zero kicks). Harder clusters (higher N, bigger orbits) need MORE of the STALL random-escape than
an easy single-cluster cube -- "the algorithm is asking for a bigger STALL," not a metric change. So STALL is
the final-needle escape whose BUDGET scales with cluster hardness; it is not universally removable, but its
necessity is about endgame difficulty, NOT about SIZE or cluster 2-basedness (both are fine).

**Concrete lever for the final needle (prior session, [[measure-tail-not-mean]] context, re-confirmed applicable
2026):** to escape deep basins, LOWER STALL_LIMIT (8 -> 4-5) -- more frequent kicks push out before the basin
"concretes over." So "give me a bigger STALL" (user) = MORE kicking = SMALLER limit (the knob is counter-
intuitive). Stronger option for the deepest cases: once in escape mode (stall >= limit) kick EVERY GA until
improvement, enabled only after the first ineffective kick. This does NOT contradict "the kick is a dead end"
(Konwersacja ~5037 vs the plateau finding): kick FREQUENCY is a dead lever on a FLAT plateau (2^5 pre-coherence)
but a REAL lever for a needle-search once the coherence eval supplies the gradient down to a clean residual --
different regime. The checkpoint/rollback/Luby machinery for the same deep-minimum problem was REJECTED in
favour of plain STALL -- do not resurrect it; but its insight stands: a reproducibly-hard cluster (e.g. 3^4's
second-to-last, seen in BOTH sessions) may be hard from its INHERITED collateral config, so redoing an earlier
cluster with fresh RNG could give it an easier start (last-resort idea, not built).

**Status (PRELIMINARY, n=10, honest):** D4-no-STALL on 2^4 = 9 runs mean ~1099 (fastest CENTER we have had,
several < 300) + 1 catastrophe 11714. Best center by far, mechanism validated. BUT the TAIL is NOT yet
structurally bounded: by our own tail-criterion, the simpler flat-discount+cliff variant ("C") showed 0/10
catastrophes (max 6183) where D4 has 1/10. So: best typical speed, tail not yet beaten. Needs n>=30 to
characterise the catastrophe rate and compare fairly with C. Do not declare "home" on the tail yet.

**The residual tail + next lever:** even the long run reaches the coherent-8 eventually -- it WANDERS the FLAT
floor (multi-group residuals, e.g. two coherent pairs in a 4, fail the whole-residual gate and fall to the floor
with no gradient -- the SCAFFOLDING limit of single-group detection) until it stumbles on one clean coherent
group. The tail lever is **GRADED coherence**: credit MULTIPLE coherent sub-groups so the floor gets a gradient.
This re-enters the partial-coherence DANGER ZONE (gameable/flattening history) -- agreeAxes (must gather onto
real shared layers) is what could keep it honest. NOT yet built. Related: [[ga-mechanism]],
[[measure-tail-not-mean]], [[project-purpose-scope]], [[glsl-macro-name-collisions]].
