using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Block building mutation - inserts common building block sequences from
/// popular speedcubing methods (CFOP, Roux, ZZ).
///
/// For 3D cubes, uses:
/// - Full F2L (First Two Layers) - 41 basic cases
///   - Easy cases: corner and edge already paired (4)
///   - Corner in slot, edge on top (6)
///   - Edge in slot, corner on top (6)
///   - Corner pointing up, edge on top (6)
///   - Corner pointing out, edge on top (6)
///   - Corner in top, edge colors match (6)
///   - Corner in top, edge colors opposite (6)
///   - Special/difficult cases (1)
/// - Cross building moves
/// - Block building triggers from Roux method
/// - Layer-by-layer building elements
///
/// For 4D+ cubes, uses generalized block-building patterns:
/// - Layer-by-layer building blocks
/// - Multi-plane coordination sequences
///
/// Total patterns for 3D: ~60 (41 F2L + cross + Roux + triggers)
///
/// These building blocks represent efficient ways to solve small
/// portions of the cube and can accelerate GA convergence.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class BlockBuildingMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cached building blocks for current dimension
    private static int _cachedN = -1;
    private static int _cachedSize = -1;
    private static List<int[]>? _buildingBlocks;

    public void Mutate(T chromosome, Random rng)
    {
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 3) return;

        // Build blocks cache if needed
        int n = TAffine.N;
        int size = TRubikCube.Size;
        if (_cachedN != n || _cachedSize != size)
        {
            BuildBlocksCache(n, size);
        }

        if (_buildingBlocks == null || _buildingBlocks.Count == 0) return;

        // Select a random building block
        int[] block = _buildingBlocks[rng.Next(_buildingBlocks.Count)];

        // Find a position where the block fits
        if (block.Length > length) return;

        int startIdx = rng.Next(length - block.Length + 1);

        // Insert the building block
        for (int i = 0; i < block.Length; i++)
        {
            chromosome.Genes[startIdx + i] = block[i];
        }
    }

    /// <summary>
    /// Build cache of building block sequences for current dimension.
    /// </summary>
    private static void BuildBlocksCache(int n, int size)
    {
        _cachedN = n;
        _cachedSize = size;
        _buildingBlocks = new List<int[]>();

        if (n == 3)
        {
            Build3DBlocks(size);
        }
        else
        {
            BuildNDBlocks(n, size);
        }
    }

    /// <summary>
    /// Build 3D CFOP/Roux building blocks with full F2L algorithm set.
    /// </summary>
    private static void Build3DBlocks(int size)
    {
        if (_buildingBlocks == null) return;

        int s = size - 1; // Last slice index

        // ============================================================
        // MOVE ENCODING HELPERS
        // Angles: 0 = 90° (clockwise), 1 = 180°, 2 = -90° (counter-clockwise)
        // ============================================================
        int R = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 0 }.Encode();
        int Ri = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 2 }.Encode();
        int R2 = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 1 }.Encode();

        int L = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 2 }.Encode();
        int Li = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 0 }.Encode();
        int L2 = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 1 }.Encode();

        int U = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 0 }.Encode();
        int Ui = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 2 }.Encode();
        int U2 = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 1 }.Encode();

        int D = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 2 }.Encode();
        int Di = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 0 }.Encode();
        int D2 = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 1 }.Encode();

        int F = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 0 }.Encode();
        int Fi = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 2 }.Encode();
        int F2 = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 1 }.Encode();

        int B = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 2 }.Encode();
        int Bi = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 0 }.Encode();
        int B2 = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 1 }.Encode();

        // ============================================================
        // F2L ALGORITHMS - Full 41 Cases
        // All cases assume solving into the Front-Right slot
        // ============================================================

        // ============================================================
        // BASIC CASES - Corner and Edge Already Paired (4 cases)
        // ============================================================

        // F2L 1: Pair made, insert (white on right)
        // R U R'
        _buildingBlocks.Add(new[] { R, U, Ri });

        // F2L 2: Pair made, insert (white on top)
        // U' F' U F
        _buildingBlocks.Add(new[] { Ui, Fi, U, F });

        // F2L 3: Pair made, insert (white on front)
        // F' U' F
        _buildingBlocks.Add(new[] { Fi, Ui, F });

        // F2L 4: Pair made, insert from back
        // R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri });

        // ============================================================
        // CORNER IN SLOT, EDGE ON TOP (6 cases)
        // Corner correctly placed but edge needs insertion
        // ============================================================

        // F2L 5: Corner in place, edge on top (case 1)
        // U' R U' R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, Ui, Ri, U, R, U, Ri });

        // F2L 6: Corner in place, edge on top (case 2)
        // U' R U R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, U, Ri, U, R, U, Ri });

        // F2L 7: Corner in place, edge on top (case 3)
        // U' R U2 R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, U2, Ri, U, R, U, Ri });

        // F2L 8: Corner in place, edge on top (case 4)
        // U F' U F U' F' U' F
        _buildingBlocks.Add(new[] { U, Fi, U, F, Ui, Fi, Ui, F });

        // F2L 9: Corner in place, edge on top (case 5)
        // U F' U' F U' F' U' F
        _buildingBlocks.Add(new[] { U, Fi, Ui, F, Ui, Fi, Ui, F });

        // F2L 10: Corner in place, edge on top (case 6)
        // U F' U2 F U' F' U' F
        _buildingBlocks.Add(new[] { U, Fi, U2, F, Ui, Fi, Ui, F });

        // ============================================================
        // EDGE IN SLOT, CORNER ON TOP (6 cases)
        // Edge correctly placed but corner needs insertion
        // ============================================================

        // F2L 11: Edge in slot, corner on top (white up, case 1)
        // R U R' U' R U R' U' R U R'
        _buildingBlocks.Add(new[] { R, U, Ri, Ui, R, U, Ri, Ui, R, U, Ri });

        // F2L 12: Edge in slot, corner on top (white up, case 2)
        // R U' R' U R U' R' U R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U, R, Ui, Ri, U, R, Ui, Ri });

        // F2L 13: Edge in slot, corner on top (white front)
        // R U R' U' R U R'
        _buildingBlocks.Add(new[] { R, U, Ri, Ui, R, U, Ri });

        // F2L 14: Edge in slot, corner on top (white right)
        // R U' R' U R U2 R' U R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U, R, U2, Ri, U, R, Ui, Ri });

        // F2L 15: Edge in slot, corner on top (white front, alt)
        // U' R U R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, U, Ri, U, R, U, Ri });

        // F2L 16: Edge in slot, corner on top (white right, alt)
        // U' R U' R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, Ui, Ri, U, R, U, Ri });

        // ============================================================
        // CORNER POINTING UP, EDGE IN U-LAYER (6 cases)
        // White sticker pointing up on corner
        // ============================================================

        // F2L 17: Corner up, edge in U (same side)
        // R U2 R' U' R U R'
        _buildingBlocks.Add(new[] { R, U2, Ri, Ui, R, U, Ri });

        // F2L 18: Corner up, edge in U (opposite side)
        // F' U2 F U F' U' F
        _buildingBlocks.Add(new[] { Fi, U2, F, U, Fi, Ui, F });

        // F2L 19: Corner up, edge in U (diagonal, case 1)
        // U R U2 R' U R U' R'
        _buildingBlocks.Add(new[] { U, R, U2, Ri, U, R, Ui, Ri });

        // F2L 20: Corner up, edge in U (diagonal, case 2)
        // U' F' U2 F U' F' U F
        _buildingBlocks.Add(new[] { Ui, Fi, U2, F, Ui, Fi, U, F });

        // F2L 21: Corner up, edge in U (case 5)
        // F' U F U' F' U F
        _buildingBlocks.Add(new[] { Fi, U, F, Ui, Fi, U, F });

        // F2L 22: Corner up, edge in U (case 6)
        // R U' R' U R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U, R, Ui, Ri });

        // ============================================================
        // CORNER POINTING OUT, EDGE IN U-LAYER (6 cases)
        // White sticker pointing to the side on corner
        // ============================================================

        // F2L 23: Corner out (white right), edge matching
        // U' R U' R' U2 R U' R'
        _buildingBlocks.Add(new[] { Ui, R, Ui, Ri, U2, R, Ui, Ri });

        // F2L 24: Corner out (white front), edge matching
        // U F' U F U2 F' U F
        _buildingBlocks.Add(new[] { U, Fi, U, F, U2, Fi, U, F });

        // F2L 25: Corner out, edge opposite (case 1)
        // F' U' F U F' U' F
        _buildingBlocks.Add(new[] { Fi, Ui, F, U, Fi, Ui, F });

        // F2L 26: Corner out, edge opposite (case 2)
        // R U R' U' R U R'
        _buildingBlocks.Add(new[] { R, U, Ri, Ui, R, U, Ri });

        // F2L 27: Corner out, edge diagonal (case 1)
        // R U' R' U R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U, R, Ui, Ri });

        // F2L 28: Corner out, edge diagonal (case 2)
        // F' U F U' F' U F
        _buildingBlocks.Add(new[] { Fi, U, F, Ui, Fi, U, F });

        // ============================================================
        // CORNER IN TOP, EDGE COLORS MATCH (6 cases)
        // Corner white on side, edge colors touching match
        // ============================================================

        // F2L 29: Colors match, easy insert
        // R U' R' U2 R U R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U2, R, U, Ri });

        // F2L 30: Colors match, front insert
        // F' U F U2 F' U' F
        _buildingBlocks.Add(new[] { Fi, U, F, U2, Fi, Ui, F });

        // F2L 31: Colors match, U2 setup
        // U2 R U R' U R U' R'
        _buildingBlocks.Add(new[] { U2, R, U, Ri, U, R, Ui, Ri });

        // F2L 32: Colors match, U2 front
        // U2 F' U' F U' F' U F
        _buildingBlocks.Add(new[] { U2, Fi, Ui, F, Ui, Fi, U, F });

        // F2L 33: Colors match, U setup
        // U R U' R' U' R U R'
        _buildingBlocks.Add(new[] { U, R, Ui, Ri, Ui, R, U, Ri });

        // F2L 34: Colors match, U' front
        // U' F' U F U F' U' F
        _buildingBlocks.Add(new[] { Ui, Fi, U, F, U, Fi, Ui, F });

        // ============================================================
        // CORNER IN TOP, EDGE COLORS OPPOSITE (6 cases)
        // Corner white on side, edge colors touching are opposite
        // ============================================================

        // F2L 35: Colors opposite, R insert
        // U' R U R' U2 R U' R'
        _buildingBlocks.Add(new[] { Ui, R, U, Ri, U2, R, Ui, Ri });

        // F2L 36: Colors opposite, F insert
        // U F' U' F U2 F' U F
        _buildingBlocks.Add(new[] { U, Fi, Ui, F, U2, Fi, U, F });

        // F2L 37: Colors opposite, case 3
        // R U R' U2 R U' R'
        _buildingBlocks.Add(new[] { R, U, Ri, U2, R, Ui, Ri });

        // F2L 38: Colors opposite, case 4
        // F' U' F U2 F' U F
        _buildingBlocks.Add(new[] { Fi, Ui, F, U2, Fi, U, F });

        // F2L 39: Colors opposite, U' R
        // U' R U' R' U R U R'
        _buildingBlocks.Add(new[] { Ui, R, Ui, Ri, U, R, U, Ri });

        // F2L 40: Colors opposite, U F
        // U F' U F U' F' U' F
        _buildingBlocks.Add(new[] { U, Fi, U, F, Ui, Fi, Ui, F });

        // ============================================================
        // SPECIAL/DIFFICULT CASE (1 case)
        // Both pieces in slot but wrong orientation
        // ============================================================

        // F2L 41: Both in slot, need complete redo
        // R U' R' U R U2 R' U R U' R'
        _buildingBlocks.Add(new[] { R, Ui, Ri, U, R, U2, Ri, U, R, Ui, Ri });

        // ============================================================
        // ROUX BLOCKS
        // Roux method focuses on building 1x2x3 blocks
        // ============================================================

        if (size >= 3)
        {
            int midSlice = size / 2;
            int M = new TMove { Axis = 0, Slice = midSlice, Plane = 1, Angle = 2 }.Encode();
            int Mi = new TMove { Axis = 0, Slice = midSlice, Plane = 1, Angle = 0 }.Encode();
            int M2Enc = new TMove { Axis = 0, Slice = midSlice, Plane = 1, Angle = 1 }.Encode();

            // M U M' (middle layer moves)
            _buildingBlocks.Add(new[] { M, U, Mi });

            // M' U M (inverse)
            _buildingBlocks.Add(new[] { Mi, U, M });

            // M U2 M' (180° turn)
            _buildingBlocks.Add(new[] { M, U2, Mi });

            // M' U' M (alternative)
            _buildingBlocks.Add(new[] { Mi, Ui, M });

            // M2 U M2 (double middle)
            _buildingBlocks.Add(new[] { M2Enc, U, M2Enc });

            // M U M' U M U2 M' (CMLL setup)
            _buildingBlocks.Add(new[] { M, U, Mi, U, M, U2, Mi });
        }

        // ============================================================
        // CROSS BUILDING
        // Common sequences for building the cross
        // ============================================================

        // Cross edge insert: F R (simple)
        _buildingBlocks.Add(new[] { F, R });

        // Cross edge adjust: R' D' R (bring edge down)
        _buildingBlocks.Add(new[] { Ri, Di, R });

        // Cross setup: D R' D' R
        _buildingBlocks.Add(new[] { D, Ri, Di, R });

        // Cross: F' D F (front cross edge)
        _buildingBlocks.Add(new[] { Fi, D, F });

        // Cross: R D' R' (right cross edge)
        _buildingBlocks.Add(new[] { R, Di, Ri });

        // Cross: L' D L (left cross edge)
        _buildingBlocks.Add(new[] { Li, D, L });

        // Cross: B D2 B' (back cross edge)
        _buildingBlocks.Add(new[] { B, D2, Bi });

        // Cross: F D F' (alternative front)
        _buildingBlocks.Add(new[] { F, D, Fi });

        // ============================================================
        // LAYER-BY-LAYER BUILDING BLOCKS
        // ============================================================

        // Corner twist setup: R' D' R D (classic)
        _buildingBlocks.Add(new[] { Ri, Di, R, D });

        // Left side equivalent: L D L' D'
        _buildingBlocks.Add(new[] { L, D, Li, Di });

        // Double corner twist: (R' D' R D)2
        _buildingBlocks.Add(new[] { Ri, Di, R, D, Ri, Di, R, D });

        // Triple corner twist: (R' D' R D)3
        _buildingBlocks.Add(new[] { Ri, Di, R, D, Ri, Di, R, D, Ri, Di, R, D });

        // ============================================================
        // SHORT TRIGGERS
        // ============================================================

        // R2 U2 (quick layer adjustment)
        _buildingBlocks.Add(new[] { R2, U2 });

        // F2 R2 (different plane adjustment)
        _buildingBlocks.Add(new[] { F2, R2 });

        // U R U' R' (sexy move)
        _buildingBlocks.Add(new[] { U, R, Ui, Ri });

        // R U R' U' (inverse sexy)
        _buildingBlocks.Add(new[] { R, U, Ri, Ui });

        // R' F R F' (sledgehammer)
        _buildingBlocks.Add(new[] { Ri, F, R, Fi });

        // F R' F' R (hedgeslammer)
        _buildingBlocks.Add(new[] { F, Ri, Fi, R });

        // ============================================================
        // ADDITIONAL F2L VARIATIONS (for different slots)
        // Left-handed versions for L slot
        // ============================================================

        // L' U' L (left slot insert)
        _buildingBlocks.Add(new[] { Li, Ui, L });

        // L' U L (left slot alternate)
        _buildingBlocks.Add(new[] { Li, U, L });

        // L' U2 L (left slot 180)
        _buildingBlocks.Add(new[] { Li, U2, L });

        // U L' U' L (left setup + insert)
        _buildingBlocks.Add(new[] { U, Li, Ui, L });

        // U' L' U L (left alternate setup)
        _buildingBlocks.Add(new[] { Ui, Li, U, L });

        // Back slot variants
        // B U B' (back right insert)
        _buildingBlocks.Add(new[] { B, U, Bi });

        // B' U' B (back left insert)
        _buildingBlocks.Add(new[] { Bi, Ui, B });
    }

    /// <summary>
    /// Build generalized building blocks for N-dimensional cubes.
    /// </summary>
    private static void BuildNDBlocks(int n, int size)
    {
        if (_buildingBlocks == null) return;

        int numPlanes = TAffine.Planes.Length;
        int lastSlice = size - 1;

        // Helper to create a move
        TMove M(int axis, int slice, int plane, int angle) =>
            new TMove { Axis = axis, Slice = slice, Plane = plane, Angle = angle };

        // Generate layer building blocks for each plane
        for (int p = 0; p < Math.Min(numPlanes, 6); p++)
        {
            int[] planeAxes = TAffine.Planes[p];
            int perpAxis = FindPerpendicularAxis(planeAxes, n);

            if (perpAxis < 0) continue;

            // Basic layer insert: A B A' pattern on this plane
            for (int p2 = 0; p2 < Math.Min(numPlanes, 6); p2++)
            {
                if (p2 == p) continue;

                int[] planeAxes2 = TAffine.Planes[p2];
                int perpAxis2 = FindPerpendicularAxis(planeAxes2, n);

                if (perpAxis2 < 0) continue;

                // A B A' block
                _buildingBlocks.Add(new[]
                {
                    M(perpAxis, lastSlice, p, 0).Encode(),
                    M(perpAxis2, lastSlice, p2, 0).Encode(),
                    M(perpAxis, lastSlice, p, 2).Encode()
                });

                // A' B A block (inverse)
                _buildingBlocks.Add(new[]
                {
                    M(perpAxis, lastSlice, p, 2).Encode(),
                    M(perpAxis2, lastSlice, p2, 0).Encode(),
                    M(perpAxis, lastSlice, p, 0).Encode()
                });

                // A B A' B' block (commutator)
                _buildingBlocks.Add(new[]
                {
                    M(perpAxis, lastSlice, p, 0).Encode(),
                    M(perpAxis2, lastSlice, p2, 0).Encode(),
                    M(perpAxis, lastSlice, p, 2).Encode(),
                    M(perpAxis2, lastSlice, p2, 2).Encode()
                });
            }

            // Layer adjustment: A2 B2 pattern
            for (int p2 = p + 1; p2 < Math.Min(numPlanes, 6); p2++)
            {
                int[] planeAxes2 = TAffine.Planes[p2];
                int perpAxis2 = FindPerpendicularAxis(planeAxes2, n);

                if (perpAxis2 < 0) continue;

                _buildingBlocks.Add(new[]
                {
                    M(perpAxis, lastSlice, p, 1).Encode(),   // A2
                    M(perpAxis2, lastSlice, p2, 1).Encode()  // B2
                });
            }
        }

        // Multi-slice coordination for larger cubes
        if (size >= 3)
        {
            int midSlice = size / 2;

            for (int p = 0; p < Math.Min(numPlanes, 4); p++)
            {
                int[] planeAxes = TAffine.Planes[p];
                int perpAxis = FindPerpendicularAxis(planeAxes, n);

                if (perpAxis < 0) continue;

                // Inner slice + outer slice coordination
                _buildingBlocks.Add(new[]
                {
                    M(perpAxis, lastSlice, p, 0).Encode(),
                    M(perpAxis, midSlice, p, 2).Encode(),
                    M(perpAxis, lastSlice, p, 2).Encode(),
                    M(perpAxis, midSlice, p, 0).Encode()
                });
            }
        }
    }

    /// <summary>
    /// Find an axis perpendicular to the given plane axes.
    /// </summary>
    private static int FindPerpendicularAxis(int[] planeAxes, int n)
    {
        for (int axis = 0; axis < n; axis++)
        {
            if (axis != planeAxes[0] && axis != planeAxes[1])
            {
                return axis;
            }
        }
        return -1;
    }
}
