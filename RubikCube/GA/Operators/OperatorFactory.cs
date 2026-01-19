using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators;

/// <summary>
/// Factory for creating GA operators based on configuration.
/// Delegates to StrategyInfo for actual instantiation.
/// </summary>
public static class OperatorFactory
{
    /// <summary>
    /// Creates a selection operator based on the specified strategy.
    /// </summary>
    public static ISelectionOperator<T> CreateSelection<T>(SelectionStrategy strategy, GAConfig? config = null)
        where T : IChromosome
        => StrategyInfo.CreateSelection<T>(strategy, config);

    /// <summary>
    /// Creates a crossover operator based on the specified strategy.
    /// </summary>
    public static ICrossoverOperator<T> CreateCrossover<T>(CrossoverStrategy strategy)
        where T : IChromosome, new()
        => StrategyInfo.CreateCrossover<T>(strategy);

    /// <summary>
    /// Creates a mutation operator based on the specified strategy.
    /// </summary>
    public static IMutationOperator<T> CreateMutation<T>(MutationStrategy strategy)
        where T : IChromosome
        => StrategyInfo.CreateMutation<T>(strategy);

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
