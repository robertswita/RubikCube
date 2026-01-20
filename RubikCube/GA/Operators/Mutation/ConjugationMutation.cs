using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Conjugation mutation - domain-specific mutation for Rubik's cube.
/// Implements the ABA' conjugation pattern using group theory symmetry.
///
/// 4D+ Optimiztion:
/// In dimensions >= 4, the operator ensures the central "B" move is on an
/// orthogonal plane relative to the surrounding "A" moves. This creates
/// "purer" conjugations that affect fewer pieces.
///
/// In 4D, orthogonal pairs are:
/// - (XY) ⊥ (ZW) - planes (0,1) and (2,3)
/// - (XZ) ⊥ (YW) - planes (0,2) and (1,3)
/// - (XW) ⊥ (YZ) - planes (0,3) and (1,2)
/// </summary>
/// <typeparam name="T">The chromosome type (must be IRubikChromosome for full functionality).</typeparam>
public class ConjugationMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cache for orthogonal plane pairs
    private static int _cachedN = -1;
    private static Dictionary<int, List<int>>? _orthogonalPlanes;

    public void Mutate(T chromosome, Random rng)
    {
        // First validate/clean the chromosome
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 3) return; // Need at least 3 positions for ABA' pattern

        // Build orthogonal pairs cache if needed
        int n = TAffine.N;
        if (_cachedN != n)
        {
            BuildOrthogonalPlanesCache(n);
        }

        // Pick a random position for the central "B" move (need room for A before and A' after)
        int geneIdx = rng.Next(1, length - 1);

        // For 4D+, optimize the central move to be on an orthogonal plane
        if (n >= 4 && _orthogonalPlanes != null && geneIdx > 0)
        {
            OptimizeCentralMove(chromosome, geneIdx, rng);
        }

        // Mirror inverted moves to create the ABA' pattern
        // Moves before geneIdx are inverted and placed symmetrically after geneIdx
        for (int i = 1; i <= geneIdx && geneIdx + i < length; i++)
        {
            int sourceIdx = geneIdx - i;
            int targetIdx = geneIdx + i;

            // Properly decode the move, invert angle, and re-encode
            var move = TMove.Decode((int)chromosome.Genes[sourceIdx]);
            move.Angle = 2 - move.Angle; // Invert angle: 0->2, 1->1, 2->0
            chromosome.Genes[targetIdx] = move.Encode();
        }
    }

    /// <summary>
    /// Optimie the central "B" move to be on an orthogonal plane relative to surrounding "A" moves.
    /// </summary>
    private static void OptimizeCentralMove(T chromosome, int geneIdx, Random rng)
    {
        if (_orthogonalPlanes == null) return;

        // Get the plane of the move before the central position (part of "A")
        var moveA = TMove.Decode((int)chromosome.Genes[geneIdx - 1]);
        int planeA = moveA.Plane;

        // Check if the current central move is already orthogonal
        var currentB = TMove.Decode((int)chromosome.Genes[geneIdx]);
        if (_orthogonalPlanes.TryGetValue(planeA, out var orthPlanes) && orthPlanes.Contains(currentB.Plane))
        {
            return; // Already optimal
        }

        // Try to find or generate a move on an orthogonal plane
        if (orthPlanes != null && orthPlanes.Count > 0)
        {
            // Generate a new move on an orthogonal plane using valid moves
            if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
            {
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
                    chromosome.Genes[geneIdx] = orthogonalValidMoves[rng.Next(orthogonalValidMoves.Count)];
                }
            }
        }
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
}
