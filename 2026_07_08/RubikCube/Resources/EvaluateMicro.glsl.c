#version 430
#include "Setup.glsl"

// 1 wątek = 1 osobnik. Blok 64 wątków przetwarza 64 osobników naraz.
layout(local_size_x = 64) in;

void main()
{
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;

    int bitsPerRow = findMSB(N - 1) + 2;
    float maxClusterState = float(maxStateValue) * float(countActive);
    float max_fA = maxClusterState * (float(countActive) + 1.0);

    // Rekordy tego konkretnego wątku (osobnika) inicjalizowane maksymalną wartością float
    float specimen_best_fitness = 1e38;
    uint specimen_best_moves_count = 0;

    // KOSTKA W REJESTRACH: Prywatna tablica wątku zmapowana na ultra-szybkie rejestry ALU
    uint local_cubies[CUBIES_COUNT];
    uint baseIndex = specimenID * CUBIES_COUNT;
    for (uint i = 0; i < CUBIES_COUNT; i++)
        local_cubies[i] = Cubies[baseIndex + i];

    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. OBRÓT KOSTKI: Wątek modyfikuje wyłącznie swoje prywatne rejestry
        for (uint cubieID = 0; cubieID < CUBIES_COUNT; cubieID++)
            local_cubies[cubieID] = TurnSingleCubie(local_cubies[cubieID], cubieID, move);

        // 2b. SEKWENCYJNA EVALUACJA: Wątek sam sumuje swoje błędy dla bieżącego kroku 'm'
        float local_fA_sum = 0.0;
        uint local_solved_errors = 0;
        for (uint i = 0; i < countActive; i++)
            local_fA_sum += GetActiveCubieError(local_cubies[ActiveCubies[i]], maxClusterState, max_fA);
        for (uint i = 0; i < countSolved; i++)
            if (local_cubies[SolvedCubies[i]] != 0)
                local_solved_errors += 1;
        float current_step_fitness = float(local_solved_errors) + local_fA_sum;
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
        }
    }

    // 3: CZYSTY ZAPIS REKORDU DO POPULACJI
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
}
