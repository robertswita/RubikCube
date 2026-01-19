using System;
using RubikCube;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Conjugation mutation - domain-specific mutation for Rubik's cube.
/// Implements the ABA^-1B^-1 conjugation pattern using group theory symmetry.
/// </summary>
/// <typeparam name="T">The chromosome type (must be IRubikChromosome for full functionality).</typeparam>
public class ConjugationMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        // First validate/clean the chromosome
        chromosome.Validate();

        int length = chromosome.Length;
        if (length < 2) return;

        // Pick a random position in the first half
        int geneIdx = rng.Next(length / 2);

        // Mirror inverted moves to the second half
        // This creates the conjugation pattern: moves before geneIdx are inverted
        // and placed symmetrically after geneIdx
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
}
