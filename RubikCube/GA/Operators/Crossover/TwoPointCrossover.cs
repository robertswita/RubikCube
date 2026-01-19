using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Two-point crossover - swaps the segment between two random points.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class TwoPointCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;

        int point1 = rng.Next(0, length - 1);
        int point2 = rng.Next(point1 + 1, length);

        var child1 = new T();
        var child2 = new T();

        for (int i = 0; i < length; i++)
        {
            if (i < point1 || i >= point2)
            {
                // Outside the swap region
                child1.Genes[i] = parent1.Genes[i];
                child2.Genes[i] = parent2.Genes[i];
            }
            else
            {
                // Inside the swap region
                child1.Genes[i] = parent2.Genes[i];
                child2.Genes[i] = parent1.Genes[i];
            }
        }

        return (child1, child2);
    }
}
