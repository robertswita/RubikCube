using System;
using System.Collections.Generic;
using System.Linq;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Mutation;
using TGL.GA.Operators.Crossover;
using TGL.GA.Operators.Selection;

namespace TGL.GA.Configuration;

/// <summary>
/// Provides metadata and factory methods for GA strategies.
/// This is the SINGLE SOURCE OF TRUTH for all strategy enums.
///
/// To add a new mutation/crossover/selection:
/// 1. Add the enum value to the respective enum
/// 2. Add the case to the CreateXxx method below
/// That's it! UI will automatically pick up the new option.
/// </summary>
public static class StrategyInfo
{
    #region Mutation Strategies

    /// <summary>
    /// All available mutation strategies in display order.
    /// </summary>
    public static IReadOnlyList<MutationStrategy> MutationStrategies { get; } =
        Enum.GetValues<MutationStrategy>().ToArray();

    /// <summary>
    /// Display names for mutation strategies. Returns enum name by default.
    /// Override here for custom display names.
    /// </summary>
    public static string GetDisplayName(MutationStrategy strategy) => strategy switch
    {
        MutationStrategy.InverseSequence => "Inverse Seq.",
        MutationStrategy.OrthogonalConjugation => "Orth. Conj.",
        MutationStrategy.BlockBuilding => "Block Build",
        MutationStrategy.LocalSearch => "Local Search",
        _ => strategy.ToString()
    };

    /// <summary>
    /// List of display names for UI dropdowns.
    /// </summary>
    public static IReadOnlyList<string> MutationDisplayNames { get; } =
        MutationStrategies.Select(GetDisplayName).ToArray();

    /// <summary>
    /// Get strategy from dropdown index.
    /// </summary>
    public static MutationStrategy GetMutationStrategy(int index) =>
        index >= 0 && index < MutationStrategies.Count
            ? MutationStrategies[index]
            : MutationStrategy.SingleGene;

    /// <summary>
    /// Get dropdown index from strategy.
    /// </summary>
    public static int GetMutationIndex(MutationStrategy strategy) =>
        MutationStrategies.IndexOf(strategy) is int idx && idx >= 0 ? idx : 0;

