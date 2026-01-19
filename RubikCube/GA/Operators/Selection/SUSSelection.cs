using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Selection;

/// <summary>
/// Stochastic Universal Sampling (SUS) selection.
///
/// SUS is a single-phase sampling algorithm that gives weaker individuals
/// a better chance of selection compared to traditional roulette wheel selection.
/// Instead of spinning the wheel N times with N random values, SUS uses a single
/// random value and N equally spaced pointers, ensuring more consistent sampling
/// and reducing bias.
///
/// Benefits over roulette wheel:
/// - Zero bias: Expected number of copies equals actual number selected
/// - Minimum spread: Difference between expected and actual is minimized
/// - More consistent diversity preservation
///
/// Works with minimization problems (lower fitness = better).
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SUSSelection<T> : ISelectionOperator<T> where T : IChromosome
{
    public IReadOnlyList<T> Select(IReadOnlyList<T> population, int count, Random rng)
    {
        int populationCount = population.Count;
        if (populationCount == 0 || count == 0)
            return Array.Empty<T>();

        // For minimization: invert fitness values so lower fitness gets higher probability
        // Use max fitness to invert: invertedFitness[i] = maxFitness - fitness[i] + epsilon
        double maxFitness = double.MinValue;
        double minFitness = double.MaxValue;

        for (int i = 0; i < populationCount; i++)
        {
            double f = population[i].Fitness;
            if (f > maxFitness) maxFitness = f;
            if (f < minFitness) minFitness = f;
        }

        // Calculate inverted fitness values
        var invertedFitness = new double[populationCount];
        double totalFitness = 0;
        double epsilon = (maxFitness - minFitness) * 0.01 + 1.0; // Small offset to avoid zero

        for (int i = 0; i < populationCount; i++)
        {
            invertedFitness[i] = maxFitness - population[i].Fitness + epsilon;
            totalFitness += invertedFitness[i];
        }

        // Handle edge case where all fitness values are the same
        if (totalFitness <= 0)
        {
            // Uniform selection
            var uniformResult = new List<T>(count);
            for (int i = 0; i < count; i++)
                uniformResult.Add(population[rng.Next(populationCount)]);
            return uniformResult;
        }

        // Build cumulative fitness array
        var cumulativeFitness = new double[populationCount];
        cumulativeFitness[0] = invertedFitness[0];
        for (int i = 1; i < populationCount; i++)
        {
            cumulativeFitness[i] = cumulativeFitness[i - 1] + invertedFitness[i];
        }

        // SUS: single random start, evenly spaced pointers
        double pointerDistance = totalFitness / count;
        double start = rng.NextDouble() * pointerDistance;

        var selection = new List<T>(count);
        int currentIndex = 0;

        for (int i = 0; i < count; i++)
        {
            double pointer = start + i * pointerDistance;

            // Find the individual at this pointer position
            while (currentIndex < populationCount - 1 && cumulativeFitness[currentIndex] < pointer)
            {
                currentIndex++;
            }

            selection.Add(population[currentIndex]);
        }

        return selection;
    }
}
