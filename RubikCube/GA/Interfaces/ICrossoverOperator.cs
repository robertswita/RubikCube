using System;

namespace TGL.GA.Interfaces;

/// <summary>
/// Interface for crossover operators that combine two parents to create offspring.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public interface ICrossoverOperator<T> where T : IChromosome
{
    /// <summary>
    /// Creates two children by combining genetic material from two parents.
    /// </summary>
    /// <param name="parent1">The first parent.</param>
    /// <param name="parent2">The second parent.</param>
    /// <param name="rng">Random number generator.</param>
    /// <returns>A tuple containing two child chromosomes.</returns>
    (T child1, T child2) Crossover(T parent1, T parent2, Random rng);
}
