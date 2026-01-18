namespace TGL.GA.Configuration;

/// <summary>
/// Available solver modes for the GA-based Rubik's cube solver.
/// </summary>
public enum SolverMode
{
    /// <summary>
    /// Iterative mode: Run GA for N generations, apply best moves, restart.
    /// Good for incremental progress visualization.
    /// </summary>
    Iterative,

    /// <summary>
    /// Complete mode: Run GA continuously until full solution found.
    /// No intermediate restarts. Better for benchmarking.
    /// </summary>
    Complete,

    /// <summary>
    /// Adaptive mode: Apply moves only when improvement exceeds threshold.
    /// Hybrid between iterative and complete.
    /// </summary>
    Adaptive
}
