using GA;
using RubikCube;
using TGL.GA.Configuration;
using TGL.GA.Interfaces;

namespace TGL.GA;

/// <summary>
/// Result of a solver iteration or run.
/// </summary>
public class SolverResult
{
    /// <summary>
    /// The moves that improve the cube state.
    /// </summary>
    public List<TMove> Moves { get; init; } = new();

    /// <summary>
    /// The fitness achieved after applying the moves.
    /// </summary>
    public double Fitness { get; init; }

    /// <summary>
    /// Whether the cube is fully solved.
    /// </summary>
    public bool IsSolved => Fitness == 0;

    /// <summary>
    /// Total generations run across all iterations.
    /// </summary>
    public int TotalGenerations { get; init; }

    /// <summary>
    /// Total evaluations performed.
    /// </summary>
    public int TotalEvaluations { get; init; }

    /// <summary>
    /// Reason for termination.
    /// </summary>
    public string? TerminationReason { get; init; }
}

/// <summary>
/// High-level solver for Rubik's cube using the configurable GA engine.
/// Supports multiple solving modes: Iterative, Complete, and Adaptive.
/// </summary>
public class RubikGASolver
{
    private readonly GAConfig _gaConfig;
    private readonly SolverConfig _solverConfig;
    private TRubikCube _cube;
    private GeneticAlgorithm<TRubikGenome>? _currentGA;

    /// <summary>
    /// Raised when a new best solution is found during solving.
    /// </summary>
    public event Action<TRubikGenome>? BestFound;

    /// <summary>
    /// Raised after each GA generation completes.
    /// </summary>
    public event Action<GAState<TRubikGenome>>? GenerationCompleted;

    /// <summary>
    /// Raised when moves are ready to be applied (in iterative mode).
    /// </summary>
    public event Action<List<TMove>>? MovesReady;

    /// <summary>
    /// Raised when the solver moves to a new cluster.
    /// </summary>
    public event Action<TRubikCube>? ClusterChanged;

    /// <summary>
    /// Raised when an iteration completes (in iterative/adaptive mode).
    /// </summary>
    public event Action<SolverResult>? IterationCompleted;

    /// <summary>
    /// The current cube being solved.
    /// </summary>
    public TRubikCube Cube => _cube;

    /// <summary>
    /// Creates a new solver with the specified configuration.
    /// </summary>
    /// <param name="cube">The cube to solve.</param>
    /// <param name="gaConfig">GA configuration (defaults to GAPresets.Default).</param>
    /// <param name="solverConfig">Solver mode configuration.</param>
    public RubikGASolver(
        TRubikCube cube,
        GAConfig? gaConfig = null,
        SolverConfig? solverConfig = null)
    {
        _cube = cube;
        _gaConfig = gaConfig ?? GAPresets.Default;
        _solverConfig = solverConfig ?? new SolverConfig();
    }

    /// <summary>
    /// Runs the solver until the cube is solved or termination conditions are met.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The final solver result.</returns>
    public SolverResult Solve(CancellationToken ct = default)
    {
        return _solverConfig.Mode switch
        {
            SolverMode.Iterative => SolveIterative(ct),
            SolverMode.Complete => SolveComplete(ct),
            SolverMode.Adaptive => SolveAdaptive(ct),
            _ => throw new ArgumentException($"Unknown solver mode: {_solverConfig.Mode}")
        };
    }

