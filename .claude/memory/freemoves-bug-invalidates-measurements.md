---
name: freemoves-bug-invalidates-measurements
description: A long-standing FreeMoves no-op/dedup bug contaminated every pre-fix metric measurement; re-baseline from scratch.
metadata: 
  node_type: memory
  type: project
  originSessionId: 95217005-1638-45fd-b8b9-7a2254e5a463
  modified: 2026-07-28T18:57:45.097Z
---

`TRubikCube.GetFreeMoves` had a long-standing bug (fixed ~2026-07-28): the dedup checked the base `gene` while the list stored `gene+angle`, so it never fired; combined with the angle-encoding shift (0..2 -> 1..3) this let angle-0 (identity/no-op) moves and duplicate genes into the GA's move pool. The GA spent moves that did nothing and sampled from a biased list.

**Why:** every batch in Docs/batch_results.txt and every metric A/B (P2*S, P2*S+E2, All-in vs gateway, aMax*S, BC, CPL, ICPL, P2+4*S) was run on this polluted pool, so all tail/mean/variance numbers and comparisons are UNRELIABLE. Several "conclusions" are likely bug artifacts, not metric properties: "P2+4*S hangs" (a clean post-fix worst-run actually SOLVES), "All-in improves", "BC/CPL/ICPL worse". The recorded "best ever" tail numbers in [[coherence-eval-solution]] are pre-fix and need revalidation.

**How to apply:** treat all pre-fix measurements as void. Re-baseline from scratch on fixed code — anchor ONE metric (start with canonical P2*S), clean batch = new zero point, then vary ONE thing at a time, judge by the tail ([[measure-tail-not-mean]]). What survives the bug: the ladder algebra (config rankings are hand-computed, not measured) and the coherence escape mechanism, demonstrated live in a clean worst-run trajectory (two-singleton 1+1 trap escaped by growing 2+2+2+2 -> 4+4+4+4 -> 8 -> solved). The metric may now rank differently: P2*S may have "won" only by tolerating the pollution better.
