---
name: ga-mechanism
description: "How the GA actually runs - one-cubie focus, short runs, retry-a-different-cubie on failure"
metadata: 
  node_type: memory
  type: project
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

How the GPU GA is actually used (corrected by the user, not obvious from code alone):

- Each GA run **focuses on ONE active cubie**. Seeds (BuildSeedMoves) and the macro-mutation (SelCrossover)
  inject sequences that solve THAT cubie. Goal: **solve the focus cubie AND not disturb already-solved ones**
  (the fitness scores the whole active cluster, so breaking a solved cubie hurts).
- GA runs are **SHORT (~50 generations, GENERATIONS_COUNT)**.
- On failure (no solution in the window), the next GA run **draws a DIFFERENT cubie** from the active cluster
  and retries. So the solver is a **greedy peel**: at each step, solve whichever cubie is currently solvable
  without breaking the rest - not "grind one hard cubie", not "solve the whole cluster at once".

Chromosome = GENES_COUNT (32) genes; a seed fills only the leading ~stride (P) genes, so even a seeded specimen
has ~26 random tail genes - **collateral material is ample regardless of SEED_RATIO** (my earlier "over-seeding
starves material" was WRONG, per the user). The fitness **evaluates all subsequences** of the chromosome (best
sub-solution wins), so longer chromosomes are ~free extra room (worth trying >32) at GPU cost. The measured
seeding sweet-spot (~50%, 100% ~2x slower) is SINGLE-SAMPLE with ~±30% RNG variance, so it's a weak signal - if
real, the penalty is likely homogeneous leading trajectories (all seeded specimens commit to the same cubie-
solve prefix), not lack of material. Deceptive-valley/commutator concern is per-cubie and time-boxed (50 gen).

**Where the solver actually struggles (user, key):** solving a SINGLE cubie works great (real gradient). The
pain is CLUSTER CLOSING - when <= N active cubies remain unsolved and they can't be peeled independently
(solving one breaks another). There the fitness is deliberately **FLAT**: for #unsolved <= N a penalty is added
and ALL arrangements score EQUALLY - the GA searches BLINDLY for the exact score-zero sequence (a commutator;
always exists since residual is an even permutation). Flattening was chosen to dodge the DECEPTIVE moves-to-solve
gradient (which misleads when solving-one-breaks-another). So the endgame is a gradient-free needle-search. The
real lever = give the flat plateau a NON-deceptive gradient (the eval-function work), or a gradient-free explorer
(novelty search). The user's coordinate-histogram idea (reward aligned residual cubies) aims at this, but has a DEEP TENSION:
aligning residuals needs moves that break solved cubies, which the primary penalty forbids - the gradient fights
the constraint (likely why it never formalized). Novelty search (fitness where #unsolved > N, novelty on the
flat <= N plateau) is the cleaner gradient-free option and stays a pure uniform GA.

**Current idea being tried (user, 2026): REMOVE the <=N flattening penalty entirely** and rely on STALL. STALL
(Form1.cs ~290: `if (Best.Fitness < Score || Stall >= StallLimit) { accept best }`) = after StallLimit failed
GA runs it ACCEPTS the best non-improving sequence anyway, advancing the cube to a new state - a built-in
escape. So with the real (deceptive) moves-to-solve gradient restored: GA climbs to a mono-twist local optimum
fast, STALL kicks past it -> iterated local search. Beats the blind flat-plateau walk (no gradient). Risk: the
deceptive gradient may re-pull to similar mono-twists, needing many kicks. Small, reversible change; user is
testing it. If it works, no new eval/novelty needed.

**Penalty removal outcome (2026): WORKS for N<=4, FAILS to generalize to N=5.** No penalty + STALL beat the
penalty on 2^4 (better median AND variance; see [[measure-tail-not-mean]]). But 2^5 (N=5) HANGS: the cube
oscillates at 5-8 unsolved and can't close. Diagnosis: the moves-to-solve gradient is really a COUNT of
scrambled cubies, and count is DECEPTIVE - a *coherent* residual (e.g. one turned hyper-layer = 8 cubies
sharing an orientation, the double-layer intersection) is 1 move from solved yet has MORE unsolved than a
scattered-5; count flees the easy coherent-8 basin (which sits UPHILL, +3 unsolved) toward hard scattered
configs. The STALL kick reaches only ~7, can't climb +3 to 8. State via the block-permutation / wreath-product
diagram: solved=identity, one layer turn=clean equal-block cyclic shift (coherent, easy), mono-twist=lone
off-identity diagonal block (hard). Easy == COHERENT, not few-unsolved.

**Coherence-eval attempt #1 FALSIFIED (2026): orientation-only grouping is GAMEABLE.** Idea: charge each
DISTINCT residual orientation once, not per cubie (a layer turn advances a whole same-oriented group together),
so coherent-8 (1 group) beats scattered-5 (5 groups) and 5->8->solved becomes monotone descent. Built behind
`#define COHERENCE` in EvaluateMicro/Macro (dedup a scrambled cubie's error if an earlier active cubie has the
same packed orientation `cur`). RESULT on 2^5: far WORSE - stalls at 3/32 solved, error crawling. Why: baseline
fA ~= count of scrambled (the big maxClusterState constant drowns the magnitude d), and that count is a GOOD
gradient (only lowerable by actually homing cubies). Replacing it with count-of-distinct-orientations gave a
count the GA lowers WITHOUT homing - just rotate scrambled cubies into a shared orientation -> it dives into a
cheap "coherent-but-scrambled" basin and plateaus. Orientation-alignment != progress. LESSON: a coherence
discount must require REAL one-turn-collapsibility (cubies sharing a LAYER = common coord on the turn axis, not
just a shared orientation), else it's gameable. Shared-layer isn't a clean bucket key (share-any-axis is
non-transitive); candidate fixes = (orientation, slice-coord) key done right, or 1-move-lookahead coherence
(best single-move error reduction = a hard, non-gameable collapsibility signal). NOT yet built.

**RESOLVED (2026) -> see [[coherence-eval-solution]].** The (orientation, slice-coord) key done right became the
**agreeAxes** detector, and it works combined with a smooth base + a hard floor at 2^(N-2) (the user's slice-
geometry insight: a move flips a whole 2^(N-1) slice, so the coherent 2^(N-1) layer is the mandatory gateway and
count below it is a cul-de-sac). 1-move-LOOKAHEAD is dead (it gives zero signal at the mono-twist, where a
commutator is needed) - do NOT re-propose it. Best runtime CENTER we have had; STALL turned out REMOVABLE. Tail
still open (graded multi-group coherence is the next lever).
Related: [[seeder-role-boundary]], [[project-purpose-scope]], [[greedy-diversity-research]], [[measure-tail-not-mean]], [[coherence-eval-solution]].
