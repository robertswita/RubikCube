using System;
using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Termination;

/// <summary>
/// Terminates when the maximum time has elapsed.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class MaxTimeTermination<T> : ITerminationCondition<T> where T : IChromosome
{
    private readonly TimeSpan _maxTime;

    public string? TerminationReason { get; private set; }

    /// <summary>
    /// Creates a max time termination condition.
    /// </summary>
    /// <param name="maxTime">Maximum time before terminating.</param>
    public MaxTimeTermination(TimeSpan maxTime)
    {
        _maxTime = maxTime;
    }

    public bool ShouldTerminate(GAState<T> state)
    {
        if (state.Elapsed.Elapsed >= _maxTime)
        {
            TerminationReason = $"Maximum time reached ({_maxTime})";
            return true;
        }

        return false;
    }
}
