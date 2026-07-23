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
    uint specimen_best_structure = 0u;                   // DIAGNOSTIC: piece histogram at the best step (see Setup)

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
        uint structure = 0u;                              // piece histogram for THIS step (0 = not decomposed)
        for (uint i = 0; i < countActive; i++) {
            uint cur = local_cubies[ActiveCubies[i]];
            if (cur != Cubies[ActiveCubies[i]]) changed = true;   // did this prefix move the active cluster?
            float e = GetActiveCubieError(cur, maxClusterState, max_fA);
            local_fA_sum += e;
            if (e != 0.0) scrambled++;
        }
#if COHERENCE
        // Coherence metric — WEAKEST-BLOCK measure ("D3FragSymmAll"), applied as a DISCOUNT for the ENDGAME
        // (0 < scrambled <= gateway 2^(N-1)). Decompose the scrambled cubies into maximal complete coherent blocks
        // (a stack scan per state-group), then price the LEVEL OF THE WEAKEST one: a config is only as good as its
        // smallest piece, because a big piece among smaller ones is a PREMATURE COMMITMENT (solving a single cubie
        // BREAKS the pair), so the cost is computed as if EVERY cubie sat in a piece of the weakest size -- 4+2+2
        // costs like 2+2+2+2, never less. SYMMETRY = the number of DISTINCT piece sizes, scaling the penalty by
        // ITSELF, which pushes a mixed tree below the uniform config at its own weakest level: 2+1+1 worse than
        // 1+1+1+1, 4+2+2 worse than 2+2+2+2, while 4+2+2 still beats eight singletons. NOT capped at 1 -- a heavily
        // mixed tree may exceed the base, a real surcharge, deliberately, since those are liabilities the search
        // SHOULD avoid; scattered configs stay a discount so they remain usable stepping stones.
        // COLLATERAL is the counterweight, and is easy to mistake for decoration: the solved cubies completing the
        // gateway slice, i.e. the interval [scrambled, G) walked as maximal aligned blocks -- one block per TREE
        // LEVEL, the nested right-siblings, depending on scrambled ALONE (a lowest-set-bit walk, without touching a
        // single solved cubie). It DECREASES as scrambled grows, exactly opposing cost -- and the opposition is
        // EXACT, not approximate: adding a symmetric partner doubles cost, i.e. adds aMax, while the collateral
        // block it swallows, [d, 2d), has agreeAxes = N - log2(d) = aMax. The SAME number. At weight 1 the doubling
        // rungs therefore come out perfectly TIED. Remove the collateral instead and the metric runs away: cost
        // alone prefers ever-SMALLER blocks, cost plus any sum-over-pieces term prefers ever-BIGGER ones.
        // The factor is (cost + 2*collat) / (N * scrambled) -- two deliberate choices:
        //   * WEIGHT 2 on the collateral tips those exact ties into strict DECREASES, so every rung of the ladder
        //     falls and none has to be crossed sideways (2^5, fitness in units of 1/(countActive+1)):
        //         1+1 5.6 > 2+0 4.4 > 2+2 3.6 > 4+0 2.6 > 4+4 2.0 > 8+0 1.2 > 8+8 0.8 > 16+0 0.2
        //   * dividing by SCRAMBLED (not by G) keeps base ~ s from cancelling, so a scattered residual keeps
        //     fitness ~ s and PEELING ALWAYS PAYS. Under /(G*N) the ladder is strict as well, but re-scattering the
        //     residual back to a full gateway becomes an improvement -- a drift that undoes the peel. Drift is a
        //     property of the DENOMINATOR, ladder strictness a property of the collateral's WEIGHT: independent.
        // Still exactly 1 for a fully SCATTERED gateway (collat = 0 there, so cost = N*s cancels), hence continuous
        // with the bare-count peel above the gateway.
        // KNOWN ROUGHNESS: an ODD residual is a local maximum, because its first collateral block is a singleton
        // worth the full N -- so removing ONE cubie from an even residual scores WORSE and cubies come off in
        // pairs. Both subsequences are monotone: even 16.0, 8.8, 8.4, 6.0, 5.6 / odd 9.8, 9.4, 7.0.
        // Because it reads only the WEAKEST piece it is blind to how MUCH structure there is, so this endgame
        // closes by SHRINKING the residual -- the opposite of the ladder alternative below, which grows structure
        // toward the gateway and finishes in one turn. Two endgame strategies, not two versions of one.
        // Baseline for the older (cost+collat)/(G*N) form, 2^4, 10 runs, no hangs: 2840, 4928, 1625, 632, 827,
        // 3591, 1142, 288, 686, 751 -- the 2*collat and /scrambled changes are UNMEASURED against it. Micro only.
        uint G = 1u << (uint(N) - 1u);                        // gateway 2^(N-1): the endgame bound, and a perf gate
        if (scrambled > 0u && scrambled <= G) {                // (above it the residual is scattered -> f ~ 1 anyway)
            uint scr[CUBIES_COUNT];                           // scrambled active cubies (the endgame residual, <= G)
            uint ns = 0u;
            for (uint i = 0u; i < countActive; i++) {
                uint id = ActiveCubies[i];
                if (cubieL1(local_cubies[id]) != 0u) scr[ns++] = id;
            }
            uint aMax = 0u;                                   // WEAKEST-PIECE rule: the deepest (smallest) piece dictates
            uint seenAgree = 0u;                              // bitmask of occurring agreeAxes -> #distinct = SYMMETRY
            uint pieces = 0u;                                 // #complete blocks (for the PAIR experiment gate)
            for (uint gi = 0u; gi < ns; gi++) {
                uint sa = local_cubies[scr[gi]];              // handle each STATE once, at its first cubie
                bool firstState = true;
                for (uint gj = 0u; gj < gi; gj++)
                    if (local_cubies[scr[gj]] == sa) { firstState = false; break; }
                if (!firstState) continue;
                // --- exact decomposition of { cubies with state == sa } into complete sub-cubes (stack of boxes) ---
                uint stMask[2 * N];                           // box = (cMask, cVal): cMask axes are fixed to cVal bits
                uint stVal[2 * N];                            // stack depth <= N+1 (one constraint added per split)
                int sp = 0;
                stMask[0] = 0u; stVal[0] = 0u; sp = 1;        // start: no axis fixed (the whole space)
                for (int guard = 0; guard < 4 * CUBIES_COUNT && sp > 0; guard++) {
                    sp--;
                    uint bMask = stMask[sp];
                    uint bVal = stVal[sp];
                    uint cnt = 0u;                            // scan the group's cubies that fall inside this box
                    bool agree[N];
                    for (uint j = 0u; j < uint(N); j++) agree[j] = true;
                    uint fa[N];
                    bool haveFa = false;
                    for (uint k = 0u; k < ns; k++) {
                        uint id = scr[k];
                        if (local_cubies[id] != sa) continue;
                        bool inBox = true;
                        for (uint j = 0u; j < uint(N); j++)
                            if ((bMask & (1u << j)) != 0u && curCoord(sa, id, j) != ((bVal >> j) & 1u)) { inBox = false; break; }
                        if (!inBox) continue;
                        cnt++;
                        if (!haveFa) { for (uint j = 0u; j < uint(N); j++) fa[j] = curCoord(sa, id, j); haveFa = true; }
                        else for (uint j = 0u; j < uint(N); j++) if (curCoord(sa, id, j) != fa[j]) agree[j] = false;
                    }
                    if (cnt == 0u) continue;                  // empty box -- no piece here
                    uint agreeAxes = 0u;
                    for (uint j = 0u; j < uint(N); j++) if (agree[j]) agreeAxes++;
                    if (cnt == (1u << (uint(N) - agreeAxes))) {
                        aMax = max(aMax, agreeAxes);          // FULLY-filled box -> one complete piece. Track the
                        seenAgree |= 1u << agreeAxes;         // WEAKEST (deepest) piece + which distinct sizes occur
                        pieces++;                             // and count the pieces (PAIR gate)
                        uint sh = 5u * (agreeAxes - 1u);      // DIAGNOSTIC histogram (UI only): bucket agreeAxes-1,
                        if (((structure >> sh) & 31u) < 31u)  // 5 bits, clamped at 31 so a bucket can never carry
                            structure += 1u << sh;            // into the next
                    } else {                                  // partial -> split the first still-varying axis, recurse both halves
                        uint j = 0u;
                        for (; j < uint(N); j++) if (!agree[j]) break;
                        if (sp + 2 <= 2 * N) {
                            stMask[sp] = bMask | (1u << j); stVal[sp] = bVal & ~(1u << j); sp++;
                            stMask[sp] = bMask | (1u << j); stVal[sp] = bVal | (1u << j); sp++;
                        }
                    }
                }
            }
            uint smallest = 1u << (uint(N) - aMax);           // size of the weakest piece
            float S = float(bitCount(seenAgree));             // self-scaling SYMMETRY = # of distinct piece sizes
            float cost = float(scrambled / smallest) * float(aMax) * S;   // as-if ALL cubies sat at the weakest level
            float collat = 0.0;                               // COLLATERAL: the interval [scrambled, G) as maximal
            uint s = scrambled;                               // aligned blocks -- depends ONLY on scrambled
            for (int g = 0; g < int(N) && s < G; g++) {
                uint blk = s & (~s + 1u);                     // lowest set bit = the maximal aligned block at s
                uint aa = uint(N) - uint(findMSB(blk));       // agreeAxes of this collateral block = N - log2(blk)
                collat += float(aa);
                s += blk;
            }
            local_fA_sum *= (cost + collat) / (float(N) * sqrt(float(scrambled) * float(G)));
            // EXPERIMENT (PAIR): tip the k+0 vs k+k rung. Fires only on exactly two equal blocks of size >= 2
            // (2+2, 4+4, 8+8) -- nothing else. Set PAIR=1.0 in Variables to disable. See the PAIR note there.
            if (pieces == 2u && bitCount(seenAgree) == 1u && aMax < uint(N))
                local_fA_sum *= float(PAIR);

            // ---------------------------------------------------------------------------------------------------
            // ALTERNATIVE, kept for A/B -- the LADDER metric. To switch: comment out the block above, uncomment
            // this one, replace the `aMax`/`seenAgree` declarations with `float invSum = 0.0;` and, in the
            // FULLY-filled branch of the loop, replace their two updates with   invSum += 1.0 / float(cnt);
            //
            // It grows structure toward the gateway instead of shrinking the residual. With d_i the piece sizes,
            // m their count and s = scrambled, the discount is 4*SUM(1/d_i)/s^2, and since base ~ s/(countActive+1)
            // the FITNESS comes out ~ 4/(m*d^2) for m equal blocks of size d. The whole ladder is then ONE
            // invariant: every rung DOUBLES m*d^2 -- adding a symmetric partner doubles m, merging a pair halves m
            // and doubles d (1/2 * 4 = 2) -- so the fitness halves at every rung, with no symmetry factor and no
            // special cases. On 2^5, in units of 1/(countActive+1):
            //     1+1 2 > 2+0 1 > 2+2 .5 > 4+0 .25 > 4+4 .125 > 8+0 .0625 > 8+8 .031 > 16+0 .016
            // The s^2 (not s) matters: base is ~s, so a single factor of s only cancels the base and leaves the
            // doubling rungs TIED. Writing the discount as SUM(1/d)/s^c, a merge always scales the fitness by 1/2
            // whatever c is, a doubling by 2^(1-c) and a descent among SCATTERED cubies by 2^(c-1) -- the last two
            // are structurally the SAME event (m doubles at fixed d) and demand OPPOSITE sides of c=1, so no
            // exponent gives both. c=2 picks the ladder, at the price that inside the gateway the search stops
            // wanting to peel at all. UNMEASURED against the baseline above.
            //
            // float s = float(scrambled);
            // local_fA_sum *= 4.0 * invSum / (s * s);
            // ---------------------------------------------------------------------------------------------------
        }
