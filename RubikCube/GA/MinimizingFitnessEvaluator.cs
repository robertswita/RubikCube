using System;
using TGL.GA.Interfaces;

namespace TGL.GA;

/// <summary>
/// Base class for fitness evaluators where lower fitness is better.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public abstract class MinimizingFitnessEvaluator<T> : IFitnessEvaluator<T> where T : IChromosome
{
    public double WorstFitness => double.MaxValue;

    public abstract double Evaluate(T chromosome);

    public bool IsBetterThan(double fitness1, double fitness2)
    {
        return fitness1 < fitness2;
    }
}

/// <summary>
/// Base class for fitness evaluators where higher fitness is better.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public abstract class MaximizingFitnessEvaluator<T> : IFitnessEvaluator<T> where T : IChromosome
{
    public double WorstFitness => double.MinValue;

    public abstract double Evaluate(T chromosome);

    public bool IsBetterThan(double fitness1, double fitness2)
    {
        return fitness1 > fitness2;
    }
}

/// <summary>
/// Delegate-based fitness evaluator for quick prototyping.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class DelegateFitnessEvaluator<T> : MinimizingFitnessEvaluator<T> where T : IChromosome
{
    private readonly Func<T, double> _evaluator;

    public DelegateFitnessEvaluator(Func<T, double> evaluator)
    {
        _evaluator = evaluator;
    }

    public override double Evaluate(T chromosome)
    {
        return _evaluator(chromosome);
    }
}
