#version 430 core
#include "Setup.glsl.c"
#extension GL_ARB_gpu_shader_int64 : enable
#extension GL_NV_shader_atomic_int64 : enable
#define uint unsigned int


struct Specimen {
    uint_64_t Fitness;
    uint BestMovesCount;
    uint Moves[100];
};

// Globalny bufor stanu populacji kostek Rubika
layout(std430, binding = 0) buffer PopulationBuffer {
    Specimen Population[]; // Tablica o rozmiarze zale¿nym od liczby osobników
};

// Globalny bufor przechowuj¹cy aktualny stan kubików dla ca³ej populacji
layout(std430, binding = 1) buffer CubiesBuffer {
    // Rozmiar tego bufora to: POPULATION_COUNT * CUBIES_COUNT
    // Aplikacja na CPU alokuje dok³adnie tyle pamiêci, ile wymaga dana kostka (SIZE^N)
    uint globalCubies[];
};

// Bufor wynikowy, przez który GPU przekazuje dane do CPU
layout(std430, binding = 4) buffer BestResultBuffer {
    Specimen Best;
};

layout(std430, binding = 1) buffer CubeStatesBuffer {
    uint CubeStates[];
};

layout(std430, binding = 1) buffer SolvedCubies {
    int SolvedCubies[];
};

layout(std430, binding = 2) buffer ActiveCubies {
    int ActiveCubies[];
};

layout(location = 0) uniform int N;    // dynamiczny wymiar, np. 3, 4, 5...
layout(location = 1) uniform int SIZE; // dynamiczny rozmiar, np. 2, 3, 4...

//const int CUBIES_COUNT = SIZE * SIZE * SIZE * SIZE;
//const int XFORM_SIZE = N * N;
const int GENES_COUNT = 32;
const int POPULATION_COUNT = 1024;
const int PLANES_COUNT = N * (N - 1) / 2;
//const float C = (SIZE - 1) / 2;
//const int CUBIES_SIZE = (XFORM_SIZE + N) * CUBIES_COUNT;

layout(local_size_x = 64) in;

struct XForm
{
    float[XFORM_SIZE] M;
    float[N] Origin;
};

layout(std430, binding = 0) buffer Cubies {
    XForm[CUBIES_COUNT] cubies;
};

layout(std430, binding = 4) buffer planes
{
    vec2 Planes[PLANES_COUNT];
};

struct Chromosome
{
    float Genes[GENES_COUNT];
    float MoveCount;
    float Fitness;
};

layout(std430, binding = 5) buffer Population
{
    Chromosome population[POPULATION_COUNT];
};

int getAngle(float cosA, float sinA)
{
    if (cosA > 0.5) return 0;
    if (sinA > 0.5) return 1;
    if (cosA < -0.5) return 2;
    if (sinA < -0.5) return 3;
    return 0;
}

vec2 setAngle(int angle)
{
    float cosA = 0;
    float sinA = 0;
    switch (angle)
    {
    case 0: cosA = 1; break;
    case 1: sinA = 1; break;
    case 2: cosA = -1; break;
    case 3: sinA = -1; break;
    }
    return vec2(cosA, sinA);
}

void RotateVec(inout float[N] v, int axis1, int axis2, vec2 rot)
{
    float a = v[axis1];
    float b = v[axis2];
    v[axis1] = rot.x * a - rot.y * b;
    v[axis2] = rot.y * a + rot.x * b;
}

void RotateMat(inout float[XFORM_SIZE] mat, int axis1, int axis2, vec2 rot)
{
    for (int i = axis1, j = axis2; i < XFORM_SIZE; i += N, j += N)
    {
        float a = mat[i];
        float b = mat[j];
        mat[i] = rot.x * a - rot.y * b;
        mat[j] = rot.y * a + rot.x * b;
    }
}

vec2[PLANES_COUNT] getEulerAngles(float[XFORM_SIZE] A)
{
    vec2[PLANES_COUNT] angles;
    for (int i = 0; i < Planes.length(); i++)
    {
        int axis1 = int(Planes[i].x);
        int axis2 = int(Planes[i].y);
        float a = A[axis1 + axis1 * N];
        float b = A[axis2 + axis1 * N];
        float r = sqrt(a * a + b * b);
        if (r < 0.1)
            angles[i] = vec2(1, 0);
        else
        {
            float cosA = a / r;
            float sinA = b / r;
            RotateMat(A, axis1, axis2, vec2(cosA, -sinA));
            angles[i] = vec2(cosA, sinA);
        }
    }
    //float[N] scale;
    //for (int i = 0; i < N; i++)
    //    scale[i] = (float)A.Cols[0].Norm;
    //var error = (A - TAffine.CreateScale(scale).M).Norm;
    //if (error > 1E-3)
    //    ;
    return angles;
}

