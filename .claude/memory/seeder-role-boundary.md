---
name: seeder-role-boundary
description: Division of labour - the seed generator only seeds; the GA does selection/optimization
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

The host seed generator (`BuildSeedMoves` / `GetSolveSeq`) exists **only to provide varied raw material** —
cubie-solving sequences (minimal + mixed-Givens macro-moves). It must NOT pre-judge or curate which seeds
are "good": no filtering to macro-only, no picking "live" modes, no scoring the pool. Selection is the GA's
job.

**Why:** the user's explicit principle — "Nie róbmy roboty za GA, my tylko seedujemy." Curating seeds encroaches
on the GA's role and biases its search with our assumptions about what matters.

**How to apply:** keep the seeder simple — random mode + backtracking solve + string dedup, feed everything to
the population. Pure host-side perf tweaks that DON'T change the seed distribution (e.g. skipping modes that
provably return null) are borderline-acceptable only if performance actually bites, but default to leaving it
plain. When tempted to make the seeder "smarter," stop. Related: [[greedy-diversity-research]].
