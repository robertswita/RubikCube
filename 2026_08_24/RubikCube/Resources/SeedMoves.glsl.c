#version 430
#include "Setup.glsl.c"

// GPU SEED PRODUCER (descent, seating enumerator). One thread = one RANDOM seating descent that solves the
// ACTIVE cubie's orientation to IDENTITY, writing the move sequence into the SeedMoves SSBO (stride SEED_STRIDE,
// unused slots = 0xFFFFFFFF sentinel). No branching, no backtracking, no reject -> ideal for GPU. Diversity comes
// from the per-thread RNG (which hole to seat next, which collateral axis). Descent only for now; the coherence
// retarget (solve X = M.Dᵀ toward a block orientation D) is a later layer -- see memory coherence-seeding-retarget.
//
// Row code = (col<<1)|sign (getRowCode); a quarter-turn (RotateMatInt) swaps two rows + one sign flip; 180 negates both.
// SEATING sign rule (derived from RotateMatInt, axis1=home t, axis2=source p): angle = sign(row_p)==1 ? 90 : 270,
// which lands the seated axis with sign 0. Negated fixed points (col==row, sign==1) are paired by one 180 each.

layout(local_size_x = 64) in;
layout(location = 1) uniform uint activeIndex;   // cluster index of the cubie whose position drives the collateral slice
layout(location = 9) uniform uint startM;        // packed orientation to REDUCE to identity (active cubie, or the
                                                 // coherence RELATIVE X = M.Dᵀ built on the host)
layout(location = 10) uniform uint realM;        // the rep's REAL orientation -- tracked in parallel for the collateral
                                                 // slice (in coherence real = X.D != X). == startM in descent.
layout(location = 11) uniform uint collAxis;     // preferred collateral axis: turning its layer moves rep's WHOLE block
                                                 // together (keeps the merge intact). 0xFFFFFFFF -> random.
layout(location = 12) uniform uint perturbPercent;   // SEED DIVERSITY %: per step, chance to take a reject-free VARIANT
                                                 // instead of the canonical move -- on a hole, a NON-SEATING split
                                                 // (hole-cycle >=4 -> 2+2); on a reflected diagonal, a RANDOM rotation
                                                 // (random partner axis, angle 90/180/270) instead of the 180 pair.
                                                 // 0 = canonical (bit-identical to old enumerator).

// Swapping the plane's axis order swaps 90<->270 and keeps 180 (RotateMatInt(y,x,1)==RotateMatInt(x,y,3)).
uint swap13(uint a) { return (a == 1u) ? 3u : ((a == 3u) ? 1u : a); }
// Plane index for the unordered pair {lo,hi}, lo<hi, matching TAffine.Planes order (row-major: row=hi, col=lo).
uint planeIndex(uint lo, uint hi) { return hi * (hi - 1u) / 2u + lo; }

