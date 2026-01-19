using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Roulette wheel selection - fitness-proportional selection.
/// Individuals with better (lower) fitness have higher probability of being selected.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RouletteSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int populationCount = population.Count;
        if (populationCount == 0 || count == 0)
            return Array.Empty<T>();

        // For minimization (lower fitness is better), use inverse fitness weighting.
        // weight[i] = 1 / (fitness[i] + epsilon) gives higher weight to lower fitness values.
        const double epsilon = 1e-10;
        var weights = new double[populationCount];
        double weightSum = 0;

        for (int i = 0; i < populationCount; i++)
        {
            double fitness = population[i].Fitness;
            // Handle negative or zero fitness by shifting to positive range
            weights[i] = 1.0 / (Math.Max(fitness, 0) + epsilon);
            weightSum += weights[i];
        }

        // Guard against zero weight sum (shouldn't happen with epsilon, but be safe)
        if (weightSum <= 0)
        {
            var uniformResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                uniformResult.Add(population[rng.Next(populationCount)]);
            return uniformResult;
        }

        // Build cumulative probability distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = weights[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + weights[i];
        }

        // Select using binary search to find smallest index where cumulativeProb[index] >= p
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double p = rng.NextDouble() * weightSum;
            int selectedIdx = BinarySearchCumulative(cumulativeProb, p);
            selection.Add(population[selectedIdx]);
        }

        return selection;
    }

    /// <summary>
    /// Binary search to find the smallest index where cumulativeProb[index] >= value.
    /// </summary>
    private static int BinarySearchCumulative(double[] cumulativeProb, double value)
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
