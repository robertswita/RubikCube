#version 430
#include "Setup.glsl.c"

// 1 wątek odpowiada za porównanie pary osobnika w sieci bitonicznej
layout(local_size_x = 256) in;

void main() {
    uint i = gl_GlobalInvocationID.x;
    if (i >= POPULATION_COUNT) return;

    // Wyznaczenie partnera do porównania w sieci bitonicznej
    uint j = i ^ u_PassModStage;
    if (j > i) {
        // Określenie kierunku sortowania (rosnąco czy malejąco)
        // Szukamy MINIMUM, więc chcemy mieć najmniejszy Fitness na indeksie 0 (rosnąco)
        bool direction = (i & u_Stage) == 0;
        uint a = direction ? i : j;
        uint b = direction ? j : i;
        if (Population[a].Fitness > Population[b].Fitness) {
            Specimen swapped = Population[a];
            Population[a] = Population[b];
            Population[b] = swapped;
        }
    }
}
