using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Pattern mutation - inserts known algorithm patterns into the chromosome.
///
/// For 3D cubes, uses classic speedcubing algorithms:
/// - T-perm, Y-perm (PLL algorithms)
/// - Sune, Anti-Sune (OLL algorithms)
/// - Sexy move (R U R' U'), Sledgehammer (R' F R F')
///
/// For 4D+ cubes, uses generalized commutator patterns that work
/// across dimensions, adapted to the available planes.
///
/// These patterns are known to perform useful permutations and
/// can help the GA discover effective move sequences faster.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class PatternMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cached patterns for current dimension
    private static int _cachedN = -1;
    private static int _cachedSize = -1;
    private static List<int[]>? _patterns;

    public void Mutate(T chromosome, Random rng)
    {
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 4) return;

        // Build patterns cache if needed
        int n = TAffine.N;
        int size = TRubikCube.Size;
        if (_cachedN != n || _cachedSize != size)
        {
            BuildPatternsCache(n, size);
        }

        if (_patterns == null || _patterns.Count == 0) return;

        // Select a random pattern
        int[] pattern = _patterns[rng.Next(_patterns.Count)];

        // Find a position where the pattern fits
        if (pattern.Length > length) return;

        int startIdx = rng.Next(length - pattern.Length + 1);

        // Insert the pattern
        for (int i = 0; i < pattern.Length; i++)
        {
            chromosome.Genes[startIdx + i] = pattern[i];
        }
    }

    /// <summary>
    /// Build cache of algorithm patterns for current dimension.
    /// </summary>
    private static void BuildPatternsCache(int n, int size)
    {
        _cachedN = n;
        _cachedSize = size;
        _patterns = new List<int[]>();

        if (n == 3)
        {
            // Classic 3D speedcubing patterns
            Build3DPatterns(size);
        }
        else
        {
            // Generalized patterns for N dimensions
            BuildNDPatterns(n, size);
        }
    }

    /// <summary>
    /// Build classic 3D speedcubing algorithm patterns.
    /// In 3D: Axis 0=X, 1=Y, 2=Z; Planes: 0=(0,1), 1=(0,2), 2=(1,2)
    /// Standard mapping: R=Axis0, U=Axis1, F=Axis2
    /// </summary>
    private static void Build3DPatterns(int size)
    {
        if (_patterns == null) return;

        int lastSlice = size - 1;

        // Helper to create a move
        TMove CreateMove(int axis, int slice, int plane, int angle)
        {
            return new TMove { Axis = axis, Slice = slice, Plane = plane, Angle = angle };
        }

        // Sexy move: R U R' U' (very common trigger)
        // R = Axis 0, Slice last, Plane 1 (XZ), Angle 0 (90°)
        // U = Axis 1, Slice last, Plane 0 (XY), Angle 0 (90°)
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(1, lastSlice, 0, 2).Encode()   // U'
        });

        // Inverse sexy: U R U' R'
        _patterns.Add(new[]
        {
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 2).Encode(),  // U'
            CreateMove(0, lastSlice, 1, 2).Encode()   // R'
        });

        // Sledgehammer: R' F R F'
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(2, lastSlice, 1, 0).Encode(),  // F
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(2, lastSlice, 1, 2).Encode()   // F'
        });

        // Hedgeslammer: F R' F' R
        _patterns.Add(new[]
        {
            CreateMove(2, lastSlice, 1, 0).Encode(),  // F
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(2, lastSlice, 1, 2).Encode(),  // F'
            CreateMove(0, lastSlice, 1, 0).Encode()   // R
        });

        // Sune: R U R' U R U2 R' (6 moves, common OLL)
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 1).Encode(),  // U2
            CreateMove(0, lastSlice, 1, 2).Encode()   // R'
        });

        // Anti-Sune: R U2 R' U' R U' R'
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 1).Encode(),  // U2
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(1, lastSlice, 0, 2).Encode(),  // U'
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 2).Encode(),  // U'
            CreateMove(0, lastSlice, 1, 2).Encode()   // R'
        });

        // Double sexy: (R U R' U')2
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(1, lastSlice, 0, 2).Encode(),  // U'
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(1, lastSlice, 0, 2).Encode()   // U'
        });

        // T-perm (simplified): R U R' U' R' F R2 U' R' U' R U R' F'
        // This is a 14-move PLL, let's use a shorter version
        // Short T-perm trigger: R U R' F' (part of T-perm setup)
        _patterns.Add(new[]
        {
            CreateMove(0, lastSlice, 1, 0).Encode(),  // R
            CreateMove(1, lastSlice, 0, 0).Encode(),  // U
            CreateMove(0, lastSlice, 1, 2).Encode(),  // R'
            CreateMove(2, lastSlice, 1, 2).Encode()   // F'
        });

        // Left-handed variants (using L instead of R)
        // L U' L' U (mirror of sexy)
        _patterns.Add(new[]
        {
            CreateMove(0, 0, 1, 0).Encode(),          // L
            CreateMove(1, lastSlice, 0, 2).Encode(),  // U'
            CreateMove(0, 0, 1, 2).Encode(),          // L'
            CreateMove(1, lastSlice, 0, 0).Encode()   // U
        });
    }

    /// <summary>
    /// Build generalized patterns for N-dimensional cubes.
    /// Uses commutator-based patterns that work across dimensions.
    /// </summary>
    private static void BuildNDPatterns(int n, int size)
    {
        if (_patterns == null) return;

        int numPlanes = TAffine.Planes.Length;
        int lastSlice = size - 1;

        // Generate commutator patterns: A B A' B' for various plane combinations
        for (int p1 = 0; p1 < Math.Min(numPlanes, 4); p1++)
        {
            for (int p2 = p1 + 1; p2 < Math.Min(numPlanes, 4); p2++)
            {
                // Find valid axes for these planes
                int[] axes1 = TAffine.Planes[p1];
                int[] axes2 = TAffine.Planes[p2];

                // Use axis perpendicular to the plane for the move
                int axis1 = FindPerpendicularAxis(axes1, n);
                int axis2 = FindPerpendicularAxis(axes2, n);

                if (axis1 >= 0 && axis2 >= 0)
                {
                    // Basic commutator: A B A' B'
                    var moveA = new TMove { Axis = axis1, Slice = lastSlice, Plane = p1, Angle = 0 };
                    var moveB = new TMove { Axis = axis2, Slice = lastSlice, Plane = p2, Angle = 0 };
                    var moveAInv = new TMove { Axis = axis1, Slice = lastSlice, Plane = p1, Angle = 2 };
                    var moveBInv = new TMove { Axis = axis2, Slice = lastSlice, Plane = p2, Angle = 2 };

                    _patterns.Add(new[]
                    {
                        moveA.Encode(),
                        moveB.Encode(),
                        moveAInv.Encode(),
                        moveBInv.Encode()
                    });

                    // Extended pattern: A B A' B' A B A' B' (double commutator)
                    _patterns.Add(new[]
                    {
                        moveA.Encode(),
                        moveB.Encode(),
                        moveAInv.Encode(),
                        moveBInv.Encode(),
                        moveA.Encode(),
                        moveB.Encode(),
                        moveAInv.Encode(),
                        moveBInv.Encode()
                    });
                }
            }
        }

        // Add conjugation patterns: A B A' for various combinations
        for (int p = 0; p < Math.Min(numPlanes, 3); p++)
        {
            int[] axes = TAffine.Planes[p];
            int axis = FindPerpendicularAxis(axes, n);

            if (axis >= 0)
            {
                var moveA = new TMove { Axis = axis, Slice = lastSlice, Plane = p, Angle = 0 };
                var moveA2 = new TMove { Axis = axis, Slice = lastSlice, Plane = p, Angle = 1 }; // 180°
                var moveAInv = new TMove { Axis = axis, Slice = lastSlice, Plane = p, Angle = 2 };

                // Find a different plane for B
                int p2 = (p + 1) % numPlanes;
                int[] axes2 = TAffine.Planes[p2];
                int axis2 = FindPerpendicularAxis(axes2, n);

                if (axis2 >= 0)
                {
                    var moveB = new TMove { Axis = axis2, Slice = lastSlice, Plane = p2, Angle = 0 };

                    // Conjugation: A B A'
                    _patterns.Add(new[]
                    {
                        moveA.Encode(),
                        moveB.Encode(),
                        moveAInv.Encode()
                    });

                    // Extended conjugation: A2 B A2
                    _patterns.Add(new[]
                    {
                        moveA2.Encode(),
                        moveB.Encode(),
                        moveA2.Encode()
                    });
                }
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
