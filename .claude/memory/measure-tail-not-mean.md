---
name: measure-tail-not-mean
description: "Judge this Las Vegas GA by runtime tail/variance, not mean/median; SEED_RATIO fixed at 100%"
metadata:
  type: feedback
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

The GA is a **Las Vegas algorithm**: output is always a solved cube, the RUNTIME (GA-iteration count)
is the random variable. The user's standing criterion for solver quality is the runtime
**distribution's tail / variance, NOT the mean or median**.

**Why:** the mean is only a *hardware-normalized speed unit* - a high mean you beat with a faster CPU.
A heavy TAIL you cannot buy your way out of: unpredictable resource demand, no finite-time guarantee, a
single run 100x the median. A fat tail means the solve happens "by accident" (the escape from the
mono-twist basin is a stochastic STALL event). Stability > average speed for a solver.

**How to apply:**
- When comparing configs, report **std / CV (std/mean) / max** (or a high quantile), not just median.
  A config with std > mean (CV > 1) is barely characterized by its "typical" value.
- Prefer a config that trades a slightly higher median for a **killed tail**.
- Prefer **regime-EDGE settings over swept interior optima**. An interior "sweet spot" (e.g. the old
  ~50% seed) is fragile - it drifts with N, cube size, RNG, and needs per-variant re-tuning. An edge
  (the max) is the same for every N, so it generalizes without tuning and removes a hyperparameter.
  Fewer knobs = easier to steer (user). Matches the pure-uniform-GA / no-fiddly-tuning ethos.

**Concrete decision (2026): SEED_RATIO = 100%** (full seeding, made possible by the repetition-fill in
BuildSeedMoves - the pool is replicated to fill the whole target instead of leaving the population
under-seeded). On 2^4, seed 100% vs 80% (n=20 vs 10): catastrophes (>10k) 2/10 -> **0/20**, std
~5500 -> ~2450, CV ~1.30 -> ~0.77, at the cost of median 1841 -> 2795. Full seeding raises the density
of macro-move material so the mono-twist escape becomes the rule, not an accident -> the tail vanishes.

The residual tail (worst ~9301 GA) is the target of the next work. The lever is the **coherence-based eval**
(see [[ga-mechanism]]), NOT the kick.

**STALL stays as a TIMER; its ESCAPE becomes a SIDEWAYS move, GATED by the limit (2026 - and a live lesson in
not rushing: I recorded "STALL completely unnecessary" one turn, reality refined it the next).** The arc:
1. STALL's old escape = accept the BEST even if WORSE = a BASIN-escape, wrong for a PLATEAU endgame.
2. So try SIDEWAYS moves: accept EQUAL fitness (`<=`). Safe because no-ops carry a 2*CUBIES_COUNT penalty, so
   `Fitness == Score` is always a REAL lateral move to a different equal state, never standing still (the strict
   `<` was a RELIC from when no-ops still tied Score).
3. But ALWAYS-`<=` THRASHES: it takes a lateral EVERY run, before the GA can search a config for a real `<`
   improvement -- so it just spins the cube on the plateau and never finds the exit (all exploration, no
   exploitation). Observed on D3/3^4.
4. FIX (user, the durable form) = gate the sideways move behind the STALL limit:
       if (Best.Fitness < Score || (Best.Fitness == Score && Stall >= StallLimit))   // == names the sideways case
   The GA gets StallLimit runs to genuinely improve from each config; only when stuck does it take ONE lateral
   step, then Stall resets and it searches the new config. STALL is NOT gone -- it is the PACING TIMER; only its
   action at the limit changed: worse-accept -> sideways-accept. This also removes the always-`<=` downsides
   (move-count inflation, lateral cycling), since laterals are now occasional.
