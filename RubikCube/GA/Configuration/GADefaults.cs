namespace TGL.GA.Configuration;

/// <summary>
/// Single source of truth for all GA default values.
/// All other configuration classes should reference these constants.
/// These defaults match the original TGA behavior from master branch.
/// </summary>
public static class GADefaults
{
    // Population settings
    public const int PopulationSize = 100;
    public const int EliteCount = 0;
    public const int GenomeLength = 50;

    // Selection settings
    public const SelectionStrategy Selection = SelectionStrategy.Unique;
    public const double SelectionRatio = 0.3;
    public const int TournamentSize = 5;
    public const double BoltzmannTemperature = 10.0;
    public const double TruncationRate = 0.5;
    public const double LinearRankingPressure = 1.5;
    public const double ExponentialRankingBase = 0.99;

    // Crossover settings
    public const CrossoverStrategy Crossover = CrossoverStrategy.SinglePoint;
    public const double CrossoverRate = 1.0;

    // Mutation settings
    public const MutationStrategy Mutation = MutationStrategy.SingleGene;
    public const double MutationRate = 0.01;

    // Termination settings
    public const int MaxGenerations = 2000;
    public const double TargetFitness = 0.0;
    public const int StagnationLimit = 0;
}
