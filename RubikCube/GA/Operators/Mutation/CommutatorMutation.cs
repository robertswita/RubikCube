using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Commutator mutation - domain-specific mutation for Rubik's cube.
/// Implements the ABA'B' commutator pattern using group theory.
/// Commutators affect only a small number of pieces, making them useful for precise solving.
///
/// 4D+ Optimization:
/// In dimensions >= 4, orthogonal plane pairs exist (planes sharing no common axes).
/// Commutators using moves on orthogonal planes are "purer" - they affect fewer pieces
/// and create more localized permutations, which is ideal for precise solving.
///
/// In 4D, orthogonal pairs are:
/// - (XY) ⊥ (ZW) - planes (0,1) and (2,3)
/// - (XZ) ⊥ (YW) - planes (0,2) and (1,3)
/// - (XW) ⊥ (YZ) - planes (0,3) and (1,2)
///
/// The mutation will prefer selecting move B on a plane orthogonal to move A's plane.
/// </summary>
/// <typeparam name="T">The chromosome type (must be IRubikChromosome for full functionality).</typeparam>
public class CommutatorMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cache for orthogonal plane pairs
    private static int _cachedN = -1;
    private static Dictionary<int, List<int>>? _orthogonalPlanes; // plane -> list of orthogonal planes

    public void Mutate(T chromosome, Random rng)
    {
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 4) return; // Need at least 4 positions for ABA'B'

        // Build orthogonal pairs cache if needed
        int n = TAffine.N;
        if (_cachedN != n)
        {
            BuildOrthogonalPlanesCache(n);
        }

        // Pick a random starting position (need room for 4 moves: A, B, A', B')
        int startIdx = rng.Next(length - 3);

        // Get move A from position startIdx
        var moveA = TMove.Decode((int)chromosome.Genes[startIdx]);

        // For 4D+, try to find a move B on an orthogonal plane
        TMove moveB;
        if (n >= 4 && _orthogonalPlanes != null && TryFindOrthogonalMove(chromosome, startIdx, moveA.Plane, rng, out moveB))
        {
            // Found an orthogonal move - write it to position startIdx + 1
            chromosome.Genes[startIdx + 1] = moveB.Encode();
        }
        else
        {
            // Fall back to using existing move at startIdx + 1
            moveB = TMove.Decode((int)chromosome.Genes[startIdx + 1]);
        }

        // Create A' (inverse of A) - invert the angle
        var moveAInverse = moveA;
        moveAInverse.Angle = 2 - moveA.Angle; // Invert: 0->2, 1->1, 2->0

        // Create B' (inverse of B) - invert the angle
        var moveBInverse = moveB;
        moveBInverse.Angle = 2 - moveB.Angle;

        // Apply the commutator pattern: A B A' B'
        // Position 0: A (already there)
        // Position 1: B (set above for orthogonal case, or already there)
        // Position 2: A'
        chromosome.Genes[startIdx + 2] = moveAInverse.Encode();
        // Position 3: B'
        chromosome.Genes[startIdx + 3] = moveBInverse.Encode();
    }

    /// <summary>
    /// Build cache mapping each plane to its orthogonal planes.
    /// </summary>
    private static void BuildOrthogonalPlanesCache(int n)
    {
        _cachedN = n;
        _orthogonalPlanes = new Dictionary<int, List<int>>();

        int numPlanes = TAffine.Planes.Length;

        for (int p1 = 0; p1 < numPlanes; p1++)
        {
            _orthogonalPlanes[p1] = new List<int>();

            for (int p2 = 0; p2 < numPlanes; p2++)
            {
                if (p1 == p2) continue;

                int[] axes1 = TAffine.Planes[p1];
                int[] axes2 = TAffine.Planes[p2];

                // Orthogonal if no shared axes
                bool orthogonal = axes1[0] != axes2[0] && axes1[0] != axes2[1] &&
                                  axes1[1] != axes2[0] && axes1[1] != axes2[1];

                if (orthogonal)
                {
                    _orthogonalPlanes[p1].Add(p2);
                }
            }
        }
    }

    /// <summary>
    /// Try to find a move on an orthogonal plane within the chromosome.
    /// Searches nearby positions first, then considers generating a new move.
    /// </summary>
    private static bool TryFindOrthogonalMove(T chromosome, int startIdx, int planeA, Random rng, out TMove move)
    {
        move = default;

        if (_orthogonalPlanes == null || !_orthogonalPlanes.TryGetValue(planeA, out var orthPlanes) || orthPlanes.Count == 0)
        {
            return false;
        }

        int length = chromosome.Length;

        // Search window: look at nearby moves (positions 1-7 from startIdx)
        int searchLimit = Math.Min(8, length - startIdx);
        for (int offset = 1; offset < searchLimit; offset++)
        {
            var candidateMove = TMove.Decode((int)chromosome.Genes[startIdx + offset]);
            if (orthPlanes.Contains(candidateMove.Plane))
            {
                move = candidateMove;
                return true;
            }
        }

        // No orthogonal move found nearby - generate one using valid moves if available
        if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
        {
            // Collect valid moves on orthogonal planes
            var orthogonalValidMoves = new List<int>();
            foreach (int validMove in rubikChromosome.ValidMoves)
            {
                var decoded = TMove.Decode(validMove);
                if (orthPlanes.Contains(decoded.Plane))
                {
                    orthogonalValidMoves.Add(validMove);
                }
            }

            if (orthogonalValidMoves.Count > 0)
            {
                int selectedMove = orthogonalValidMoves[rng.Next(orthogonalValidMoves.Count)];
                move = TMove.Decode(selectedMove);
                return true;
            }
        }

        return false;
    }
}