5. TUNING DIRECTION FLIPPED (user): with the OLD worse-accept STALL, prior-art LOWERED StallLimit (more frequent
   kicks escape basins). With gated-SIDEWAYS the direction inverts -- lowering StallLimit approaches the
   always-`<=` THRASH (spin the cube in circles), so you RAISE it: more GA search (exploitation) per config
   before a lateral step. There is still an optimum (too high wastes runs before a needed lateral). Hard
   clusters need a HIGHER limit: StallLimit=8 left 3^4's LAST cluster unsolved (though it cleanly solved
   cluster 3, error 16->8->4->0 = the coherence ladder).
6. RESULT: StallLimit=20 SOLVED 3^4 fully, 9554 GA total ("quite good") -- the first deep multi-cluster N=4 win.
   Cluster 4 closed at ~2400 GA: error dropped 12->3 and HELD there (the OLD worse-accept STALL would have
   kicked it off that good state; gated-sideways let it STAY and search), then descended a level and closed --
   the exact payoff. On the LAST cluster sideways moves are RARE, rarer than StallLimit (e.g. 43 runs). The
   gated condition adapts: `Stall >= limit` goes true but nothing is accepted until a sideways (`== Score`)
   actually appears, so a sparse plateau = a longer WAIT (~43), not a hang. Consequence: on the hardest cluster
   the binding constraint is sideways AVAILABILITY (~43), NOT StallLimit (20) -- so the limit is not
   hypersensitive; it mainly controls exploitation on MID-hard clusters (cluster 4 needed 20 over 8). Sweep it
   like MUTATION_RATIO.
7. PATH ABANDONED (user, after measuring): StallLimit is a FRAGILE per-cube INTERIOR knob. On 2^4 with
   StallLimit=20 (tuned for 3^4) gated-sideways gave the BEST median ever (860) but the WORST TAIL: 2
   catastrophes >10k, max 21426, CV 1.42 -- vs C's 0 catastrophes / max 6183. Both edges bad (limit->inf =
   strict-`<` hang; limit->0 = thrash), so the good value is a per-cube interior optimum (3^4 wants ~20, 2^4
   wants less) = exactly the fragile knob the tail principle rejects. So the user DROPPED STALL-tuning and
   returned to the EVAL -> **graded / multi-group coherence** (see [[coherence-eval-solution]]): score the
   residual's DECOMPOSITION into coherent sub-intersections (a 2^m residual as 2 coherent 2^(m-1) halves,
   recursively), giving the floor a gradient. Final cost formula (NOT sum-of-depths -- that exceeded the floor for
   small groups on 2^4): **cost = (N - log2(scrambled)) + log2(groups)**. 1 group -> depth (the ladder), 2
   groups -> depth+1, fully scattered (groups=scrambled) -> N (the floor EMERGES from the formula). A piece
   earns the discount ONLY as a real intersection (agreeAxes: shared state AND layer), so same-state-scattered
   (the 3/32 cheat) stays expensive (g=1 but not-coherent -> floor).
8. BUILT (minimal 2-halves) + RESULT: group by STATE; 2 distinct states each a complete intersection of
   scrambled/2 -> charge depth+1 (between single coherent group `depth` and floor `T`). On 2^4 it KILLED the
   tail: catastrophes 2 -> 0, p90 17646 -> 3733, max 21426 -> 9583, mean halved vs the gated-sideways baseline
   without it; median rose 860 -> 2682 (the tail-for-median trade we WANT). Reaches C-level tail (0 catastrophes)
   from the much-worse gated-sideways start -- the graded coherence does real work. VISIBLE on the state diagram
   (a coherent-8 = 2 coherent-4 collapsing to solved). Design note: subcoherent-4 == coherent-2 (both 3) BY
   DESIGN -- graded coherence lifts structured states OFF the floor, it need not finely rank them, and
   prematurely peeling one pair may not be progress. Docelowo UNIFY the single-group + 2-halves branches into
   ONE state-grouping loop (single group = g=1) -> g=4,8 recursion for free. NEXT: test the tail on 2^5 / 3^4.
