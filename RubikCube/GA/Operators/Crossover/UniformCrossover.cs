using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Uniform crossover - each gene is independently chosen from either parent with 50% probability.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class UniformCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    private readonly double _swapProbability;

    /// <summary>
    /// Creates a uniform crossover operator.
    /// </summary>
    /// <param name="swapProbability">Probability of taking gene from second parent (default 0.5).</param>
    public UniformCrossover(double swapProbability = 0.5)
    {
        _swapProbability = swapProbability;
    }

    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;

        var child1 = new T();
        var child2 = new T();

        for (int i = 0; i < length; i++)
        {
            if (rng.NextDouble() < _swapProbability)
            {
                child1.Genes[i] = parent2.Genes[i];
                child2.Genes[i] = parent1.Genes[i];
            }
            else
            {
                child1.Genes[i] = parent1.Genes[i];
                child2.Genes[i] = parent2.Genes[i];
            }
        }

        return (child1, child2);
    }
}