    /// <summary>
    /// Create a mutation operator instance.
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
            MutationStrategy.Adaptive => new AdaptiveMutation<T>(),
            MutationStrategy.Commutator => new CommutatorMutation<T>(),
            MutationStrategy.Neighbor => new NeighborMutation<T>(),
            MutationStrategy.Simplify => new SimplifyMutation<T>(),
            MutationStrategy.InverseSequence => new InverseSequenceMutation<T>(),
            MutationStrategy.Insert => new InsertMutation<T>(),
            MutationStrategy.Shift => new ShiftMutation<T>(),
            MutationStrategy.Displacement => new DisplacementMutation<T>(),
            MutationStrategy.Translocation => new TranslocationMutation<T>(),
            MutationStrategy.Creep => new CreepMutation<T>(),
            MutationStrategy.Gaussian => new GaussianMutation<T>(),
            MutationStrategy.Hyperplane => new HyperplaneMutation<T>(),
            MutationStrategy.OrthogonalConjugation => new OrthogonalConjugationMutation<T>(),
            MutationStrategy.Pattern => new PatternMutation<T>(),
            MutationStrategy.BlockBuilding => new BlockBuildingMutation<T>(),
            MutationStrategy.LocalSearch => new LocalSearchMutation<T>(),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown mutation strategy")
        };
    }

    #endregion

    #region Crossover Strategies

    /// <summary>
    /// All available crossover strategies in display order.
    /// </summary>
    public static IReadOnlyList<CrossoverStrategy> CrossoverStrategies { get; } =
        Enum.GetValues<CrossoverStrategy>().ToArray();

    /// <summary>
    /// Display names for crossover strategies.
    /// </summary>
    public static string GetDisplayName(CrossoverStrategy strategy) => strategy switch
    {
        CrossoverStrategy.SinglePoint => "Single Point",
        CrossoverStrategy.TwoPoint => "Two Point",
        CrossoverStrategy.OrderBased => "Order (OX)",
        CrossoverStrategy.PartiallyMapped => "PMX",
        CrossoverStrategy.SegmentPreserving => "Segment Pres.",
        CrossoverStrategy.Cycle => "Cycle (CX)",
        CrossoverStrategy.EdgeRecombination => "Edge (ERX)",
        _ => strategy.ToString()
    };

    /// <summary>
    /// List of display names for UI dropdowns.
    /// </summary>
    public static IReadOnlyList<string> CrossoverDisplayNames { get; } =
        CrossoverStrategies.Select(GetDisplayName).ToArray();

    /// <summary>
    /// Get strategy from dropdown index.
    /// </summary>
    public static CrossoverStrategy GetCrossoverStrategy(int index) =>
        index >= 0 && index < CrossoverStrategies.Count
            ? CrossoverStrategies[index]
            : CrossoverStrategy.SinglePoint;

    /// <summary>
    /// Get dropdown index from strategy.
    /// </summary>
    public static int GetCrossoverIndex(CrossoverStrategy strategy) =>
        CrossoverStrategies.IndexOf(strategy) is int idx && idx >= 0 ? idx : 0;

    /// <summary>
    /// Create a crossover operator instance.
    /// </summary>
    public static ICrossoverOperator<T> CreateCrossover<T>(CrossoverStrategy strategy)
        where T : IChromosome, new()
    {
        return strategy switch
        {
            CrossoverStrategy.SinglePoint => new SinglePointCrossover<T>(),
            CrossoverStrategy.TwoPoint => new TwoPointCrossover<T>(),
            CrossoverStrategy.Uniform => new UniformCrossover<T>(),
            CrossoverStrategy.OrderBased => new OrderCrossover<T>(),
            CrossoverStrategy.PartiallyMapped => new PMXCrossover<T>(),
            CrossoverStrategy.SegmentPreserving => new SegmentPreservingCrossover<T>(),
            CrossoverStrategy.Cycle => new CycleCrossover<T>(),
            CrossoverStrategy.EdgeRecombination => new EdgeRecombinationCrossover<T>(),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown crossover strategy")
        };
    }

    #endregion

    #region Selection Strategies

    /// <summary>
    /// All available selection strategies in display order.
    /// </summary>
    public static IReadOnlyList<SelectionStrategy> SelectionStrategies { get; } =
        Enum.GetValues<SelectionStrategy>().ToArray();

    /// <summary>
    /// Display names for selection strategies.
    /// </summary>
    public static string GetDisplayName(SelectionStrategy strategy) => strategy switch
    {
        SelectionStrategy.RouletteRank => "Roulette Rank",
        SelectionStrategy.StochasticUniversalSampling => "SUS",
        SelectionStrategy.LinearRanking => "Linear Rank",
        SelectionStrategy.ExponentialRanking => "Exp. Rank",
        _ => strategy.ToString()
    };

    /// <summary>
    /// List of display names for UI dropdowns.
    /// </summary>
    public static IReadOnlyList<string> SelectionDisplayNames { get; } =
        SelectionStrategies.Select(GetDisplayName).ToArray();

    /// <summary>
    /// Get strategy from dropdown index.
    /// </summary>
    public static SelectionStrategy GetSelectionStrategy(int index) =>
        index >= 0 && index < SelectionStrategies.Count
            ? SelectionStrategies[index]
            : SelectionStrategy.Unique;

    /// <summary>
    /// Get dropdown index from strategy.
    /// </summary>
    public static int GetSelectionIndex(SelectionStrategy strategy) =>
        SelectionStrategies.IndexOf(strategy) is int idx && idx >= 0 ? idx : 0;

    /// <summary>
    /// Create a selection operator instance.
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
            SelectionStrategy.StochasticUniversalSampling => new SUSSelection<T>(),
            SelectionStrategy.Boltzmann => new BoltzmannSelection<T>(config?.BoltzmannTemperature ?? 10.0),
            SelectionStrategy.Truncation => new TruncationSelection<T>(config?.TruncationRate ?? 0.5),
            SelectionStrategy.LinearRanking => new LinearRankingSelection<T>(config?.LinearRankingPressure ?? 1.5),
            SelectionStrategy.ExponentialRanking => new ExponentialRankingSelection<T>(config?.ExponentialRankingBase ?? 0.99),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown selection strategy")
        };
    }

    #endregion

    #region Helper Extension

    private static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(list[i], item))
                return i;
        }
        return -1;
    }

    #endregion
}