Boundary: on a plateau sideways moves abound so the gated `<=` fires; a strict isolated min (all moves worse,
no lateral) would not fire and would need the old unconditional worse-accept -- not expected, not seen.
9. UNIFIED coherence (variant E, `cost = N - log2(scrambled) + log2(g)`, per-STATE decomposition, one loop
   replacing floor+ladder+2-halves; see [[coherence-eval-solution]]) HANGS occasionally on 2^4 (>100k on ~1/7
   runs; the other 6 were FAST, 155-8685). LESSON (user, corrects my wrong "escape gap" framing): a hang here
   is a METRIC FLAW, not a missing escape. If the search finds nothing better, the metric rated that state TOO
   WELL. The trap is a PARTIAL-coherence state ("one half solved, one half not") -- genuinely hard to close --
   that the fine `log2(g)` discount rates BELOW the floor, luring the search into a dead end the coherent part
   can't complete. This is the deceptive-partial-coherence DANGER ZONE we kept flagging; the fine gradient
   (which guides well when it works) re-introduced it. **Do NOT patch it with a worse-accept fallback -- that
   MASKS the flaw (and hides the trap-rate we want to measure).** Fix belongs in the METRIC: not rewarding
   partial structure that is a dead-end vs on-path to full coherence (hard: needs to tell them apart without
   lookahead, which is dead). Plan: benchmark E as-is (hangs counted = trap-rate), THEN fix the metric.
