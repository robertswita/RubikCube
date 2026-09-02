#version 430
#include "Setup.glsl.c"

// One thread produces one child specimen via rank selection + crossover.
// Parents are read from Population (binding 0), already sorted ascending by fitness
// by the bitonic sort. Children are written to NewPopulation (binding 6).
layout(local_size_x = 64) in;

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
    uint movesCount = 2 * stopPos;

    // 1. Build the first part with a single write per gene (no redundant full-mom copy):
    //    mom prefix [0, startPos), then dad's middle [startPos, stopPos).
    uint child[GENES_COUNT];
    for (uint i = 0u; i < startPos; i++)
        child[i] = Population[momIdx].Moves[i];
    for (uint i = startPos; i < stopPos; i++)
        child[i] = Population[dadIdx].Moves[i];

    // PERTURBING mutation (the ONLY mutation operator now): with MUTATION_RATIO% probability change ONE core gene to a
    // random free move BEFORE the reverse-complement, so it propagates into the inverse half and the child stays a
    // valid commutator [M',D] (still break-free). movesCount unchanged (2*stopPos). NO re-seeding -- Init already seeds
    // and GA runs are short, so a raw re-seed just breaks Solved clusters and dies; this is LOCAL exploration around the
    // commutator, the search step the once-eval evaluator needs. FreeMoves -> validSlices, so SolvedInSlices stays valid.
    if (hash(seed + 8u) % 100u < uint(MUTATION_RATIO)) {
        uint freeMovesCount = uint(FreeMoves.length());
        child[hash(seed + 9u) % stopPos] = FreeMoves[hash(seed + 10u) % freeMovesCount];
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
    movesCount = (hash(seed + 300u) % 2u == 0u) ? 0u : movesCount;

    // 5b. Macro-mutation (mirror CPU Mutate + Conjugate): with MUTATION_RATIO% probability overwrite the
    //     leading genes with a fresh seed sequence (solves one active-cluster cubie) and conjugate a
    //     block. Re-injects cubie-solvers every generation - the CPU's main search driver - which
    //     crossover then recombines into commutators. This is now the only mutation operator.
    //if (numSeeds > 0u && hash(seed + 8u) % 100u < uint(MUTATION_RATIO)) {
    //    uint sbase = (hash(seed + 9u) % numSeeds) * SEED_STRIDE;
    //    for (uint g = 0u; g < SEED_STRIDE; g++) {
    //        uint code = SeedMoves[sbase + g];
    //        if (code == 0xFFFFFFFFu) break;
    //        child[g] = code;
    //    }
    //    //uint cStart = hash(seed + 10u) % (GENES_COUNT / 2u);
    //    //uint cStop = cStart + 1u + hash(seed + 11u) % uint(N - 1);
    //    //if (cStop > GENES_COUNT / 2u) cStop = GENES_COUNT / 2u;
    //    //uint cpos = cStop;
    //    //for (int i = int(cStop) - 1; i >= int(cStart); i--)
    //    //    child[cpos++] = getRevCode(child[i]);
    //    movesCount = 0;
    //}

    // 6. write the child out; fitness is recomputed by the evaluate shader next generation.
    for (uint i = 0u; i < GENES_COUNT; i++)
        NewPopulation[cid].Moves[i] = child[i];
    NewPopulation[cid].Fitness = floatBitsToUint(1e38);
    //movesCount = (hash(seed + 300u) % 2u == 0u) ? movesCount : movesCount / 2;
    NewPopulation[cid].SeedLen = startPos + stopPos;
    NewPopulation[cid].MovesCount = 0;// movesCount;
}
