using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA.Termination;

/// <summary>
/// Terminates when the target fitness is reached.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class TargetFitnessTermination<T> : ITerminationCondition<T> where T : IChromosome
{
    private readonly double _targetFitness;
    private readonly double _tolerance;

    public string? TerminationReason { get; private set; }

    /// <summary>
    /// Creates a target fitness termination condition.
    /// </summary>
    /// <param name="targetFitness">The target fitness value.</param>
    /// <param name="tolerance">Tolerance for comparison (default: 1e-10).</param>
    public TargetFitnessTermination(double targetFitness, double tolerance = 1e-10)
    {
        _targetFitness = targetFitness;
        _tolerance = tolerance;
    }

    public bool ShouldTerminate(GAState<T> state)
    {
        if (state.Best == null)
            return false;

        // Check if fitness is at or below target (for minimization)
        if (state.Best.Fitness <= _targetFitness + _tolerance)
        {
            TerminationReason = $"Target fitness reached ({state.Best.Fitness:F6} <= {_targetFitness})";
            return true;
        }

        return false;
    }
}
