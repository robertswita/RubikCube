using System;
using System.Collections.Generic;
using RubikCube;
using TGL;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Simplify mutation - detects and removes redundant move patterns.
///
/// Basic simplifications (all dimensions):
/// - R R -> R2 (two 90° = one 180°)
/// - R R R -> R' (three 90° = one -90°)
/// - R R' -> remove both (cancel out)
///
/// Extended simplifications for 4D+ (orthogonal patterns):
/// - Moves on orthogonal planes commute (can be reordered)
/// - Detects simplifiable pairs separated by orthogonal moves
/// - Pattern: A, B, A where B ⊥ A can be simplified to B, A² or B (if A cancels)
///
/// In 4D, orthogonal plane pairs are:
/// - (0,1) ⊥ (2,3) — XY ⊥ ZW
/// - (0,2) ⊥ (1,3) — XZ ⊥ YW
/// - (0,3) ⊥ (1,2) — XW ⊥ YZ
///
/// Moves on orthogonal planes don't interfere with each other, allowing
/// reordering to bring simplifiable moves together.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SimplifyMutation<T> : IMutationOperator<T> where T : IChromosome
{
    // Cache for orthogonal plane pairs
    private static int _cachedN = -1;
    private static HashSet<(int, int)>? _orthogonalPairs;

    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Build orthogonal pairs cache if needed
        int n = TAffine.N;
        if (_cachedN != n)
        {
            BuildOrthogonalPairsCache(n);
        }

        // Pick a random starting position
        int startIdx = rng.Next(length - 1);

        // First try basic simplification (same axis/plane/slice)
        if (TryBasicSimplification(chromosome, startIdx, rng))
            return;

        // For 4D+ (N >= 4), try orthogonal pattern simplification
        if (n >= 4 && length >= 3)
        {
            TryOrthogonalSimplification(chromosome, startIdx, rng);
        }
    }

    /// <summary>
    /// Basic simplification: combine consecutive moves on same axis/plane/slice.
    /// </summary>
    private bool TryBasicSimplification(T chromosome, int startIdx, Random rng)
    {
        var move1 = TMove.Decode((int)chromosome.Genes[startIdx]);
        var move2 = TMove.Decode((int)chromosome.Genes[startIdx + 1]);

        // Check if moves are on the same axis, plane, and slice
        if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
        {
            CombineMoves(chromosome, startIdx, move1, move2, rng);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Orthogonal simplification: find simplifiable pairs separated by orthogonal moves.
    /// Pattern: A, B, C where B ⊥ A and C matches A -> can simplify A and C
    /// </summary>
    private void TryOrthogonalSimplification(T chromosome, int startIdx, Random rng)
    {
        int length = chromosome.Length;

        // Look for pattern in a small window (up to 4 moves)
        int windowSize = Math.Min(4, length - startIdx);
        if (windowSize < 3) return;

        var move1 = TMove.Decode((int)chromosome.Genes[startIdx]);

        // Search for a matching move that can be combined with move1
        for (int offset = 2; offset < windowSize; offset++)
        {
            int idx2 = startIdx + offset;
            var move2 = TMove.Decode((int)chromosome.Genes[idx2]);

            // Check if move2 can be combined with move1
            if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
            {
                // Check if all moves in between are orthogonal to move1
                bool allOrthogonal = true;
                for (int i = startIdx + 1; i < idx2 && allOrthogonal; i++)
                {
                    var middleMove = TMove.Decode((int)chromosome.Genes[i]);
                    if (!AreOrthogonal(move1.Plane, middleMove.Plane))
                    {
                        allOrthogonal = false;
                    }
                }

                if (allOrthogonal)
                {
                    // We can combine move1 and move2!
                    // Strategy: combine them at position idx2, then shift middle moves back
                    CombineDistantMoves(chromosome, startIdx, idx2, move1, move2, rng);
                    return;
                }
            }
        }

        // Alternative: Look for commutable pair that can be swapped for future benefit
        // Swap orthogonal adjacent moves with some probability to create simplification opportunities
        if (startIdx + 1 < length && rng.NextDouble() < 0.3)
        {
            var moveA = TMove.Decode((int)chromosome.Genes[startIdx]);
            var moveB = TMove.Decode((int)chromosome.Genes[startIdx + 1]);

            if (AreOrthogonal(moveA.Plane, moveB.Plane))
            {
                // Swap them - this may enable future simplifications
                chromosome.Genes[startIdx] = moveB.Encode();
                chromosome.Genes[startIdx + 1] = moveA.Encode();
            }
        }
    }

    /// <summary>
    /// Combine two consecutive moves.
    /// </summary>
    private static void CombineMoves(T chromosome, int startIdx, TMove move1, TMove move2, Random rng)
    {
        // Combine angles: angles are 0, 1, 2 representing 90°, 180°, -90°
        int qt1 = move1.Angle + 1; // Convert to quarter turns (1, 2, 3)
        int qt2 = move2.Angle + 1;
        int combinedQt = (qt1 + qt2) % 4; // 0, 1, 2, 3

        if (combinedQt == 0)
        {
            // Moves cancel out (4 quarter turns = identity)
            ReplaceWithRandomMoves(chromosome, startIdx, 2, rng);
        }
        else
        {
            // Combine into single move
            move1.Angle = combinedQt - 1; // Convert back (0, 1, 2)
            chromosome.Genes[startIdx] = move1.Encode();

            // Replace second move with a random move
            ReplaceWithRandomMoves(chromosome, startIdx + 1, 1, rng);
        }
    }

    /// <summary>
    /// Combine two moves that are separated by orthogonal moves.
    /// </summary>
    private static void CombineDistantMoves(T chromosome, int idx1, int idx2, TMove move1, TMove move2, Random rng)
    {
        int qt1 = move1.Angle + 1;
        int qt2 = move2.Angle + 1;
        int combinedQt = (qt1 + qt2) % 4;

        if (combinedQt == 0)
        {
            // Both moves cancel out - replace both with random moves
            ReplaceWithRandomMoves(chromosome, idx1, 1, rng);
            ReplaceWithRandomMoves(chromosome, idx2, 1, rng);
        }
        else
        {
            // Combine at idx1, replace idx2 with random
            move1.Angle = combinedQt - 1;
            chromosome.Genes[idx1] = move1.Encode();
            ReplaceWithRandomMoves(chromosome, idx2, 1, rng);
        }
    }

    /// <summary>
    /// Replace genes with random valid moves.
    /// </summary>
    private static void ReplaceWithRandomMoves(T chromosome, int startIdx, int count, Random rng)
    {
        if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
        {
            for (int i = 0; i < count && startIdx + i < chromosome.Length; i++)
            {
                chromosome.Genes[startIdx + i] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
            }
        }
    }

    /// <summary>
    /// Build cache of orthogonal plane pairs for current dimension.
    /// </summary>
    private static void BuildOrthogonalPairsCache(int n)
    {
        _cachedN = n;
        _orthogonalPairs = new HashSet<(int, int)>();

        int numPlanes = TAffine.Planes.Length;

        for (int p1 = 0; p1 < numPlanes; p1++)
        {
            for (int p2 = p1 + 1; p2 < numPlanes; p2++)
            {
                int[] axes1 = TAffine.Planes[p1];
                int[] axes2 = TAffine.Planes[p2];

                // Orthogonal if no shared axes
                bool orthogonal = axes1[0] != axes2[0] && axes1[0] != axes2[1] &&
                                  axes1[1] != axes2[0] && axes1[1] != axes2[1];

                if (orthogonal)
                {
                    _orthogonalPairs.Add((p1, p2));
                    _orthogonalPairs.Add((p2, p1)); // Add both directions for easy lookup
                }
            }
        }
    }

    /// <summary>
    /// Check if two planes are orthogonal (share no common axes).
    /// </summary>
    private static bool AreOrthogonal(int plane1, int plane2)
    {
        if (_orthogonalPairs == null) return false;
        return _orthogonalPairs.Contains((plane1, plane2));
    }
}
