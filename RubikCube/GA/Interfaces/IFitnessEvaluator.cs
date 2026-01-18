namespace TGL.GA.Interfaces;

/// <summary>
/// Interface for fitness evaluators that score chromosomes.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public interface IFitnessEvaluator<T> where T : IChromosome
{
    /// <summary>
    /// Evaluates the fitness of a chromosome.
    /// </summary>
    /// <param name="chromosome">The chromosome to evaluate.</param>
    /// <returns>The fitness value (lower is better by default).</returns>
    double Evaluate(T chromosome);

    /// <summary>
    /// Determines if the first fitness value is better than the second.
    /// </summary>
    /// <param name="fitness1">The first fitness value.</param>
    /// <param name="fitness2">The second fitness value.</param>
    /// <returns>True if fitness1 is better than fitness2.</returns>
    bool IsBetterThan(double fitness1, double fitness2);

    /// <summary>
    /// Gets the worst possible fitness value.
    /// </summary>
    double WorstFitness { get; }
}
