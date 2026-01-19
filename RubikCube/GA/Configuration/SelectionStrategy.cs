namespace TGL.GA.Configuration;

/// <summary>
/// Available selection strategies for choosing parents.
/// </summary>
public enum SelectionStrategy
{
    /// <summary>
    /// Selects the top N individuals by fitness (deterministic).
    /// </summary>
    Rank,

    /// <summary>
    /// Tournament selection - randomly picks groups and selects best from each.
    /// </summary>
    Tournament,

    /// <summary>
    /// Fitness-proportional selection (higher fitness = higher probability).
    /// </summary>
    Roulette,

    /// <summary>
    /// Rank-based roulette - probability based on rank, not raw fitness.
    /// </summary>
    RouletteRank,

    /// <summary>
    /// Selects individuals with unique fitness values (diversity preserving).
    /// </summary>
    Unique,

    /// <summary>
    /// Stochastic Universal Sampling - reduces selection bias.
    /// </summary>
    StochasticUniversalSampling,

    /// <summary>
    /// Boltzmann selection - temperature-based probability, good for simulated annealing hybrid.
    /// </summary>
    Boltzmann,

    /// <summary>
    /// Truncation selection - only top k% of population are eligible for selection.
    /// </summary>
    Truncation,

    /// <summary>
    /// Linear ranking selection - probability linearly proportional to rank.
    /// </summary>
    LinearRanking,

    /// <summary>
    /// Exponential ranking selection - probability exponentially proportional to rank.
    /// </summary>
    ExponentialRanking
}
