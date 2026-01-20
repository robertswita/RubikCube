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

    /// <summary>
    /// Creates a mock chromosome with specified genes.
    /// </summary>
    public static MockChromosome WithGenes(params double[] genes)
    {
        var chromosome = new MockChromosome(genes.Length);
        Array.Copy(genes, chromosome.Genes, genes.Length);
        return chromosome;
    }

    /// <summary>
    /// Creates a mock chromosome with integer genes for easier testing.
    /// </summary>
    public static MockChromosome WithIntGenes(params int[] genes)
    {
        var chromosome = new MockChromosome(genes.Length);
        for (int i = 0; i < genes.Length; i++)
            chromosome.Genes[i] = genes[i];
        return chromosome;
    }
}

/// <summary>
/// A mock chromosome that implements IRubikChromosome for testing crossover operators.
/// </summary>
public class MockRubikChromosome : IRubikChromosome
{
    private readonly double[] _genes;
    private IReadOnlyList<int> _validMoves = Array.Empty<int>();

    public double Fitness { get; set; } = double.MaxValue;

    public double[] Genes => _genes;

    public int Length => _genes.Length;

    public int MovesCount { get; set; }

    public IReadOnlyList<int> ValidMoves
    {
        get => _validMoves;
        set => _validMoves = value;
    }

    public MockRubikChromosome() : this(10)
    {
    }

    public MockRubikChromosome(int length)
    {
        _genes = new double[length];
        MovesCount = length;
    }

    public MockRubikChromosome(double fitness, int length = 10, int movesCount = -1) : this(length)
    {
        Fitness = fitness;
        MovesCount = movesCount < 0 ? length : movesCount;
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
        var clone = new MockRubikChromosome(_genes.Length)
        {
            Fitness = Fitness,
            MovesCount = MovesCount
        };
        Array.Copy(_genes, clone._genes, _genes.Length);
        return clone;
    }

    /// <summary>
    /// Creates a mock Rubik chromosome with specified genes.
    /// </summary>
    public static MockRubikChromosome WithGenes(double fitness, int movesCount, params double[] genes)
    {
        var chromosome = new MockRubikChromosome(fitness, genes.Length, movesCount);
        Array.Copy(genes, chromosome.Genes, genes.Length);
        return chromosome;
    }
}
