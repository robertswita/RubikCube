#version 430
#include "Setup.glsl.c"

// Jeden blok (np. 256 w¹tków) przetwarza JEDNEGO osobnika.
// gl_WorkGroupID.x to indeks osobnika w populacji.
layout(local_size_x = 256) in;

// PAMIÊÆ WSPÓ£DZIELONA BLOKU (Szybki Scratchpad)
shared uint local_cubies[CUBIES_COUNT];

// Zmienne wspó³dzielone do redukcji równoleg³ej wewn¹trz bloku
shared float shared_fA_sum[gl_WorkGroupSize.x];
shared uint  shared_solved_errors[gl_WorkGroupSize.x];

// Œledzenie rekordu lokalnego dla tego konkretnego osobnika (dostêpne dla w¹tku 0)
shared float specimen_best_fitness;
shared uint  specimen_best_moves_count;

void main()
{
    uint tid = gl_LocalInvocationID.x;       // Indeks w¹tku w bloku (0 - 255)
    uint specimenID = gl_WorkGroupID.x;     // Indeks osobnika w populacji
    if (specimenID >= Population.length()) return;

    int bitsPerRow = findMSB(N - 1) + 2;
    float maxClusterState = float(maxStateValue) * float(countActive);
    float max_fA = maxClusterState * (float(countActive) + 1.0);

    if (tid == 0) {
        specimen_best_fitness = 1e38;
        specimen_best_moves_count = 0;
    }

    // KROK 1: Równoleg³e ³adowanie stanu kostki z VRAM do pamiêci shared (Coalesced Memory Access)
    uint baseIndex = specimenID * CUBIES_COUNT;
    for (uint cubieID = tid; cubieID < CUBIES_COUNT; cubieID += gl_WorkGroupSize.x) {
        local_cubies[cubieID] = Cubies[baseIndex + cubieID];
    }
    barrier(); // Czekamy na za³adowanie ca³ej kostki przez wszystkie w¹tki

    // KROK 2: PÊTLA PO RUCHACH (CHROMOSOMACH)
    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. OBRÓT KOSTKI: Wszystkie w¹tki równolegle wykonuj¹ transformacjê w pamiêci shared
        for (uint cubieID = tid; cubieID < CUBIES_COUNT; cubieID += gl_WorkGroupSize.x)
            local_cubies[cubieID] = TurnSingleCubie(local_cubies[cubieID], cubieID, move);
        barrier(); // Czekamy, a¿ obrót siê zakoñczy, zanim przejdziemy do ewaluacji stanu

        // 2b. RÓWNOLEG£A EVALUACJA: Wyliczanie wk³adów b³êdów przez poszczególne w¹tki
        float local_fA_contribution = 0.0;
        uint local_solved_error = 0;
        for (uint i = tid; i < countActive; i += gl_WorkGroupSize.x)
            local_fA_contribution += GetActiveCubieError(local_cubies[ActiveCubies[i]], maxClusterState, max_fA);
        for (uint i = tid; i < countSolved; i += gl_WorkGroupSize.x)
            if (local_cubies[SolvedCubies[i]] != 0)
                local_solved_error += 1;
        // Zapis wyników cz¹stkowych w¹tków do pamiêci redukcji
        shared_fA_sum[tid] = local_fA_contribution;
        shared_solved_errors[tid] = local_solved_error;
        barrier();

        // Logarytmiczna redukcja równoleg³a (Parallel Reduction) wewn¹trz bloku
        for (uint stride = gl_WorkGroupSize.x / 2; stride > 0; stride /= 2) {
            if (tid < stride) {
                shared_fA_sum[tid] += shared_fA_sum[tid + stride];
                shared_solved_errors[tid] += shared_solved_errors[tid + stride];
            }
            barrier(); // Synchronizacja na ka¿dym kroku drzewa redukcji
        }

        // 2c. Sprawdzenie lokalnego rekordu trajektorii przez W¹tek 0
        if (tid == 0) {
            float current_step_fitness = float(shared_solved_errors[0]) + shared_fA_sum[0];
            if (current_step_fitness < specimen_best_fitness) {
                specimen_best_fitness = current_step_fitness;
                specimen_best_moves_count = uint(m + 1);
            }
        }
        barrier(); // Synchronizacja ca³ego bloku przed przejœciem do kolejnego ruchu 'm'
    }

    // KROK 3: CZYSTY ZAPIS REKORDU DO POPULACJI (Tylko w¹tek 0 bloku)
    if (tid == 0) {
        Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
        Population[specimenID].MovesCount = specimen_best_moves_count;
    }
}


