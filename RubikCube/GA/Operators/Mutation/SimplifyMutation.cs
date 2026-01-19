using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Simplify mutation - detects and removes redundant move patterns.
/// Combines consecutive moves on the same axis/slice:
/// - R R -> R2 (two 90° = one 180°)
/// - R R R -> R' (three 90° = one -90°)
/// - R R' -> remove both (cancel out)
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SimplifyMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Pick a random starting position to look for simplification
        int startIdx = rng.Next(length - 1);

        // Decode consecutive moves
        var move1 = TMove.Decode((int)chromosome.Genes[startIdx]);
        var move2 = TMove.Decode((int)chromosome.Genes[startIdx + 1]);

        // Check if moves are on the same axis, plane, and slice
        if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
        {
            // Combine angles: angles are 0, 1, 2 representing 90°, 180°, -90°
            // In terms of quarter turns: 0=1qt, 1=2qt, 2=3qt (or -1qt)
            // Combined effect: (a1 + a2 + 2) mod 4 - 1, where we add 1 to each angle to get quarter turns
            int qt1 = move1.Angle + 1; // Convert to quarter turns (1, 2, 3)
            int qt2 = move2.Angle + 1;
            int combinedQt = (qt1 + qt2) % 4; // 0, 1, 2, 3

            if (combinedQt == 0)
            {
                // Moves cancel out (4 quarter turns = identity)
                // Replace both with random moves from FreeMoves if available
                if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
                {
                    chromosome.Genes[startIdx] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
                    chromosome.Genes[startIdx + 1] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
                }
            }
            else
            {
                // Combine into single move
                move1.Angle = combinedQt - 1; // Convert back (0, 1, 2)
                chromosome.Genes[startIdx] = move1.Encode();

                // Replace second move with a random move
                if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
                {
                    chromosome.Genes[startIdx + 1] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
                }
            }
        }
        // If moves are not combinable, do nothing (no simplification possible)
    }
}
