namespace TGL.GA.Configuration;

/// <summary>
/// Configuration for the Rubik's cube GA solver.
/// </summary>
public record SolverConfig
{
    /// <summary>
    /// The solver mode to use.
    /// </summary>
    public SolverMode Mode { get; init; } = SolverMode.Iterative;

    /// <summary>
    /// For Iterative mode: number of generations per iteration before applying moves.
    /// </summary>
    public int GenerationsPerIteration { get; init; } = 100;

    /// <summary>
    /// For Adaptive mode: minimum fitness improvement required to apply moves.
    /// </summary>
    public double ImprovementThreshold { get; init; } = 0.1;

    /// <summary>
    /// For Adaptive mode: maximum iterations without meeting threshold before stopping.
    /// </summary>
    public int MaxIterationsWithoutImprovement { get; init; } = 10;

    /// <summary>
    /// For Complete mode: maximum total generations before stopping.
    /// </summary>
    public int MaxTotalGenerations { get; init; } = 10000;
}