int getState(XForm transform)
{
    vec2[PLANES_COUNT] EulerAngles = getEulerAngles(transform.M);
    int state = 0;
    int shift = (PLANES_COUNT - 1) << 1;
    for (int i = 0; i < PLANES_COUNT; i++)
    {
        int angle = getAngle(EulerAngles[i].x, EulerAngles[i].y);
        state |= angle << shift;
        shift -= 2;
    }
    return state;
}

int getRotationCount(int state)
{
    int RotationCount = 0;
    for (int i = 0; i < PLANES_COUNT; i++)
    {
        if ((state & 3) > 0)
            RotationCount++;
        state >>= 2;
    }
    return RotationCount;
}

struct Move
{
    int Axis;
    int Slice;
    int Plane;
    int Angle;
};

Move getMove(int code)
{
    int size = SIZE * PLANES_COUNT * 3;
    int axis = code / size;
    code -= axis * size;
    size /= SIZE;
    int slice = code / size;
    code -= slice * size;
    size /= PLANES_COUNT;
    int plane = code / size;
    code -= plane * size;
    int angle = code;
    return Move(axis, slice, plane, angle);
}

float evaluateCubies(XForm[CUBIES_COUNT] workCubies)
{
    float score = 0;
    int scrambled = 0;
    float maxClusterState = float(1 << 2 * PLANES_COUNT) * activeCubies.length() * N;
    for (int i = 0; i < activeCubies.length(); i++)
    {
        //XForm cubie;
        //cubie = workCubies[activeCubies[i]];
        //score += cubie.M[0] + cubie.M[1] + cubie.M[2] + cubie.M[3];
        int state = getState(workCubies[activeCubies[i]]);
        if (state != 0)
        {
            score += maxClusterState + state +(getRotationCount(state) << PLANES_COUNT);
            scrambled++;
        }
    }
    score /= maxClusterState * (activeCubies.length() + 1);
    if (scrambled == 1)
        score *= 2;
    for (int i = 0; i < solvedCubies.length(); i++)
    {
        int state = getState(workCubies[solvedCubies[i]]);
        if (state != 0)
            score++;
    }
    return 100 * score;
}

void Rotate(inout XForm cubie, int plane, int angle)
{
    int axis1 = int(Planes[plane].x);
    int axis2 = int(Planes[plane].y);
    vec2 rot = setAngle(angle);
    RotateMat(cubie.M, axis1, axis2, rot);
    RotateVec(cubie.Origin, axis1, axis2, rot);
}


//void Turn(inout XForm[CUBIES_COUNT] workCubies, Move move)
//{
//    for (int i = 0; i < workCubies.length(); i++)
//    {
//        float v = round(workCubies[i].Origin[move.Axis] + C);
//        if (int(v) == move.Slice)
//            Rotate(workCubies[i], move.Plane, move.Angle + 1);
//    }       
//}





layout(location = 0) uniform int N;
layout(location = 1) uniform int SIZE;

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
    // Wyci¹gamy tylko dwa interesuj¹ce nas wiersze, które tworz¹ p³aszczyznê obrotu
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

void TurnCubie(inout uint cubieMatrix, int cubieID, Move move)
{
    int col, sign;
    // Pobieramy informacje tylko dla JEDNEGO wiersza (interesuj¹cej nas osi)
    getRow(cubieMatrix, move.Axis, col, sign);

    // Pobieramy pojedyncz¹ wspó³rzêdn¹ z wektora pocz¹tkowego kubika
    int startLayerIndex = getStartCoordinate(cubieID, col);
    int currentLayer = (sign == 1) ? startLayerIndex : (SIZE - 1) - startLayerIndex;
    if (currentLayer == move.Slice) {
        int axis1 = int(Planes[move.Plane].x);
        int axis2 = int(Planes[move.Plane].y);
        RotateMatInt(cubieMatrix, axis1, axis2, move.Angle);
    }
}


