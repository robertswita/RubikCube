namespace TGL.GA.Configuration;

/// <summary>
/// Predefined GA configurations for common use cases.
/// </summary>
public static class GAPresets
{
    /// <summary>
    /// Default configuration matching the original TGA behavior.
    /// Good balance of exploration and exploitation.
    /// </summary>
    public static GAConfig Default => new()
    {
        PopulationSize = 200,
        EliteCount = 2,
        GenomeLength = 50,
        Selection = SelectionStrategy.Unique,
        SelectionRatio = 0.1,
        TournamentSize = 5,
        Crossover = CrossoverStrategy.SinglePoint,
        CrossoverRate = 1.0, // Original always does crossover
        Mutation = MutationStrategy.SingleGene,
        MutationRate = 1.0, // Original mutates all non-elites
        Termination = new TerminationConfig
        {
            MaxGenerations = 100,
            TargetFitness = 0.0,
            StagnationLimit = 0 // Disabled
        }
    };

    /// <summary>
    /// High exploration configuration for finding new solution paths.
    /// Larger population, more diversity, higher mutation.
    /// </summary>
    public static GAConfig Exploratory => new()
    {
        PopulationSize = 200,
        EliteCount = 2,
        GenomeLength = 50,
        Selection = SelectionStrategy.Tournament,
        SelectionRatio = 0.4,
        TournamentSize = 3, // Lower pressure for more diversity
        Crossover = CrossoverStrategy.Uniform,
        CrossoverRate = 0.9,
        Mutation = MutationStrategy.Scramble,
        MutationRate = 0.3,
        Termination = new TerminationConfig
        {
            MaxGenerations = 500,
            TargetFitness = 0.0,
            StagnationLimit = 100
        }
    };

    /// <summary>
    /// High exploitation configuration for refining known solutions.
    /// Smaller population, stronger selection pressure, lower mutation.
    /// </summary>
    public static GAConfig Exploitative => new()
    {
        PopulationSize = 50,
        EliteCount = 5,
        GenomeLength = 50,
        Selection = SelectionStrategy.Rank,
        SelectionRatio = 0.2,
        TournamentSize = 5,
        Crossover = CrossoverStrategy.TwoPoint,
        CrossoverRate = 0.7,
        Mutation = MutationStrategy.Swap,
        MutationRate = 0.05,
        Termination = new TerminationConfig
        {
            MaxGenerations = 200,
            TargetFitness = 0.0,
            StagnationLimit = 30
        }
    };

    /// <summary>
    /// Long run configuration for finding complete solutions.
    /// More generations, stagnation detection, balanced parameters.
    /// </summary>
    public static GAConfig LongRun => new()
    {
        PopulationSize = 150,
        EliteCount = 3,
        GenomeLength = 50,
        Selection = SelectionStrategy.Tournament,
        SelectionRatio = 0.25,
        TournamentSize = 5,
        Crossover = CrossoverStrategy.SinglePoint,
        CrossoverRate = 0.85,
        Mutation = MutationStrategy.Conjugation,
        MutationRate = 0.15,
        Termination = new TerminationConfig
        {
            MaxGenerations = 10000,
            TargetFitness = 0.0,
            StagnationLimit = 200
        }
    };

    /// <summary>
    /// Fast configuration for quick iterations.
    /// Small population, few generations, aggressive selection.
    /// </summary>
    public static GAConfig Fast => new()
    {
        PopulationSize = 50,
        EliteCount = 2,
        GenomeLength = 50,
        Selection = SelectionStrategy.Rank,
        SelectionRatio = 0.1,
        TournamentSize = 3,
        Crossover = CrossoverStrategy.SinglePoint,
        CrossoverRate = 0.8,
        Mutation = MutationStrategy.Conjugation,
        MutationRate = 0.5,
        Termination = new TerminationConfig
        {
            MaxGenerations = 50,
            TargetFitness = 0.0,
            StagnationLimit = 15
        }
    };

    /// <summary>
    /// Creates a custom configuration based on a preset with modifications.
    /// </summary>
    /// <param name="preset">The base preset to start from.</param>
    /// <returns>A new GAConfig that can be modified.</returns>
    public static GAConfig FromPreset(GAConfig preset) => preset with { };
}
