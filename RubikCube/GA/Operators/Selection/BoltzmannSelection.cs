using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Boltzmann selection inspired by simulated annealing.
///
/// Selection probability follows the Boltzmann distribution:
/// P(i) = exp(-fitness[i] / T) / Σexp(-fitness[j] / T)
///
/// Where T is the temperature parameter that controls selection pressure:
/// - High T (e.g., 100): Near-uniform selection, promotes exploration
/// - Low T (e.g., 1): Strong bias toward best individuals, promotes exploitation
///
/// This selection method is particularly useful for:
/// - Avoiding premature convergence in early generations (high T)
/// - Fine-tuning solutions in later generations (low T)
/// - Maintaining population diversity while still favoring better individuals
///
/// For minimization problems (lower fitness = better), the negative exponent
/// ensures that individuals with lower fitness get higher selection probability.
///
/// The implementation uses log-sum-exp trick for numerical stability when
/// dealing with large fitness differences.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class BoltzmannSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    private readonly double _temperature;

    /// <summary>
    /// Creates a Boltzmann selection operator.
    /// </summary>
    /// <param name="temperature">
    /// Temperature parameter controlling selection pressure.
    /// Higher values (50-100) give more uniform selection (exploration).
    /// Lower values (1-10) strongly favor better individuals (exploitation).
    /// Default is 10.0, providing moderate selection pressure.
    /// </param>
    public BoltzmannSelection(double temperature = 10.0)
    {
        if (temperature <= 0)
            throw new ArgumentException("Temperature must be positive", nameof(temperature));
        _temperature = temperature;
    }

    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int populationCount = population.Count;
        if (populationCount == 0 || count == 0)
            return Array.Empty<T>();

        // Calculate Boltzmann factors using log-sum-exp for numerical stability
        // For minimization: exp(-fitness / T) gives higher probability to lower fitness
        var logBoltzmann = new double[populationCount];
        double maxLogBoltzmann = double.MinValue;

        for (int i = 0; i < populationCount; i++)
        {
            // Negative fitness because we're minimizing (lower fitness = better)
            logBoltzmann[i] = -population[i].Fitness / _temperature;
            if (logBoltzmann[i] > maxLogBoltzmann)
                maxLogBoltzmann = logBoltzmann[i];
        }

        // Compute probabilities using log-sum-exp trick
        // P(i) = exp(logBoltzmann[i] - maxLogBoltzmann) / Σexp(logBoltzmann[j] - maxLogBoltzmann)
        var probabilities = new double[populationCount];
        double sumExp = 0;

        for (int i = 0; i < populationCount; i++)
        {
            probabilities[i] = Math.Exp(logBoltzmann[i] - maxLogBoltzmann);
            sumExp += probabilities[i];
        }

        // Handle edge case where all probabilities are effectively zero
        if (sumExp <= 0 || double.IsNaN(sumExp) || double.IsInfinity(sumExp))
        {
            // Fall back to uniform selection
            var uniformResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                uniformResult.Add(population[rng.Next(populationCount)]);
            return uniformResult;
        }

        // Normalize to get actual probabilities
        for (int i = 0; i < populationCount; i++)
        {
            probabilities[i] /= sumExp;
        }

        // Build cumulative distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = probabilities[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + probabilities[i];
        }

        // Select using binary search on cumulative distribution
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double r = rng.NextDouble();
            int selectedIdx = BinarySearch(cumulativeProb, r);
            selection.Add(population[selectedIdx]);
        }

        return selection;
    }

    /// <summary>
    /// Binary search to find the index where cumulative probability exceeds the random value.
    /// </summary>
    private static int BinarySearch(double[] cumulativeProb, double value)
    {
        int left = 0;
        int right = cumulativeProb.Length - 1;

        while (left < right)
        {
            int mid = (left + right) / 2;
            if (cumulativeProb[mid] < value)
                left = mid + 1;
            else
                right = mid;
        }

        return left;
    }
}
