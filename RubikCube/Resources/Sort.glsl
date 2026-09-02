#version 430
#include "Setup.glsl"

// 1 thread handles the comparison of a specimen pair in the bitonic network
layout(local_size_x = 256) in;

void main() {
    uint i = gl_GlobalInvocationID.x;
    if (i >= POPULATION_COUNT) return;

    // Determine the partner to compare with in the bitonic network
    uint j = i ^ u_PassModStage;
    if (j > i) {
        // Determine the sort direction (ascending or descending)
        // We look for the MINIMUM, so we want the smallest Fitness at index 0 (ascending)
        bool direction = (i & u_Stage) == 0;
        uint a = direction ? i : j;
        uint b = direction ? j : i;
        // SelectUnique folded into the sort: equal fitness means a duplicate, so demote one (b) by a
        // penalty the size of the maximum fitness (CUBIES_COUNT). Distinct-fitness specimens rise,
        // duplicates sink out of the winner pool, keeping the parent gene-pool diverse (mirrors the
        // CPU's unique selection). The penalty only grows fitness, so the global minimum is never
        // penalised and index 0 stays a true best.
        if (Population[a].Fitness == Population[b].Fitness) {
            Population[b].Fitness = floatBitsToUint(uintBitsToFloat(Population[b].Fitness) + float(CUBIES_COUNT));
        } else if (Population[a].Fitness > Population[b].Fitness) {
            Specimen swapped = Population[a];
            Population[a] = Population[b];
            Population[b] = swapped;
        }
    }
}
