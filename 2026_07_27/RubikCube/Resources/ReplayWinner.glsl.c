#version 430
#include "Setup.glsl.c"

// Replays the winning specimen's moves and writes the RESULTING packed cube state back to the host.
// GPU single-source-of-truth for the endgame state: the GA already applied these moves while scoring,
// so the host no longer replays them (TAffine turns) nor decomposes orientation (Euler angles) -- it
// ingests the OrthoPack uints computed here. One thread per cubie.
//
// Called by the host RIGHT AFTER a new global best is found (Population[0] just sorted to index 0, before
// selection/crossover overwrites it), reading the winner IN PLACE -- so no move upload and no dependence
// on the final generation's Population[0] (which need not be the global best without elitism).
//
// Starts from Cubies[] (the state at the START of this GA run, binding 2) and applies Moves[0..MovesCount)
// of Population[0] -- exactly the prefix the GA scored as best. ResultState[id] is then Cubies[id] turned
// through that prefix, in the SAME OrthoPack encoding PackCubies produces, so the host can feed it straight
// back as the next run's Cubies with no CPU matrix math.

layout(local_size_x = 64) in;

layout(std430, binding = 12) buffer ResultStateBuffer { uint ResultState[]; };

void main()
{
    uint id = gl_GlobalInvocationID.x;
    if (id >= uint(CUBIES_COUNT)) return;

    uint c = Cubies[id];
    uint movesCount = Population[0].MovesCount;
    for (uint m = 0u; m < movesCount; m++)
        c = TurnSingleCubie(c, id, getMove(Population[0].Moves[m]));

    ResultState[id] = c;
}
