using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TGL.GA.Interfaces;

namespace TGL.GA.Configuration;

/// <summary>
/// Represents the current state of a genetic algorithm run.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class GAState<T> where T : IChromosome
{
    /// <summary>
    /// The current population of chromosomes.
    /// </summary>
    public List<T> Population { get; set; } = new();

    /// <summary>
    /// The best chromosome found so far.
    /// </summary>
    public T? Best { get; set; }

    /// <summary>
    /// The current generation number (0-based).
    /// </summary>
    public int Generation { get; set; }

    /// <summary>
    /// The generation in which the best solution was found.
    /// </summary>
    public int GenerationOfBest { get; set; }

    /// <summary>
    /// Random number generator for this run.
    /// </summary>
    public Random Rng { get; } = new();

    /// <summary>
    /// Stopwatch tracking elapsed time since the run started.
    /// </summary>
    public Stopwatch Elapsed { get; } = new();

    /// <summary>
    /// Number of fitness evaluations performed.
    /// </summary>
    public long EvaluationCount { get; set; }

    /// <summary>
    /// The number of generations since the best solution was found.
    /// </summary>
    public int StagnationCount => Generation - GenerationOfBest;

    /// <summary>
    /// Average fitness of the current population.
    /// </summary>
    public double AverageFitness => Population.Count > 0
        ? Population.Average(c => c.Fitness)
        : double.MaxValue;

    /// <summary>
    /// Best fitness in the current population.
    /// </summary>
    public double BestFitness => Best?.Fitness ?? double.MaxValue;

    /// <summary>
    /// Resets the state for a new run.
    /// </summary>
    public void Reset()
    {
        Population.Clear();
        Best = default;
        Generation = 0;
        GenerationOfBest = 0;
        EvaluationCount = 0;
        Elapsed.Reset();
    }

    /// <summary>
    /// Starts tracking elapsed time.
    /// </summary>
    public void Start()
    {
        Elapsed.Start();
    }

    /// <summary>
    /// Stops tracking elapsed time.
    /// </summary>
    public void Stop()
    {
        Elapsed.Stop();
    }
}