    /// <summary>
    /// Runs the solver asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The final solver result.</returns>
    public async Task<SolverResult> SolveAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => Solve(ct), ct);
    }

    /// <summary>
    /// Runs a single iteration of the GA. Useful for step-by-step solving.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of this iteration.</returns>
    public SolverResult RunIteration(CancellationToken ct = default)
    {
        return RunSingleIteration(ct);
    }

    /// <summary>
    /// Updates the cube state (e.g., after external moves are applied).
    /// </summary>
    /// <param name="cube">The new cube state.</param>
    public void UpdateCube(TRubikCube cube)
    {
        _cube = cube;
    }

    private SolverResult SolveIterative(CancellationToken ct)
    {
        var allMoves = new List<TMove>();
        int totalGenerations = 0;
        int totalEvaluations = 0;
        double bestFitness = double.MaxValue;
        string? terminationReason = null;

        while (!ct.IsCancellationRequested)
        {
            // Move to next cluster if needed
            if (bestFitness == 0 || _cube.ActiveCluster == null)
            {
                _cube.NextCluster();
                if (_cube.ActiveCubie != null)
                {
                    TRubikGenome.FreeMoves = _cube.GetFreeMoves();
                }
                ClusterChanged?.Invoke(_cube);
                bestFitness = double.MaxValue;
            }

            if (_cube.ActiveCluster == null)
            {
                terminationReason = "All clusters solved";
                break;
            }

            var result = RunSingleIteration(ct);
            totalGenerations += result.TotalGenerations;
            totalEvaluations += result.TotalEvaluations;

            if (result.Fitness < bestFitness && result.Moves.Count > 0)
            {
                bestFitness = result.Fitness;
                allMoves.AddRange(result.Moves);

                // Apply moves to the cube
                foreach (var move in result.Moves)
                {
                    _cube.Turn(move);
                }

                MovesReady?.Invoke(result.Moves);
            }

            IterationCompleted?.Invoke(result);

            if (result.IsSolved)
            {
                // Check if there are more clusters
                _cube.NextCluster();
                if (_cube.ActiveCluster == null)
                {
                    terminationReason = "All clusters solved";
                    break;
                }
                ClusterChanged?.Invoke(_cube);
                bestFitness = double.MaxValue;
            }
        }

        if (ct.IsCancellationRequested)
        {
            terminationReason = "Cancelled";
        }

        return new SolverResult
        {
            Moves = allMoves,
            Fitness = _cube.Evaluate(),
            TotalGenerations = totalGenerations,
            TotalEvaluations = totalEvaluations,
            TerminationReason = terminationReason
        };
    }

    private SolverResult SolveComplete(CancellationToken ct)
    {
        var allMoves = new List<TMove>();
        int totalGenerations = 0;
        int totalEvaluations = 0;

        // Configure for complete solving
        var config = _gaConfig with
        {
            Termination = _gaConfig.Termination with
            {
                MaxGenerations = _solverConfig.MaxTotalGenerations,
                TargetFitness = 0.0
            }
        };

        // Process each cluster
        while (!ct.IsCancellationRequested)
        {
            _cube.NextCluster();
            if (_cube.ActiveCluster == null)
            {
                break;
            }

            if (_cube.ActiveCubie != null)
            {
                TRubikGenome.FreeMoves = _cube.GetFreeMoves();
            }

            ClusterChanged?.Invoke(_cube);

            // Set static chromosome length (required by TChromosome)
            TChromosome.GenesLength = _gaConfig.GenomeLength;

            var evaluator = new RubikFitnessEvaluator(_cube);
            var ga = new GeneticAlgorithm<TRubikGenome>(config, evaluator);
            _currentGA = ga;

            ga.GenerationCompleted += state =>
            {
                GenerationCompleted?.Invoke(state);
            };

            ga.NewBestFound += best =>
            {
                BestFound?.Invoke(best);
            };

            var best = ga.Run(ct);
            totalGenerations += ga.State.Generation;
            totalEvaluations += (int)ga.State.EvaluationCount;

            if (best.Fitness < double.MaxValue && best.MovesCount > 0)
            {
                var moves = ExtractMoves(best);
                allMoves.AddRange(moves);

                foreach (var move in moves)
                {
                    _cube.Turn(move);
                }

                MovesReady?.Invoke(moves);
            }
        }

        _currentGA = null;

        return new SolverResult
        {
            Moves = allMoves,
            Fitness = _cube.Evaluate(),
            TotalGenerations = totalGenerations,
            TotalEvaluations = totalEvaluations,
            TerminationReason = ct.IsCancellationRequested ? "Cancelled" : "Complete"
        };
    }

    private SolverResult SolveAdaptive(CancellationToken ct)
    {
        var allMoves = new List<TMove>();
        int totalGenerations = 0;
        int totalEvaluations = 0;
        int iterationsWithoutImprovement = 0;
        double previousBestFitness = double.MaxValue;

        while (!ct.IsCancellationRequested)
        {
            // Move to next cluster if needed
            if (_cube.ActiveCluster == null)
            {
                _cube.NextCluster();
                if (_cube.ActiveCubie != null)
                {
                    TRubikGenome.FreeMoves = _cube.GetFreeMoves();
                }
                ClusterChanged?.Invoke(_cube);
                previousBestFitness = double.MaxValue;
            }

            if (_cube.ActiveCluster == null)
            {
                break;
            }

            var result = RunSingleIteration(ct);
            totalGenerations += result.TotalGenerations;
            totalEvaluations += result.TotalEvaluations;

            double improvement = previousBestFitness - result.Fitness;

            if (improvement >= _solverConfig.ImprovementThreshold && result.Moves.Count > 0)
            {
                iterationsWithoutImprovement = 0;
                previousBestFitness = result.Fitness;
                allMoves.AddRange(result.Moves);

                foreach (var move in result.Moves)
                {
                    _cube.Turn(move);
                }

                MovesReady?.Invoke(result.Moves);
            }
            else
            {
                iterationsWithoutImprovement++;
            }

            IterationCompleted?.Invoke(result);

            if (result.IsSolved)
            {
                _cube.NextCluster();
                if (_cube.ActiveCluster == null)
                {
                    break;
                }
                ClusterChanged?.Invoke(_cube);
                previousBestFitness = double.MaxValue;
                iterationsWithoutImprovement = 0;
            }
            else if (iterationsWithoutImprovement >= _solverConfig.MaxIterationsWithoutImprovement)
            {
                // Move to next cluster even without solving
                _cube.NextCluster();
                if (_cube.ActiveCluster == null)
                {
                    break;
                }
                ClusterChanged?.Invoke(_cube);
                previousBestFitness = double.MaxValue;
                iterationsWithoutImprovement = 0;
            }
        }

        return new SolverResult
        {
            Moves = allMoves,
            Fitness = _cube.Evaluate(),
            TotalGenerations = totalGenerations,
            TotalEvaluations = totalEvaluations,
            TerminationReason = ct.IsCancellationRequested ? "Cancelled" : "Complete"
        };
    }

    private SolverResult RunSingleIteration(CancellationToken ct)
    {
        if (_cube.ActiveCluster == null)
        {
            _cube.NextCluster();
            if (_cube.ActiveCubie != null)
            {
                TRubikGenome.FreeMoves = _cube.GetFreeMoves();
            }
        }

        if (_cube.ActiveCluster == null)
        {
            return new SolverResult
            {
                Moves = new List<TMove>(),
                Fitness = 0,
                TotalGenerations = 0,
                TotalEvaluations = 0,
                TerminationReason = "All clusters solved"
            };
        }

        // Configure for single iteration
        var config = _gaConfig with
        {
            Termination = _gaConfig.Termination with
            {
                MaxGenerations = _solverConfig.GenerationsPerIteration
            }
        };

        // Set static chromosome length (required by TChromosome)
        TChromosome.GenesLength = _gaConfig.GenomeLength;

        var evaluator = new RubikFitnessEvaluator(_cube);
        var ga = new GeneticAlgorithm<TRubikGenome>(config, evaluator);
        _currentGA = ga;

        ga.GenerationCompleted += state =>
        {
            GenerationCompleted?.Invoke(state);
        };

        ga.NewBestFound += best =>
        {
            BestFound?.Invoke(best);
        };

        var best = ga.Run(ct);
        _currentGA = null;

        var moves = new List<TMove>();
        if (best.Fitness < double.MaxValue && best.MovesCount > 0)
        {
            moves = ExtractMoves(best);
        }

        return new SolverResult
        {
            Moves = moves,
            Fitness = best.Fitness,
            TotalGenerations = ga.State.Generation,
            TotalEvaluations = (int)ga.State.EvaluationCount,
            TerminationReason = null
        };
    }

    private static List<TMove> ExtractMoves(TRubikGenome genome)
    {
        var moves = new List<TMove>();
        for (int i = 0; i < genome.MovesCount; i++)
        {
            moves.Add(TMove.Decode((int)genome.Genes[i]));
        }
        return moves;
    }
}
