using TGL.GA.Configuration;

namespace TGL.GA.Interfaces;

/// <summary>
/// Interface for termination conditions that determine when the GA should stop.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public interface ITerminationCondition<T> where T : IChromosome
{
    /// <summary>
    /// Determines if the GA should terminate based on the current state.
    /// </summary>
    /// <param name="state">The current GA state.</param>
    /// <returns>True if the GA should stop.</returns>
    bool ShouldTerminate(GAState<T> state);

    /// <summary>
    /// Gets a description of why termination occurred.
    /// </summary>
    string? TerminationReason { get; }
}
