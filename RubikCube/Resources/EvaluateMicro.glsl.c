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
        uint local_solved_errors = 0;
        for (uint i = 0; i < countActive; i++)
            local_fA_sum += GetActiveCubieError(local_cubies[ActiveCubies[i]], maxClusterState, max_fA);
        for (uint i = 0; i < countSolved; i++)
            if (cubieL1(local_cubies[SolvedCubies[i]]) != 0u)
                local_solved_errors += 1;
        float current_step_fitness = float(local_solved_errors) + local_fA_sum;
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
        }
    }

    // 3: Write the record back to the population
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
}
