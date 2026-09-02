#version 430
#include "Setup.glsl.c"

// One block (e.g. 256 threads) processes a SINGLE specimen.
// gl_WorkGroupID.x is the specimen index in the population.
layout(local_size_x = 256) in;

// BLOCK SHARED MEMORY (fast scratchpad)
shared uint local_cubies[CUBIES_COUNT];

// Shared variables for parallel reduction within the block
shared float shared_fA_sum[gl_WorkGroupSize.x];
shared uint  shared_solved_errors[gl_WorkGroupSize.x];
shared uint  shared_scrambled[gl_WorkGroupSize.x];
shared uint  shared_changed[gl_WorkGroupSize.x];       // 1 if the prefix moved any active cubie

// Tracking the local record for this specimen (accessible to thread 0)
shared float specimen_best_fitness;
shared uint  specimen_best_moves_count;

void main()
{
    uint tid = gl_LocalInvocationID.x;       // Thread index within the block (0 - 255)
    uint specimenID = gl_WorkGroupID.x;     // Specimen index in the population
    if (specimenID >= Population.length()) return;

    float maxClusterState = float(MAX_CUBIE_STATE) * float(countActive);
    float max_fA = maxClusterState * (countActive + 1.0);

    if (tid == 0) {
        specimen_best_fitness = 1e38;
        specimen_best_moves_count = 0;
    }

    // STEP 1: Parallel load of the shared read-only starting state from VRAM into this block's
    // private shared-memory scratchpad (one block = one specimen), coalesced across threads.
    for (uint cubieID = tid; cubieID < CUBIES_COUNT; cubieID += gl_WorkGroupSize.x) {
        local_cubies[cubieID] = Cubies[cubieID];
    }
    barrier(); // Wait until all threads have loaded the whole cube

    // STEP 2: LOOP OVER MOVES (CHROMOSOMES)
    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. CUBE ROTATION: all threads transform the shared memory in parallel
        for (uint cubieID = tid; cubieID < CUBIES_COUNT; cubieID += gl_WorkGroupSize.x)
            local_cubies[cubieID] = TurnSingleCubie(local_cubies[cubieID], cubieID, move);
        barrier(); // Wait for the rotation to finish before evaluating the state

        // 2b. PARALLEL EVALUATION: each thread computes its error contribution
        float local_fA_contribution = 0.0;
        uint local_scrambled = 0u;
        uint local_solved_error = 0;
        uint local_changed = 0u;
        for (uint i = tid; i < countActive; i += gl_WorkGroupSize.x) {
            uint cur = local_cubies[ActiveCubies[i]];
            if (cur != Cubies[ActiveCubies[i]]) local_changed = 1u;   // did this prefix move the active cluster?
            float e = GetActiveCubieError(cur, maxClusterState, max_fA);
            local_fA_contribution += e;
            if (e != 0.0) local_scrambled++;
        }
        for (uint i = tid; i < countSolved; i += gl_WorkGroupSize.x)
            if (cubieL1(local_cubies[SolvedCubies[i]]) != 0u)
                local_solved_error += 1;
        // Store per-thread partial results into the reduction memory
        shared_fA_sum[tid] = local_fA_contribution;
        shared_solved_errors[tid] = local_solved_error;
        shared_scrambled[tid] = local_scrambled;
        shared_changed[tid] = local_changed;
        barrier();

        // Logarithmic parallel reduction within the block
        for (uint stride = gl_WorkGroupSize.x / 2; stride > 0; stride /= 2) {
            if (tid < stride) {
                shared_fA_sum[tid] += shared_fA_sum[tid + stride];
                shared_solved_errors[tid] += shared_solved_errors[tid + stride];
                shared_scrambled[tid] += shared_scrambled[tid + stride];
                shared_changed[tid] |= shared_changed[tid + stride];
            }
            barrier(); // Synchronize at every step of the reduction tree
        }

        // 2c. Thread 0 checks the local trajectory record
        if (tid == 0) {
            // Endgame amplification (mirrors EvaluateMicro / CPU Evaluate): once <= N active cubies
            // remain unsolved, drop the count reward and scale the magnitude by N/scrambled.
            float fA = shared_fA_sum[0];
            uint scrambled = shared_scrambled[0];
            if (scrambled > 0u && scrambled <= uint(N))
                fA *= float(N) / float(scrambled);
            float current_step_fitness = float(shared_solved_errors[0]) + fA;
            if (shared_changed[0] == 0u) current_step_fitness += float(2 * CUBIES_COUNT);   // no-op on the active cluster: rank below every real move (max real fitness <= CUBIES_COUNT, since solved+active = CUBIES_COUNT and the active term <= 1)
            if (current_step_fitness < specimen_best_fitness) {
                specimen_best_fitness = current_step_fitness;
                specimen_best_moves_count = uint(m + 1);
            }
        }
        barrier(); // Synchronize the whole block before moving to the next move 'm'
    }

    // STEP 3: Write the record back to the population (only thread 0 of the block)
    if (tid == 0) {
        Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
        Population[specimenID].MovesCount = specimen_best_moves_count;
    }
}
