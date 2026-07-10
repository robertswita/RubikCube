#version 430
#include "Setup.glsl.c"

// One thread produces one child specimen via rank selection + crossover.
// Parents are read from Population (binding 0), already sorted ascending by fitness
// by the bitonic sort. Children are written to NewPopulation (binding 6).
layout(local_size_x = 64) in;

// Inverse move: keep axis/plane/slice, negate the angle (0<->2, 1 stays). Matches TMove.GetRevCode.
uint getRevCode(uint code) {
    return (code & ~3u) | (2u - (code & 3u));
}

void main() {
    uint cid = gl_GlobalInvocationID.x;
    if (cid >= POPULATION_COUNT) return;

    uint seed = cid ^ TimeSeed;

    // Rank selection: draw two distinct parents from the top WINNERS_RATIO percent.
    uint winnerCount = (POPULATION_COUNT * WINNERS_RATIO) / 100u;
    if (winnerCount < 2u) winnerCount = 2u;
    uint momIdx = hash(seed + 1u) % winnerCount;
    uint dadIdx = (momIdx + 1u + hash(seed + 2u) % (winnerCount - 1u)) % winnerCount;

    // Split point in the first half. Mirrors CPU: Rnd.Next(GENES_COUNT) / 2.
    uint startPos = (hash(seed + 3u) % GENES_COUNT) / 2u;
    uint stopPos = startPos + 1u + hash(seed + 4u) % uint(N - 1);
    if (stopPos > GENES_COUNT / 2u) stopPos = GENES_COUNT / 2u;

    // 1. Build the first part with a single write per gene (no redundant full-mom copy):
    //    mom prefix [0, startPos), then dad's middle [startPos, stopPos).
    uint child[GENES_COUNT];
    for (uint i = 0u; i < startPos; i++)
        child[i] = Population[momIdx].Moves[i];
    for (uint i = startPos; i < stopPos; i++)
        child[i] = Population[dadIdx].Moves[i];

    // 3. mutation with probability MUTATION_RATIO percent: replace one gene in the first part.
    if (hash(seed + 5u) % 100u < uint(MUTATION_RATIO)) {
        uint mpos = hash(seed + 6u) % stopPos;
        child[mpos] = FreeMoves[hash(seed + 7u) % uint(FreeMoves.length())];
    }

    // 4. second half = reverse-complement of the first part: the mom block [0,startPos)
    //    and the dad block [startPos,stopPos), each reversed with negated angles.
    uint pos = stopPos;
    for (int i = int(startPos) - 1; i >= 0; i--)
        child[pos++] = getRevCode(child[i]);
    for (int i = int(stopPos) - 1; i >= int(startPos); i--)
        child[pos++] = getRevCode(child[i]);

    // 5. mom's tail beyond the reversible core [2*stopPos, GENES_COUNT) stays as mom (as on the CPU).
    for (uint i = 2u * stopPos; i < GENES_COUNT; i++)
        child[i] = Population[momIdx].Moves[i];

    // 5b. Macro-mutation (mirror CPU Mutate + Conjugate): with SEED_MUT_RATIO% probability overwrite the
    //     leading genes with a fresh seed sequence (solves one active-cluster cubie) and conjugate a
    //     block. Re-injects cubie-solvers every generation - the CPU's main search driver - which
    //     crossover then recombines into commutators.
    if (numSeeds > 0u && hash(seed + 8u) % 100u < uint(SEED_MUT_RATIO)) {
        uint sbase = (hash(seed + 9u) % numSeeds) * SEED_STRIDE;
        for (uint g = 0u; g < SEED_STRIDE; g++) {
            uint code = SeedMoves[sbase + g];
            if (code == 0xFFFFFFFFu) break;
            child[g] = code;
        }
        uint cStart = hash(seed + 10u) % (GENES_COUNT / 2u);
        uint cStop = cStart + 1u + hash(seed + 11u) % uint(N - 1);
        if (cStop > GENES_COUNT / 2u) cStop = GENES_COUNT / 2u;
        uint cpos = cStop;
        for (int i = int(cStop) - 1; i >= int(cStart); i--)
            child[cpos++] = getRevCode(child[i]);
    }

    // 6. write the child out; fitness is recomputed by the evaluate shader next generation.
    for (uint i = 0u; i < GENES_COUNT; i++)
        NewPopulation[cid].Moves[i] = child[i];
    NewPopulation[cid].Fitness = floatBitsToUint(1e38);
    NewPopulation[cid].MovesCount = 0u;
}