void main()
{
    uint tid = gl_GlobalInvocationID.x;
    if (tid >= numSeeds) return;

    uint M = startM;                             // decompose THIS to identity (relative X in coherence)
    uint real = realM;                           // the rep's real orientation, evolved by the SAME moves (for the slice)
    uint cubieID = ActivePos[activeIndex];       // rep's home position (for the collateral slice)
    uint base = tid * SEED_STRIDE;
    uint rng = tid ^ TimeSeed;
    uint step = 0u;
    uint wr = 0u;                                // moves written so far

    // Bounded by SEED_STRIDE (a valid orientation solves in <= N-1 + N/2 moves); the guard also stops a bad state
    // from spinning instead of hanging the GPU.
    for (uint iter = 0u; iter < SEED_STRIDE && M != IDENTITY; iter++)
    {
        // Classify rows by the diagonal (i,i) entry: 0 = hole (displaced axis), -1 = seated but negated, +1 = solved.
        uint holes = 0u, negs = 0u, holeCount = 0u;
        for (uint r = 0u; r < uint(N); r++)
        {
            int d = getElem(M, r, r);
            if (d == 0)       { MASK_SET(holes, r); holeCount++; }
            else if (d == -1) { MASK_SET(negs, r); }
        }

        uint a1, a2, angle;
        if (holeCount > 0u)
        {
            bool chosen = false;

            // (A') PERTURBATION: with prob perturbPercent, take a NON-SEATING progress move instead of seating.
            // c(r) = axisInRow(M,r) = the axis sitting in row r; its cycles are the displaced-axis structure. Swapping
            // two SAME-cycle rows at cycle-distance 2 (c^2(start)) splits the cycle into two pieces each >= 2 -- for a
            // 4-cycle exactly the "opposite" swap 4-cycle -> 2+2. That is a MINIMAL factorisation too (same move count
            // as seating), just a different Cayley tree: the 2+2 structures pure seating never produces. The 90/270
            // sign flip lands on a still-displaced axis and is corrected for free when that axis is later seated, so
            // it stays a clean progress move (det always +1). Reject-free: one non-seating move + seating completes.
            // Needs a cycle of length >= 4 (a 3-cycle's distance-2 swap would seat); else fall back to seating.
            if (perturbPercent > 0u && (hash(rng + step) % 100u) < perturbPercent)
            {
                step++;
                uint pick = hash(rng + step) % holeCount; step++;
                uint start = 0u, seen = 0u;
                for (uint r = 0u; r < uint(N); r++)
                    if (MASK_TEST(holes, r)) { if (seen == pick) { start = r; break; } seen++; }
                uint len = 0u, cur = start;                    // walk the c-cycle from start, count its length
                for (uint i = 0u; i < uint(N); i++) { cur = axisInRow(M, cur); len++; if (cur == start) break; }
                if (len >= 4u)
                {
                    uint b = axisInRow(M, axisInRow(M, start));   // c^2(start): distance-2, non-adjacent for len>=4
                    a1 = min(start, b); a2 = max(start, b);
                    angle = ((hash(rng + step) % 2u) == 0u) ? 1u : 3u; step++;   // either quarter-turn swaps the two rows
                    chosen = true;
                }
            }

            if (!chosen)
            {
                // (A) SEATING: seat a RANDOM displaced axis t.
                uint pick = hash(rng + step) % holeCount; step++;
                uint t = 0u, seen = 0u;
                for (uint r = 0u; r < uint(N); r++)
                    if (MASK_TEST(holes, r)) { if (seen == pick) { t = r; break; } seen++; }
                uint p = 0u;                                   // row currently holding axis t
                for (uint r = 0u; r < uint(N); r++)
                    if (axisInRow(M, r) == t) { p = r; break; }
                uint ang = (getElem(M, p, t) == -1) ? 1u : 3u;   // seat t with sign 0 (getElem(p,t) == -1 <=> row p's entry is reflected)
                a1 = min(t, p); a2 = max(t, p);
                angle = (t < p) ? ang : swap13(ang);           // express in stored plane order
            }
        }
        else
        {
            // (B) REFLECTED DIAGONAL (getElem == -1). Canonical fix is a 180 pairing two negs. For DIVERSITY, when the
            // die fires take a FULLY RANDOM reject-free rotation at a RANDOM neg instead: random partner axis j, random
            // angle {90,180,270}. det +1 is invariant so ANY of these lets the loop still finish (90/270 displace the
            // axis into holes that reseat; 180 toggles two signs) -- maximal fresh material at the sign stage, no parity
            // bookkeeping. p=0 -> always the canonical 180 pair (bit-identical to the old behaviour).
            uint a = 0u;
            for (uint r = 0u; r < uint(N); r++) if (MASK_TEST(negs, r)) { a = r; break; }
            if (perturbPercent > 0u && (hash(rng + step) % 100u) < perturbPercent)
            {
                step++;
                uint negCount = bitCount(negs);                    // random pivot among the reflected axes
                uint pk = hash(rng + step) % negCount; step++;
                uint seenN = 0u;
                for (uint r = 0u; r < uint(N); r++)
                    if (MASK_TEST(negs, r)) { if (seenN == pk) { a = r; break; } seenN++; }
                uint j = hash(rng + step) % uint(N); step++;       // random partner axis
                if (j == a) j = (j + 1u) % uint(N);
                a1 = min(a, j); a2 = max(a, j);
                angle = 1u + (hash(rng + step) % 3u); step++;      // random 90 / 180 / 270
            }
            else
            {
                uint b = a;                                    // canonical: pair a with the next neg via 180
                for (uint r = a + 1u; r < uint(N); r++) if (MASK_TEST(negs, r)) { b = r; break; }
                a1 = min(a, b); a2 = max(a, b); angle = 2u;
            }
        }

        // Apply the move to BOTH matrices: X = M drives the seating, real drives the slice. (a1,a2,angle) is the
        // stored-order form of the seating rotation, so it evolves X identically to the in-branch (t,p,ang).
        RotateMatInt(M, a1, a2, angle);
        RotateMatInt(real, a1, a2, angle);

        // Collateral: a random axis NOT in the plane; slice = the cubie's current layer there (axis stable, out of
        // the plane, so reading it after the rotation is fine). This picks WHICH layer turns (the diversity that
        // does not touch the target's solve).
        uint ax;
        if (collAxis < uint(N) && collAxis != a1 && collAxis != a2)
            ax = collAxis;                                   // keep rep's block co-layered (merge stays intact)
        else
        {
            ax = hash(rng + step) % uint(N); step++;
            while (ax == a1 || ax == a2) ax = (ax + 1u) % uint(N);
        }
        uint slice = curCoord(real, cubieID, ax);

        SeedMoves[base + wr] = angle | (planeIndex(a1, a2) << 2u) | (ax << 7u) | (slice << 10u);
        wr++;
    }

    for (uint k = wr; k < SEED_STRIDE; k++) SeedMoves[base + k] = 0xFFFFFFFFu;   // sentinel pad
}
