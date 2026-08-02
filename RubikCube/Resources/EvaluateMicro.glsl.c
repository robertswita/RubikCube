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
    uint specimen_best_ladder = 0u;                      // ACCEPT metric: integer structure value at the best step (Setup: Ladder)

    // CUBE IN REGISTERS: thread-private array mapped to ultra-fast ALU registers.
    // Cubies holds the shared read-only starting state; each thread copies it into its private
    // working array and mutates only that copy, so the single shared buffer is race-free.
    uint local_active[MAX_CLUSTER];                  // ONLY the active cluster in registers (not the whole cube) -> cube-size-independent
    for (uint i = 0; i < countActive; i++)
        local_active[i] = Cubies[ActiveCubies[i]];

    for (int m = 0; m < GENES_COUNT; m++)
    {
        Move move = getMove(Population[specimenID].Moves[m]);

        // 2a. CUBE ROTATION: the thread modifies only its own private registers
        for (uint i = 0; i < countActive; i++)
            local_active[i] = TurnSingleCubie(local_active[i], ActiveCubies[i], move);

        // 2b. SEQUENTIAL EVALUATION: the thread sums its own errors for the current step 'm'
        float local_fA_sum = 0.0;
        uint scrambled = 0u;
        bool changed = false;
        uint structure = 0u;                              // piece histogram for THIS step (0 = not decomposed)
        uint ladder = 0u;                                 // integer structure value for THIS step (0 = solved/descent, the best possible)
        //if (COHERENT == 0u)
        //{
            for (uint i = 0; i < countActive; i++) {
                uint cur = local_active[i];
                if (cur != Cubies[ActiveCubies[i]]) changed = true;   // did this prefix move the active cluster?
                float e = GetActiveCubieError(cur, maxClusterState, max_fA);
                if (e != 0.0) scrambled++;
                // ENDGAME (COHERENT): the base is BINARY -- solved (0) or not (1). Structurally-equal configs then
                // tie EXACTLY, and the count cancels the factor's /scrambled, so fitness = the PURE coherence factor.
                // That lets the accept rule step SIDEWAYS between equally-valued structures (the ties finally fire).
                // DESCENT (COHERENT 0): keep the graded per-cubie error so the count-peel keeps its gradient.
                //local_fA_sum += (COHERENT != 0u) ? (e != 0.0 ? 1.0 / float(countActive + 1) : 0.0) : e;
                local_fA_sum += e;
            }
        //}
        // ACTIVE FORM: discount suma/(G*N) with suma = cost + collat (the long note below documents an older
        // (cost+2*collat)/(N*s) variant -- kept as history). COHERENT is a UNIFORM LATCH (Setup loc 8), NOT a
        // compile flag: the host keeps it 0 through the count-peel descent and latches it to 1 once the residual
        // reaches the floor, so coherence shapes ONLY the endgame. The count is cancelled INSIDE the factor
        // (the /scrambled), so once latched the fitness is the bare coherence factor across the whole endgame.
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
        //else
        //{
            uint G = 1u << (uint(N) - 1u);                        // gateway 2^(N-1); the endgame TARGET is G+G (scrambled = 2G)
            uint NORM = 2u * (FLOOR - 1u) + 4u * (uint(N) - uint(findMSB(FLOOR)));   // floor value of 2*(P+E) at all-singletons FLOOR
        //if (COHERENT != 0u && scrambled > 0u && (scrambled & (scrambled - 1u)) == 0u && scrambled <= 2 * G) {   // POWER-OF-2 FILTER: only a 2^k count can be ONE complete coherent block, so pay the decomposition ONLY there (non-powers scored as scattered in the else below)
        //if (COHERENT != 0u && scrambled > 0u && (scrambled & (scrambled - 1u)) == 0u) {   // POW2 pre-guard: score only power-of-2 counts (G+G ladder rungs 1,2,4,...,G,2G); non-powers -> scattered else; the 2G full-block trap is walled by the aMax check below
//            if (COHERENT != 0u && scrambled > 0u)
            //if (scrambled > 0 && scrambled <= FLOOR)
            //    local_fA_sum *= float(FLOOR) / float(scrambled);
            if (COHERENT != 0u && scrambled > 0u) {   // UNIFIED: decompose EVERY residual for its REAL structure -- an isolated singleton (mono-twist) gets its own EXPENSIVE ladder (deepest structure), so the structure-dominant additive below deters isolating the last cubie. No lie, no even/odd split. Full-cluster aMax==0 trap walled below.
            //local_fA_sum *= float(FLOOR) / float(scrambled);
            uint scr[MAX_CLUSTER];                            // scrambled active cubies (the endgame residual) -- sized by the largest cluster, not the whole cube
            uint ns = 0u;
            for (uint i = 0u; i < countActive; i++)
                if (cubieL1(local_active[i]) != 0u) scr[ns++] = i;   // scr holds the ACTIVE-INDEX (not the cubie id)
            uint aMax = 0u;                                   // WEAKEST-PIECE rule: max agreeAxes = the smallest (weakest) piece
            uint aSum = 0u;
            uint seenAgree = 0u;                              // bitmask of occurring agreeAxes -> #distinct = SYMMETRY
            uint pieces = 0u;                                 // #complete blocks (for the PAIR experiment gate)
            uint dHist[N + 1];                                // block count by depth (agreeAxes) -- for the m measure
            for (uint di = 0u; di <= uint(N); di++) dHist[di] = 0u;
            // STATE-SEEN bitmask: replaces the old O(ns^2) backward "firstState" scan. The per-state grp[] collection
            // below marks every cubie of the state as it gathers them, so the outer loop skips duplicates in O(1).
            // Same states, same order -> identical result.
            uint stateDone[(MAX_CLUSTER + 31) / 32] = uint[](0);
            //for (uint di = 0u; di < uint((MAX_CLUSTER + 31) / 32); di++) stateDone[di] = 0u;
            for (uint gi = 0u; gi < ns; gi++) {
                if ((stateDone[gi >> 5u] & (1u << (gi & 31u))) != 0u) continue;   // state already decomposed at an earlier cubie
                uint sa = local_active[scr[gi]];              // state via active-index (handle each STATE once, at its first cubie)
                uint grp[MAX_CLUSTER]; uint gn = 0u;          // members of THIS state (ACTIVE-INDICES), collected ONCE -- DFS scans grp (O(g)/pop)
                for (uint k = 0u; k < ns; k++)
                    if (local_active[scr[k]] == sa) { grp[gn++] = scr[k]; stateDone[k >> 5u] |= (1u << (k & 31u)); }
                // GENERALISED decomposition (ANY SIZE): split { cubies with state sa } into maximal COMPLETE
                // combinatorial boxes. A box fixes some axes to a full coord VALUE (0..SIZE-1), NOT a bit; branching
                // per axis = the DISTINCT values present (up to 2N), not 2 -- a 2N-ary tree, not binary. Box =
                // (fixedMask, refCubie): refCubie's coords give the fixed values (avoids packing arbitrary SIZE).
                // "Complete" = cnt == product of per-axis distinct-value counts. (SIZE <= 32 for the value bitmask.)
                uint stMask[2 * N * N];                        // fixed-axes mask per stacked box (2N-ary DFS)
                uint stRef[2 * N * N];                         // a representative in-box cubie per box (defines its fixed values)
                int sp = 0;
                stMask[0] = 0u; stRef[0] = scr[gi]; sp = 1;    // whole box: no axis fixed
                for (int guard = 0; guard < 4 * MAX_CLUSTER && sp > 0; guard++) {
                    sp--;
                    uint bMask = stMask[sp];
                    uint refId = ActiveCubies[stRef[sp]];         // stRef holds active-index; resolve to cubie id for curCoord (home position)
                    uint cnt = 0u;
                    uint valMask[N];                          // per-axis set of coord VALUES present in this box
                    for (uint j = 0u; j < uint(N); j++) valMask[j] = 0u;
                    for (uint k = 0u; k < gn; k++) {
                        uint id = ActiveCubies[grp[k]];           // grp holds active-index; resolve to cubie id for curCoord
                        bool inBox = true;
                        for (uint j = 0u; j < uint(N); j++)
                            if ((bMask & (1u << j)) != 0u && curCoord(sa, id, j) != curCoord(sa, refId, j)) { inBox = false; break; }
                        if (!inBox) continue;
                        cnt++;
                        for (uint j = 0u; j < uint(N); j++) valMask[j] |= (1u << curCoord(sa, id, j));
                    }
                    if (cnt == 0u) continue;
                    // COMPLETENESS = cnt == size of the SUB-ORBIT spanned by the VARYING axes (distinctCount > 1):
                    // size = perms * 2^nz, where perms = (#varying)! / prod(magnitude-multiplicity!) counts the multiset
                    // arrangements ({a,a,a,a} -> 1 -> 16, {a,a,b,b} -> 6 -> 96), and nz = #varying axes with nonzero
                    // magnitude. NOT a grid. (Parity /2 for all-distinct-nonzero clusters deferred -> those still
                    // over-split into singletons for now; oddStab clusters -- repeats/zeros -- are correct.)
                    uint agreeAxes = 0u;
                    uint mags[N];                             // |coord - centre|*2 of each VARYING axis (from the ref cubie)
                    uint nf = 0u;
                    uint nz = 0u;
                    for (uint j = 0u; j < uint(N); j++) {
                        if (bitCount(valMask[j]) == 1u) { agreeAxes++; continue; }
                        uint mg = uint(abs(int(2u * curCoord(sa, refId, j)) - int(SIZE) + 1));
                        mags[nf] = mg; nf++;
                        if (mg != 0u) nz++;
                    }
                    uint permsN = 1u;
                    for (uint i = 2u; i <= nf; i++) permsN *= i;                       // (#varying)!
                    for (uint i = 0u; i < nf; i++) {                                   // divide by each distinct magnitude's (multiplicity)!
                        bool firstOcc = true;
                        for (uint k = 0u; k < i; k++) if (mags[k] == mags[i]) { firstOcc = false; break; }
                        if (firstOcc) {
                            uint c = 0u;
                            for (uint k = i; k < nf; k++) if (mags[k] == mags[i]) c++;
                            uint f = 1u; for (uint m = 2u; m <= c; m++) f *= m;
                            permsN /= f;
                        }
                    }
                    if (cnt == permsN << nz) {                 // complete coherent block at depth agreeAxes
                        aMax = max(aMax, agreeAxes);
                        dHist[agreeAxes] += 1u;
                        seenAgree |= 1u << agreeAxes;
                        if (agreeAxes >= 1u) {                // agreeAxes==0 = whole-cluster block (the global-rotation trap): no histogram bucket
                            uint sh = 5u * (agreeAxes - 1u);
                            if (((structure >> sh) & 31u) < 31u) structure += 1u << sh;
                        }
                    } else {                                  // partial -> split the first VARYING axis by its ACTUAL values
                        uint a = 0u;
                        for (; a < uint(N); a++) if (bitCount(valMask[a]) > 1u) break;
                        uint seenVals = 0u;                   // one sub-box per distinct value on axis a (ref = a cubie carrying it)
                        for (uint k = 0u; k < gn; k++) {
                            uint ai = grp[k];                 // active-index
                            uint id = ActiveCubies[ai];       // cubie id for curCoord
                            bool inBox = true;
                            for (uint j = 0u; j < uint(N); j++)
                                if ((bMask & (1u << j)) != 0u && curCoord(sa, id, j) != curCoord(sa, refId, j)) { inBox = false; break; }
                            if (!inBox) continue;
                            uint v = curCoord(sa, id, a);
                            if ((seenVals & (1u << v)) == 0u && sp < 2 * N * N) {
                                seenVals |= (1u << v);
                                stMask[sp] = bMask | (1u << a);
                                stRef[sp] = ai;               // push the ACTIVE-INDEX
                                sp++;
                            }
                        }
                    }
                }
            }
            //uint smallest = 1u << (uint(N) - aMax);           // size of the weakest piece
            uint S = bitCount(seenAgree);             // self-scaling SYMMETRY = # of distinct piece sizes
            //float cost = float(scrambled / smallest) * float(aMax) * S;   // as-if ALL cubies sat at the weakest level
            //float collat = 0.0;                               // COLLATERAL: the interval [scrambled, G) as maximal
            //uint s = scrambled;                               // aligned blocks -- depends ONLY on scrambled
            //for (int g = 0; g < int(N) && s < G; g++) {
            //    uint blk = s & (~s + 1u);                     // lowest set bit = the maximal aligned block at s
            //    uint aa = uint(N) - uint(findMSB(blk));       // agreeAxes of this collateral block = N - log2(blk)
            //    collat += float(aa);
            //    s += blk;
            //}
            //local_fA_sum *= float(N * aMax + S - 1) / float(N * N);
            //local_fA_sum *= (float(aMax*aMax) + float(SSIGN) * S) / float((N*N) + (SSIGN));
            //local_fA_sum *= float(N * aMax + SSIGN * (S - 1)) / float(N * N * scrambled);
            //local_fA_sum *= float(aSum + 2 * (S - 1u)) / float(G + 2 * (N - 1));
            // MEASURE P2 * S: P2 = depth-weighted sum over the PARENT nodes ONLY (empty partners excluded), each
            // weighted by depth+1 (root at depth 0 contributes 1, not 0 -- keeps 8+0 off fitness 0). Carry: at
            // depth d, par = ceil(occ/2) parents at depth d-1, weight (d-1)+1 = d. Times S = distinct block sizes.
            // Count-cancelled by /scrambled; scattered floor (P2 = N(N+3)/2, S = 1) pins to 1.0.
            uint occ = dHist[N];
            uint P = 0u;                                      // PLAIN parent count
            uint E = 0u;                                      // PLAIN empty-partner count = #unpaired blocks (breaks k+0=k+k toward k+k)
            uint P2 = 0u;                                     // (depth-weighted variants kept for A/B, now unused)
            uint E2 = 0u;
            for (int d = N; d >= 1; d--) {
                uint par = (occ + 1u) >> 1;                   // ceil(occ/2): parents at depth d-1
                P += par;
                E += (2u * par - occ);                        // +1 per depth with ODD occupancy = a block with no partner
                P2 += par * uint(d);
                E2 += (2u * par - occ) * uint(d);
                occ = dHist[d - 1] + par;                     // dHist[0] stays 0 (no whole-cube block in the residual)
            }
            // PAIR-ESCAPE: the floor's pairing-neighbour 2+1+1 (>=2 singletons AND a 2-block) is pulled BELOW the
            // floor so the endgame can GROW structure out of the floor, not only shrink singletons. The gate fires on
            // the whole {2+1+1, 2+2+1+1, 4+2+1+1} class, but only 2+1+1's base (P2*S=20) is low enough to cross under
            // the floor (14) after *0.65 -> ~0.93; 2+2+1+1 / 4+2+1+1 stay walls. dHist[N]=singletons, dHist[N-1]=2-blocks.
            //float pairEscape = (dHist[N] >= 2u && dHist[N - 1] >= 1u) ? 0.65 : 1.0;
            //float Beta = 0.35;
            //float Alpha = float(1) + Beta * float(S-1);
            //local_fA_sum *= float(P2) * Alpha / float((uint(N) * (uint(N) + 3u) / 2u) * scrambled);   // P3: P2*(1+Beta*(S-1)) -- measured WORSE than P2*S (fat tail)
            // MEASURE aMax * S: weakest-block depth x distinct sizes. Uniform-k configs (k+0 = k+k = k+k+k+k) tie at
            // aMax = N - log2(k) -- rungs are block SIZES, count-within-level irrelevant; jump k -> 2k is one rung.
            // Mixed (S >= 2) stay walls above the floor. Floor = all singletons -> aMax = N, S = 1 -> N; /(N*scrambled) pins to 1.0.
            //local_fA_sum *= float(aMax * S) / float(uint(N) * scrambled);   // aMax*S: ties k+0=k+k=k+k+k+k -> count FLAT within a size level -> backward drift (4 scattered == 8 scattered as a free sideways move). Rejected.
            //local_fA_sum *= float(P2 * S) / float((uint(N) * (uint(N) + 3u) / 2u) * scrambled);   // P2 * S -- baseline (block-coherence off)
            // FINE FITNESS, but NORMALISED to < 1 so local_solved_errors (+1 per broken Solved cubie) and the no-op
            // penalty always dominate. Structure (P2+N*S) is the big part; the graded base (already in local_fA_sum
            // from the loop, < 1) rides along as a sub-unit twist tie-break -> the GA sorts by STRUCTURE then twist,
            // never an exact tie, so selection works. The ACCEPT compares the STRUCTURE ladder value (not this fitness).
            //if (aMax > 0)
            ladder = 2 * (P2 + uint(N) * S);
            local_fA_sum = (float(ladder) + local_fA_sum) / float((10 * N - 11) * (countActive + 1));   // OLD: P2 + N*S (depth-weighted)
            // MEASURE P + N*S (P = PLAIN parent count). Universal floor normalizer, FLOOR a power of 2: floor value =
            // P_floor + N = (FLOOR-1) + (N - log2 FLOOR) + N = FLOOR + 2N - 1 - findMSB(FLOOR). Times (countActive+1)
            // so local_fA_sum stays < 1 (structure dominates the sub-unit graded twist already inside local_fA_sum).
            //local_fA_sum = (float(P2 + uint(N) * S) + local_fA_sum)
            //             / float((FLOOR + 2u * uint(N) - 1u - uint(findMSB(FLOOR))) * (countActive + 1u));
            // BLOCK-COHERENCE tie-break: b = 1 iff two SAME-SIZE blocks vary on DIFFERENT axes (crossed -> won't merge,
            // they interfere); aligned or no same-size pair -> b = 0. Sub-unit splitter: *2 gives exactly one bit of
            // headroom (min P2*S gap between levels is 1 -> *2 = 2 > b), so b never crosses a level. Floor 2*14+0 = 28.
            //uint b = 0u;   // BLOCK-COHERENCE (replaced by coupling penalty below; measured worse than P2*S)
            //for (uint bi = 0u; bi < pieces; bi++)
            //    for (uint bj = bi + 1u; bj < pieces; bj++)
            //        if (bitCount(blockU[bi]) == bitCount(blockU[bj]) && blockU[bi] != blockU[bj]) b = 1u;
            //local_fA_sum *= float(2u * P2 * S + b) / float((uint(N) * (uint(N) + 3u)) * scrambled);   // BC
            // COUPLING penalty: pieces sharing a FIXED coordinate lie in a common layer -> one move rotates them
            // together, can't separate or merge them in one move. Penalize sum over piece-pairs of SHARED FIXED
            // coords (axis fixed in BOTH + same value). Gives the SINGLETON regime (P2*S-flat) a gradient: spread
            // cheap, clustered dear. Normalized to [0,1) so it stays a sub-unit tie-break under *2 (K=2).
            
            // COUNTING identity for the pair loop: C = sum over (axis ax, value v) of choose(k,2), where k =
            // #blocks fixing axis ax to value v. A pair sharing t fixed (axis,value) coords is counted once per
            // shared coord in the bucket sum, so bucket-pairs == pair-shares -- EXACTLY the old triple loop's C,
            // but O(pieces*N) instead of O(pieces^2*N). (Old O(pieces^2*N) pair form kept in git history.)
            // COUPLING penalty Cnorm in [0,1): blocks sharing a FIXED layer-coord lie in a common layer (one move
            // rotates them together). Counting identity: C = sum over (axis,value) of choose(k,2), O(pieces*N). It is a
            // sub-unit tie-break in the FITNESS (GA sort) ONLY -- NOT the ladder, so the accept still ties -> sideways
            // fire. NOTE: Cnorm = 0 for a SINGLE-block config (pairs = 0), so it shapes only MULTI-block intermediates.
            //uint coupCnt[N * 4];                             // coupCnt[ax*4+v]: #blocks with axis ax FIXED to value v (SIZE <= 4)
            //for (uint bkt = 0u; bkt < uint(N) * 4u; bkt++) coupCnt[bkt] = 0u;
            //for (uint blk = 0u; blk < pieces; blk++)
            //    for (uint ax = 0u; ax < uint(N); ax++)
            //        if (((blockU[blk] >> ax) & 1u) == 0u)    // axis ax FIXED in this block
            //            coupCnt[ax * 4u + ((blockPos[blk] >> (2u * ax)) & 3u)] += 1u;
            //uint C = 0u;
            //for (uint bkt = 0u; bkt < uint(N) * 4u; bkt++) C += coupCnt[bkt] * (coupCnt[bkt] - 1u) / 2u;
            //uint pairs = pieces * (pieces - 1u) / 2u;
            //float Cnorm = float(C) / float(pairs * uint(N) + 1u);   // [0,1): shared-coord fraction, sub-unit under *2
            
            // Ladder = 2*P*S (P = plain parents, S = distinct block sizes): a STRUCTURE gradient for ALL residuals,
            // so the GA always has something to build with. (Was `scrambled <= G`, which walled EVERY larger config
            // into flat scatter -> the search starved above the base-2 gateway.) WALL ONLY the full-cluster trap:
            // aMax==0 means the whole active cluster sits in ONE orientation (P=0 -> ladder 0 = looks SOLVED), so
            // score it as all-singletons instead, so it reads as far-from-solved.
            // ADDITIVE, STRUCTURE-DOMINANT: fitness = (ladder + graded)/NORM, ladder = 2*P2*S. The integer ladder
            // DOMINATES the sub-unit graded, so STRUCTURE OVERRIDES COUNT -- a coherent config beats a lower-count
            // scattered one, and an isolated singleton (mono-twist, ladder = 2*P2*S = 20 for N=4) scores WORSE than a
            // 2-block (12), so the peel keeps a partner rather than isolating the last cubie. Multiplicative can only
            // modulate the base DOWN, never lift a structureless singleton above it -> it isolated the mono-twist;
            // additive doesn't. WALL the full-cluster aMax==0 trap (P2=0 -> false-solved) as all-singletons. No
            // average-depth on the graded (it over-amplified 2^4) -- the raw graded rides as a sub-unit twist tie-break.
            ////if (aMax > 0u) {
            ////    ladder = 2u * P2 * S;
            ////} else {                                          // full-cluster (agreeAxes=0) trap -> wall as all-singletons
            ////    uint sc = scrambled; uint Pw = 0u; uint Ew = 0u;
            ////    for (int d = N; d >= 1; d--) { uint par = (sc + 1u) >> 1; Pw += par; Ew += (2u * par - sc); sc = par; }
            ////    ladder = 2u * (Pw + Ew);
            ////    structure = min(scrambled, 31u) << (5u * (uint(N) - 1u));
            ////}
            ////local_fA_sum = (float(ladder) + local_fA_sum) / float(NORM * (countActive + 1u));

            //local_fA_sum *= (float(2u * P2 * S) + 1 - Cnorm) / float((uint(N) * (uint(N) + 1u)) * scrambled);   // 2*P2*S + coupling, K=2
            // MEASURE P2 * S + E2: adds depth-weighted empty partners. Breaks the k+0=k+k and 2+1+1=2+1+0 rung ties
            // (E2 penalizes incomplete pairings) -- A/B vs pure P2*S. Floor (4 singletons, N=4): P2*S=14, E2=3 -> 17.
            //local_fA_sum *= float(P2 * S + E2) / float((uint(N) * (uint(N) + 3u) / 2u + 3u) * scrambled);   // +3u = E2 at the 4-singleton floor (2^4)
            //local_fA_sum *= (cost + collat) / sqrt(float(81) * float(scrambled));   // suma/(G*N): no /s, no sqrt (baseline)
            // PAIR: tip the k+0 vs k+k rung. Fires only on exactly two equal blocks of size >= 2 (2+2, 4+4, 8+8) --
            // nothing else. PAIR=EQUAL (the default, = 1.0) is a no-op: k+0 and k+k already tie in suma. See Variables.
            //if (pieces == 2u && bitCount(seenAgree) == 1u && aMax < uint(N))
            //    local_fA_sum *= float(PAIR);

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

        // The factor is P2 * S -- P2 = depth-weighted sum over the PARENT nodes (empty partners excluded, root
        // weighted depth+1), S = number of distinct block sizes. Count-cancelled by /scrambled so the endgame
        // reads as the bare P2*S / (N(N+3)/2): scattered floor (P2 = N(N+3)/2, S = 1) is 1.0. FLOOR (uniform 7)
        // is now ONLY the host-side latch threshold -- the factor no longer reads it (dead-uniform cleanup
        // pending). On the descent (Coherent = 0) this block is skipped; the bare count peels.
        // (aMax/aSum/m2 still in history but unused -- earlier variants kept commented for A/B.)
        // UNIFIED: per-step fitness is ACTIVE-only (metric + no-op). The Solved break-check is DEFERRED to the best
        // step and done by replay after the loop -- so no whole-cube solved read here. (Best step is chosen without
        // the break: CONSERVATIVE. If it later breaks a Solved cubie, the replay penalty pushes fitness >= 1 -> reject.)
        float current_step_fitness = local_fA_sum;
        if (!changed) current_step_fitness += float(2 * CUBIES_COUNT);   // no-op on the active cluster: rank below every real move (max real fitness <= CUBIES_COUNT, since solved+active = CUBIES_COUNT and the active term <= 1)
        if (current_step_fitness < specimen_best_fitness) {
            specimen_best_fitness = current_step_fitness;
            specimen_best_moves_count = uint(m + 1);
            specimen_best_structure = structure;          // keep the histogram of the step we actually record
            specimen_best_ladder = ladder;                // and its integer ladder value -- the host accept reads this
        }
    }
    // SOLVED break-check at the BEST step, by per-cubie replay (O(1) storage each -- see solvedBrokenCount). A broken
    // Solved cubie adds 1, pushing fitness >= 1 so the host rejects the move. On single-cluster cubes (2^4) countSolved
    // = 0, so this is a no-op and results are IDENTICAL to the old per-step solved loop -> clean 2^4 regress-guard.
    specimen_best_fitness += float(solvedBrokenCount(specimenID, specimen_best_moves_count));

    // 3: Write the record back to the population
    Population[specimenID].Fitness = floatBitsToUint(specimen_best_fitness);
    Population[specimenID].MovesCount = specimen_best_moves_count;
    Population[specimenID].Structure = specimen_best_structure;
    Population[specimenID].Ladder = specimen_best_ladder;
}
