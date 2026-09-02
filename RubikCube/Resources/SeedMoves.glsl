#version 430
#include "Setup.glsl"

// GPU SEED PRODUCER. One thread = one RANDOM walk down the MINIMAL decomposition tree, solving the ACTIVE cubie's
// orientation to IDENTITY and writing the move sequence into the SeedMoves SSBO (stride SEED_STRIDE, unused slots =
// 0xFFFFFFFF). No branching, no backtracking, no reject -> ideal for GPU. Each step takes a UNIFORM same-cycle
// transposition (seating + splitting unified -> every Cayley tree is an EQUAL branch of the pool); the residual
// signs are then paired by 180. Diversity = the per-thread RNG (which branch, which collateral axis). The coherence
// retarget (solve X = M.Dᵀ toward a block orientation D) is a later layer -- see memory coherence-seeding-retarget.
//
// Row code = (col<<1)|sign (getRowCode); a quarter-turn (RotateMatInt) swaps two rows + one sign flip; 180 negates both.
// SEATING sign rule (derived from RotateMatInt, axis1 < axis2): angle = sign(row_p)==-1 ? 90 : 270,
// which lands the seated axis with sign 0. Negated fixed points (col==row, sign==1) are paired by one 180 each.

layout(local_size_x = 64) in;
layout(location = 1) uniform uint targetCubieID;   // cubie Id whose position drives the collateral slice
layout(location = 9) uniform uint startM;        // packed orientation to REDUCE to identity (active cubie, or the
                                                 // coherence RELATIVE X = M.Dᵀ built on the host)
//layout(location = 10) uniform uint targetDPack;  // packed D^T (or IDENTITY) supplied by host; used to compute target cubie id
//                                                 // If IDENTITY is passed we can skip the packing and reuse cubieID directly.
layout(location = 11) uniform uint collAxis;     // preferred collateral axis: turning its layer moves rep's WHOLE block
                                                 // together (keeps the merge intact). 0xFFFFFFFF -> random.

// Swapping the plane's axis order swaps 90<->270 and keeps 180 (RotateMatInt(y,x,1)==RotateMatInt(x,y,3)).
//uint swap13(uint a) { return (a == 1u) ? 3u : ((a == 3u) ? 1u : a); }
// Plane index for the unordered pair {lo,hi}, lo<hi, matching TAffine.Planes order (row-major: row=hi, col=lo).
uint planeIndex(uint lo, uint hi) { return hi * (hi - 1u) / 2u + lo; }

void main()
{
    uint tid = gl_GlobalInvocationID.x;
    if (tid >= numSeeds) return;

    uint X = startM;                             // decompose THIS to identity
    uint base = tid * SEED_STRIDE;
    uint step = 0u;
    uint wr = 0u;                                // moves written so far

    // Bounded by SEED_STRIDE (a valid orientation solves in <= N-1 + N/2 moves); the guard also stops a bad state
    // from spinning instead of hanging the GPU.
    for (uint iter = 0u; iter < SEED_STRIDE && X != IDENTITY; iter++)
    {
        // Classify rows by the diagonal (i,i) entry: 0 = hole (displaced axis), -1 = seated but negated, +1 = solved.
        uint seats[N], seatsCount = 0u;
        for (uint r = 0u; r < N; r++)
            if (getElem(X, r, r) <= 0)
                seats[seatsCount++] = r;

        if (seatsCount == 0u)
            break;
            // UNIFORM MINIMAL-TREE BRANCH. Pick a random displaced axis i, then a random OTHER axis j in the SAME
            // permutation cycle (c(r) = axisInRow(X,r)), and swap them. A same-cycle transposition is EXACTLY a minimal
            // (distance-reducing) permutation move; ADJACENT in the cycle it SEATS an axis, NON-adjacent it SPLITS the
            // cycle (e.g. 4-cycle -> 2+2). So seating and splitting are one family, sampled together -> every Cayley
            // tree is an equal branch of the pool. Reject-free: after any such move seating still completes.
        uint pick = randNext(seatsCount);
        uint i = seats[pick];
        uint cyc[N];
        uint clen = 0u, cur = i; // collect i's cycle (bounded by N)
        for (uint k = 0u; k < N; k++)
        {
            cyc[clen++] = cur;
            cur = axisFromCode(getRowCode(X, cur));
            if (cur == i)
                break;
        }
        uint j = clen == 1 ? seats[(pick + 1u + randNext(seatsCount - 1u)) % seatsCount] : // 1-cycle (only i in the cycle) -> pick a random OTHER displaced axis
                             cyc[1u + randNext(clen - 1u)]; // random partner in the cycle (index 0 == i)
        uint a1 = min(i, j);
        uint a2 = max(i, j);
        uint angle = 2u;
        if (clen > 1)
        {
            int Xa1a2 = getElem(X, a1, a2);
            int Xa2a1 = getElem(X, a2, a1);           
            if (Xa1a2 == Xa2a1) 
                angle = (randNext(2u) == 0u) ? 1u : 3u; // Symmetry, contradiction, or 0==0 -> cycle-breaker
            else if (Xa1a2 == -1) 
                angle = 3u; // Seats axis a2 (reverts -1 above diagonal to X[a2,a2] = 1)
            else if (Xa1a2 == 1) 
                angle = 1u; // Seats axis a2 (reverts 1 above diagonal to X[a2,a2] = 1)
            else if (Xa2a1 == 1) 
                angle = 3u; // Seats axis a1 (reverts 1 below diagonal to X[a1,a1] = 1)
            else // if (Xa2a1 == -1)
                angle = 1u; // Seats axis a1 (reverts -1 below diagonal to X[a1,a1] = 1)
        }
        // Apply the move to X (drives the seating). The targetCubieID encodes v' = startM * v and does
        // not need per-step updates; using curCoord(X, targetCubieID, ax) below yields the collateral slice
        // equivalent to evolving the rep's real matrix in lockstep.
        RotateMatInt(X, a1, a2, angle);

        // Collateral: a random axis NOT in the plane; slice = the cubie's current layer there (axis stable, out of
        // the plane, so reading it after the rotation is fine). This picks WHICH layer turns (the diversity that
        // does not touch the target's solve).
        uint ax;
        if (collAxis < N && collAxis != a1 && collAxis != a2)
            ax = collAxis;                                   // keep rep's block co-layered (merge stays intact)
        else
        {
            ax = randNext(N);
            while (ax == a1 || ax == a2) ax = (ax + 1u) % N;
        }
        uint slice = curCoord(X, targetCubieID, ax);

        SeedMoves[base + wr] = angle | (planeIndex(a1, a2) << 2u) | (ax << 7u) | (slice << 10u);
        wr++;
    }

    //if (X != IDENTITY)
    //{
    //    // Zapisujemy łatwy do znalezienia wzorzec błędu na początku bloku tego seeda
    //    SeedMoves[base] = 0xAAAAAAAAu;
    //    //SeedMoves[2000000000u] = 0xAAAAAAAAu;

    //    // Opcjonalnie: zerujemy lub oznaczamy pozostałe ruchy w tym seedzie
    //    for (uint k = 1u; k < SEED_STRIDE; k++)
    //    {
    //        SeedMoves[base + k] = 0xFFFFFFFFu;
    //    }
    //    return;
    //}


    for (uint k = wr; k < SEED_STRIDE; k++) SeedMoves[base + k] = 0xFFFFFFFFu;   // sentinel pad
}