#endif
        // FLOOR flattens the COUNT (never the structure) over 0 < scrambled <= FLOOR, by cancelling the base's
        // ~scrambled: fitness there becomes FLOOR * factor, so only coherence and the magnitude sub-gradient
        // decide. Above it the bare count drives the peel. It is a UNIFORM (Setup, location 7), not a #define,
        // so the host can move it WITHOUT recompiling the evaluator -- the intent is a WALKING floor,
        // min(2*d_max, G) taken from the CURRENT cube: while the residual is scattered d_max = 1, so FLOOR = 2
        // and the peel keeps its full gradient; once a coherent block exists the flat range widens by exactly
        // ONE rung, making k+0 -> k+k locally downhill. It must follow the CUBE, not the candidate -- keyed to
        // the candidate's own d_max it would raise the floor on every merge and push the merged state back up.
        // Measured on 2^4: FLOOR=2 median 1705, FLOOR=G median 3292. A fixed G cancels the count across the
        // WHOLE endgame (fitness = G * factor exactly) and hands it to a measure of structure, which has no way
        // to peel single cubies -- that is why it is slower, not faster.
        if (scrambled > 0u && scrambled <= FLOOR)
            local_fA_sum *= float(FLOOR) / float(scrambled);
        for (uint i = 0; i < countSolved; i++)
            if (cubieL1(local_cubies[SolvedCubies[i]]) != 0u)
                local_solved_errors += 1;
        float current_step_fitness = float(local_solved_errors) + local_fA_sum;
        if (!changed) current_step_fitness += float(2 * CUBIES_COUNT);   // no-op on the active cluster: rank below every real move (max real fitness <= CUBIES_COUNT, since solved+active = CUBIES_COUNT and the active term <= 1)
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
            specimen_best_structure = structure;          // keep the histogram of the step we actually record
        }
    }

    // 3: Write the record back to the population
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
    Population[specimenID].Structure = specimen_best_structure;
}
