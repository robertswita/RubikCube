---
name: unified-evaluator-direction
description: "COMPLETE 2026-08-04 (milestone): ONE cube-size-independent evaluator for ALL cubes -- single-cluster
(2^4, 2^5) AND multi-cluster (6^3) -- no Micro/Macro split, no single-eval-vs-scan size gate. Holds only the active
cluster (MAX_CLUSTER); the evaluator ALWAYS scans every prefix (best-of-prefixes beats single-eval, measured); the
Solved break-check is the precomputed per-prefix solvedMask (NOT solvedBrokenCount re-replay). solvedMask was the
final enabling piece."
metadata:
  type: project
---

**Unified evaluator: drop the Micro/Macro split by storing only the active cluster.** Established with the user
2026-08-01 and **BUILT + VALIDATED the same day** (below). Supersedes my earlier claim that Micro must hold the whole
cube for the Solved break-check.

**COMPLETE 2026-08-04 (MILESTONE, user-declared).** The refactoring closed successfully: ONE evaluator now runs
single-cluster (2^4, 2^5) AND multi-cluster (6^3) near-optimally -- no reason to ever go back to two evaluators for
small vs big. Two final pieces landed after 08-01:
1. **ALWAYS-SCAN (best-of-prefixes) replaced single-eval.** The 08-01 build evaluated ONCE at the genome's cut
   (commutator end); that REGRESSED single-cluster stability badly. Best-of-prefixes (scan every prefix, keep the min,
   MovesCount = best prefix) restores it. MEASURED on 2^4 (pop 4096, 10 runs): scan var ~124k vs single-eval ~3.25M
   (~26x), tail 1634 vs 6066. Same wall-clock (~3x cost/gen, ~3x fewer gens). So DO NOT reintroduce single-eval or a
   size gate; scan is the one path.
2. **solvedMask replaced solvedBrokenCount.** The break-check is now a precomputed per-prefix BITMASK: one forward
   replay per Solved cubie (O(1) storage each, one at a time), setting bit g iff that cubie is broken (cubieL1!=0)
   AFTER g+1 moves; OR across cubies at each fixed prefix. `uint solvedBroken[(GENES_COUNT+31)/32]` (one word per 32
   genes -> GENES_COUNT>32 just adds words). Cost O(countSolved*GENES), not the scan's O(countSolved*GENES^2)
   re-replay. This is what made ALWAYS-SCAN affordable on multi-cluster cubes -> unification. It also RESOLVES "THE
   TRAP" below: recording per-prefix (NOT a sticky bit) correctly reads a commutator that BREAKS then RESTORES a
   Solved cubie as intact at its restore prefix. On single-cluster cubes countSolved=0 -> mask all-zero -> no-op.
Also fixed en route: the endgame baseline (ScoreCube via a zero/`changed=false` specimen) was skipping the ladder
decomposition -> ScoreLadder=0 -> the COHERENT accept `Ladder <= ScoreLadder` locked EVERYTHING. Fix = peel-proxy:
`if (!changed) local_fA_sum += 2*CUBIES_COUNT; ladder = uint(local_fA_sum * NORM)` -- a guaranteed-large upper-bound
seed so the flip's first accept passes and installs the real baseline. 6^3 @ pop 4096: 895 GA / 33.6s, PURE PEEL
(coherence never had to engage). CAVEAT: 2^4 result gains this week are from POPULATION 4096, not 2^4-specific code --
do not attribute them to the metric.

**BUILT & VALIDATED 2026-08-01:**
- EvaluateMicro now holds `local_active[MAX_CLUSTER]` (was `local_cubies[CUBIES_COUNT]`); all paths index by
  active-index, `ActiveCubies[idx]` resolves the cubie id where `curCoord` needs the home position. Per-thread storage
  is now cube-size-independent (no `CUBIES_COUNT` array left).
- Solved break-check = `solvedBrokenCount(specimenID, prefixLen)` in Setup.glsl: replays the move prefix on ONE cubie
  (from `Cubies[id]`), O(1) storage each. Best step is chosen by the ACTIVE metric alone (per-step); the break penalty
  is added ONCE at the best step (conservative -- reject if it breaks a Solved cubie). `local_solved_errors` gone.
- Gate raised: `micro = TRubikCube.MaxClusterSize <= 82` (was `Cubies.Length <= 82`), so big N=3 cubes (6^3,
  MaxClusterSize~24) run the coherence eval on Micro.
