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
/// - F2L (First Two Layers) pair insertion sequences
/// - Cross building moves
/// - Block building triggers from Roux method
///
/// For 4D+ cubes, uses generalized block-building patterns:
/// - Layer-by-layer building blocks
/// - Multi-plane coordination sequences
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
    /// Build 3D CFOP/Roux building blocks.
    /// </summary>
    private static void Build3DBlocks(int size)
    {
        if (_buildingBlocks == null) return;

        int lastSlice = size - 1;

        // Helper to create a move
        TMove M(int axis, int slice, int plane, int angle) =>
            new TMove { Axis = axis, Slice = slice, Plane = plane, Angle = angle };

        // ===== F2L PAIR INSERTIONS =====
        // These are common sequences to insert corner-edge pairs

        // Basic F2L case: R U R' (insert pair from top)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, lastSlice, 0, 0).Encode(),  // U
            M(0, lastSlice, 1, 2).Encode()   // R'
        });

        // F2L case: R U' R' (different angle)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, lastSlice, 0, 2).Encode(),  // U'
            M(0, lastSlice, 1, 2).Encode()   // R'
        });

        // F2L case: R U2 R' (180° turn)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, lastSlice, 0, 1).Encode(),  // U2
            M(0, lastSlice, 1, 2).Encode()   // R'
        });

        // F2L case: F' U F (front-based insertion)
        _buildingBlocks.Add(new[]
        {
            M(2, lastSlice, 1, 2).Encode(),  // F'
            M(1, lastSlice, 0, 0).Encode(),  // U
            M(2, lastSlice, 1, 0).Encode()   // F
        });

        // F2L case: F' U' F
        _buildingBlocks.Add(new[]
        {
            M(2, lastSlice, 1, 2).Encode(),  // F'
            M(1, lastSlice, 0, 2).Encode(),  // U'
            M(2, lastSlice, 1, 0).Encode()   // F
        });

        // F2L case: U R U' R' (pair setup + insert)
        _buildingBlocks.Add(new[]
        {
            M(1, lastSlice, 0, 0).Encode(),  // U
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, lastSlice, 0, 2).Encode(),  // U'
            M(0, lastSlice, 1, 2).Encode()   // R'
        });

        // F2L case: U' R U R' (alternative)
        _buildingBlocks.Add(new[]
        {
            M(1, lastSlice, 0, 2).Encode(),  // U'
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, lastSlice, 0, 0).Encode(),  // U
            M(0, lastSlice, 1, 2).Encode()   // R'
        });

        // ===== ROUX BLOCKS =====
        // Roux method focuses on building 1x2x3 blocks

        // Roux block trigger: r U r' (wide move - simulated with slice)
        // Using middle slice for approximation
        if (size >= 3)
        {
            int midSlice = size / 2;

            // M U M' (middle layer moves)
            _buildingBlocks.Add(new[]
            {
                M(0, midSlice, 1, 0).Encode(),   // M (middle slice)
                M(1, lastSlice, 0, 0).Encode(),  // U
                M(0, midSlice, 1, 2).Encode()    // M'
            });

            // M' U M (inverse)
            _buildingBlocks.Add(new[]
            {
                M(0, midSlice, 1, 2).Encode(),   // M'
                M(1, lastSlice, 0, 0).Encode(),  // U
                M(0, midSlice, 1, 0).Encode()    // M
            });
        }

        // ===== CROSS BUILDING =====
        // Common sequences for building the cross

        // Cross edge insert: F R (simple)
        _buildingBlocks.Add(new[]
        {
            M(2, lastSlice, 1, 0).Encode(),  // F
            M(0, lastSlice, 1, 0).Encode()   // R
        });

        // Cross edge adjust: R' D' R (bring edge down)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 2).Encode(),  // R'
            M(1, 0, 0, 2).Encode(),          // D'
            M(0, lastSlice, 1, 0).Encode()   // R
        });

        // Cross setup: D R' D' R
        _buildingBlocks.Add(new[]
        {
            M(1, 0, 0, 0).Encode(),          // D
            M(0, lastSlice, 1, 2).Encode(),  // R'
            M(1, 0, 0, 2).Encode(),          // D'
            M(0, lastSlice, 1, 0).Encode()   // R
        });

        // ===== LAYER-BY-LAYER BUILDING =====

        // Corner twist setup: R' D' R D (classic)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 2).Encode(),  // R'
            M(1, 0, 0, 2).Encode(),          // D'
            M(0, lastSlice, 1, 0).Encode(),  // R
            M(1, 0, 0, 0).Encode()           // D
        });

        // Left side equivalent: L D L' D'
        _buildingBlocks.Add(new[]
        {
            M(0, 0, 1, 0).Encode(),          // L
            M(1, 0, 0, 0).Encode(),          // D
            M(0, 0, 1, 2).Encode(),          // L'
            M(1, 0, 0, 2).Encode()           // D'
        });

        // ===== SHORT TRIGGERS =====

        // R2 U2 (quick layer adjustment)
        _buildingBlocks.Add(new[]
        {
            M(0, lastSlice, 1, 1).Encode(),  // R2
            M(1, lastSlice, 0, 1).Encode()   // U2
        });

        // F2 R2 (different plane adjustment)
        _buildingBlocks.Add(new[]
        {
            M(2, lastSlice, 1, 1).Encode(),  // F2
            M(0, lastSlice, 1, 1).Encode()   // R2
        });
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
