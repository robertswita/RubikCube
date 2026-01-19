using TGL.GA.Configuration;
using TGL.GA.Interfaces;
using TGL.GA.Operators;

namespace TGL.GA;

/// <summary>
/// A configurable genetic algorithm engine.
/// Uses pluggable operators for selection, crossover, and mutation.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class GeneticAlgorithm<T> where T : IChromosome, new()
{
    private readonly GAConfig _config;
    private readonly IFitnessEvaluator<T> _evaluator;
    private readonly ISelectionOperator<T> _selection;
    private readonly ICrossoverOperator<T> _crossover;
    private readonly IMutationOperator<T> _mutation;
    private readonly ITerminationCondition<T>? _termination;

    /// <summary>
    /// Current state of the genetic algorithm.
    /// </summary>
    public GAState<T> State { get; private set; } = new();

    /// <summary>
    /// The best solution found so far.
    /// </summary>
    public T? Best => State.Best;

    /// <summary>
    /// Current generation number.
    /// </summary>
    public int Generation => State.Generation;

    #region Events

    /// <summary>
    /// Raised after each generation is completed.
    /// </summary>
    public event Action<GAState<T>>? GenerationCompleted;

    /// <summary>
    /// Raised when a new best solution is found.
    /// </summary>
    public event Action<T>? NewBestFound;

    /// <summary>
    /// Raised when the algorithm completes.
    /// </summary>
    public event Action<GAState<T>>? RunCompleted;

    #endregion

    /// <summary>
    /// Creates a new genetic algorithm with the specified configuration.
    /// </summary>
    /// <param name="config">The GA configuration.</param>
    /// <param name="evaluator">The fitness evaluator.</param>
    /// <param name="selection">Optional custom selection operator.</param>
    /// <param name="crossover">Optional custom crossover operator.</param>
    /// <param name="mutation">Optional custom mutation operator.</param>
    /// <param name="termination">Optional custom termination condition.</param>
    public GeneticAlgorithm(
        GAConfig config,
        IFitnessEvaluator<T> evaluator,
        ISelectionOperator<T>? selection = null,
        ICrossoverOperator<T>? crossover = null,
        IMutationOperator<T>? mutation = null,
        ITerminationCondition<T>? termination = null)
    {
        _config = config;
        _config.Validate();

        _evaluator = evaluator;
        _selection = selection ?? OperatorFactory.CreateSelection<T>(config.Selection, config);
        _crossover = crossover ?? OperatorFactory.CreateCrossover<T>(config.Crossover);
        _mutation = mutation ?? OperatorFactory.CreateMutation<T>(config.Mutation);
        _termination = termination;
    }

    /// <summary>
    /// Runs the genetic algorithm synchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The best solution found.</returns>
    public T Run(CancellationToken ct = default)
    {
        Initialize();

        while (!ShouldTerminate() && !ct.IsCancellationRequested)
        {
            RunGeneration();
            GenerationCompleted?.Invoke(State);
        }

        State.Stop();
        RunCompleted?.Invoke(State);

        return State.Best!;
    }

    /// <summary>
    /// Runs the genetic algorithm asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The best solution found.</returns>
    public async Task<T> RunAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => Run(ct), ct);
    }

    /// <summary>
    /// Runs a single generation. Useful for step-by-step execution.
    /// </summary>
    /// <returns>True if the algorithm should continue, false if termination condition is met.</returns>
    public bool Step()
    {
        if (State.Population.Count == 0)
        {
            Initialize();
        }

        if (ShouldTerminate())
        {
            return false;
        }

        RunGeneration();
        GenerationCompleted?.Invoke(State);

        return !ShouldTerminate();
    }

    /// <summary>
    /// Resets the algorithm state for a new run.
    /// </summary>
    public void Reset()
    {
        State = new GAState<T>();
    }

    private void Initialize()
    {
        State.Reset();
        State.Start();

        // Create initial population
        for (int i = 0; i < _config.PopulationSize; i++)
        {
            var chromosome = new T();
            chromosome.Randomize(State.Rng);
            State.Population.Add(chromosome);
        }

        // Evaluate initial population
        EvaluatePopulation();

        // Sort by fitness (best first)
        State.Population.Sort();

        // Track best
        if (State.Population.Count > 0)
        {
            State.Best = (T)State.Population[0].Clone();
            State.GenerationOfBest = 0;
        }
    }

    private void RunGeneration()
    {
        // 1. Evaluate population
        EvaluatePopulation();

        // 2. Sort by fitness (best first)
        State.Population.Sort();

        // 3. Track best
        var currentBest = State.Population[0];
        if (State.Best == null || _evaluator.IsBetterThan(currentBest.Fitness, State.Best.Fitness))
        {
            State.Best = (T)currentBest.Clone();
            State.GenerationOfBest = State.Generation;
            NewBestFound?.Invoke(State.Best);
        }

        // 4. Elitism - preserve top individuals
        var nextGen = new List<T>(_config.PopulationSize);
        for (int i = 0; i < _config.EliteCount && i < State.Population.Count; i++)
        {
            nextGen.Add((T)State.Population[i].Clone());
        }

        // 5. Selection
        int parentCount = Math.Max(2, (int)(_config.PopulationSize * _config.SelectionRatio));
        var parents = _selection.Select(State.Population, parentCount, State.Rng);

        // 6. Crossover and fill population
        while (nextGen.Count < _config.PopulationSize)
        {
            var parent1 = parents[State.Rng.Next(parents.Count)];
            var parent2 = parents[State.Rng.Next(parents.Count)];

            if (State.Rng.NextDouble() < _config.CrossoverRate)
            {
                var (child1, child2) = _crossover.Crossover(parent1, parent2, State.Rng);
                nextGen.Add(child1);
                if (nextGen.Count < _config.PopulationSize)
                {
                    nextGen.Add(child2);
                }
            }
            else
            {
                // No crossover - clone parent but reset fitness so it gets re-evaluated after mutation
                var clone = (T)parent1.Clone();
                clone.Fitness = _evaluator.WorstFitness;
                nextGen.Add(clone);
            }
        }

        // 7. Mutation (skip elites)
        for (int i = _config.EliteCount; i < nextGen.Count; i++)
        {
            if (State.Rng.NextDouble() < _config.MutationRate)
            {
                _mutation.Mutate(nextGen[i], State.Rng);
                // Reset fitness after mutation since genes changed
                nextGen[i].Fitness = _evaluator.WorstFitness;
            }
        }

        State.Population = nextGen;
        State.Generation++;
    }

    private void EvaluatePopulation()
    {
        foreach (var chromosome in State.Population)
        {
            if (chromosome.Fitness == _evaluator.WorstFitness)
            {
                chromosome.Fitness = _evaluator.Evaluate(chromosome);
                State.EvaluationCount++;
            }
        }
    }

    private bool ShouldTerminate()
    {
        // Check custom termination condition first
        if (_termination != null && _termination.ShouldTerminate(State))
        {
            return true;
        }

        // Check built-in termination conditions from config
        var term = _config.Termination;

        // Max generations
        if (State.Generation >= term.MaxGenerations)
        {
            return true;
        }

        // Target fitness reached
        if (State.Best != null && _evaluator.IsBetterThan(State.Best.Fitness, term.TargetFitness))
        {
            return true;
        }

        // Exact target fitness
        if (State.Best != null && Math.Abs(State.Best.Fitness - term.TargetFitness) < double.Epsilon)
        {
            return true;
        }

        // Stagnation limit
        if (term.StagnationLimit > 0 && State.StagnationCount >= term.StagnationLimit)
        {
            return true;
        }

        // Time limit
        if (term.MaxTime.HasValue && State.Elapsed.Elapsed >= term.MaxTime.Value)
        {
            return true;
        }

        return false;
    }
}
