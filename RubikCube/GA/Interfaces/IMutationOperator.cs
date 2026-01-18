namespace TGL.GA.Interfaces;

/// <summary>
/// Interface for mutation operators that introduce variation in chromosomes.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public interface IMutationOperator<T> where T : IChromosome
{
    /// <summary>
    /// Mutates the given chromosome in place.
    /// </summary>
    /// <param name="chromosome">The chromosome to mutate.</param>
    /// <param name="rng">Random number generator.</param>
    void Mutate(T chromosome, Random rng);
}
