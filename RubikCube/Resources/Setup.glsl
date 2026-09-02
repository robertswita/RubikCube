#include "Variables.glsl"

// Definicje makr ukrywaj�ce operacje bitowe
// Baza: Operacje na pojedynczym s�owie uint (lub pojedynczej masce)
#define MASK_SET(m, idx)        ((m) |= (1u << (idx)))
#define MASK_TEST(m, idx)        (((m) & (1u << (idx))) != 0)

// Nadbudowa: Operacje na tablicy wielos�owowej, u�ywaj�ce makr bazowych
#define ARR_MASK_SET(a, idx)    MASK_SET((a)[(idx) >> 5u], (idx) & 31u)
#define ARR_MASK_TEST(a, idx)    MASK_TEST((a)[(idx) >> 5u], (idx) & 31u)

struct Specimen
{
    uint Fitness; // floatBitsToUint(fitness_float)
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
uint getRevCode(uint code)
{
    return (code & ~3u) | ((4u - (code & 3u)) & 3u);
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
layout(location = 6) uniform uint numSeeds; // Init: number of specimens pre-seeded from SeedMoves
// Coherence LATCH THRESHOLD, reused as the endgame NORMALIZER base. A uniform (single source of truth =
// Gpu.Floor), so the host and the evaluator can never disagree. It no longer WALKS -- it is the fixed floor
// (FloorStart) that (a) the host uses to trip Coherent 0->1 and (b) the factor divides by, as FLOOR * N, to
// pin the scattered floor state to fitness 1.0.
layout(location = 7) uniform uint FLOOR;
// Coherence LATCH (0/1). A uniform, not a #define, so the phase switch costs no shader rebuild: the host keeps it
// 0 through the count-peel descent and sets it to 1 once the active cluster's residual reaches the floor. When 0
// the evaluator is the bare per-cubie count; when 1 the endgame coherence discount applies (scrambled <= gateway).
layout(location = 8) uniform uint COHERENT;

// Per-invocation counter (incremented each call to rand())
uint RngStep = 0u;

// Return next 32-bit unsigned random
uint randNext(uint max)
{
    RngStep++;
    uint x = gl_GlobalInvocationID.x ^ TimeSeed + RngStep;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x % max;
}

uint getRowCode(uint M, uint rowIdx)
{
    return (M >> (rowIdx * BITS_PER_ROW)) & BITS_PER_ROW_MASK;
}

void setRowCode(inout uint M, uint rowIdx, uint row)
{
    uint shift = rowIdx * BITS_PER_ROW;
    M &= ~(BITS_PER_ROW_MASK << shift);
    M |= row << shift;
}

// SEMANTIC ORIENTATION ACCESSORS -- the interface the rest of the code uses. Only these and RotateMatInt know
// the packed (col<<1)|sign encoding behind getRowCode. getElem = signed matrix element, axisInRow = permutation image.

// The axis currently sitting in row i (the permutation image) -- the one decode getElem cannot give in O(1).
//uint axisInRow(uint M, uint i) { return getRowCode(M, i) >> 1u; }
uint axisFromCode(uint code)
{
    return code >> 1u;
}

bool negSignFromCode(uint code)
{
    return (code & 1u) != 0u;
}

// Signed (i,j) entry of orientation M: +1 / -1 if row i holds axis j (reflection bit 0 / 1), else 0. A signed
// permutation matrix has ONE non-zero per row, so this is 0 for all but one j. No +0/-0 collision: 0 means
// "no entry at (i,j)"; the +-1 is only returned once the column matches. getElem(M,i,i) is the diagonal.
int getElem(uint M, uint i, uint j)
{
    uint code = getRowCode(M, i);
    if (axisFromCode(code) != j)
        return 0;
    return negSignFromCode(code) ? -1 : 1;
}

void RotateMatInt(inout uint M, uint axis1, uint axis2, uint angleStep)
{
		// Extract only the two rows we care about, which form the rotation plane
    uint row1 = getRowCode(M, axis1);
    uint row2 = getRowCode(M, axis2);

		// angleStep is the number of quarter-turns: 0 = identity (no rotation), 1/2/3 = 90/180/270.
    if (angleStep == 1)      // 90 degrees left
    {
        setRowCode(M, axis1, row2 ^ 1u);
        setRowCode(M, axis2, row1);
    }
    else if (angleStep == 2) // 180 degrees
    {
        setRowCode(M, axis1, row1 ^ 1u);
        setRowCode(M, axis2, row2 ^ 1u);
    }
    else if (angleStep == 3) // 270 degrees (90 degrees right)
    {
        setRowCode(M, axis1, row2);
        setRowCode(M, axis2, row1 ^ 1u);
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
uint curCoord(uint M, uint cubieID, uint axis)
{
    uint rowData = getRowCode(M, axis);
    uint start = getStartCoordinate(cubieID, axisFromCode(rowData));
    return negSignFromCode(rowData) ? (SIZE - 1u) - start : start;
}


