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

        var fitness = new double[populationCount];
        double fitnessSum = 0;

        // Calculate inverted fitness (since lower is better)
        for (int i = 0; i < populationCount; i++)
        {
            fitness[i] = population[i].Fitness;
            fitnessSum += fitness[i];
        }

        // Guard against all-zero fitness (fall back to uniform selection)
        if (fitnessSum <= 0)
        {
            var uniformResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                uniformResult.Add(population[rng.Next(populationCount)]);
            return uniformResult;
        }

        // Reverse so that best individuals (lowest fitness) have highest cumulative probability.
        // After reverse: fitness[0] = worst fitness (highest value), fitness[N-1] = best fitness (lowest value).
        // This gives more probability mass to lower indices, which map to better individuals.
        Array.Reverse(fitness);

        // Build cumulative probability distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = fitness[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + fitness[i];
        }

        // Select using binary search to find smallest index where cumulativeProb[index] >= p
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double p = rng.NextDouble() * fitnessSum;
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
