using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Swap mutation - exchanges the positions of two random genes.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SwapMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        int idx1 = rng.Next(length);
        int idx2 = rng.Next(length);

        // Ensure different indices
        while (idx2 == idx1)
        {
            idx2 = rng.Next(length);
        }

        // Swap
        (chromosome.Genes[idx1], chromosome.Genes[idx2]) =
            (chromosome.Genes[idx2], chromosome.Genes[idx1]);
    }
}
