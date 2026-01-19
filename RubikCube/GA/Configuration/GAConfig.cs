using System;

namespace TGL.GA.Configuration;

/// <summary>
/// Immutable configuration for a genetic algorithm run.
/// </summary>
public record GAConfig
{
    #region Population Settings

    /// <summary>
    /// Number of individuals in the population.
    /// </summary>
    public int PopulationSize { get; init; } = GADefaults.PopulationSize;

    /// <summary>
    /// Number of best individuals to preserve unchanged (elitism).
    /// </summary>
    public int EliteCount { get; init; } = GADefaults.EliteCount;

    /// <summary>
    /// Length of each chromosome (number of genes).
    /// </summary>
    public int GenomeLength { get; init; } = GADefaults.GenomeLength;

    #endregion

    #region Selection Settings

    /// <summary>
    /// The selection strategy to use.
    /// </summary>
    public SelectionStrategy Selection { get; init; } = GADefaults.Selection;

    /// <summary>
    /// Proportion of population to select as parents (0.0 to 1.0).
    /// </summary>
    public double SelectionRatio { get; init; } = GADefaults.SelectionRatio;

    /// <summary>
    /// Size of tournament for Tournament selection.
    /// </summary>
    public int TournamentSize { get; init; } = GADefaults.TournamentSize;

    /// <summary>
    /// Temperature for Boltzmann selection.
    /// Higher values (50-100) give more uniform selection (exploration).
    /// Lower values (1-10) strongly favor better individuals (exploitation).
    /// </summary>
    public double BoltzmannTemperature { get; init; } = GADefaults.BoltzmannTemperature;

    /// <summary>
    /// Truncation rate for Truncation selection (0.0 to 1.0).
    /// Only the top TruncationRate% of population are eligible for selection.
    /// Default 0.5 means top 50% can be selected.
    /// </summary>
    public double TruncationRate { get; init; } = GADefaults.TruncationRate;

    /// <summary>
    /// Selection pressure for Linear Ranking selection (1.0 to 2.0).
    /// 1.0 = uniform selection, 2.0 = maximum linear pressure.
    /// Default 1.5 provides moderate pressure.
    /// </summary>
    public double LinearRankingPressure { get; init; } = GADefaults.LinearRankingPressure;

    /// <summary>
    /// Base for Exponential Ranking selection (0.0 to 1.0).
    /// Higher values = less decay = more uniform selection.
    /// Lower values = steeper decay = stronger pressure on top individuals.
    /// Default 0.99 provides good balance.
    /// </summary>
    public double ExponentialRankingBase { get; init; } = GADefaults.ExponentialRankingBase;

    #endregion

    #region Crossover Settings

    /// <summary>
    /// The crossover strategy to use.
    /// </summary>
    public CrossoverStrategy Crossover { get; init; } = GADefaults.Crossover;

    /// <summary>
    /// Probability of crossover occurring (0.0 to 1.0).
    /// </summary>
    public double CrossoverRate { get; init; } = GADefaults.CrossoverRate;

    #endregion

    #region Mutation Settings

    /// <summary>
    /// The mutation strategy to use.
    /// </summary>
    public MutationStrategy Mutation { get; init; } = GADefaults.Mutation;

    /// <summary>
    /// Probability of mutation occurring per individual (0.0 to 1.0).
    /// </summary>
    public double MutationRate { get; init; } = GADefaults.MutationRate;

    #endregion

    #region Termination Settings

    /// <summary>
    /// Configuration for termination conditions.
    /// </summary>
    public TerminationConfig Termination { get; init; } = new();

    #endregion

    #region Validation

    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    public void Validate()
    {
        if (PopulationSize < 2)
            throw new ArgumentException("PopulationSize must be at least 2", nameof(PopulationSize));

        if (EliteCount < 0 || EliteCount >= PopulationSize)
            throw new ArgumentException("EliteCount must be between 0 and PopulationSize - 1", nameof(EliteCount));

        if (GenomeLength < 1)
            throw new ArgumentException("GenomeLength must be at least 1", nameof(GenomeLength));

        if (SelectionRatio <= 0 || SelectionRatio > 1)
            throw new ArgumentException("SelectionRatio must be between 0 and 1", nameof(SelectionRatio));

        if (TournamentSize < 2)
            throw new ArgumentException("TournamentSize must be at least 2", nameof(TournamentSize));

        if (CrossoverRate < 0 || CrossoverRate > 1)
            throw new ArgumentException("CrossoverRate must be between 0 and 1", nameof(CrossoverRate));

        if (MutationRate < 0 || MutationRate > 1)
            throw new ArgumentException("MutationRate must be between 0 and 1", nameof(MutationRate));

        if (Termination.MaxGenerations < 1)
            throw new ArgumentException("MaxGenerations must be at least 1", nameof(Termination.MaxGenerations));

        if (Termination.StagnationLimit < 0)
            throw new ArgumentException("StagnationLimit must be non-negative", nameof(Termination.StagnationLimit));
    }

    #endregion
}
