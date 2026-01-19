using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Exponential Ranking Selection - selection probability decreases exponentially with rank.
///
/// Algorithm:
/// 1. Assign ranks: best individual gets rank 1, worst gets rank N
/// 2. Calculate exponential probability: P(i) = c * (1 - e^(-rank(i)))
///    Or alternatively: P(i) = (1/c) * e^(-k*rank(i))
///    where c is normalization constant, k controls the steepness
/// 3. Select using these probabilities
///
/// The base parameter controls the exponential decay:
/// - base close to 1.0: Very steep decay, strong pressure on top individuals
/// - base close to 0.0: Flatter decay, more uniform selection
/// - Default 0.99: Good balance for typical GA populations
///
/// Advantages:
/// - Stronger differentiation between top-ranked individuals than linear ranking
/// - Bottom-ranked individuals still have non-zero (but very small) probability
/// - Good for fine-tuning when population is converging
///
/// Example with N=5 and base=0.9:
/// Ranks (best to worst): 1, 2, 3, 4, 5
/// Raw values: 0.9^1, 0.9^2, 0.9^3, 0.9^4, 0.9^5 = 0.9, 0.81, 0.729, 0.656, 0.590
/// Probabilities (normalized): 0.244, 0.220, 0.198, 0.178, 0.160
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class ExponentialRankingSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    private readonly double _base;

    /// <summary>
    /// Creates an exponential ranking selection operator.
    /// </summary>
    /// <param name="expBase">Base for exponential decay (0.0 to 1.0). Default is 0.99.</param>
    public ExponentialRankingSelection(double expBase = 0.99)
    {
        // Clamp to valid range, avoiding 0 and 1 exactly
        _base = Math.Clamp(expBase, 0.01, 0.9999);
    }

    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int n = population.Count;
        if (n == 0)
            return Array.Empty<T>();

        // Single element population: return it count times
        if (n == 1)
        {
            var singleResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                singleResult.Add(population[0]);
            return singleResult;
        }

        // Calculate cumulative probabilities using exponential distribution
        // For minimization: population is sorted best-first, so index 0 should have rank 1 (highest prob)
        var cumulativeProbs = new double[n];
        double cumulative = 0;

        for (int i = 0; i < n; i++)
        {
            // Rank: best (index 0) gets rank 1, worst (index N-1) gets rank N
            int rank = i + 1;

            // Exponential probability: base^rank (higher base = less decay)
            // Best individual (rank 1) gets base^1, worst gets base^N
            double prob = Math.Pow(_base, rank);

            cumulative += prob;
            cumulativeProbs[i] = cumulative;
        }

        // Normalize
        for (int i = 0; i < n; i++)
        {
            cumulativeProbs[i] /= cumulative;
        }

        // Select using roulette wheel on exponential rank probabilities
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
