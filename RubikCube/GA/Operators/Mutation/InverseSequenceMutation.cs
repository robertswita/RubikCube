using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Inverse sequence mutation - replaces a segment with its inverse.
/// Reverses the order of moves and inverts each move's angle.
/// The resulting sequence "undoes" the original segment.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class InverseSequenceMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        // Select a random segment
        int start = rng.Next(length - 1);
        int end = rng.Next(start + 1, length);
        int segmentLength = end - start;

        // Store the inverted moves
        var invertedMoves = new double[segmentLength];
        for (int i = 0; i < segmentLength; i++)
        {
            // Read from end to start (reverse order)
            int sourceIdx = end - 1 - i;
            var move = TMove.Decode((int)chromosome.Genes[sourceIdx]);

            // Invert the angle: 0->2, 1->1, 2->0
            move.Angle = 2 - move.Angle;

            invertedMoves[i] = move.Encode();
        }

        // Write back the inverted sequence
        for (int i = 0; i < segmentLength; i++)
        {
            chromosome.Genes[start + i] = invertedMoves[i];
        }
    }
}