layout(location = 0) uniform int N;
layout(location = 1) uniform int SIZE;
layout(location = 2) uniform int CUBIES_COUNT; // Przekazywane dynamicznie z CPU (SIZE^N)

void main()
{
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;
    bitsPerRow = findMSB(N - 1) + 2;
    uint baseIndex = specimenID * CUBIES_COUNT;
    float bestFitness = 100 * cubies.length();
    uint bestMovesCount = 0;
    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = decodeMove(Population[specimenID].moves[m]);
        //uint axis1 = uint(Planes[move.Plane].x);
        //uint axis2 = uint(Planes[move.Plane].y);
        for (int cubieID = 0; cubieID < CUBIES_COUNT; cubieID++)
        {
            uint globalCubieIndex = baseIndex + cubieID;

            // Pobieramy spakowan¹ macierz z pamiêci globalnej
            uint cubieMatrix = globalCubies[globalCubieIndex];

            // Wykonujemy operacjê Turn (modyfikacja lokalnej zmiennej)
            if (isCubieInLayer(cubieMatrix, cubieID, move.Axis, move.Layer))
            {
                RotateMatInt(cubieMatrix, move.Axis1, move.Axis2, move.AngleStep);
                globalCubies[globalCubieIndex] = cubieMatrix;
            }
        }

        float fitness = Evaluate();
        if (fitness < bestFitness)
        {
            bestFitness = fitness;
            bestMovesCount = m + 1;
        }
    }
    specimens[specimenID].Fitness = bestFitness;
    specimens[specimenID].MovesCount = bestMovesCount;
    uint previousMin = atomicMin(globalBestFitness, bestFitness);
    if (bestFitness < previousMin) {
        Best.MovesCount = bestMovesCount;
        for (int g = 0; g < finalMovesCount; g++)
            Best.Moves[g] = specimens[specimenID].moves[g];
    }
}

void Evaluate()
{
    // =========================================================================
    // 1. DYNAMICZNA ARYTMETYKA BITOWA (MSB-first, bez float/log2)
    // =========================================================================
    //uint bitsPerRow = uint(findMSB(N - 1)) + 2u;
    uint bitsForCubeState = uint(N) * bitsPerRow;

    // findMSB(X) + 1u wyznacza dok³adn¹ liczbê bitów potrzebn¹ do zapisu liczby X
    uint bitsForACount = uint(findMSB(aCount)) + 1u;

    // Wyznaczenie punktów przesuniêæ (Y oraz X)
    uint shiftForBadACount = bitsForACount + bitsForCubeState; // Nasz punkt Y
    uint shiftForSolved = shiftForBadACount + bitsForACount;    // Nasz punkt X

    // Dynamiczny próg saturacji dla Solved (wszystkie bity powy¿ej punktu X)
    uint64_t maxPenalty = (1UL << (64u - shiftForSolved)) - 1UL;

    // =========================================================================
    // 2. EWALUACJA KOSTKI NA GPU
    // =========================================================================

    // KROK A: Zliczanie kar (Masa zepsutych kubików Solved)
    uint64_t brokenSolvedCount = 0UL;
    for (int i = 0; i < solvedCount; i++)
    {
        if (CubeStates[SolvedIndices[i]] != 0u)
        {
            brokenSolvedCount++;
            if (brokenSolvedCount >= maxPenalty) {
                brokenSolvedCount = maxPenalty;
                break;
            }
        }
    }

    // KROK B: Zliczanie b³êdów w grupie aktywnej A
    uint64_t badACount = 0UL;
    uint64_t sumAStates = 0UL;

    for (int j = 0; j < aCount; j++)
    {
        uint64_t state = uint64_t(CubeStates[GroupAIndices[j]]);
        if (state != 0UL)
        {
            badACount++;       // Kryterium nadrzêdne dla grupy A
            sumAStates += state; // Kryterium pomocnicze dla grupy A
        }
    }

    // =========================================================================
    // 3. PAKOWANIE TRÓJSTOPNIOWEGO KLUCZA ATOMOWEGO
    // =========================================================================
    uint64_t fitnessKey = (brokenSolvedCount << shiftForSolved)
        | (badACount << shiftForBadACount)
        | sumAStates;

    // =========================================================================
    // 4. SELEKCJA ELITY
    // =========================================================================
    uint64_t originalValue = atomicMin(elite.fitnessKey, fitnessKey);

    if (fitnessKey < originalValue)
    {
        elite.chromosomeId = threadId;
    }
}