- COHERENCE LATCH is now STALL-gated, not Floor-alone: `Coherent==0 && Scrambled < Floor && Stall > Floor` (Floor gates
  BOTH size and stall). `Stall` resets ONLY on a STRICT score drop (sideways keeps it growing); reset per cluster in
  NextCluster. Peel-first, coherence-as-escape-hatch. (Floor still lives in the metric NORM -- not fully excised.)
- SolvedInSlices: the break-check replays only Solved cubies with a coord in the cluster's validSlices (computed once
  per cluster in NextCluster). Correct because ALL genes stay in validSlices (Init=FreeMoves, crossover recombines,
  seeds have Slice = a cluster cubie's coord). Runs in BOTH phases, so it speeds the peel too.
- RESULTS: 2^4 and 3^3 bit-identical through the refactor (regress-guard). 6^3 (which the old Micro could NOT run,
  216 > 82) now runs on Micro: peel-only 1950 GA / 21s (beats old peel 2500/22s); with the stall-latch + SolvedInSlices
  2311 GA / 27s, STABLE (coherence engages on a few stuck endgames and closes them). Multi-layer closing of LARGER
  coherent structures (4+4) is still the metric's weak spot -- separate from this refactor.

**Why the split exists today:** EvaluateMicro holds `local_cubies[CUBIES_COUNT]` per THREAD (1 thread = 1 specimen);
beyond ~82 cubies that overflows registers, so big cubes use EvaluateMacro (whole cube in SHARED memory, 1 workgroup =
1 specimen, threads cooperate). The 82 gate is a register-capacity wall, not a metric limit.

**Why it's not fundamental (the three moves, all agreed):**
1. **Cubies outside Solved ∪ ActiveCluster are irrelevant** — the metric never reads them. Don't rotate, don't store.
   Where they land is computed once, after Best returns its moves (host already does Best.Correct()).
2. **Solved cubies need only a break-CHECK, not stored state.** "Unbroken" = the chromosome's move composition is
   IDENTITY on that cubie. Check it PER-CUBIE by replaying the moves on ONE cubie (start = home/identity,
   TurnSingleCubie step by step, O(1) storage), verifying the result is identity. Never need all Solved states at once.
3. **Active cluster is the only thing needing full per-step state** — sized `MAX_CLUSTER = N!*2^(N-1)`-ish, a function
   of N only (host-injects it, see [[cluster-orbit-structure]]). SIZE-independent.

**Result:** per-thread registers = MAX_CLUSTER (not CUBIES_COUNT) -> no overflow on big cubes -> ONE evaluator for all
sizes. On big/multi-layer cubes MAX_CLUSTER << SIZE^N, so occupancy jumps exactly where peel's tail explodes (the target
regime). Storage->compute trade is FAVORABLE because this GPU is storage/occupancy-bound (adding scratch arrays -- grp,
the scattered-hash fast-path -- both measured WORSE despite fewer ops; occupancy dominates ALU here).

**THE TRAP to avoid (also agreed):** a PER-STEP break penalty (current best = min(active+break) at every prefix) forces
storing the cumulative state of every DISPLACED Solved cubie -- and one layer turn displaces ~SIZE^(N-1) Solved at once,
so that set grows back toward the whole cube -> lose cube-independence. A boolean "displaced" bit does NOT work: it can't
detect a COMMUTATOR restoring the cubie, so it would penalize the endgame's own tool. Therefore use best-step-only:
find the best step by the ACTIVE metric alone (MAX_CLUSTER storage), then check Solved-break at that step by per-cubie
replay; reject if broken. If the conservative semantics underperform, recover via TOP-K best-active steps (check break
at each by replay, pick the best unbroken) -- still MAX_CLUSTER storage, K~2-3. NOT per-step tracking.

**Regulator:** the conservative best-step rejects a few more chromosomes; the freed occupancy funds a LARGER population
that offsets it (and adds diversity -> often fewer generations too). Self-financing.

**Measure when built:** 2^4 as a regress-guard (there MAX_CLUSTER == CUBIES_COUNT, no storage gain, so it only proves
no regression) + ONE big cube (e.g. 6^3/6^4) as the payoff proof (storage + occupancy + does it finish where Macro/peel
struggle). Related: [[ga-mechanism]], [[endgame-tail-is-findability]].
