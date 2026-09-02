#define uint unsigned int
#define N 3
#define SIZE 3
#define CUBIES_COUNT 27
#define PLANES_COUNT (N*(N-1)/2)
#define GENERATIONS_COUNT 50
#define GENES_COUNT 32
#define POPULATION_COUNT 1024
#define WINNERS_RATIO 10
// Percentage of children rebuilt by macro-mutation (fresh seed + conjugate) in SelCrossover.
// The only mutation operator - single-gene random mutation was removed.
#define MUTATION_RATIO 20
// Percentage of the initial population pre-seeded by Init; the rest are random, for the diversity
// the (incomplete) seed decompositions miss.
#define SEED_RATIO 80

// Row packing of the orientation matrix (N in range [3, 8]): bits for the column index + 1 sign bit.
// Equivalent to findMSB(N-1)+1, but as a compile-time constant (findMSB is not a constant expression).
#define BITS_FOR_COL (N <= 4 ? 2 : 3)
#define BITS_PER_ROW (BITS_FOR_COL + 1)

// Rigorous upper bound on the per-cubie L1 distance.
// This exceeds the achievable maximum, so each active-cluster
// magnitude term stays strictly below 1.
#define MAX_CUBIE_L1 (N * ((1 << BITS_FOR_COL) + N - 1))

// Per-cubie penalty = moves-to-solve (active axes, dominant) * (MAX_CUBIE_L1 + 1) + L1 (tiebreak).
// Rigorous upper bound: m <= N active axes, L1 <= MAX_CUBIE_L1; still exceeds the achievable maximum,
// so each active-cluster magnitude term stays strictly below 1.
#define MAX_CUBIE_STATE (N * (MAX_CUBIE_L1 + 1) + MAX_CUBIE_L1)

// Slot width (in genes) reserved for one Init seed sequence: solving a cubie takes at most N-1 moves.
#define SEED_STRIDE (uint(N) - 1u)

struct Specimen {
    uint Fitness;      // floatBitsToUint(fitness_float)
    uint MovesCount;
    uint Moves[GENES_COUNT];
};

struct Move
{
    uint Axis;
    uint Slice;
    uint Plane;
    uint Angle;
};

Move getMove(uint code)
{
    uint angle = code & 3u;
    uint plane = (code >> 2u) & 31u;
    uint axis = (code >> 7u) & 7u;
    uint slice = code >> 10u;
    return Move(axis, slice, plane, angle);
}

