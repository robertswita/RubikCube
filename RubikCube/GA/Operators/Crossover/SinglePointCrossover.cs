using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Single-point crossover - splits chromosomes at one random point and swaps segments.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SinglePointCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;
        int splitIdx = rng.Next(1, length); // At least 1 gene from each parent

        var child1 = new T();
        var child2 = new T();

        // Child1: parent1[0..split) + parent2[split..end)
        Array.Copy(parent1.Genes, child1.Genes, splitIdx);
        Array.Copy(parent2.Genes, splitIdx, child1.Genes, splitIdx, length - splitIdx);

        // Child2: parent2[0..split) + parent1[split..end)
        Array.Copy(parent2.Genes, child2.Genes, splitIdx);
        Array.Copy(parent1.Genes, splitIdx, child2.Genes, splitIdx, length - splitIdx);

        return (child1, child2);
    }
}
