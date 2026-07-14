#define uint unsigned int
#define N 3
#define SIZE 3
// Cubies Count = Size^N
#define CUBIES_COUNT 27
#define PLANES_COUNT (N*(N-1)/2)
#define GENERATIONS_COUNT 50
#define GENES_COUNT 32
#define POPULATION_COUNT 1024
#define WINNERS_RATIO 10
// Percentage of children rebuilt by macro-mutation (fresh seed + conjugate) in SelCrossover.
// The only mutation operator - single-gene random mutation was removed.
#define MUTATION_RATIO 10
// Target (as a percentage of the population) for the number of DISTINCT seed sequences the host
// builds. Init pre-seeds that many specimens (specimenID < numSeeds) with a cubie-undoing sequence;
// the rest get fully random genes. The actual numSeeds is capped by how many distinct decompositions
// the active cubie has - often far fewer than the target (e.g. <= 3 for N=3, where the collateral
// axis is forced), so most of the population starts random and the real seeding pressure comes from
// macro-mutation, not Init.
#define SEED_RATIO 100

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
#define STALL_LIMIT 8

// Max number of scene lights the render fragment shader can consume (sizes the Lights UBO array).
// The host uploads only the enabled lights (up to this cap) and their actual count in the header.
#define MAX_LIGHTS 8
