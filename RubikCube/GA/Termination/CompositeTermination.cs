using System.Collections.Generic;
using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Termination;

/// <summary>
/// Combines multiple termination conditions with OR logic.
/// Terminates when any condition is met.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class CompositeTermination<T> : ITerminationCondition<T> where T : IChromosome
{
    private readonly List<ITerminationCondition<T>> _conditions = new();

    public string? TerminationReason { get; private set; }

    /// <summary>
    /// Creates an empty composite termination condition.
    /// </summary>
    public CompositeTermination()
    {
    }

    /// <summary>
    /// Creates a composite termination from configuration.
    /// </summary>
    public CompositeTermination(TerminationConfig config)
    {
        Add(new MaxGenerationsTermination<T>(config.MaxGenerations));

        if (config.TargetFitness < double.MaxValue)
        {
            Add(new TargetFitnessTermination<T>(config.TargetFitness));
        }

        if (config.StagnationLimit > 0)
        {
            Add(new StagnationTermination<T>(config.StagnationLimit));
        }

        if (config.MaxTime.HasValue)
        {
            Add(new MaxTimeTermination<T>(config.MaxTime.Value));
        }
    }

    /// <summary>
    /// Adds a termination condition.
    /// </summary>
    public CompositeTermination<T> Add(ITerminationCondition<T> condition)
    {
        _conditions.Add(condition);
        return this;
    }

    public bool ShouldTerminate(GAState<T> state)
    {
        foreach (var condition in _conditions)
        {
            if (condition.ShouldTerminate(state))
            {
                TerminationReason = condition.TerminationReason;
                return true;
            }
        }

        return false;
    }
}
