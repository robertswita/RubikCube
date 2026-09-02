#version 430 core
#include "Setup.glsl.c"

layout(local_size_x = 64) in;

void main() {
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;
    uint seed = specimenID ^ TimeSeed;
    uint freeMovesCount = uint(FreeMoves.length());

    // The first numSeeds specimens take their leading genes from a host-built sequence that undoes an
    // active-cluster cubie (SeedMoves, one SEED_STRIDE-wide slot per specimen); 0xFFFFFFFF ends a
    // shorter sequence. Remaining genes (and every gene of the other specimens) are random free moves.
    uint g = 0u;
    if (specimenID < numSeeds) {
        uint base = specimenID * SEED_STRIDE;
        for (; g < SEED_STRIDE; g++) {
            uint code = SeedMoves[base + g];
            if (code == 0xFFFFFFFFu) break;
            Population[specimenID].Moves[g] = code;
        }
    }
    for (; g < GENES_COUNT; g++) {
        uint randValue = hash(seed + g);
        Population[specimenID].Moves[g] = FreeMoves[randValue % freeMovesCount];
    }
}
