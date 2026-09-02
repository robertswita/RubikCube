---
name: glsl-macro-name-collisions
description: "GLSL shaders inject Variables.glsl.c macros (N, SIZE, ...) - never name a local after an uppercase macro"
metadata: 
  node_type: memory
  type: project
  originSessionId: f2be91dc-fb0a-479e-bb5b-f5756624ca6b
---

The render + compute shaders (`RubikCube/Resources/*.glsl.c`) textually inject `Variables.glsl.c`
via `Gpu.CompileRenderProgram` / `CompileComputeProgram` (replacing `#include "Variables.glsl.c"`).
So every uppercase macro from Variables — `N`, `SIZE`, `CUBIES_COUNT`, `PLANES_COUNT`,
`GENES_COUNT`, `POPULATION_COUNT`, `MAX_LIGHTS`, `STALL_LIMIT`, etc. — is preprocessor-expanded
inside the shader body.

**Pitfall:** naming a GLSL local/variable after one of these (e.g. `vec3 N = ...`) expands to
`vec3 3 = ...`, producing confusing errors far from the real cause: "unexpected integer constant",
"swizzle mask element not present in operand z" (`N.z` -> `3.z`), "dot(int, vec3)" (`dot(N, L)` ->
`dot(3, L)`). Cost a debugging session where the error line pointed at unrelated code and the
symptom looked like `dFdx` was unsupported.

**Rule:** in shader bodies use lowercase / non-macro names for locals (e.g. `nrm`, not `N`). The
uppercase tokens belong to Variables macros only. Shader compile errors surface as a thrown
`System.Exception` with the info log from `OpenGL.CompileShader` / `GetShaderInfoLog`. Related:
[[gpu-shader-include-chain]].
