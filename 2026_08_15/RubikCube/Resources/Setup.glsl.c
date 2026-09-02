#include "Variables.glsl.c"

// Definicje makr ukrywaj¹ce operacje bitowe
// Baza: Operacje na pojedynczym s³owie uint (lub pojedynczej masce)
#define MASK_SET(m, idx)        ((m) |= (1u << (idx)))
#define MASK_TEST(m, idx)        (((m) & (1u << (idx))) != 0)

// Nadbudowa: Operacje na tablicy wielos³owowej, u¿ywaj¹ce makr bazowych
#define ARR_MASK_SET(a, idx)    MASK_SET((a)[(idx) >> 5u], (idx) & 31u)
#define ARR_MASK_TEST(a, idx)    MASK_TEST((a)[(idx) >> 5u], (idx) & 31u)


struct Specimen {
    uint Fitness;      // floatBitsToUint(fitness_float)
    uint MovesCount;
    uint Moves[GENES_COUNT];
    // DIAGNOSTIC (appended LAST so no existing offset shifts): the coherence piece-size histogram of the state at
    // the best step. 5 bits per bucket, bucket b = pieces with agreeAxes = b+1, i.e. of size 2^(N-1-b); counts
    // clamped at 31. 0 = not decomposed (no coherence / above the gateway). Host decodes it into "4+2" etc.
    uint Structure;
    // The INTEGER structure value the coherence accept compares -- the metric's integer term (currently
    // 2*(P2+N*S)) at the best step. The host reads THIS instead of re-deriving it, so changing the metric
    // lives entirely in EvaluateMicro (one line) and never needs a matching edit on the C# side.
    uint Ladder;
    uint SeedLen;
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

// Inverse move: keep axis/plane/slice, negate the angle in quarter-turns (1<->3, 2 and 0 stay).
// Matches TMove.GetRevCode. Shared by Init (commutator [S,R]) and SelCrossover (commutator [M,D]).
uint getRevCode(uint code) {
    return (code & ~3u) | ((4u - (code & 3u)) & 3u);
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
layout(std430, binding = 2) buffer ClusterBuffer { uint ClusterCubies[]; };
layout(std430, binding = 3) buffer FreeMovesBuffer { uint FreeMoves[]; };
layout(std430, binding = 4) buffer SolvedPosBuffer { uint SolvedPos[]; };
layout(std430, binding = 5) buffer ActivePosBuffer { uint ActivePos[]; };
layout(std140, binding = 6) uniform PlanesBuffer { ivec2 Planes[PLANES_COUNT]; };
layout(std430, binding = 7) buffer SeedBuffer { uint SeedMoves[]; };   // Init: per-specimen seed sequences

// 3. Uniform location registry
layout(location = 0) uniform uint TimeSeed;
// Cluster sizes come from the host, not buffer .length(): a first cluster has an empty SolvedCubies
// buffer, and .length() on a zero-size SSBO is unreliable across drivers.
layout(location = 4) uniform uint countSolved;
//layout(location = 5) uniform uint countActive;
layout(location = 2) uniform uint u_Stage;
layout(location = 3) uniform uint u_PassModStage;
layout(location = 6) uniform uint numSeeds;   // Init: number of specimens pre-seeded from SeedMoves
// Coherence LATCH THRESHOLD, reused as the endgame NORMALIZER base. A uniform (single source of truth =
// Gpu.Floor), so the host and the evaluator can never disagree. It no longer WALKS -- it is the fixed floor
// (FloorStart) that (a) the host uses to trip Coherent 0->1 and (b) the factor divides by, as FLOOR * N, to
// pin the scattered floor state to fitness 1.0.
layout(location = 7) uniform uint FLOOR;
// Coherence LATCH (0/1). A uniform, not a #define, so the phase switch costs no shader rebuild: the host keeps it
// 0 through the count-peel descent and sets it to 1 once the active cluster's residual reaches the floor. When 0
// the evaluator is the bare per-cubie count; when 1 the endgame coherence discount applies (scrambled <= gateway).
layout(location = 8) uniform uint COHERENT;


uint getRow(uint M, uint rowIdx)
{
    return (M >> (rowIdx * BITS_PER_ROW)) & BITS_PER_ROW_MASK;
}

void setRow(inout uint M, uint rowIdx, uint row)
{
    uint shift = rowIdx * BITS_PER_ROW;
    uint mask = BITS_PER_ROW_MASK << shift;
    M = (M & ~mask) | (row << shift);
}

void RotateMatInt(inout uint M, uint axis1, uint axis2, uint angleStep)
{
    // Extract only the two rows we care about, which form the rotation plane
    uint row1 = getRow(M, axis1);
    uint row2 = getRow(M, axis2);

    // angleStep is the number of quarter-turns: 0 = identity (no rotation), 1/2/3 = 90/180/270.
    if (angleStep == 1)      // 90 degrees left
    {
        setRow(M, axis1, row2 ^ 1u);
        setRow(M, axis2, row1);
    }
    else if (angleStep == 2) // 180 degrees
    {
        setRow(M, axis1, row1 ^ 1u);
        setRow(M, axis2, row2 ^ 1u);
    }
    else if (angleStep == 3) // 270 degrees (90 degrees right)
    {
        setRow(M, axis1, row2);
        setRow(M, axis2, row1 ^ 1u);
    }
}

uint getStartCoordinate(uint cubieID, uint col)
{
    uint shift = col * BITSIZE;
    return (cubieID >> shift) & BITSIZE_MASK;
}

// Current coordinate of a cubie along a given axis - the layer index a move on that axis addresses.
// Mirrors the currentLayer computation in TurnSingleCubie: the axis row picks a column, its home
// coordinate along that column, flipped when the axis is reflected. Two cubies sharing this value on
// SOME axis lie in a common layer, so one turn of that layer moves them together.
uint curCoord(uint M, uint cubieID, uint axis) {
    uint rowData = getRow(M, axis);
    uint col = rowData >> 1u;
    uint sign = rowData & 1u;
    uint start = getStartCoordinate(cubieID, col);
    return (sign == 0u) ? start : (SIZE - 1u) - start;
}

// BINARY SIDE of a coordinate within its reflection pair {v, SIZE-1-v}: 0 for the low half, 1 for the high.
// A layer rotation maps v <-> SIZE-1-v, so within a cluster each axis is effectively BINARY (a +/- pair) and the
// cluster is "2^N cubies on a larger orbit". The coherence decomposition works in this SIDE-space, so it stays
// 2-based (ladder + gateway binary) for ANY SIZE. SIZE=2 -> identity (0->0, 1->1).
uint coordSide(uint M, uint cubieID, uint axis) {
    return (2u * curCoord(M, cubieID, axis) >= uint(SIZE)) ? 1u : 0u;
}

uint TurnSingleCubie(uint cubieMatrix, uint cubieID, Move move) {
    uint currentLayer = curCoord(cubieMatrix, cubieID, move.Axis);
    if (currentLayer == move.Slice) {
        uint axis1 = Planes[move.Plane].x;
        uint axis2 = Planes[move.Plane].y;
        RotateMatInt(cubieMatrix, axis1, axis2, move.Angle);
    }
    return cubieMatrix;
}

// L1 distance between two packed orientation matrices, field by field. matL1(M, IDENTITY) == cubieL1(M).
// The endgame orientation sub-gradient uses it as "distance from a REFERENCE orientation": the cluster's
// DOMINANT orientation while building coherence (drive to uniformity = one block = the gateway), then IDENTITY
// once uniform (collapse the block to solved).
uint matL1(uint M, uint R) {
    uint dist = 0u;
    for (uint r = 0u; r < uint(N); r++) {
        uint rowM = getRow(M, r);
        uint rowR = getRow(R, r);
        //rowM = (rowM & 1) << BITS_PER_ROW - 1 | rowM >> 1;
        //rowR = (rowR & 1) << BITS_PER_ROW - 1 | rowR >> 1;
        dist += (rowM > rowR) ? (rowM - rowR) : (rowR - rowM);
    }
    return dist;
}

// L1 (Manhattan) distance of a packed orientation matrix from IDENTITY.
// In new approach: The sign
// is kept on the low bit of the field, so a reflected axis differs by 1,
// (the "lowest difference") so distinct orientations do not
// collide to the same L1.
uint cubieL1(uint M) {
    return matL1(M, IDENTITY);
}

// Number of axes the orientation does not leave fixed (row r != identity value r). A cubie needs
// m - 1 moves to solve (Givens spanning tree of its active axes), so m is a moves-to-solve measure.
uint activeAxes(uint M) {
    uint m = 0u;
    for (uint r = 0u; r < uint(N); r++) {
        uint rowM = getRow(M, r);
        uint rowI = getRow(IDENTITY, r);
        if (rowM != rowI) m++;
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
    // D4: smooth (cliff-free) base. The cliff form (maxClusterState + d)/max_fA adds a big fixed premium per
    // scrambled cubie, so fA is essentially a COUNT and the floor plateau below T is flat -> STALL blind-walks
    // out of it (the D3 tail). This form charges only the magnitude d/maxClusterState, giving the floor a
    // sub-gradient toward less twist = toward solved. Trade-off: it is a GLOBAL change (mid-game homing loses
    // the cliff, may slow) and the floor's count-block softens. A/B against the cliff form above.
    //return float(d) / maxClusterState;
    return d == 0u ? 0.0 : (maxClusterState + float(d)) / max_fA;
}
