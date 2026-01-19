using TGL.GA.Configuration;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Crossover;
using TGL.GA.Operators.Mutation;
using TGL.GA.Operators.Selection;

namespace TGL.GA.Operators;

/// <summary>
/// Factory for creating GA operators based on configuration.
/// </summary>
public static class OperatorFactory
{
    /// <summary>
    /// Creates a selection operator based on the specified strategy.
    /// </summary>
    public static ISelectionOperator<T> CreateSelection<T>(SelectionStrategy strategy, GAConfig? config = null)
        where T : IChromosome
    {
        return strategy switch
        {
            SelectionStrategy.Rank => new RankSelection<T>(),
            SelectionStrategy.Tournament => new TournamentSelection<T>(config?.TournamentSize ?? 5),
            SelectionStrategy.Roulette => new RouletteSelection<T>(),
            SelectionStrategy.RouletteRank => new RouletteRankSelection<T>(),
            SelectionStrategy.Unique => new UniqueSelection<T>(),
            SelectionStrategy.StochasticUniversalSampling => new RouletteSelection<T>(), // TODO: Implement SUS
            SelectionStrategy.Boltzmann => new TournamentSelection<T>(), // TODO: Implement Boltzmann
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown selection strategy")
        };
    }

    /// <summary>
    /// Creates a crossover operator based on the specified strategy.
    /// </summary>
    public static ICrossoverOperator<T> CreateCrossover<T>(CrossoverStrategy strategy)
        where T : IChromosome, new()
    {
        return strategy switch
        {
            CrossoverStrategy.SinglePoint => new SinglePointCrossover<T>(),
            CrossoverStrategy.TwoPoint => new TwoPointCrossover<T>(),
            CrossoverStrategy.Uniform => new UniformCrossover<T>(),
            CrossoverStrategy.OrderBased => new SinglePointCrossover<T>(), // TODO: Implement OX
            CrossoverStrategy.PartiallyMapped => new SinglePointCrossover<T>(), // TODO: Implement PMX
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown crossover strategy")
        };
    }

    /// <summary>
    /// Creates a mutation operator based on the specified strategy.
    /// </summary>
    public static IMutationOperator<T> CreateMutation<T>(MutationStrategy strategy)
        where T : IChromosome
    {
        return strategy switch
        {
            MutationStrategy.SingleGene => new SingleGeneMutation<T>(),
            MutationStrategy.Random => new RandomMutation<T>(),
            MutationStrategy.Conjugation => new ConjugationMutation<T>(),
            MutationStrategy.Swap => new SwapMutation<T>(),
            MutationStrategy.Inversion => new InversionMutation<T>(),
            MutationStrategy.Scramble => new ScrambleMutation<T>(),
            MutationStrategy.Adaptive => new ScrambleMutation<T>(), // TODO: Implement Adaptive
            MutationStrategy.Commutator => new CommutatorMutation<T>(),
            MutationStrategy.Neighbor => new NeighborMutation<T>(),
            MutationStrategy.Simplify => throw new NotImplementedException("SimplifyMutation not yet implemented"),
            MutationStrategy.InverseSequence => throw new NotImplementedException("InverseSequenceMutation not yet implemented"),
            MutationStrategy.Insert => throw new NotImplementedException("InsertMutation not yet implemented"),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown mutation strategy")
        };
    }

    /// <summary>
    /// Creates all operators from a configuration.
    /// </summary>
    public static (
        ISelectionOperator<T> Selection,
        ICrossoverOperator<T> Crossover,
        IMutationOperator<T> Mutation
    ) CreateOperators<T>(GAConfig config)
        where T : IChromosome, new()
    {
        return (
            CreateSelection<T>(config.Selection, config),
            CreateCrossover<T>(config.Crossover),
            CreateMutation<T>(config.Mutation)
        );
    }
}
