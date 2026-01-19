using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Pattern mutation - inserts known algorithm patterns into the chromosome.
///
/// For 3D cubes, includes the FULL PLL set (21 algorithms) plus OLL and triggers:
///
/// PLL (Permutation of Last Layer):
/// - Edge-only: Ua, Ub, H, Z
/// - Corner-only: Aa, Ab, E
/// - Adjacent corner swap: T, F, Ja, Jb, Ra, Rb
/// - Diagonal corner swap: Y, V, Na, Nb
/// - G-perms (corner+edge cycles): Ga, Gb, Gc, Gd
///
/// OLL (Orientation of Last Layer):
/// - Sune, Anti-Sune
///
/// Basic triggers:
/// - Sexy move (R U R' U'), Sledgehammer (R' F R F')
/// - Hedgeslammer, Left sexy, Double sexy
/// - Corner twist, slot inserts
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
    /// Standard mapping: R=Axis0/last, L=Axis0/0, U=Axis1/last, D=Axis1/0, F=Axis2/last, B=Axis2/0
    /// </summary>
    private static void Build3DPatterns(int size)
    {
        if (_patterns == null) return;

        int s = size - 1; // Last slice index

        // ============================================================
        // MOVE ENCODING HELPERS
        // Angles: 0 = 90° (clockwise), 1 = 180°, 2 = -90° (counter-clockwise)
        // ============================================================
        int R = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 0 }.Encode();
        int Ri = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 2 }.Encode();  // R'
        int R2 = new TMove { Axis = 0, Slice = s, Plane = 1, Angle = 1 }.Encode();

        int L = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 2 }.Encode();   // L (opposite direction)
        int Li = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 0 }.Encode();  // L'
        int L2 = new TMove { Axis = 0, Slice = 0, Plane = 1, Angle = 1 }.Encode();

        int U = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 0 }.Encode();
        int Ui = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 2 }.Encode();  // U'
        int U2 = new TMove { Axis = 1, Slice = s, Plane = 0, Angle = 1 }.Encode();

        int D = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 2 }.Encode();   // D (opposite direction)
        int Di = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 0 }.Encode();  // D'
        int D2 = new TMove { Axis = 1, Slice = 0, Plane = 0, Angle = 1 }.Encode();

        int F = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 0 }.Encode();
        int Fi = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 2 }.Encode();  // F'
        int F2 = new TMove { Axis = 2, Slice = s, Plane = 1, Angle = 1 }.Encode();

        int B = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 2 }.Encode();   // B (opposite direction)
        int Bi = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 0 }.Encode();  // B'
        int B2 = new TMove { Axis = 2, Slice = 0, Plane = 1, Angle = 1 }.Encode();

        // ============================================================
        // BASIC TRIGGERS (commonly used subsequences)
        // ============================================================

        // Sexy move: R U R' U'
        _patterns.Add(new[] { R, U, Ri, Ui });

        // Inverse sexy: U R U' R'
        _patterns.Add(new[] { U, R, Ui, Ri });

        // Sledgehammer: R' F R F'
        _patterns.Add(new[] { Ri, F, R, Fi });

        // Hedgeslammer: F R' F' R
        _patterns.Add(new[] { F, Ri, Fi, R });

        // Double sexy: (R U R' U')2
        _patterns.Add(new[] { R, U, Ri, Ui, R, U, Ri, Ui });

        // Left sexy: L' U' L U
        _patterns.Add(new[] { Li, Ui, L, U });

        // ============================================================
        // OLL ALGORITHMS (Last Layer Orientation)
        // ============================================================

        // Sune: R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri });

        // Anti-Sune: R U2 R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // ============================================================
        // PLL ALGORITHMS (Last Layer Permutation) - Full Set of 21
        // ============================================================

        // --- EDGE-ONLY PLLs (4) ---

        // 1. Ua-perm (edge 3-cycle): R U' R U R U R U' R' U' R2
        _patterns.Add(new[] { R, Ui, R, U, R, U, R, Ui, Ri, Ui, R2 });

        // 2. Ub-perm (edge 3-cycle): R2 U R U R' U' R' U' R' U R'
        _patterns.Add(new[] { R2, U, R, U, Ri, Ui, Ri, Ui, Ri, U, Ri });

        // 3. H-perm (edge double swap): R2 U2 R U2 R2 U2 R2 U2 R U2 R2
        _patterns.Add(new[] { R2, U2, R, U2, R2, U2, R2, U2, R, U2, R2 });

        // 4. Z-perm (edge adjacent swap): R' U' R U' R U R U' R' U R U R2 U' R'
        _patterns.Add(new[] { Ri, Ui, R, Ui, R, U, R, Ui, Ri, U, R, U, R2, Ui, Ri });

        // --- CORNER-ONLY PLLs (2) ---

        // 5. Aa-perm (corner 3-cycle): R' F R' B2 R F' R' B2 R2
        _patterns.Add(new[] { Ri, F, Ri, B2, R, Fi, Ri, B2, R2 });

        // 6. Ab-perm (corner 3-cycle): R2 B2 R F R' B2 R F' R
        _patterns.Add(new[] { R2, B2, R, F, Ri, B2, R, Fi, R });

        // 7. E-perm (diagonal corner swap): R2 U R' U' R' U' R' U R' U2
        // Alternative: R B' R' F R B R' F' R B R' F R B' R' F' (16 moves but no rotation)
        _patterns.Add(new[] { R, Bi, Ri, F, R, B, Ri, Fi, R, B, Ri, F, R, Bi, Ri, Fi });

        // --- ADJACENT CORNER SWAP PLLs (6) ---

        // 8. T-perm: R U R' U' R' F R2 U' R' U' R U R' F'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R2, Ui, Ri, Ui, R, U, Ri, Fi });

        // 9. F-perm: R' U' F' R U R' U' R' F R2 U' R' U' R U R' U R
        _patterns.Add(new[] { Ri, Ui, Fi, R, U, Ri, Ui, Ri, F, R2, Ui, Ri, Ui, R, U, Ri, U, R });

        // 10. Ja-perm: R' U L' U2 R U' R' U2 R L
        _patterns.Add(new[] { Ri, U, Li, U2, R, Ui, Ri, U2, R, L });

        // 11. Jb-perm: R U R' F' R U R' U' R' F R2 U' R'
        _patterns.Add(new[] { R, U, Ri, Fi, R, U, Ri, Ui, Ri, F, R2, Ui, Ri });

        // 12. Ra-perm: R U' R' U' R U R D R' U' R D' R' U2 R'
        _patterns.Add(new[] { R, Ui, Ri, Ui, R, U, R, D, Ri, Ui, R, Di, Ri, U2, Ri });

        // 13. Rb-perm: R' U2 R U2 R' F R U R' U' R' F' R2
        _patterns.Add(new[] { Ri, U2, R, U2, Ri, F, R, U, Ri, Ui, Ri, Fi, R2 });

        // --- DIAGONAL CORNER SWAP PLLs (4) ---

        // 14. Y-perm: F R U' R' U' R U R' F' R U R' U' R' F R F'
        _patterns.Add(new[] { F, R, Ui, Ri, Ui, R, U, Ri, Fi, R, U, Ri, Ui, Ri, F, R, Fi });

        // 15. V-perm: R' U R' U' R D' R' D R' U D' R2 U' R2 D R2
        _patterns.Add(new[] { Ri, U, Ri, Ui, R, Di, Ri, D, Ri, U, Di, R2, Ui, R2, D, R2 });

        // 16. Na-perm: R U R' U R U R' F' R U R' U' R' F R2 U' R' U2 R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, U, Ri, Fi, R, U, Ri, Ui, Ri, F, R2, Ui, Ri, U2, R, Ui, Ri });

        // 17. Nb-perm: R' U R U' R' F' U' F R U R' F R' F' R U' R
        _patterns.Add(new[] { Ri, U, R, Ui, Ri, Fi, Ui, F, R, U, Ri, F, Ri, Fi, R, Ui, R });

        // --- G-PERMS (Corner + Edge 3-cycles) (4) ---

        // 18. Ga-perm: R2 U R' U R' U' R U' R2 U' D R' U R D'
        _patterns.Add(new[] { R2, U, Ri, U, Ri, Ui, R, Ui, R2, Ui, D, Ri, U, R, Di });

        // 19. Gb-perm: R' U' R U D' R2 U R' U R U' R U' R2 D
        _patterns.Add(new[] { Ri, Ui, R, U, Di, R2, U, Ri, U, R, Ui, R, Ui, R2, D });

        // 20. Gc-perm: R2 U' R U' R U R' U R2 U D' R U' R' D
        _patterns.Add(new[] { R2, Ui, R, Ui, R, U, Ri, U, R2, U, Di, R, Ui, Ri, D });

        // 21. Gd-perm: R U R' U' D R2 U' R U' R' U R' U R2 D'
        _patterns.Add(new[] { R, U, Ri, Ui, D, R2, Ui, R, Ui, Ri, U, Ri, U, R2, Di });

        // ============================================================
        // ADDITIONAL USEFUL TRIGGERS AND PATTERNS
        // ============================================================

        // RUF trigger: R U F
        _patterns.Add(new[] { R, U, F });

        // RUL trigger: R U L
        _patterns.Add(new[] { R, U, L });

        // Back-slot insert: R' U' R U
        _patterns.Add(new[] { Ri, Ui, R, U });

        // Front-slot insert: F U F' U'
        _patterns.Add(new[] { F, U, Fi, Ui });

        // Corner twist: R' D' R D
        _patterns.Add(new[] { Ri, Di, R, D });

        // Double corner twist: (R' D' R D)2
        _patterns.Add(new[] { Ri, Di, R, D, Ri, Di, R, D });
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
