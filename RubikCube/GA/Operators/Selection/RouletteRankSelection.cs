using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Roulette rank selection - probability based on rank rather than raw fitness.
/// Provides more uniform selection pressure.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class RouletteRankSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int populationCount = population.Count;
        if (populationCount == 0 || count == 0)
            return Array.Empty<T>();

        // Single element: rankSum would be 0, just return it directly
        if (populationCount == 1)
        {
            var singleResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                singleResult.Add(population[0]);
            return singleResult;
        }

        var ranks = new double[populationCount];
        double rankSum = 0;

        // Use rank instead of fitness (population assumed sorted, best first).
        // Rank 0 = best, Rank N-1 = worst.
        for (int i = 0; i < populationCount; i++)
        {
            ranks[i] = i;
            rankSum += ranks[i];
        }

        // Reverse so that best individuals (originally rank 0) have highest cumulative probability.
        // After reverse: ranks[0] = N-1 (highest), ranks[N-1] = 0 (lowest).
        // This gives more probability mass to lower indices, which map to better individuals.
        Array.Reverse(ranks);

        // Build cumulative probability distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = ranks[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + ranks[i];
        }

        // Select using binary search to find smallest index where cumulativeProb[index] >= p
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double p = rng.NextDouble() * rankSum;
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
