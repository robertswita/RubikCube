---
name: pace-explain-before-code
description: "Working-style preference - explain changes in plain terms first, small steps; don't dump code fast"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

The user loses track when I generate code quickly or in large batches ("Kiedy to ty piszesz jakiś fragment
kodu, ja łatwo się gubię, bo nie nadążam"). It is not about correctness — it is pace.

**Why:** the user follows and reasons about every change; fast multi-edit dumps outrun them and break the
collaboration they value.

**How to apply:** before editing, state in plain language WHAT will change and WHY. Prefer small, single-focus
edits over several at once. Narrate as you go. Pause at natural points so they can follow / steer. Speed is not
the goal — keeping them in the loop is. This user enjoys the process itself ("jestem szczęśliwy nawet jak nic z
tego nie wyjdzie"), so the shared understanding matters more than throughput.

**Don't preempt edits the user intends to make themselves (2026).** When they signal ownership of a change —
"sam zmienię", "zrobię to za Ciebie", "nic nie poprawiaj" — hand them the EXACT change in text and let them
apply it; do NOT rush in with the Edit tool. This session I made many rapid tool edits and got "Już nie bądź
taki" (ease off). Especially on their working code (they edit live in Visual Studio, unsaved), a disk edit from
me can also collide with their VS buffer. Default to proposing; edit only when they clearly ask me to.
