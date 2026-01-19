using System;
using System.Collections.Generic;
using System.Linq;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Truncation Selection - only the top k% of the population are eligible for selection.
///
/// Algorithm:
/// 1. Determine truncation threshold: keep top truncationRate% of population
/// 2. Select uniformly at random from the truncated (elite) pool
///
/// This creates strong selection pressure by completely excluding
/// the bottom portion of the population from reproduction.
///
/// Example with truncationRate = 0.5 (50%):
/// Population of 100 → only top 50 individuals can be selected
/// Each of the top 50 has equal probability of being chosen
///
/// Common values:
/// - 0.5 (50%): Moderate pressure, balanced exploration/exploitation
/// - 0.25 (25%): Strong pressure, faster convergence but risk of premature convergence
/// - 0.1 (10%): Very strong pressure, used in evolution strategies (μ,λ) selection
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class TruncationSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    private readonly double _truncationRate;

    /// <summary>
    /// Creates a truncation selection operator.
    /// </summary>
    /// <param name="truncationRate">Fraction of population to keep (0.0 to 1.0). Default is 0.5 (top 50%).</param>
    public TruncationSelection(double truncationRate = 0.5)
    {
        _truncationRate = Math.Clamp(truncationRate, 0.01, 1.0);
    }

    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        if (population.Count == 0)
            return Array.Empty<T>();

        // Calculate truncation point
        int truncationPoint = Math.Max(1, (int)(population.Count * _truncationRate));

        // Population is assumed to be sorted by fitness (best first for minimization)
        var elitePool = population.Take(truncationPoint).ToList();

        // Select uniformly from the elite pool
        var selected = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(elitePool.Count);
            selected.Add(elitePool[idx]);
        }

        return selected;
    }
}
