using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Linear Ranking Selection - selection probability is linearly proportional to rank.
///
/// Algorithm:
/// 1. Assign ranks: best individual gets rank N, worst gets rank 1 (for minimization: reversed)
/// 2. Calculate linear probability: P(i) = (2 - s)/N + 2*rank(i)*(s - 1)/(N*(N+1))
///    where s is selection pressure parameter (1.0 to 2.0)
/// 3. Select using these probabilities
///
/// The selection pressure parameter s controls the bias:
/// - s = 1.0: Uniform selection (all equal probability)
/// - s = 2.0: Maximum linear pressure (best has 2x average probability, worst has 0)
/// - s = 1.5: Moderate pressure (recommended default)
///
/// Advantages over fitness-proportional (roulette):
/// - Avoids domination by super-fit individuals
/// - Works well when fitness values have large variance
/// - Maintains consistent selection pressure regardless of fitness scaling
///
/// Example with N=5 and s=1.5:
/// Ranks (best to worst): 5, 4, 3, 2, 1
/// Probabilities: 0.30, 0.25, 0.20, 0.15, 0.10
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class LinearRankingSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    private readonly double _selectionPressure;

    /// <summary>
    /// Creates a linear ranking selection operator.
    /// </summary>
    /// <param name="selectionPressure">Selection pressure (1.0 to 2.0). Default is 1.5.</param>
    public LinearRankingSelection(double selectionPressure = 1.5)
    {
        _selectionPressure = Math.Clamp(selectionPressure, 1.0, 2.0);
    }

    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int n = population.Count;
        if (n == 0)
            return Array.Empty<T>();

        if (n == 1)
            return new List<T> { population[0] };

        // Calculate cumulative probabilities
        // For minimization: population is sorted best-first, so index 0 should have highest rank
        var cumulativeProbs = new double[n];
        double cumulative = 0;
        double s = _selectionPressure;

        for (int i = 0; i < n; i++)
        {
            // Rank: best (index 0) gets rank N, worst (index N-1) gets rank 1
            int rank = n - i;

            // Linear ranking probability formula
            // P(rank) = (2 - s)/N + 2*(rank - 1)*(s - 1)/(N*(N - 1))
            double prob = (2.0 - s) / n + 2.0 * (rank - 1) * (s - 1.0) / (n * (n - 1));

            cumulative += prob;
            cumulativeProbs[i] = cumulative;
        }

        // Normalize to handle floating point errors
        for (int i = 0; i < n; i++)
        {
            cumulativeProbs[i] /= cumulative;
        }

        // Select using roulette wheel on rank probabilities
        var selected = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double r = rng.NextDouble();
            int idx = 0;
            while (idx < n - 1 && cumulativeProbs[idx] < r)
            {
                idx++;
            }
            selected.Add(population[idx]);
        }

        return selected;
    }
}
