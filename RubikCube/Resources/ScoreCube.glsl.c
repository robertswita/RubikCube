#version 430 core
#include "Setup.glsl.c"

layout(local_size_x = 1) in;

// Computes the host's cube.Score baseline on the GPU using the SAME metric the evaluators use
// (scoreState), so the CPU no longer mirrors the scoring formula. One thread scores the starting
// cube currently in Cubies against the active/solved cluster and writes it to CubeScore[0].
void main() {
    uint c[CUBIES_COUNT];
    for (uint i = 0u; i < uint(CUBIES_COUNT); i++)
        c[i] = Cubies[i];
    CubeScore[0] = scoreState(c);
}
