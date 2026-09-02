---
name: greedy-diversity-research
description: Greedy decomposition diversity research (mode/perm/order/reversed) - findings + diagnostic tools
metadata: 
  node_type: memory
  type: project
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

Research into which QR-decomposition policy maximises greedy solving-sequence diversity (macro-move material
for the GA). Findings documented in `Docs/Rozklad_greedy_roznorodnosc.md`. Diagnostic in
`RubikCube/TGreedyDiversity.cs` + File-menu items: *Greedy Diversity*, *Orientation Cluster*, *Verify Manoeuvres*.

Metric axes: **perm** (axis permutation PᵀMP), **order** (QR plane order), **reversed** (transpose Mᵀ),
**mode** (per-plane mixed Givens). mode originally had 2 strategies/plane {pod-L, nad-L} (2^P); now
**4/plane {pod-L, nad-L, pod-R, nad-R}** (4^P) — the R (right/column op, RotatePost, +sin) strategies are
realizable too: a right op in the reduction is still a real cube move in the solving sequence, because
L·M·R = I ⇒ M⁻¹ = R·L (a product of Givens applied left). TGreedyDiversity.RotCount packs the 4-way strategy
2 bits/plane in modeMask (base-4); SpreadLeft() reproduces the old left-only 2^P sweep.

Key result (rigorous, whole-cube-effect verified, 100% solve): **mode is the only macro-move generator** — it
produces non-minimal (longer) solving sequences; +310% distinct macro-moves vs baseline at N=4, vs perm +49%,
order +32%, reversed +0% (dead). mode nearly subsumes the rest. Correct-string count overcounts vs true effect
because Correct doesn't reorder **commuting** (disjoint-plane) moves: N=3 has no disjoint planes → 0% duplicates;
N=4 does → ~41%. Example: `Rz²Rx² = Ry²` on the target but different collateral → distinct macro-move.

**Right ops (4^P) confirmed alive** (2026): adding pod-R/nad-R over mode-L gives, by Correct-string, +10.4% at
N=3 and **+52.3% at N=4** (10752→16370) — the gain grows with N and concentrates in the LONGEST macro-moves
(N=4 length-5 extras 2297→6069, +164%; length-4 +32%). So right ops are the *deepest* macro-move generator.
reversed stays dead (+0%), so it's the per-plane L/R *mixing* that adds, not transposition (reversed dies because
transpose = inverse for orthogonal M and solving-distance is inverse-invariant, so it equals baseline - NOT
because it's redundant with right-ops). **Big correction (2026): the metric space is MULTIPLICATIVE, not additive.**
Measuring axes independently (union of marginals) badly undercounts. mode-LR looked to absorb perm/order (+1-2%
on top), but the JOINT cross is huge: at N=3, `strategy x perm` = +81% over strategy-alone and `strategy x order`
= +29% (vs the +2.4% the marginal union showed - 34x). Synergy concentrated in length-3 macro-moves. Real effects
(N=3 Correct==effect). So perm/order are FAR from absorbed once combined jointly with the 4^P strategies. TGreedy-
Diversity.Run now measures the partial crosses + the full composite (marginal per-axis sums disabled). Closure
(N=3): the composite `strategy x perm x order` = 154 = EXACTLY `strategy x perm` (+0.0% for adding order). So
order is fully ABSORBED by perm in the joint space. BUT that was measured with the LEFT-BIASED order set:
AllOrders/OrderDfs/GetOrder generate only orders valid for LEFT (row) reduction (zero-propagation rule derived
for row ops); for right/mixed strategies those orders are wrong, so the diagnostic UNDERCOUNTED order. Fix:
TGreedyDiversity now uses **AllPlaneOrders()** = ALL P! plane orders, and the greedy's solve-check keeps only
the ones valid for the given (per-plane-mixed) strategy - complete, no per-strategy derivation. (order validity
is per-plane-mixed, so even left-valid ∪ right-valid would miss mixed-valid orders; only all-P! catches all.)
Result (N=3, exact): with all 6 orders, `strategy x order` jumps 110→154 = `strategy x perm` = composite. So
**perm and order are EQUIVALENT generating axes** (each absorbs the other given strategy) - the complete set is
`strategy x perm` OR `strategy x order` alone. **CLOSED (N=4, sampled 5 orient x full strat x 24/720 orders): the full-order fix makes pruning LOSSLESS at
N=4 (composite == composite-no-same-plane, gap 0).** So the "fold-only" manoeuvres were reachable all along -
via the right/mixed-valid orders the left-biased AllOrders was missing. **There is NO missing axis:
`strategy(4^P) x perm x order` (with ALL P! orders) spans the whole manoeuvre space.** The whole apparent
incompleteness was the crippled (left-only) order axis. **FINAL (N=4, full 720 orders, 5 sampled orientations, full strat): perm is REDUNDANT.** (order U perm) ==
strategy x order (10305) exactly -> the full order-cross already contains everything perm reaches. The earlier
"perm & order complementary at N=4" was an artifact of the 24-order SAMPLE (order crippled); with all 720 orders,
order SUBSUMES perm - including perm's strategy-relabeling, because (perm-induced order, perm-relabeled strategy)
is just one point in the full order x strategy sweep. So the complete generating set is **strategy(4^P) x order
(all P!)** - reversed dead, perm redundant. order is the master axis (reaches length-6 macro-moves perm can't).
Production implication: perm was wired into seeds but is redundant; **random plane ORDER per draw** (+ stuck-
redraw) is the axis that gives the full diversity. (5 orientations -> re-run 2-3x to confirm perm stays redundant.) **Effect-verified only at N=3** (Verify: 76→84 distinct whole-cube effects,
100% solve; Correct==effect there because no disjoint planes). At N=4 disjoint planes fold ~41% of Correct into
effect-duplicates, so the real effect gain is lower than +52.3% — Verify@N=4 (needs Size≥7, slow) not yet run.

Production status: `mode` IS wired into GA seeding (SEED_MODE define, BuildSeedMoves), but its GA benefit is
weak — seeding has a volume sweet-spot (~50% ratio), over-seeding (SEED_RATIO=100) crushes diversity and slows
solving; deep cubies (where mode fires) are a minority. #GAIter variance is ~±30% so single runs don't settle
fine comparisons. The 4^P right-ops are a solid *generative* result but NOT yet shown to speed up the GA. See
[[project-purpose-scope]], [[seeder-role-boundary]]. Open: wire 4^P into the production greedy; N=4 confirmation;
the evaluation function is the likelier speed lever. **Production greedy now uses 4^P** (TRubikCube.RotCountMode
extended to the base-4 strategy; BuildSeedMoves draws modeSpace = 4^P when SEED_MODE=1; SEED_STRIDE stays P
since rc0 <= P still). Related: [[glsl-macro-name-collisions]].
