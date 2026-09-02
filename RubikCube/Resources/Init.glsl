#version 430 core
#include "Setup.glsl"

layout(local_size_x = 64) in;

void main() {
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;
    //uint seed = specimenID ^ TimeSeed;
    uint freeMovesCount = uint(FreeMoves.length());

    // The first numSeeds specimens get a COMMUTATOR [S,R] = S R S^-1 R^-1 seeded from the host sequence:
    //   S = SeedMoves (solves one active-cluster cubie but disturbs collateral, SEED_STRIDE-wide, 0xFFFFFFFF ends
    //       a shorter one), R = 1..N-1 random free moves, then S^-1 R^-1 restore. This is BREAK-FREE on any Solved
    //   cubie R doesn't touch (S^-1 undoes S when R didn't intervene; R is small) -- so it survives the break-check
    //   from generation 0, unlike a raw seed (which breaks solved clusters and is rejected). Break-free ONLY at the
    //   END, so MovesCount = the full commutator length (the single eval point). All moves stay within FreeMoves
    //   (validSlices) -- S, R, and their inverses -- so SolvedInSlices stays valid. Remaining genes (and every gene
    //   of the non-seeded specimens) are random free moves -- dormant material the crossover recombines.
    uint g = 0u;
    //uint movesCount = hash(seed + 300u) % GENES_COUNT;                          // non-seeded specimens: cut = full random sequence
    if (specimenID < numSeeds) {
        uint base = specimenID * SEED_STRIDE;
        uint sLen = 0u;
        for (; sLen < SEED_STRIDE; sLen++) {                 // S
            uint code = SeedMoves[base + sLen];
            if (code == 0xFFFFFFFFu) break;
            Population[specimenID].Moves[sLen] = code;
        }
        uint rLen = 1u + randNext(GENES_COUNT / 2 - sLen - 1); // R length 1..N-1 (as in the mutation)
        uint pos = sLen;
        for (uint r = 0u; r < rLen && pos < GENES_COUNT; r++, pos++)                    // R (random free moves)
            Population[specimenID].Moves[pos] = FreeMoves[randNext(freeMovesCount)];
        for (int i = int(sLen) - 1; i >= 0 && pos < GENES_COUNT; i--)                   // S^-1
            Population[specimenID].Moves[pos++] = getRevCode(Population[specimenID].Moves[i]);
        for (int i = int(sLen + rLen) - 1; i >= int(sLen) && pos < GENES_COUNT; i--)    // R^-1
            Population[specimenID].Moves[pos++] = getRevCode(Population[specimenID].Moves[i]);

         //cut RANDOMLY at the seed end (|S| = sLen -- the pure-seed solve, what PEEL wants) OR the commutator end
         //(2*(sLen+rLen) = pos -- break-free, what the ENDGAME wants). The population then holds BOTH kinds; selection
         //keeps whichever the current phase rewards. The full commutator stays in the genome either way (crossover
         //material); only the eval cut differs.
        g = pos;
        //g = sLen;
    }
    for (; g < GENES_COUNT; g++)
        Population[specimenID].Moves[g] = FreeMoves[randNext(freeMovesCount)];
    //movesCount = hash(seed + 300u) % GENES_COUNT;
    //Population[specimenID].MovesCount = 0;// movesCount;
    //Population[specimenID].SeedLen = movesCount;
}