10. FIX = SYMMETRY (user, a beautiful reframe). "The cube's beauty is its symmetry; ASYMMETRIC configurations
    are incoherent and are the TRAP." So reward ONLY a SYMMETRIC coherent decomposition: the residual splits by
    STATE into g EQUAL groups, EACH a complete intersection of size scrambled/g (a power of 2). Cost =
    symmetric ? (N - log2(groupSize)) : N -- i.e. the DEPTH OF ONE EQUAL GROUP; g=1 -> depth, g=2 (halves) ->
    depth+1, g=4 -> depth+2, ... all the levels; equal singletons (g=scrambled) -> N (floor). Anything NOT a
    clean symmetric split (unequal groups, or a same-state-scattered group) -> N. This drops variant E's
    arbitrary-g log2(g): the asymmetric partials E rewarded (1 coherent-4 + scattered) are exactly the traps.
    TWO wins: (a) no asymmetric dead-ends below the floor -> no hang; (b) costs are INTEGER again (N - findMSB,
    no float log2) so equal-cost TIES return -> the gated sideways move has somewhere to go (E's floats broke
    the ties). On 2^4 it reproduces the working 2-halves values (2/3/4) exactly = safe regression; 2^5 gets the
    higher symmetry levels (g=4,8) without the traps. This is the current build; test 2^4 (hang gone?) then 2^5.
    Refinement (user): only a POWER-OF-2 residual can be symmetric-coherent (g equal 2^k groups needs g a
    power of 2 too -> scrambled a power of 2), so 5/6/7 are floored to N and the O(k^2) decomposition is SKIPPED
    for them -- both correct (an odd "3 pairs in a 6" split is a trap, not coherence -> the bug my code had) and
    cheaper (resolves the earlier "non-power-of-2 is a burden" concern; the symmetric measure has no partial
    gradient, so it genuinely doesn't need them). RESULT 2^4 (n=10): HANG GONE, 0 catastrophes, med 2482, CV
    0.77, max 8509 -- tighter tail than the 2-halves (CV 0.94) and no traps; C (flat, old STALL) still edges the
    max (6183) but on a different mechanism. Regression passed. The real test is 2^5 (symmetric structures are
    discrete points below a flat floor; open question = does symmetry alone guide enough, or does the
    search wander the 5/6/7 flat via sideways). NOT yet run.
11. FLOOR = 2^(N-2), NOT emergent N (user caught it, "Stop!"). Points 7-10 wrote the scattered/asymmetric floor
    as N (from the log2 formula). WRONG for N>=5 (N < 2^(N-2)). The floor MUST equal T = 2^(N-2) so it matches
    the COUNT at the threshold (count = T at scrambled = T -> smooth hand-off). A lower floor N makes a DROP at
    the threshold: reducing the unsolved COUNT to T is over-rewarded (cost ~T -> N) = the SAME "fewer is better"
    mono-twist deception the floor exists to kill. With floor T, scattered stays expensive (T), so the only way
    under the floor is REAL coherence, not fewer cubies. Fix: `cost = float(T)` default; coherent ladder fires
    only for groups BIGGER than singletons (`symmetric && firstSize > 1` -> N - log2(groupSize) < T); singletons/
    asymmetric/non-pow2 -> T. On 2^4 unchanged (T = N = 4 there, so the n=10 result above stands); the change
    only affects N>=5. (This is the ORIGINAL D3 floor `fA *= T/scrambled` restored, now under the symmetric ladder.)
12. 2^5 FIRST-EVER SOLVE (milestone) but a FAT TAIL. With the symmetric metric + T lowered to 4 (coherent-4
    target = "3-move" on 2^5), 2^5 CLOSED at 101289 GA -- the first clean 2^5 solve in the whole saga. Proof
    the coherence metric CAN solve N=5. BUT 101k GA is exactly the "solved by accident after enormous time"
    profile the tail criterion rejects. Diagnosis of the bottleneck (all confirmed): the 5-8 region is the hard
    core. With T=8 (coherence covers 5-8) it is a big FLAT floor -> needle-search hang. With T=4 (count covers
    5-8) the count is DECEPTIVE there (the original "count flees the coherent-8, stuck ~5-7") -> it crawls (30k
    to reach 5, 35k to 4) then "comes alive" once it reaches the coherence region (<=4). So THREE approaches to
    5-8, three failures: count=deceptive-crawl, flat-coherence=needle-hang, partial-coherence-E=trap-hang. A
    clean non-deceptive non-trapping gradient for 5-8 probably needs lookahead (dead). Open: attack the 5-8
    bottleneck (shallow tie-break signal? E + worse-accept escape?) or accept 101k as the N=5 reality (per
    [[project-purpose-scope]]: N>4 is an imagination exercise, not a fast solver). Perf note: the O(k^2)
    decomposition fires ONLY for power-of-2 residuals <= T, so those GA runs are heavier (visible as the GA
    counter's wall-clock rate varying with the residual's power-of-2-ness -- a per-run cost, not solver progress).
Supersedes the "STALL kick / STALL removable" thread below and in [[coherence-eval-solution]].

**The STALL kick is a DEAD END - do not re-propose it (2026, user).** This memory used to list "STALL kick
(accept best + K perturbation moves, ILS-style, K escalating with Stall)" as a candidate; that sent a whole
session down a blind alley, so it is struck. The reasoning that kills it: the ILS framing assumes the solver
sits in a **basin** the kick must escape. At cluster closing there is no basin - there is a **PLATEAU**.
Baseline fA is essentially a count of scrambled cubies, and the COHERENCE gate only fires at
`maxHist == scrambled` && power-of-2 (i.e. on the target), so EVERY residual with the same count scores
IDENTICALLY. A kick - weak or strong, random or structured - just moves the cube from one 5-unsolved state
to another indistinguishable 5-unsolved state. There is no signal saying the new point is better, so kick
STRENGTH cannot be a lever. **The kick is transport, not a compass.** The user put it as: perturbing best
achieves the same as best itself, which by definition worsens the cube - nothing is won.

Corollaries also settled that session: an incumbent/**record** score is NOT needed either (in the tail the
GA genuinely cannot improve, so Stall reaches the limit honestly - it is not being spuriously reset). And a
seed CANNOT be appended after a move sequence: seeds are **state-dependent** (BuildSeedMoves computes them
for the cube state at the start of the GA run), so they only solve a cubie from the LEADING position; run
from any other state a seed is just noise, "nie jest makroruchem i wszystko psuje poza jednym kubikiem".

Related: [[ga-mechanism]], [[seeder-role-boundary]], [[project-purpose-scope]].
