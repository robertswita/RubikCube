---
name: project-purpose-scope
description: "RubikCubeND is a pure-GA intellectual/teaching artifact, not a practical solver - constrains what \"improvement\" means"
metadata: 
  node_type: memory
  type: project
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

The goal of RubikCubeND is **not** a fast/practical solver (the user knows what a solved cube looks like).
Purpose: 3D variant = an illustrative example for students; N>4 = an imagination/geometry exercise.

**Hard constraints on approach** (the user stated these explicitly):
- Keep it a **pure, uniform genetic algorithm**. Do NOT special-case the last (corner) clusters.
- Reluctant about **hybridizing** GA with classical Rubik techniques (setup moves, commutator routines, etc.) -
  don't propose bolt-on solvers for the endgame.
- Don't optimize "solve at any cost." Elegance/uniformity of the method matters more than raw speed.

**Where improvement is allowed to come from:** the GA's own levers - evaluation/fitness function, mutation,
seeding, STALL, population size. Reason theoretically first; avoid big batch experiments (they overheat the
user's hardware, and single runs have ~±30% RNG variance so they rarely settle a question anyway).

Current status: seeding works but only weakly (see [[greedy-diversity-research]], [[seeder-role-boundary]]).
The endgame (last clusters) dominates runtime with huge variance.