// Fast bitwise hash for randomness generation (shared by Init and SelCrossover)
uint hash(uint x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

// 2. Central buffer registry
layout(std430, binding = 0) buffer PopulationBuffer { Specimen Population[]; };
layout(std430, binding = 1) buffer NewPopulationBuffer { Specimen NewPopulation[]; }; // Crossover output (ping-pong with Population)
layout(std430, binding = 2) buffer CubiesBuffer { uint Cubies[]; };
layout(std430, binding = 3) buffer FreeMovesBuffer { uint FreeMoves[]; };
layout(std430, binding = 4) buffer SolvedBuffer { uint SolvedCubies[]; };
layout(std430, binding = 5) buffer ActiveBuffer { uint ActiveCubies[]; };
layout(std140, binding = 6) uniform PlanesBuffer { ivec2 Planes[PLANES_COUNT]; };
layout(std430, binding = 7) buffer SeedBuffer { uint SeedMoves[]; };   // Init: per-specimen seed sequences

// 3. Uniform location registry
//layout(location = 0) uniform uint N;
//layout(location = 1) uniform uint SIZE;
//layout(location = 2) uniform uint CUBIES_COUNT; // SIZE^N
//layout(location = 3) uniform uint PLANES_COUNT;
layout(location = 0) uniform uint TimeSeed;
// Cluster sizes come from the host, not buffer .length(): a first cluster has an empty SolvedCubies
// buffer, and .length() on a zero-size SSBO is unreliable across drivers.
layout(location = 4) uniform uint countSolved;
layout(location = 5) uniform uint countActive;
layout(location = 2) uniform uint u_Stage;
layout(location = 3) uniform uint u_PassModStage;
layout(location = 6) uniform uint numSeeds;   // Init: number of specimens pre-seeded from SeedMoves


void getRow(uint M, uint row, out uint outCol, out int outSign)
{
    uint rowData = (M >> (row * BITS_PER_ROW)) & ((1u << BITS_PER_ROW) - 1u);

    outCol = rowData & ((1u << BITS_FOR_COL) - 1u);
    outSign = ((rowData >> BITS_FOR_COL) & 1u) == 1u ? -1 : 1;
}

uint setRow(uint M, uint row, uint col, int sign)
{
    uint signBit = (sign == -1) ? 1u : 0u;
    uint rowData = (signBit << BITS_FOR_COL) | (col & ((1u << BITS_FOR_COL) - 1u));
    uint shift = row * BITS_PER_ROW;
    uint mask = ((1u << BITS_PER_ROW) - 1u) << shift;
    return (M & ~mask) | (rowData << shift);
}

void RotateMatInt(inout uint M, uint axis1, uint axis2, uint angleStep)
{
    // Extract only the two rows we care about, which form the rotation plane
    uint col1, col2;
    int sign1, sign2;
    getRow(M, axis1, col1, sign1);
    getRow(M, axis2, col2, sign2);

    if (angleStep == 0)      // 90 degrees left
    {
        M = setRow(M, axis1, col2, -sign2);
        M = setRow(M, axis2, col1, sign1);
    }
    else if (angleStep == 1) // 180 degrees
    {
        M = setRow(M, axis1, col1, -sign1);
        M = setRow(M, axis2, col2, -sign2);
    }
    else if (angleStep == 2) // 270 degrees (90 degrees right)
    {
        M = setRow(M, axis1, col2, sign2);
        M = setRow(M, axis2, col1, -sign1);
    }
}

uint getStartCoordinate(uint cubieID, uint col)
{
    // Home coordinate along dimension col. The CPU linear index (TMatrix.Coords2Index) is
    // coords[0]*SIZE^(N-1) + ... + coords[N-1], so axis col has stride SIZE^(N-1-col), i.e. axis 0
    // is the most significant digit. This keeps the shader's axis numbering identical to the CPU's.
    uint shift = N - 1u - col;
    if (SIZE == 2)
        return (cubieID >> shift) & 1u; // Fast bitwise op for SIZE = 2
    else
    {
        uint divisor = 1u;
        for (uint i = 0u; i < shift; i++)
            divisor *= SIZE;
        return (cubieID / divisor) % SIZE;
    }
}

uint TurnSingleCubie(uint cubieMatrix, uint cubieID, Move move) {
    uint col;
    int sign;
    getRow(cubieMatrix, move.Axis, col, sign);
    uint startLayerIndex = getStartCoordinate(cubieID, col);
    uint currentLayer = (sign == 1) ? startLayerIndex : (SIZE - 1) - startLayerIndex;
    if (currentLayer == move.Slice) {
        uint axis1 = Planes[move.Plane].x;
        uint axis2 = Planes[move.Plane].y;
        RotateMatInt(cubieMatrix, axis1, axis2, move.Angle);
    }
    return cubieMatrix;
}

// L1 (Manhattan) distance of a packed orientation matrix from IDENTITY.
// The identity row r encodes (col = r, sign = +), i.e. field value r; a solved cubie -> 0. The sign
// is kept on the high bit of the field, so a reflected axis differs by 2^BITS_FOR_COL (>= N),
// dominating any column displacement (the "largest difference") so distinct orientations do not
// collide to the same L1.
uint cubieL1(uint M) {
    uint dist = 0u;
    for (uint r = 0u; r < uint(N); r++) {
        uint field = (M >> (r * BITS_PER_ROW)) & ((1u << BITS_PER_ROW) - 1u);
        dist += (field > r) ? (field - r) : (r - field);
    }
    return dist;
}

// Number of axes the orientation does not leave fixed (row r != identity value r). A cubie needs
// m - 1 moves to solve (Givens spanning tree of its active axes), so m is a moves-to-solve measure.
uint activeAxes(uint M) {
    uint m = 0u;
    for (uint r = 0u; r < uint(N); r++) {
        uint field = (M >> (r * BITS_PER_ROW)) & ((1u << BITS_PER_ROW) - 1u);
        if (field != r) m++;
    }
    return m;
}

// Per-cubie penalty: moves-to-solve (active axes, dominant) refined by L1 orientation distance.
// 0 <=> solved. m dominates because each active axis is weighted above the whole L1 range, so the
// search minimises the number of moves first, then the orientation distance within that.
uint cubieState(uint M) {
    return activeAxes(M) * (uint(MAX_CUBIE_L1) + 1u) + cubieL1(M);
}

float GetActiveCubieError(uint cubieMatrix, float maxClusterState, float max_fA) {
    uint d = cubieState(cubieMatrix);
    if (d != 0u)
        return (maxClusterState + float(d)) / max_fA;
    return 0.0;
}
