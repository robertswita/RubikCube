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

// A/B switch for the host seed generator (read by BuildSeedMoves, not the shader): 1 = mixed-Givens modes
// (draw a random mode per seed -> macro-moves on deep cubies), 0 = baseline (mode 0 only -> standard minimal
// decompositions). Flip to 0 to measure whether the macro-move seeds actually speed up solving.
#define SEED_MODE 1

// A/B switch for the host seed generator (read by BuildSeedMoves, not the shader): 1 = FAST path (single random
// descent per draw, NO backtracking, NO dedup - "draw until valid, throw in as they come"; O(rc) per attempt so
// cheap even when a narrow mode dead-ends and is redrawn), 0 = the reliable backtracking-DFS + dedup + saturation
// model. Fast trades macro-move seeds (narrow modes rarely survive a single descent) for CPU speed on deep cubies.
#define SEED_FAST 0

// A/B switch for the evaluator: 1 = COHERENCE, 0 = baseline per-cubie sum. Baseline fA is essentially a
// COUNT of scrambled cubies, which is deceptive - a COHERENT residual (cubies a single turn advances
// together: shared orientation AND a shared layer, e.g. one turned hyper-layer) is a few moves no matter
// how many cubies it spans, so fewer-but-scattered wrongly beats more-but-coherent and the N>=5 endgame
// stalls (2^5 oscillates at 5-8 unsolved, unable to climb to the easy coherent-8). COHERENCE refunds the
// largest such one-turn-collapsible group (maxHist) so 5-scattered -> 8-coherent -> solved descends
// monotonically. NOTE: prototyped ONLY in EvaluateMicro (cubes <= 64 cubies: 2^4, 2^5); EvaluateMacro
// (> 64, e.g. 3^4) stays baseline until Micro validates, so a big cube with COHERENCE=1 runs known
// baseline, not a half-built metric. (An earlier orientation-ONLY version was gameable - the GA aligned
// scattered orientations in place, 3/32; the shared-LAYER requirement is what fixes that.)
#define COHERENCE 1

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

// Slot width (in genes) reserved for one Init seed sequence. Mixed-Givens seed modes trace macro-move
// paths longer than the N-1 minimal solve; the greedy path length is bounded by the plane count
// P = N*(N-1)/2 (mode moves-to-solve can't exceed the number of planes), so reserve that many genes.
#define SEED_STRIDE (uint(N) * (uint(N) - 1u) / 2u)
#define STALL_LIMIT 20

// Max number of scene lights the render fragment shader can consume (sizes the Lights UBO array).
// The host uploads only the enabled lights (up to this cap) and their actual count in the header.
#define MAX_LIGHTS 8
