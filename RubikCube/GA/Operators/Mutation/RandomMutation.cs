using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Mutation;

/// <summary>
/// Random mutation - replaces random genes with new random values.
/// For IRubikChromosome, uses ValidMoves to get valid random moves.
/// For other chromosomes, uses a save/restore approach with Randomize.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RandomMutation<T> : IMutationOperator<T> where T : IChromosome
{
    private readonly int _genesToMutate;

    /// <summary>
    /// Creates a random mutation operator.
    /// </summary>
    /// <param name="genesToMutate">Number of genes to mutate per call (default 1).</param>
    public RandomMutation(int genesToMutate = 1)
    {
        _genesToMutate = genesToMutate;
    }

    public void Mutate(T chromosome, Random rng)
    {
        // For Rubik chromosomes, use the valid moves directly
        if (chromosome is IRubikChromosome rubikChromosome && rubikChromosome.ValidMoves.Count > 0)
        {
            for (int i = 0; i < _genesToMutate; i++)
            {
                int idx = rng.Next(chromosome.Length);
                chromosome.Genes[idx] = rubikChromosome.ValidMoves[rng.Next(rubikChromosome.ValidMoves.Count)];
            }
            return;
        }

        // Generic fallback: save genes, randomize, pick what we need, restore
        for (int i = 0; i < _genesToMutate; i++)
        {
            int idx = rng.Next(chromosome.Length);

            // Save all genes
            var savedGenes = new double[chromosome.Length];
            Array.Copy(chromosome.Genes, savedGenes, chromosome.Length);

            // Randomize to get new random values
            chromosome.Randomize(rng);

            // Get the new value for the target index
            double newValue = chromosome.Genes[idx];

            // Restore all genes
            Array.Copy(savedGenes, chromosome.Genes, chromosome.Length);

            // Set just the one gene we want to mutate
            chromosome.Genes[idx] = newValue;
        }
    }
}
