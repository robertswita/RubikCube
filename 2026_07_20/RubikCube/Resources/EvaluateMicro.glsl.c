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
        // Coherence metric — COLLATERAL-TREE cost, applied as a DISCOUNT for the ENDGAME (0 < scrambled <= gateway
        // 2^(N-1)). The residual is assessed against the GATEWAY slice (the first-turn target): decompose the
        // scrambled cubies into maximal complete coherent blocks (a stack scan per state-group; each complete
        // 2^(N-f) block = one leaf costing agreeAxes^2 = f^2), then ADD the COLLATERAL -- the solved cubies that
        // turning the gateway slice would disturb. By symmetry the unsolved blocks tile [0, scrambled) exactly, so
        // the collateral is always the interval [scrambled, gateway), whose maximal aligned blocks depend ONLY on
        // scrambled (a lowest-set-bit walk, no need to touch solved cubies). cost = SUM over ALL leaves (unsolved +
        // collateral) of agreeAxes^2. So a coherent-2 entangled with 2 solved cubies costs like (2+2), not a free 2
        // -- "nice" but entangled endgames stop being under-priced (the stuck coherent-6 was one). The factor
        // cost / (gateway * N^2) is a DISCOUNT in (0,1]: a fully-scattered FULL gateway = 1 (no discount ->
        // CONTINUOUS with the bare-count peel above the gateway, no boundary), any coherence < 1. It SUBSUMES the
        // old floor -- a mono-twist is the MOST expensive endgame (45 on 2^4), so the search never over-descends
        // into it. Validated: coherent-8=1, coherent-4=8, coherent-2=22, 4+2=22, 2+2+1=59, mono=45. Micro only.
        uint G = 1u << (uint(N) - 1u);                        // gateway 2^(N-1) = the first-turn target slice
        bool isPow2 = (scrambled & (scrambled - 1u)) == 0u;
        if (scrambled > 0u && scrambled <= G && isPow2) {
            uint scr[CUBIES_COUNT];                           // scrambled active cubies (the endgame residual, <= G)
            uint ns = 0u;
            for (uint i = 0u; i < countActive; i++) {
                uint id = ActiveCubies[i];
                if (cubieL1(local_cubies[id]) != 0u) scr[ns++] = id;
            }
            float cost = 0.0;                                 // SUM of complete-piece depths over ALL state-groups
            uint firstAgree = 0u; bool firstPiece = true;     // SYMMETRY tracking: symmetric = all pieces the SAME
            bool allSame = true;                              // size (agreeAxes), scrambled a power of 2, size >= 2
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
                        //cost += float(agreeAxes * agreeAxes); // FULLY-filled box -> one complete piece (agreeAxes^2)
                        cost += float(agreeAxes); // FULLY-filled box -> one complete piece (agreeAxes^2)
                        if (firstPiece) { firstAgree = agreeAxes; firstPiece = false; }
                        else if (agreeAxes != firstAgree) allSame = false;   // pieces differ in size -> ASYMMETRIC tree
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
            // SYMMETRY GATE: only assess a config whose binary tree is SYMMETRIC -- all pieces the SAME size (>=2)
            // and scrambled a power of 2 (equal blocks * a power-of-2 count fill a COMPLETE box that the collateral
            // completes cleanly). Asymmetric configs (2+1+1, 1+1+1+1, 4+2+2, 2+2+2+2+2) are LEFT AT BASE so the
            // search wanders through them freely: a premature lone pair (2+1+1) is a LIABILITY (solving a single
            // BREAKS the pair), NOT an asset -- it must NOT out-rank 1+1+1+1. Rewarding asymmetry inverts the
            // hierarchy and traps the search (that was the stall). Only clean symmetric structure earns the discount.
            if (isPow2 && allSame && firstAgree < uint(N)) {  // power-of-2 count, uniform size, size >= 2 (not singletons)
                float collat = 0.0;                           // COLLATERAL: solved cubies filling the gateway slice =
                uint s = scrambled;                           // the interval [scrambled, gateway) as maximal aligned
                for (int g = 0; g < int(N) && s < G; g++) {   // blocks (lowest-set-bit walk) -- depends ONLY on scrambled
                    uint blk = s & (~s + 1u);                 // lowest set bit = the maximal aligned block at s (always fits)
                    uint aa = uint(N) - uint(findMSB(blk));   // agreeAxes of this collateral block = N - log2(blk)
                    collat += float(aa);
                    s += blk;
                }
                local_fA_sum *= (cost + collat) / (float(G) * float(N));   // DISCOUNT in (0,1] for SYMMETRIC configs only
            }
        }
#endif
        // FLOOR (kept alongside the collateral): the collateral SUBSUMES the anti-over-descent (mono is priciest),
        // but base ~ scrambled makes the DEEP endgame nearly flat in fitness (mono 1*45 ~ coherent-2 2*22), so the
        // search wanders sideways to close -- SLOW. The floor cancels scrambled -> fitness ~ magnitude * cost, a
        // real CLOSING gradient (descend the twist; mono 45 clearly > coherent-2 22). Different axis from the
        // collateral (structure): the floor is magnitude/closing. Boundary stays smooth (*N/N = *1 at scrambled=N).
        uint FLOOR = 4;
        if (scrambled > 0u && scrambled <= uint(FLOOR))
            local_fA_sum *= float(FLOOR) / float(scrambled);
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
