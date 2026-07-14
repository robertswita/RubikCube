#version 430
#include "Setup.glsl.c"

// 1 thread = 1 specimen. A block of 64 threads processes 64 specimens at once.
layout(local_size_x = 64) in;

void main()
{
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;

    float maxClusterState = float(MAX_CUBIE_STATE) * float(countActive);
    float max_fA = maxClusterState * (float(countActive) + 1.0);

    // Per-thread (per-specimen) records initialized to the maximum float value
    float specimen_best_fitness = 1e38;
    uint specimen_best_moves_count = 0;

    // CUBE IN REGISTERS: thread-private array mapped to ultra-fast ALU registers.
    // Cubies holds the shared read-only starting state; each thread copies it into its private
    // working array and mutates only that copy, so the single shared buffer is race-free.
    uint local_cubies[CUBIES_COUNT];
    for (uint i = 0; i < CUBIES_COUNT; i++)
        local_cubies[i] = Cubies[i];

    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. CUBE ROTATION: the thread modifies only its own private registers
        for (uint cubieID = 0; cubieID < CUBIES_COUNT; cubieID++)
            local_cubies[cubieID] = TurnSingleCubie(local_cubies[cubieID], cubieID, move);

        // 2b. SEQUENTIAL EVALUATION: the thread sums its own errors for the current step 'm'
        float local_fA_sum = 0.0;
        uint scrambled = 0u;
        uint local_solved_errors = 0;
        bool changed = false;
        for (uint i = 0; i < countActive; i++) {
            uint cur = local_cubies[ActiveCubies[i]];
            if (cur != Cubies[ActiveCubies[i]]) changed = true;   // did this prefix move the active cluster?
            float e = GetActiveCubieError(cur, maxClusterState, max_fA);
            local_fA_sum += e;
            if (e != 0.0) scrambled++;
        }
        // Endgame: once <= N active cubies remain unsolved, stop rewarding fewer of them (fewer is
        // not easier - a lone twisted cubie / mono-twist is very hard to escape). Amplify by
        // N/scrambled so the search then optimises the orientation magnitude, not the count.
        if (scrambled > 0u && scrambled <= uint(N))
            local_fA_sum *= float(N) / float(scrambled);
        for (uint i = 0; i < countSolved; i++)
            if (cubieL1(local_cubies[SolvedCubies[i]]) != 0u)
                local_solved_errors += 1;
        float current_step_fitness = float(local_solved_errors) + local_fA_sum;
        if (!changed) current_step_fitness += float(2 * CUBIES_COUNT);   // no-op on the active cluster: rank below every real move (max real fitness <= CUBIES_COUNT, since solved+active = CUBIES_COUNT and the active term <= 1)
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
        }
    }

    // 3: Write the record back to the population
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
}
