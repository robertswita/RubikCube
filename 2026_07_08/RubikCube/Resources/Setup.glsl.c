#define uint unsigned int
#define GENERATIONS_COUNT 50
#define GENES_COUNT 32
#define POPULATION_COUNT 1024
#define WINNERS_RATIO 10
#define MUTATION_RATIO 1
#define MAX_PLANES_COUNT 28 // 8D

struct Specimen {
    uint Fitness;      // floatBitsToUint(fitness_float)
    uint MovesCount;
    int Moves[GENES_COUNT];
};

struct Move
{
    int Axis;
    int Slice;
    int Plane;
    int Angle;
};

Move getMove(int code)
{
    int angle = code & 3;
    int plane = (code >> 2) & 31;
    int axis = (code >> 7) & 7;
    int slice = code >> 10;
    return Move(axis, slice, plane, angle);
}

// 2. CENTRALNY REJESTR BUFORÓW
layout(std430, binding = 0) buffer PopulationBuffer { Specimen Population[]; };
layout(std430, binding = 1) buffer CubiesBuffer { uint Cubies[]; };
layout(std430, binding = 2) buffer SolvedBuffer { uint SolvedCubies[]; };
layout(std430, binding = 3) buffer ActiveBuffer { uint ActiveCubies[]; };
layout(std430, binding = 4) buffer BestResultBuffer { Specimen Best; };
layout(std430, binding = 5) buffer NewPopulationBuffer { Specimen NewPopulation[]; }; // Dla krzyżowania
layout(std140, binding = 6) uniform PlanesBuffer { ivec2 Planes[MAX_PLANES_COUNT]; };

// 3. REJESTR LOKACJI UNIFORMÓW
layout(location = 0) uniform uint N;
layout(location = 1) uniform uint SIZE;
layout(location = 2) uniform uint CUBIES_COUNT; // SIZE^N
layout(location = 2) uniform uint PLANES_COUNT; // SIZE^N

layout(location = 3) uniform uint countSolved;
layout(location = 4) uniform uint countActive;
layout(location = 5) uniform uint maxStateValue;

layout(location = 6) uniform uint u_Stage;
layout(location = 7) uniform uint u_PassModStage;


int bitsForCol;
int bitsPerRow;

void getRow(uint M, int row, out int outCol, out int outSign)
{
    uint rowData = (M >> (row * bitsPerRow)) & ((1u << bitsPerRow) - 1u);

    outCol = int(rowData & ((1u << bitsForCol) - 1u));
    outSign = ((rowData >> bitsForCol) & 1u) == 1u ? -1 : 1;
}

uint setRow(uint M, int row, int col, int sign)
{
    uint signBit = (sign == -1) ? 1u : 0u;
    uint rowData = (signBit << bitsForCol) | (uint(col) & ((1u << bitsForCol) - 1u));
    uint shift = uint(row) * bitsPerRow;
    uint mask = ~(((1u << bitsPerRow) - 1u) << shift);
    return (M & mask) | (rowData << shift);
}

void RotateMatInt(inout uint M, int axis1, int axis2, int angleStep)
{
    // Wyciągamy tylko dwa interesujące nas wiersze, które tworzą płaszczyznę obrotu
    int col1, sign1;
    int col2, sign2;
    getRow(M, axis1, col1, sign1);
    getRow(M, axis2, col2, sign2);

    if (angleStep == 0)      // 90 stopni w lewo
    {
        M = setRow(M, axis1, col2, -sign2);
        M = setRow(M, axis2, col1, sign1);
    }
    else if (angleStep == 1) // 180 stopni
    {
        M = setRow(M, axis1, col1, -sign1);
        M = setRow(M, axis2, col2, -sign2);
    }
    else if (angleStep == 2) // 270 stopni (90 stopni w prawo)
    {
        M = setRow(M, axis1, col2, sign2);
        M = setRow(M, axis2, col1, -sign1);
    }
}

int getStartCoordinate(int cubieID, int col)
{
    //Wyznaczenie indeksu warstwy (0 do SIZE-1) dla wybranego wymiaru (col)
    if (SIZE == 2) return (cubieID >> col) & 1; // Szybka operacja bitowa dla SIZE = 2
    int divisor = 1;
    for (int i = 0; i < col; i++)
        divisor *= SIZE;
    return (cubieID / divisor) % SIZE;
}

uint TurnSingleCubie(uint cubieMatrix, uint cubieID, Move move) {
    int col, sign;
    getRow(cubieMatrix, move.Axis, col, sign);
    int startLayerIndex = getStartCoordinate(int(cubieID), col);
    int currentLayer = (sign == 1) ? startLayerIndex : (int(SIZE) - 1) - startLayerIndex;
    if (currentLayer == move.Slice) {
        int axis1 = Planes[move.Plane].x;
        int axis2 = Planes[move.Plane].y;
        RotateMatInt(cubieMatrix, axis1, axis2, move.Angle);
    }
    return cubieMatrix;
}

float GetActiveCubieError(uint cubieMatrix, float maxClusterState, float max_fA) {
    if (cubieMatrix != 0)
        return (maxClusterState + float(cubieMatrix)) / max_fA;
    return 0.0;
}
