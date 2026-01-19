using System;

namespace TGL.GA.Configuration;

/// <summary>
/// Configuration for GA termination conditions.
/// </summary>
public record TerminationConfig
{
    /// <summary>
    /// Maximum number of generations before stopping.
    /// </summary>
    public int MaxGenerations { get; init; } = GADefaults.MaxGenerations;

    /// <summary>
    /// Target fitness value - stop when reached (lower is better).
    /// </summary>
    public double TargetFitness { get; init; } = GADefaults.TargetFitness;

    /// <summary>
    /// Stop if no improvement for this many generations.
    /// Set to 0 to disable stagnation check.
    /// </summary>
    public int StagnationLimit { get; init; } = GADefaults.StagnationLimit;

    /// <summary>
    /// Maximum time before stopping. Null means no time limit.
    /// </summary>
    public TimeSpan? MaxTime { get; init; } = null;
}
