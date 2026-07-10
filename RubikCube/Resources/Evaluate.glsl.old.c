#version 430 core
// Definicja pojedynczego osobnika w populacji
struct Specimen {
    uint fitness;      // Wynik oceny dopasowania kostki
    uint movesCount;    // Liczba ruchów w tym chromosomie
    uint moves[100];    // Tablica genów (sekwencja ruchów)
};

// Globalny bufor stanu populacji kostek Rubika
layout(std430, binding = 0) buffer PopulationBuffer {
    Specimen specimens[]; // Tablica o rozmiarze zależnym od liczby osobników
};

// Globalny bufor przechowujący aktualny stan kubików dla całej populacji
layout(std430, binding = 1) buffer CubiesBuffer {
    // Rozmiar tego bufora to: POPULATION_COUNT * CUBIES_COUNT
    // Aplikacja na CPU alokuje dokładnie tyle pamięci, ile wymaga dana kostka (SIZE^N)
    uint globalCubies[];
};




layout(location = 0) uniform int N;    // dynamiczny wymiar, np. 3, 4, 5...
layout(location = 1) uniform int SIZE; // dynamiczny rozmiar, np. 2, 3, 4...

const int CUBIES_COUNT = SIZE * SIZE * SIZE * SIZE;
//const int XFORM_SIZE = N * N;
const int GENES_COUNT = 32;
const int POPULATION_COUNT = 1024;
const int PLANES_COUNT = N * (N - 1) / 2;
const float C = (SIZE - 1) / 2;
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

layout(std430, binding = 1) buffer SolvedCubies {
    int solvedCubies[];
};

layout(std430, binding = 2) buffer ActiveCubies {
    int activeCubies[];
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
            score += maxClusterState + state + (getRotationCount(state) << PLANES_COUNT);
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


int getStartCoordinate(int cubieID, int col)
{
    //Wyznaczenie indeksu warstwy (0 do SIZE-1) dla wybranego wymiaru (col)
    if (SIZE == 2) return (cubieID >> col) & 1; // Szybka operacja bitowa dla SIZE = 2
    // Uniwersalne rozwiązanie dla dowolnego SIZE (np. 3, 4, 5...)
    int divisor = 1;
    for (int i = 0; i < col; i++)
        divisor *= SIZE;
    return (cubieID / divisor) % SIZE;
}


void TurnCubie(inout uint cubieMatrix, int cubieID, Move move)
{
    int col, sign;
    // Pobieramy informacje tylko dla JEDNEGO wiersza (interesującej nas osi)
    getRow(cubieMatrix, move.Axis, col, sign);

    // Pobieramy pojedynczą współrzędną z wektora początkowego kubika
    int startLayerIndex = getStartCoordinate(cubieID, col);
    int currentLayer = (sign == 1) ? startLayerIndex : (SIZE - 1) - startLayerIndex;
    if (currentLayer == move.Slice) {
        int axis1 = int(Planes[move.Plane].x);
        int axis2 = int(Planes[move.Plane].y);
        RotateMatInt(cubieMatrix, axis1, axis2, move.Angle);
    }
}


// Funkcja pomocnicza: pobiera wartość (1 lub -1) oraz indeks kolumny dla danego wiersza
void getRow(uint M, int row, out int outCol, out int outSign)
{
    uint rowData = (M >> (row * 3)) & 7u; // 7u = binary 111
    outCol = int(rowData & 3u);          // 3u = binary 11 (indeks kolumny 0-3)
    outSign = ((rowData >> 2) & 1u) == 1u ? -1 : 1; // 3. bit to znak
}

// Funkcja pomocnicza: ustawia dane dla konkretnego wiersza w spakowanej macierzy
uint setRow(uint M, int row, int col, int sign)
{
    uint signBit = (sign == -1) ? 1u : 0u;
    uint rowData = (signBit << 2) | (uint(col) & 3u);

    uint mask = ~(7u << (row * 3));
    return (M & mask) | (rowData << (row * 3));
}

// Nowa, niezwykle szybka funkcja obrotu macierzy bez użycia float i trygonometrii!
// Kąt w kostce Rubika to zawsze wielokrotność 90 stopni (krok = 0, 1, 2)
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



void main()
{
    int specimenID = int(gl_GlobalInvocationID.x);
    if (specimenID >= POPULATION_COUNT) return;
    Chromosome specimen = population[specimenID];
    float bestFitness = 100 * cubies.length();
    //XForm[CUBIES_COUNT] workCubies = cubies;
    // Zamiast tablicy struktur, przechowujesz tylko tablicę macierzy!
    uint workCubies[CUBIES_COUNT]; // 16 * 4 bajty = 64 bajty na całą kostkę 4D!
    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(int(specimen.Genes[m])
            for (int cubieID = 0; cubieID < CUBIES_COUNT; cubieID++)
                TurnCubie(workCubies[cubieID], cubieID, move);
        float fitness = evaluateCubies(workCubies);
        if (fitness < bestFitness)
        {
            bestFitness = fitness;
            specimen.MoveCount = cubieID + 1;
        }
    }
    specimen.Fitness = bestFitness;
    population[specimenID] = specimen;
}