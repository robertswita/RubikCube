using TGL.GA.Interfaces;

namespace RubikCube.Tests.Mocks;

/// <summary>
/// A simple mock chromosome for testing genetic algorithm operators.
/// </summary>
public class MockChromosome : IChromosome
{
    private readonly double[] _genes;

    public double Fitness { get; set; } = double.MaxValue;

    public double[] Genes => _genes;

    public int Length => _genes.Length;

    public MockChromosome() : this(10)
    {
    }

    public MockChromosome(int length)
    {
        _genes = new double[length];
    }

    public MockChromosome(double fitness) : this(10)
    {
        Fitness = fitness;
    }

    public MockChromosome(double fitness, int length) : this(length)
    {
        Fitness = fitness;
    }

    public void Randomize(Random rng)
    {
        for (int i = 0; i < _genes.Length; i++)
        {
            _genes[i] = rng.NextDouble() * 100;
        }
    }

    public void Validate()
    {
        // No-op for mock
    }

    public int CompareTo(IChromosome? other)
    {
        if (other == null) return -1;
        return Fitness.CompareTo(other.Fitness);
    }

    public object Clone()
    {
        var clone = new MockChromosome(_genes.Length)
        {
            Fitness = Fitness
        };
        Array.Copy(_genes, clone._genes, _genes.Length);
        return clone;
    }

    /// <summary>
    /// Creates a population of mock chromosomes with specified fitness values.
    /// </summary>
    public static List<MockChromosome> CreatePopulation(params double[] fitnessValues)
    {
        return fitnessValues.Select(f => new MockChromosome(f)).ToList();
    }

    /// <summary>
    /// Creates a sorted population (best first) with fitness values 1.0, 2.0, 3.0, ..., count.
    /// </summary>
    public static List<MockChromosome> CreateSortedPopulation(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new MockChromosome((double)i))
            .ToList();
    }
}
