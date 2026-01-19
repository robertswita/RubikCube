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
    public int PopulationSize { get; init; } = 200;

    /// <summary>
    /// Number of best individuals to preserve unchanged (elitism).
    /// </summary>
    public int EliteCount { get; init; } = 2;

    /// <summary>
    /// Length of each chromosome (number of genes).
    /// </summary>
    public int GenomeLength { get; init; } = 50;

    #endregion

    #region Selection Settings

    /// <summary>
    /// The selection strategy to use.
    /// </summary>
    public SelectionStrategy Selection { get; init; } = SelectionStrategy.Tournament;

    /// <summary>
    /// Proportion of population to select as parents (0.0 to 1.0).
    /// </summary>
    public double SelectionRatio { get; init; } = 0.3;

    /// <summary>
    /// Size of tournament for Tournament selection.
    /// </summary>
    public int TournamentSize { get; init; } = 5;

    #endregion

    #region Crossover Settings

    /// <summary>
    /// The crossover strategy to use.
    /// </summary>
    public CrossoverStrategy Crossover { get; init; } = CrossoverStrategy.SinglePoint;

    /// <summary>
    /// Probability of crossover occurring (0.0 to 1.0).
    /// </summary>
    public double CrossoverRate { get; init; } = 1.0;

    #endregion

    #region Mutation Settings

    /// <summary>
    /// The mutation strategy to use.
    /// </summary>
    public MutationStrategy Mutation { get; init; } = MutationStrategy.SingleGene;

    /// <summary>
    /// Probability of mutation occurring per individual (0.0 to 1.0).
    /// </summary>
    public double MutationRate { get; init; } = 0.1;

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
