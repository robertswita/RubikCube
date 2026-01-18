using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Termination;

/// <summary>
/// Terminates when the maximum number of generations is reached.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class MaxGenerationsTermination<T> : ITerminationCondition<T> where T : IChromosome
{
    private readonly int _maxGenerations;

    public string? TerminationReason { get; private set; }

    public MaxGenerationsTermination(int maxGenerations)
    {
        _maxGenerations = maxGenerations;
    }

    public bool ShouldTerminate(GAState<T> state)
    {
        if (state.Generation >= _maxGenerations)
        {
            TerminationReason = $"Maximum generations reached ({_maxGenerations})";
            return true;
        }

        return false;
    }
}
