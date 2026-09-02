#version 430 core
#define uint unsigned int

layout(local_size_x = 64) in;

struct Specimen {
    uint Fitness;
    uint BestMovesCount;
    uint Moves[GENES_COUNT];
};

// Globalny bufor stanu populacji kostek Rubika
layout(std430, binding = 0) buffer PopulationBuffer {
    Specimen Population[]; // Tablica o rozmiarze zależnym od liczby osobników
};
// Bufor zawierający listę dozwolonych ruchów dla bieżącego klastra
layout(std430, binding = 2) buffer FreeMovesBuffer
{
    uint FreeMoves[];
};

// Szybki hash bitowy do generowania losowości
uint hash(uint x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

void main() {
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;
    uint seed = specimenID ^ TimeSeed;
    uint freeMovesCount = uint(freeMoves.length());
    for (uint g = 0; g < GENES_COUNT; g++) {
        uint randValue = hash(seed + g);
        Population[specimenID].moves[g] = FreeMoves[randValue % freeMovesCount];
    }
}
