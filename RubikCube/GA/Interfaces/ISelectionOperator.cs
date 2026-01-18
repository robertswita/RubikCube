namespace TGL.GA.Interfaces;

/// <summary>
/// Interface for selection operators that choose parents for reproduction.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public interface ISelectionOperator<T> where T : IChromosome
{
    /// <summary>
    /// Selects a subset of the population to become parents.
    /// </summary>
    /// <param name="population">The current population, sorted by fitness (best first).</param>
    /// <param name="count">The number of parents to select.</param>
    /// <param name="rng">Random number generator.</param>
    /// <returns>The selected parents.</returns>
    IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng);
}
