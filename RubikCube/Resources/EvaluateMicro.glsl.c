#version 430
#include "Setup.glsl.c"

// 1 thread = 1 specimen. A block of 64 threads processes 64 specimens at once.
layout(local_size_x = 64) in;

void main()
{
    uint specimenID = gl_GlobalInvocationID.x;
    if (specimenID >= Population.length()) return;

    float maxClusterState = float(MAX_CUBIE_STATE) * float(countActive);
    float max_fA = maxClusterState *(float(countActive) + 1.0);

    // Per-thread (per-specimen) records initialized to the maximum float value
    float specimen_best_fitness = 1e38;
    uint specimen_best_moves_count = 0;

    // CUBE IN REGISTERS: thread-private array mapped to ultra-fast ALU registers.
    // Cubies holds the shared read-only starting state; each thread copies it into its private
    // working array and mutates only that copy, so the single shared buffer is race-free.
    uint local_cubies[CUBIES_COUNT];
    for (uint i = 0; i < CUBIES_COUNT; i++)
        local_cubies[i] = Cubies[i];

    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. CUBE ROTATION: the thread modifies only its own private registers
        for (uint cubieID = 0; cubieID < CUBIES_COUNT; cubieID++)
            local_cubies[cubieID] = TurnSingleCubie(local_cubies[cubieID], cubieID, move);

        // 2b. SEQUENTIAL EVALUATION: the thread sums its own errors for the current step 'm'
        float local_fA_sum = 0.0;
        uint scrambled = 0u;
        uint local_solved_errors = 0;
        bool changed = false;
        for (uint i = 0; i < countActive; i++) {
            uint cur = local_cubies[ActiveCubies[i]];
            if (cur != Cubies[ActiveCubies[i]]) changed = true;   // did this prefix move the active cluster?
            float e = GetActiveCubieError(cur, maxClusterState, max_fA);
            local_fA_sum += e;
            if (e != 0.0) scrambled++;
        }
#if COHERENCE
        // Coherence metric (SYMMETRIC), N-anchored. A residual splits (by STATE) into g EQUAL groups, each a
        // COMPLETE shared-LAYER intersection of size firstSize = scrambled/g (a power of 2). Such a structure is
        // charged cost = N - log2(scrambled) + 0.5*log2(g), g = #equal groups (fA *= cost/scrambled, fA ~ count).
        // The 0.5*log2(g) makes SUBCOHERENCE a HALF rung between two coherent levels. Ladder on 2^5:
        //     coherent-16 (gateway, g=1) 1 < subcoherent-16 (two coh-8) 1.5 < coherent-8 2 < subcoherent-8 2.5 <
        //     coherent-4 3 < subcoherent-4 (two coh-2) 3.5 < coherent-2 4. A subcoherent-2^k is thus BETTER than
        //     the lone coherent of its HALF size (one merge from a full coherent-2^k), not equal; and subcoherent-4
        //     = 3.5 dips BELOW the floor (4), so a gathered 2+2 is rewarded with NO positional pass. gateway = 1,
        //     cost >= 1 (never 0). Uses only scrambled + firstSize.
        //   The anchor is N (NOT a constant): only that keeps the gateway = 1 for EVERY N -- a constant anchor
        //   gave the 2^5 gateway (coherent-16) cost 0 = a false "solved". firstSize <= G = 2^(N-1) so cost >= 1
        //   and the coherent-2^N degeneracy (all cubies one state, agreeAxes 0) is out of range: the whole-cube
        //   "solved up to a global frame rotation" state can't masquerade as solved in our slice-move currency
        //   (that rotation costs ~SIZE moves per plane, not 0).
        //   NON-coherent residual -> DEFAULT cost: FLOOR = T (=4) for scrambled <= T (blocks the mono-twist
        //   "fewer is better" deception on small residuals), else the BARE COUNT. ASYMMETRIC splits (unequal
        //   groups, or a group that is not an intersection) stay non-coherent: they are TRAPS ("one half solved,
        //   one half not"), and rewarding partial g via log2(g) is what hung variant E.
        //   Costs are INTEGER (N - findMSB, no float log2) so equal-cost TIES abound -> the sideways move has
        //   somewhere to go. Non-gameable: coherence needs a real shared LAYER (agreeAxes), not just a shared
        //   orientation (the 3/32 cheat raises no agreeAxes). Fires for scrambled <= G, power-of-2 only.
        //   O(k^2 * N), Micro only (k <= 82).
        uint T = N;                                          // floor size for SMALL residuals: 4 = the four quarter-turn
                                                              // orientations (also 2^(N-2) on 2^4, 2^(N-3) on 2^5)
        uint G = 1u << (uint(N) - 1u);                        // coherence ceiling = gateway 2^(N-1) (one slice = the 1-move layer)
        if (scrambled > 0u && scrambled <= G) {
            // Only a POWER-OF-2 residual can be a symmetric coherent structure: g equal groups of 2^k give
            // scrambled = g*2^k, and true (2-based) symmetry needs g itself a power of 2 too -> scrambled a
            // power of 2. So 5/6/7... cannot be coherent (an uneven/odd split like "3 pairs in a 6" is a trap,
            // not coherence) -> default cost, and we skip the O(k^2) decomposition for them.
            // Default cost for a NON-coherent residual:
            //   scrambled <= T : FLOOR = T. Flooring the small residuals blocks the mono-twist "fewer is better"
            //                    deception (dropping the bare COUNT below the reachable target earns nothing).
            //   scrambled  > T : BARE COUNT (cost = scrambled -> *= 1, unchanged) -- above the floor the plain
            //                    count still drives the greedy peel; only real coherence (below) earns a discount.
            float cost = (scrambled <= T) ? float(T) : float(scrambled);
            if ((scrambled & (scrambled - 1u)) == 0u) {
                // Collect the scrambled cubie ids ONCE, then decompose only over them (cluster is mostly solved).
                uint scr[1 << (N - 1)];   // room for up to G = 2^(N-1) scrambled cubies
                uint ns = 0u;
                for (uint i = 0u; i < countActive; i++) {
                    uint id = ActiveCubies[i];
                    if (cubieL1(local_cubies[id]) != 0u) scr[ns++] = id;
                }
                uint firstSize = 0u;
                bool symmetric = true;                        // all state-groups EQUAL size AND each COMPLETE
                for (uint a = 0u; a < ns; a++) {
                    uint ida = scr[a];
                    uint sa = local_cubies[ida];
                    bool first = true;                        // handle each STATE once, at its first cubie
                    for (uint b = 0u; b < a; b++)
                        if (local_cubies[scr[b]] == sa) { first = false; break; }
                    if (!first) continue;
                    uint ca[N];
                    for (uint j = 0u; j < uint(N); j++) ca[j] = curCoord(sa, ida, j);
                    bool agree[N];
                    for (uint j = 0u; j < uint(N); j++) agree[j] = true;
                    uint gsize = 1u;
                    for (uint b = a + 1u; b < ns; b++) {
                        uint idb = scr[b];
                        uint sb = local_cubies[idb];
                        if (sb != sa) continue;
                        gsize++;
                        for (uint j = 0u; j < uint(N); j++)
                            if (curCoord(sb, idb, j) != ca[j]) agree[j] = false;
                    }
                    uint agreeAxes = 0u;
                    for (uint j = 0u; j < uint(N); j++) if (agree[j]) agreeAxes++;
                    bool complete = (gsize & (gsize - 1u)) == 0u && int(agreeAxes) == int(N) - findMSB(gsize);
                    if (firstSize == 0u) firstSize = gsize;   // first group sets the required size
                    else if (gsize != firstSize) symmetric = false;   // unequal groups -> asymmetric
                    if (!complete) symmetric = false;         // a non-intersection group -> incoherent
                }
                // Real coherent structure -> cost = N - log2(scrambled) + 0.5*log2(g), g = #equal groups =
                // scrambled/firstSize (equivalently N - 0.5*(log2 scrambled + log2 firstSize)). The 0.5*log2(g)
                // makes SUBCOHERENCE a HALF rung: a subcoherent-2^k (two coherent-2^(k-1) halves, ONE merge from
                // a full coherent-2^k) sits 0.5 ABOVE that coherent-2^k and 0.5 BELOW coherent-2^(k-1) -- so it is
                // BETTER than the lone coherent of its half size (subcoherent-8 = 2.5 < coherent-4 = 3), not equal,
                // and subcoherent-4 = 3.5 dips BELOW the floor (4) so a gathered 2+2 is rewarded with no positional
                // pass. Gateway (g=1, firstSize 2^(N-1)) -> 1; fully coherent (g=1) -> N - log2(firstSize) as
                // before; cost stays >= 1 (never 0). Singletons (firstSize == 1) are scattered -> default cost.
                if (symmetric && firstSize > 1u)
                {
                    int ls = findMSB(scrambled), lf = findMSB(firstSize);
                    cost = float(int(N) - ls) + float(ls - lf) / float(ls);
                }
            }
            local_fA_sum *= cost / float(scrambled);
        }
#endif
        // Endgame: once <= N active cubies remain unsolved, stop rewarding fewer of them (fewer is
        // not easier - a lone twisted cubie / mono-twist is very hard to escape). Amplify by
        // N/scrambled so the search then optimises the orientation magnitude, not the count.
        //if (scrambled > 0u && scrambled <= uint(N))
        //    local_fA_sum *= float(N) / float(scrambled);
        for (uint i = 0; i < countSolved; i++)
            if (cubieL1(local_cubies[SolvedCubies[i]]) != 0u)
                local_solved_errors += 1;
        float current_step_fitness = float(local_solved_errors) + local_fA_sum;
        if (!changed) current_step_fitness += float(2 * CUBIES_COUNT);   // no-op on the active cluster: rank below every real move (max real fitness <= CUBIES_COUNT, since solved+active = CUBIES_COUNT and the active term <= 1)
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
        }
    }

    // 3: Write the record back to the population
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
}
