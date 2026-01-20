using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Inversion mutation - reverses a random subsequence of genes.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class InversionMutation<T> : IMutationOperator<T> where T : IChromosome
{
    public void Mutate(T chromosome, Random rng)
    {
        int length = chromosome.Length;
        if (length < 2) return;

        int start = rng.Next(length - 1);
        int end = rng.Next(start + 1, length + 1);

        // Reverse the segment [start, end)
        Array.Reverse(chromosome.Genes, start, end - start);
    }
}
