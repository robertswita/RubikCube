---
name: endgame-tail-is-findability
description: "The coherence-endgame tail is a findability problem (finding the merge/commutator), not a metric problem; the lever is endgame seeds, not the ladder."
metadata: 
  node_type: memory
  type: project
  originSessionId: 95217005-1638-45fd-b8b9-7a2254e5a463
  modified: 2026-07-30T06:14:49.123Z
---

The coherence ladder (P2 + N*S) is a RICHER gradient than the count-peel descent: it rewards building structure UP (e.g. N=5: 1+1 (20) -> 2 (15) -> 2+2 (15) -> 4+4 (11) -> 8 (8) -> 16 (6) -> collapse, each merge a ladder DROP). So coherence HAS an escape route out of the endgame trap that the count-peel structurally LACKS.

**Why:** two different "stuck"s -- the count-peel utyka because it can't build structure (no escape); coherence utyka because the GA can't FIND the merge move. So the endgame belongs to coherence: set Floor HIGH (descent is only the cheap trivial-bulk phase, don't expect it to reach 1+1 and build). The persistent tail is NOT the metric -- the ladder already rewards the escape -- it is the GA finding the MERGE/COMMUTATOR (solve/merge an entangled k+k without breaking the rest).

**How to apply:** attack findability, not the metric. `BuildSeedMoves` only reverse-solves a SINGLE active cubie, which breaks its partner, so it never produces merges/commutators -> the GA has no seed candidate for the escape. Lever = endgame merge/commutator seeds (respecting [[seeder-role-boundary]]) or more search depth. Stop tweaking the ladder for the tail ([[measure-tail-not-mean]]).

**Architecture at this point** (2^5 endgame working, "chodzi na boki"): GA fitness is FINE + NORMALISED to < 1 so local_solved_errors (+1/broken Solved cubie) and the no-op penalty dominate -- `(P2+N*S + graded)/NORM` in EvaluateMicro. The coherence ACCEPT (TRubikCube.GetNextMoves) compares the STRUCTURE ladder value `TRubikGenome.LadderValue(structure, N)` (decoded host-side from the piece histogram), NOT the fitness, so structurally-equal configs tie and sideways fire; a `Best.Fitness < 1` gate rejects any move (even sideways) that breaks a Solved cluster. Descent still accepts by fitness. best-ever was tried and REVERTED (per-gen readback + early-exit restored) -- see why in the notes. The `+N*S` coefficient (not the old hardcoded `+4S`, which only tied the floor/pair on N=4) generalises the sideways ties to any N.
