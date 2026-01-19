using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Pattern mutation - inserts known algorithm patterns into the chromosome.
///
/// For 3D cubes, includes comprehensive speedcubing algorithm sets:
///
/// PLL (Permutation of Last Layer) - 21 algorithms:
/// - Edge-only: Ua, Ub, H, Z (4)
/// - Corner-only: Aa, Ab, E (3)
/// - Adjacent corner swap: T, F, Ja, Jb, Ra, Rb (6)
/// - Diagonal corner swap: Y, V, Na, Nb (4)
/// - G-perms (corner+edge cycles): Ga, Gb, Gc, Gd (4)
///
/// OLL (Orientation of Last Layer) - 57 algorithms:
/// - All Edges Oriented (Cross): 21-27 (7)
/// - T-shapes, Squares, C-shapes, W-shapes, Corners Oriented (10)
/// - P-shapes, I-shapes, Fish shapes, Knight Move (16)
/// - Awkward shapes, L-shapes, Lightning Bolt, Dot cases (26)
///
/// COLL (Corners of Last Layer) - 42 algorithms:
/// - H, Pi, U (Sune), T, L, AS (Anti-Sune), S cases (7 categories × 6 each)
///
/// ZBLL Subset (Zborowski-Bruchem) - 30 algorithms:
/// - Most common T, U, L, H, Pi, S, AS cases
///
/// Winter Variation (WV) - 27 algorithms:
/// - Orient corners while inserting last F2L pair
///
/// VLS (Valk Last Slot) - 24 algorithms:
/// - Orient edges while inserting last F2L pair
/// - Dot, Line, L-shape, Cross cases
///
/// Basic triggers:
/// - Sexy move, Sledgehammer, Hedgeslammer
/// - Left sexy, Double sexy, Corner twist
///
/// For 4D+ cubes, uses generalized commutator patterns that work
/// across dimensions, adapted to the available planes.
///
/// Total patterns for 3D: ~210 (21 PLL + 57 OLL + 42 COLL + 30 ZBLL + 27 WV + 24 VLS + triggers)
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
        // OLL ALGORITHMS (Last Layer Orientation) - Full Set of 57
        // ============================================================
        // Note: Some algorithms use f/r (wide moves). For 3x3, we encode
        // f = F + S (where S is middle slice following F)
        // r = R + M' (where M is middle slice following L)
        // For simplicity, we use alternative non-wide algorithms where possible.

        // Middle slice moves (for 3x3, slice index = 1)
        int M = size >= 3 ? new TMove { Axis = 0, Slice = 1, Plane = 1, Angle = 2 }.Encode() : R;  // M follows L
        int Mi = size >= 3 ? new TMove { Axis = 0, Slice = 1, Plane = 1, Angle = 0 }.Encode() : Ri;
        int M2 = size >= 3 ? new TMove { Axis = 0, Slice = 1, Plane = 1, Angle = 1 }.Encode() : R2;

        int S = size >= 3 ? new TMove { Axis = 2, Slice = 1, Plane = 1, Angle = 0 }.Encode() : F;  // S follows F
        int Si = size >= 3 ? new TMove { Axis = 2, Slice = 1, Plane = 1, Angle = 2 }.Encode() : Fi;

        // --- ALL EDGES ORIENTED (Cross on top) - 7 cases ---

        // OLL 21 (H/Double Sune): R U R' U R U' R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // OLL 22 (Pi): R U2 R2 U' R2 U' R2 U2 R
        _patterns.Add(new[] { R, U2, R2, Ui, R2, Ui, R2, U2, R });

        // OLL 23 (Headlights): R2 D R' U2 R D' R' U2 R'
        _patterns.Add(new[] { R2, D, Ri, U2, R, Di, Ri, U2, Ri });

        // OLL 24 (Chameleon): F R' F' R U R U' R' (alternative without wide moves)
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, Ui, Ri });

        // OLL 25 (Bowtie): F' R U R' U' R' F R (alternative)
        _patterns.Add(new[] { Fi, R, U, Ri, Ui, Ri, F, R });

        // OLL 26 (Antisune): R U2 R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // OLL 27 (Sune): R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri });

        // --- T-SHAPES - 2 cases ---

        // OLL 33: R U R' U' R' F R F'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi });

        // OLL 45: F R U R' U' F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi });

        // --- SQUARES - 2 cases ---

        // OLL 5: R' U2 R U R' U R (alternative to r' U2 R U R' U r)
        _patterns.Add(new[] { Ri, U2, R, U, Ri, U, R });

        // OLL 6: R U2 R' U' R U' R' (same as antisune, different recognition)
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // --- C-SHAPES - 2 cases ---

        // OLL 34: R U R2 U' R' F R U R U' F'
        _patterns.Add(new[] { R, U, R2, Ui, Ri, F, R, U, R, Ui, Fi });

        // OLL 46: R' U' R' F R F' U R
        _patterns.Add(new[] { Ri, Ui, Ri, F, R, Fi, U, R });

        // --- W-SHAPES - 2 cases ---

        // OLL 36: L' U' L U' L' U L U L F' L' F
        _patterns.Add(new[] { Li, Ui, L, Ui, Li, U, L, U, L, Fi, Li, F });

        // OLL 38: R U R' U R U' R' U' R' F R F'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, Ui, Ri, F, R, Fi });

        // --- CORNERS ORIENTED - 2 cases ---

        // OLL 28: R U R' U' M' U R U' R' (with M = r' R)
        // Alternative: R U R' U' R' F R F' U' F R U R' U' F'
        _patterns.Add(new[] { R, U, Ri, Ui, Mi, U, R, Ui, Ri });

        // OLL 57 (H): R U R' U' M' U R U' R' U' M
        // Alternative without M: R U R' U' R' F R F' R U R' U' R' F R F'
        _patterns.Add(new[] { R, U, Ri, Ui, Mi, U, R, Ui, Ri, Ui, M });

        // --- P-SHAPES - 4 cases ---

        // OLL 31: R' U' F U R U' R' F' R
        _patterns.Add(new[] { Ri, Ui, F, U, R, Ui, Ri, Fi, R });

        // OLL 32: R U B' U' R' U R B R'
        _patterns.Add(new[] { R, U, Bi, Ui, Ri, U, R, B, Ri });

        // OLL 43: F' U' L' U L F (alternative to f' L' U' L U f)
        _patterns.Add(new[] { Fi, Ui, Li, U, L, F });

        // OLL 44: F U R U' R' F' (alternative to f R U R' U' f')
        _patterns.Add(new[] { F, U, R, Ui, Ri, Fi });

        // --- I-SHAPES (LINE) - 4 cases ---

        // OLL 51: F U R U' R' U R U' R' F'
        _patterns.Add(new[] { F, U, R, Ui, Ri, U, R, Ui, Ri, Fi });

        // OLL 52: R U R' U R U' B U' B' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, B, Ui, Bi, Ri });

        // OLL 55: R' F R U R U' R2 F' R2 U' R' U R U R'
        _patterns.Add(new[] { Ri, F, R, U, R, Ui, R2, Fi, R2, Ui, Ri, U, R, U, Ri });

        // OLL 56: F R U R' U' R F' R U R' U' R' F R F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, R, Fi, R, U, Ri, Ui, Ri, F, R, Fi });

        // --- FISH SHAPES - 4 cases ---

        // OLL 9: R U R' U' R' F R2 U R' U' F'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R2, U, Ri, Ui, Fi });

        // OLL 10: R U R' U R' F R F' R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, Ri, F, R, Fi, R, U2, Ri });

        // OLL 35: R U2 R2 F R F' R U2 R'
        _patterns.Add(new[] { R, U2, R2, F, R, Fi, R, U2, Ri });

        // OLL 37: F R U' R' U' R U R' F'
        _patterns.Add(new[] { F, R, Ui, Ri, Ui, R, U, Ri, Fi });

        // --- KNIGHT MOVE SHAPES - 4 cases ---

        // OLL 13: F U R U' R2 F' R U R U' R'
        _patterns.Add(new[] { F, U, R, Ui, R2, Fi, R, U, R, Ui, Ri });

        // OLL 14: R' F R U R' F' R F U' F'
        _patterns.Add(new[] { Ri, F, R, U, Ri, Fi, R, F, Ui, Fi });

        // OLL 15: R' F' R L' U' L U R' F R (alternative without wide r)
        _patterns.Add(new[] { Ri, Fi, R, Li, Ui, L, U, Ri, F, R });

        // OLL 16: R U R' L U L' U' R U' R' (alternative without wide r)
        _patterns.Add(new[] { R, U, Ri, L, U, Li, Ui, R, Ui, Ri });

        // --- AWKWARD SHAPES - 4 cases ---

        // OLL 29: R U R' U' R U' R' F' U' F R U R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri, Fi, Ui, F, R, U, Ri });

        // OLL 30: F U R U2 R' U' R U2 R' U' F'
        _patterns.Add(new[] { F, U, R, U2, Ri, Ui, R, U2, Ri, Ui, Fi });

        // OLL 41: R U R' U R U2 R' F R U R' U' F'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri, F, R, U, Ri, Ui, Fi });

        // OLL 42: R' U' R U' R' U2 R F R U R' U' F'
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U2, R, F, R, U, Ri, Ui, Fi });

        // --- L-SHAPES - 6 cases ---

        // OLL 47: F' L' U' L U L' U' L U F
        _patterns.Add(new[] { Fi, Li, Ui, L, U, Li, Ui, L, U, F });

        // OLL 48: F R U R' U' R U R' U' F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, R, U, Ri, Ui, Fi });

        // OLL 49: R B' R2 F R2 B R2 F' R (alternative without wide r)
        _patterns.Add(new[] { R, Bi, R2, F, R2, B, R2, Fi, R });

        // OLL 50: R B' R B R2 U2 F R' F' R (alternative without wide r)
        _patterns.Add(new[] { R, Bi, R, B, R2, U2, F, Ri, Fi, R });

        // OLL 53: F R U R' U' F' R U R' U' R' F R F' (alternative)
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi, R, U, Ri, Ui, Ri, F, R, Fi });

        // OLL 54: R U R' U' R' F R F' R U R' U' R' F R F' (alternative)
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi, R, U, Ri, Ui, Ri, F, R, Fi });

        // --- LIGHTNING BOLT SHAPES - 8 cases ---

        // OLL 7: R U R' U R U2 R' (same as Sune for lightning bolt case)
        // Actually: F R U R' U' F' U F R U R' U' F' (alternative)
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi, U, F, R, U, Ri, Ui, Fi });

        // OLL 8: R' U' R U' R' U2 R (mirror of Sune)
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U2, R });

        // OLL 11: F' L' U' L U F U' F' L' U' L U F (alternative without M)
        _patterns.Add(new[] { Fi, Li, Ui, L, U, F, Ui, Fi, Li, Ui, L, U, F });

        // OLL 12: F R U R' U' F' U F R U R' U' F' (with adjusted ending)
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi, U, F, R, U, Ri, Ui, Fi });

        // OLL 39: L F' L' U' L U F U' L'
        _patterns.Add(new[] { L, Fi, Li, Ui, L, U, F, Ui, Li });

        // OLL 40: R' F R U R' U' F' U R
        _patterns.Add(new[] { Ri, F, R, U, Ri, Ui, Fi, U, R });

        // --- DOT CASES (No Edges Oriented) - 8 cases ---

        // OLL 1: R U2 R2 F R F' U2 R' F R F'
        _patterns.Add(new[] { R, U2, R2, F, R, Fi, U2, Ri, F, R, Fi });

        // OLL 2: F R U R' U' F' U2 F' L' U' L U F (alternative without f)
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi, U2, Fi, Li, Ui, L, U, F });

        // OLL 3: F' L' U' L U F U' F' L' U' L U F (alternative without f)
        _patterns.Add(new[] { Fi, Li, Ui, L, U, F, Ui, Fi, Li, Ui, L, U, F });

        // OLL 4: F' L' U' L U F U F' L' U' L U F (alternative without f)
        _patterns.Add(new[] { Fi, Li, Ui, L, U, F, U, Fi, Li, Ui, L, U, F });

        // OLL 17: R U R' U R' F R F' U2 R' F R F'
        _patterns.Add(new[] { R, U, Ri, U, Ri, F, R, Fi, U2, Ri, F, R, Fi });

        // OLL 18: R U2 R2 F R F' U2 M' U R U' R' (alternative)
        _patterns.Add(new[] { R, U2, R2, F, R, Fi, U2, Mi, U, R, Ui, Ri });

        // OLL 19: R' U2 F R U R' U' F2 U2 F R (alternative)
        _patterns.Add(new[] { Ri, U2, F, R, U, Ri, Ui, F2, U2, F, R });

        // OLL 20: R U R' U' M' U' R U R' U M U R U2 R' (alternative without wide r)
        // Simpler: R U R' U R U' R' U R U2 R' U' R U R' U' R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri, Ui, R, U, Ri, Ui, R, Ui, Ri });

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
        // COLL ALGORITHMS (Corner Orientation + Permutation) - 42 cases
        // Preserves edge orientation while solving corners
        // ============================================================

        // --- COLL H (All Corners Twisted Same Direction) - 4 cases ---

        // H1 (edges solved): R U R' U R U' R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // H2: R U2 R' U' R U R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, U, Ri, Ui, R, Ui, Ri });

        // H3: F R U R' U' R U R' U' R U R' U' F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, R, U, Ri, Ui, R, U, Ri, Ui, Fi });

        // H4: R U R' U R U L' U R' U' L
        _patterns.Add(new[] { R, U, Ri, U, R, U, Li, U, Ri, Ui, L });

        // --- COLL Pi (Two Adjacent Corners Twisted CW, Two CCW) - 6 cases ---

        // Pi1: R U2 R' U' R U R' U2 R' F R F'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, U, Ri, U2, Ri, F, R, Fi });

        // Pi2: F R' F' R U2 R U' R' U R U2 R'
        _patterns.Add(new[] { F, Ri, Fi, R, U2, R, Ui, Ri, U, R, U2, Ri });

        // Pi3: R' U' R' F R F' R U' R' U2 R
        _patterns.Add(new[] { Ri, Ui, Ri, F, R, Fi, R, Ui, Ri, U2, R });

        // Pi4: R U2 R' U' R U R' U' R U R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, U, Ri, Ui, R, U, Ri, Ui, R, Ui, Ri });

        // Pi5: R' F' R U R' U' R' F R2 U' R' U2 R
        _patterns.Add(new[] { Ri, Fi, R, U, Ri, Ui, Ri, F, R2, Ui, Ri, U2, R });

        // Pi6: R U R' U' R' F R2 U R' U' R U R' U' F'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R2, U, Ri, Ui, R, U, Ri, Ui, Fi });

        // --- COLL U (Sune Shape - One Corner Twisted) - 6 cases ---

        // U1: R U R' U R U2 R' (basic Sune - all corners same permutation)
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri });

        // U2: R U R' U R U' R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // U3: R2 D R' U2 R D' R' U2 R'
        _patterns.Add(new[] { R2, D, Ri, U2, R, Di, Ri, U2, Ri });

        // U4: R2 D' R U2 R' D R U2 R
        _patterns.Add(new[] { R2, Di, R, U2, Ri, D, R, U2, R });

        // U5: F R U' R' U R U R' U R U' R' F'
        _patterns.Add(new[] { F, R, Ui, Ri, U, R, U, Ri, U, R, Ui, Ri, Fi });

        // U6: R' U' R U' R' U R U' R' U2 R
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U, R, Ui, Ri, U2, R });

        // --- COLL T (T-Shape - Two Opposite Twisted CW) - 6 cases ---

        // T1: R U R' U' R' F R F' (T-perm ending)
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi });

        // T2: L' U' L U L F' L' F
        _patterns.Add(new[] { Li, Ui, L, U, L, Fi, Li, F });

        // T3: R U2 R' U' R U' R2 U2 R U R' U R
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, R2, U2, R, U, Ri, U, R });

        // T4: R U R D R' U R D' R2
        _patterns.Add(new[] { R, U, R, D, Ri, U, R, Di, R2 });

        // T5: R' U R U2 R' L' U R U' L
        _patterns.Add(new[] { Ri, U, R, U2, Ri, Li, U, R, Ui, L });

        // T6: L' U' L U2 L R U' L' U R'
        _patterns.Add(new[] { Li, Ui, L, U2, L, R, Ui, Li, U, Ri });

        // --- COLL L (L-Shape - Adjacent Corners Twisted) - 6 cases ---

        // L1: F R U' R' U' R U R' F'
        _patterns.Add(new[] { F, R, Ui, Ri, Ui, R, U, Ri, Fi });

        // L2: F' L' U L U L' U' L F
        _patterns.Add(new[] { Fi, Li, U, L, U, Li, Ui, L, F });

        // L3: R' U' R U R' F' R U R' U' R' F R2
        _patterns.Add(new[] { Ri, Ui, R, U, Ri, Fi, R, U, Ri, Ui, Ri, F, R2 });

        // L4: R U R' U' R U' R' F' U' F R U R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri, Fi, Ui, F, R, U, Ri });

        // L5: F R' F' R U2 R U2 R'
        _patterns.Add(new[] { F, Ri, Fi, R, U2, R, U2, Ri });

        // L6: F' L F L' U2 L' U2 L
        _patterns.Add(new[] { Fi, L, F, Li, U2, Li, U2, L });

        // --- COLL AS (Anti-Sune Shape) - 6 cases ---

        // AS1: R U2 R' U' R U' R' (basic Anti-Sune)
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // AS2: R' U' R U' R' U R U' R' U2 R
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U, R, Ui, Ri, U2, R });

        // AS3: L' U R U' L U R'
        _patterns.Add(new[] { Li, U, R, Ui, L, U, Ri });

        // AS4: R U' L' U R' U' L
        _patterns.Add(new[] { R, Ui, Li, U, Ri, Ui, L });

        // AS5: F' R U R' U' R' F R U R U' R'
        _patterns.Add(new[] { Fi, R, U, Ri, Ui, Ri, F, R, U, R, Ui, Ri });

        // AS6: R U R' U R U2 R' U' R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri, Ui, R, U, Ri, U, R, U2, Ri });

        // --- COLL S (Sune-like, different angles) - 6 cases ---

        // S1: R U R' U' R' F R F' R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi, R, U, Ri, U, R, U2, Ri });

        // S2: L' U' L U L F' L' F L' U' L U' L' U2 L
        _patterns.Add(new[] { Li, Ui, L, U, L, Fi, Li, F, Li, Ui, L, Ui, Li, U2, L });

        // S3: R U R' U R U2 R' U R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri, U, R, U, Ri, U, R, U2, Ri });

        // S4: R U R' U R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U, Ri, U, R, U2, Ri });

        // S5: F R' F' R U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, Ui, Ri });

        // S6: F' L F L' U' L' U L
        _patterns.Add(new[] { Fi, L, F, Li, Ui, Li, U, L });

        // ============================================================
        // ZBLL SUBSET (Most Common Cases) - ~30 algorithms
        // Full ZBLL has 493 algorithms, here we include the most useful
        // ============================================================

        // --- ZBLL T Cases (T-shape, edges oriented) ---

        // ZBLL T1: R U R' U' R' F R2 U' R' U' R U R' F' (T-perm)
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R2, Ui, Ri, Ui, R, U, Ri, Fi });

        // ZBLL T2: R U R' U' R' F R F' U2 R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi, U2, R, U, Ri, U, R, U2, Ri });

        // ZBLL T3: R' U R U2 L' R' U R U' L
        _patterns.Add(new[] { Ri, U, R, U2, Li, Ri, U, R, Ui, L });

        // ZBLL T4: R U2 R' U' R U' R' L U' L' U2 L U' L'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri, L, Ui, Li, U2, L, Ui, Li });

        // --- ZBLL U Cases (Sune shape, edges oriented) ---

        // ZBLL U1: R' U' R U' R' U2 R2 U R' U R U2 R'
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U2, R2, U, Ri, U, R, U2, Ri });

        // ZBLL U2: R U2 R' U' R U R' U' R U R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, U, Ri, Ui, R, U, Ri, Ui, R, Ui, Ri });

        // ZBLL U3: R' U L U' R U L' U' R' U L U' R U L'
        _patterns.Add(new[] { Ri, U, L, Ui, R, U, Li, Ui, Ri, U, L, Ui, R, U, Li });

        // ZBLL U4: R U' L' U R' U' L U R U' L' U R' U' L
        _patterns.Add(new[] { R, Ui, Li, U, Ri, Ui, L, U, R, Ui, Li, U, Ri, Ui, L });

        // --- ZBLL L Cases (L-shape, edges oriented) ---

        // ZBLL L1: F R U' R' U R U R' U R U' R' F'
        _patterns.Add(new[] { F, R, Ui, Ri, U, R, U, Ri, U, R, Ui, Ri, Fi });

        // ZBLL L2: R U R' U R U' R' U R U' R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // ZBLL L3: R U2 R' U R U R' U R U' R' U R U2 R'
        _patterns.Add(new[] { R, U2, Ri, U, R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // ZBLL L4: F' L' U L U' L' U' L U' L' U L F
        _patterns.Add(new[] { Fi, Li, U, L, Ui, Li, Ui, L, Ui, Li, U, L, F });

        // --- ZBLL H Cases (H-shape, edges oriented) ---

        // ZBLL H1: R U R' U R U' R' U R U2 R' (double Sune)
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, U2, Ri });

        // ZBLL H2: R U2 R' U' R U' R' U2 R U R' U R U2 R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri, U2, R, U, Ri, U, R, U2, Ri });

        // ZBLL H3: R2 U R' U R' U' R U' R2 U' D R' U R D'
        _patterns.Add(new[] { R2, U, Ri, U, Ri, Ui, R, Ui, R2, Ui, D, Ri, U, R, Di });

        // ZBLL H4: R' U2 R U R' U R U R' U' R U' R' U2 R
        _patterns.Add(new[] { Ri, U2, R, U, Ri, U, R, U, Ri, Ui, R, Ui, Ri, U2, R });

        // --- ZBLL Pi Cases (Pi-shape, edges oriented) ---

        // ZBLL Pi1: R' U' F' R U R' U' R' F R2 U' R' U' R U R' U R
        _patterns.Add(new[] { Ri, Ui, Fi, R, U, Ri, Ui, Ri, F, R2, Ui, Ri, Ui, R, U, Ri, U, R });

        // ZBLL Pi2: F R U R' U' F' R U R' U' R' F R F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, Fi, R, U, Ri, Ui, Ri, F, R, Fi });

        // ZBLL Pi3: R U R' U' R' F R F' U2 R' F R F'
        _patterns.Add(new[] { R, U, Ri, Ui, Ri, F, R, Fi, U2, Ri, F, R, Fi });

        // ZBLL Pi4: R U2 R2 U' R2 U' R2 U2 R
        _patterns.Add(new[] { R, U2, R2, Ui, R2, Ui, R2, U2, R });

        // --- ZBLL S Cases (Sune-like, edges oriented) ---

        // ZBLL S1: R U R' U R U2 R' U' R U R' U R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, U2, Ri, Ui, R, U, Ri, U, R, U2, Ri });

        // ZBLL S2: R' U' R U' R' U2 R U R' U' R U' R' U2 R
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U2, R, U, Ri, Ui, R, Ui, Ri, U2, R });

        // ZBLL S3: F R U R' U' R U' R' U' R U R' F'
        _patterns.Add(new[] { F, R, U, Ri, Ui, R, Ui, Ri, Ui, R, U, Ri, Fi });

        // ZBLL S4: F' L' U' L U L' U L U L' U' L F
        _patterns.Add(new[] { Fi, Li, Ui, L, U, Li, U, L, U, Li, Ui, L, F });

        // --- ZBLL AS Cases (Anti-Sune shape, edges oriented) ---

        // ZBLL AS1: R U2 R' U' R U' R' U R U2 R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri, U, R, U2, Ri, Ui, R, Ui, Ri });

        // ZBLL AS2: R' U2 R U R' U R U' R' U2 R U R' U R
        _patterns.Add(new[] { Ri, U2, R, U, Ri, U, R, Ui, Ri, U2, R, U, Ri, U, R });

        // ZBLL AS3: R' U' R U' R' U R U R' U R U R' U2 R
        _patterns.Add(new[] { Ri, Ui, R, Ui, Ri, U, R, U, Ri, U, R, U, Ri, U2, R });

        // ZBLL AS4: R U R' U R U' R' U' R U' R' U' R U2 R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, Ui, R, Ui, Ri, Ui, R, U2, Ri });

        // ============================================================
        // WINTER VARIATION (WV) - 27 algorithms
        // Orient corners while inserting the last F2L pair
        // R U' R' case (pair in front-right, corner twisted)
        // ============================================================

        // WV 1: R U R' U' R U' R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri });

        // WV 2: R U' R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, Ui, Ri });

        // WV 3: R U2 R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // WV 4: R U' R' U2 R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U2, R, Ui, Ri });

        // WV 5: R' F R F' R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, R, Ui, Ri });

        // WV 6: R U R' U R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri });

        // WV 7: R U' R' U' R U' R'
        _patterns.Add(new[] { R, Ui, Ri, Ui, R, Ui, Ri });

        // WV 8: R U2 R' U R U' R'
        _patterns.Add(new[] { R, U2, Ri, U, R, Ui, Ri });

        // WV 9: R U R' U' R U2 R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, U2, Ri });

        // WV 10: R U' R' U R U2 R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, U2, Ri });

        // WV 11: F R' F' R U' R U R'
        _patterns.Add(new[] { F, Ri, Fi, R, Ui, R, U, Ri });

        // WV 12: R U' R' U' R U R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, Ui, R, U, Ri, U, R, Ui, Ri });

        // WV 13: R' F R F' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, U, R, Ui, Ri });

        // WV 14: R U' R' U R U R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, U, Ri, U, R, Ui, Ri });

        // WV 15: U' R U' R' U R U R'
        _patterns.Add(new[] { Ui, R, Ui, Ri, U, R, U, Ri });

        // WV 16: U R U' R' U' R U R'
        _patterns.Add(new[] { U, R, Ui, Ri, Ui, R, U, Ri });

        // WV 17: R U R' U2 R U R' U R U' R'
        _patterns.Add(new[] { R, U, Ri, U2, R, U, Ri, U, R, Ui, Ri });

        // WV 18: R U2 R' U R U R' U R U' R'
        _patterns.Add(new[] { R, U2, Ri, U, R, U, Ri, U, R, Ui, Ri });

        // WV 19: R' F R F' R U R' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, R, U, Ri, U, R, Ui, Ri });

        // WV 20: F R' F' R U R U R' U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, U, Ri, U, R, Ui, Ri });

        // WV 21: R U' R' U2 R U R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U2, R, U, Ri, U, R, Ui, Ri });

        // WV 22: R U R' U R U' R' U R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, Ui, Ri });

        // WV 23: R U2 R' U' R U R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, U, Ri, Ui, R, Ui, Ri });

        // WV 24: R U' R' U R U' R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, Ui, Ri, U, R, Ui, Ri });

        // WV 25: R U R' U' R U' R' U R U' R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri, U, R, Ui, Ri });

        // WV 26: R' F R2 U' R' U' R U R' F'
        _patterns.Add(new[] { Ri, F, R2, Ui, Ri, Ui, R, U, Ri, Fi });

        // WV 27: R' F R F' U2 R U' R' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, U2, R, Ui, Ri, U, R, Ui, Ri });

        // ============================================================
        // VLS (Valk Last Slot) - ~40 common cases
        // Orient last layer edges while inserting last F2L pair
        // ============================================================

        // --- VLS Dot Cases (no edges oriented) ---

        // VLS Dot 1: R' F R F' U' F' U F R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, Ui, Fi, U, F, R, Ui, Ri });

        // VLS Dot 2: F R' F' R U R U R' U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, U, Ri, U, R, Ui, Ri });

        // VLS Dot 3: R' F R F' R U R' U' R U R' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, R, U, Ri, Ui, R, U, Ri, U, R, Ui, Ri });

        // VLS Dot 4: R U R' U' R U' R' U' F' U F R U' R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri, Ui, Fi, U, F, R, Ui, Ri });

        // --- VLS Line Cases (two opposite edges oriented) ---

        // VLS Line 1: R U' R' U R U' R' U' F' U' F R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, Ui, Ri, Ui, Fi, Ui, F, R, Ui, Ri });

        // VLS Line 2: F R' F' R U R U' R' U' F' U F R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, Ui, Ri, Ui, Fi, U, F, R, Ui, Ri });

        // VLS Line 3: R U R' U R U' R' U F' U' F R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, Fi, Ui, F, R, Ui, Ri });

        // VLS Line 4: R' F R F' U R U' R' U F' U' F R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, U, R, Ui, Ri, U, Fi, Ui, F, R, Ui, Ri });

        // --- VLS L-shape Cases (two adjacent edges oriented) ---

        // VLS L1: R U R' U' F' U' F R U' R'
        _patterns.Add(new[] { R, U, Ri, Ui, Fi, Ui, F, R, Ui, Ri });

        // VLS L2: R U' R' U' F' U' F R U' R'
        _patterns.Add(new[] { R, Ui, Ri, Ui, Fi, Ui, F, R, Ui, Ri });

        // VLS L3: R' F R F' U' R U' R' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, Ui, R, Ui, Ri, U, R, Ui, Ri });

        // VLS L4: F R' F' R U2 R U' R' U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U2, R, Ui, Ri, U, R, Ui, Ri });

        // VLS L5: R U2 R' U' F' U F R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, Fi, U, F, R, Ui, Ri });

        // VLS L6: R U R' U2 F' U F R U' R'
        _patterns.Add(new[] { R, U, Ri, U2, Fi, U, F, R, Ui, Ri });

        // VLS L7: R' F R F' R U2 R' U R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, R, U2, Ri, U, R, Ui, Ri });

        // VLS L8: F R' F' R U R U R' U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, U, Ri, U, R, Ui, Ri });

        // --- VLS Cross Cases (all edges oriented) ---

        // VLS Cross 1: R U' R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, Ui, Ri });

        // VLS Cross 2: R U R' U' R U' R'
        _patterns.Add(new[] { R, U, Ri, Ui, R, Ui, Ri });

        // VLS Cross 3: R U2 R' U' R U' R'
        _patterns.Add(new[] { R, U2, Ri, Ui, R, Ui, Ri });

        // VLS Cross 4: R U' R' U R U2 R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, U2, Ri });

        // VLS Cross 5: R' F R F' R U' R'
        _patterns.Add(new[] { Ri, F, R, Fi, R, Ui, Ri });

        // VLS Cross 6: F R' F' R U R U' R'
        _patterns.Add(new[] { F, Ri, Fi, R, U, R, Ui, Ri });

        // VLS Cross 7: R U R' U R U' R' U R U' R'
        _patterns.Add(new[] { R, U, Ri, U, R, Ui, Ri, U, R, Ui, Ri });

        // VLS Cross 8: R U' R' U R U R' U R U' R'
        _patterns.Add(new[] { R, Ui, Ri, U, R, U, Ri, U, R, Ui, Ri });

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
