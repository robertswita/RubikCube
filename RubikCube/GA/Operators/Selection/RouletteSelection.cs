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
        var fitness = new double[populationCount];
        double fitnessSum = 0;

        // Calculate inverted fitness (since lower is better)
        for (int i = 0; i < populationCount; i++)
        {
            fitness[i] = population[i].Fitness;
            fitnessSum += fitness[i];
        }

        // Reverse so that best individuals have highest cumulative probability
        Array.Reverse(fitness);

        // Build cumulative probability distribution
        var cumulativeProb = new double[populationCount];
        cumulativeProb[0] = fitness[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeProb[i] = cumulativeProb[i - 1] + fitness[i];
        }

        // Select using binary search
        var selection = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            double p = rng.NextDouble() * fitnessSum;
            int first = 0;
            int last = populationCount - 1;

            while (first < last - 1)
            {
                int middle = (last + first) / 2;
                if (p < cumulativeProb[middle])
                    last = middle;
                else
                    first = middle;
            }

            selection.Add(population[first]);
        }

        return selection;
    }
}
