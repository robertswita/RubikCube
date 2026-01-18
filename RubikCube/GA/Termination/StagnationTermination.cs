using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Termination;

/// <summary>
/// Terminates when no improvement has been made for a specified number of generations.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class StagnationTermination<T> : ITerminationCondition<T> where T : IChromosome
{
    private readonly int _stagnationLimit;

    public string? TerminationReason { get; private set; }

    /// <summary>
    /// Creates a stagnation termination condition.
    /// </summary>
    /// <param name="stagnationLimit">Number of generations without improvement before terminating.</param>
    public StagnationTermination(int stagnationLimit)
    {
        _stagnationLimit = stagnationLimit;
    }

    public bool ShouldTerminate(GAState<T> state)
    {
        if (state.StagnationCount >= _stagnationLimit)
        {
            TerminationReason = $"Stagnation limit reached ({_stagnationLimit} generations without improvement)";
            return true;
        }

        return false;
    }
}
