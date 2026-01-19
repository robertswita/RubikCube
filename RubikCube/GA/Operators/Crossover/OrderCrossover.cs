using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Order Crossover (OX) - a crossover operator that preserves the relative order
/// of genes from parents.
///
/// Classic OX Algorithm (for permutation problems):
/// 1. Select two random crossover points
/// 2. Copy the segment between points from Parent1 to Child1
/// 3. Fill remaining positions with genes from Parent2, starting after the
///    second crossover point and wrapping around, skipping genes already present
///
/// For Rubik's Cube (non-permutation problem where genes can repeat):
/// Since genes represent moves that can appear multiple times, we adapt OX to:
/// 1. Copy the segment from Parent1 directly to Child1
/// 2. Fill remaining positions from Parent2 in order, preserving relative order
///
/// This preserves the "building blocks" from Parent1's segment while incorporating
/// the relative ordering of moves from Parent2.
///
/// Example:
/// P1: [A B | C D E | F G H]  (segment marked with |)
/// P2: [H G F E D C B A]
///
/// Child1: [G F | C D E | B A]  (segment from P1, rest filled from P2 in order)
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class OrderCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;
        // Order crossover requires at least 3 genes to have a meaningful segment to preserve.
        // With fewer genes, fall back to copying parents (let mutation provide variation).
        if (length < 3)
        {
            var c1 = new T();
            var c2 = new T();
            Array.Copy(parent1.Genes, c1.Genes, length);
            Array.Copy(parent2.Genes, c2.Genes, length);
            return (c1, c2);
        }

        // Select two crossover points (ensuring at least 1 gene in segment)
        int point1 = rng.Next(0, length - 1);
        int point2 = rng.Next(point1 + 1, length);

        var child1 = CreateChild(parent1, parent2, point1, point2, length);
        var child2 = CreateChild(parent2, parent1, point1, point2, length);

        return (child1, child2);
    }

    private static T CreateChild(T segmentParent, T fillParent, int point1, int point2, int length)
    {
        var child = new T();

        // Step 1: Copy segment from segmentParent to child
        for (int i = point1; i < point2; i++)
        {
            child.Genes[i] = segmentParent.Genes[i];
        }

        // Step 2: Fill remaining positions from fillParent in order
        // Start filling from position after point2, wrapping around
        int fillPosition = point2;
        int sourcePosition = point2;

        // Fill positions from point2 to end, then from 0 to point1
        int positionsToFill = length - (point2 - point1);

        for (int filled = 0; filled < positionsToFill; filled++)
        {
            // Wrap fill position
            if (fillPosition >= length)
                fillPosition = 0;

            // Skip the segment area
            if (fillPosition >= point1 && fillPosition < point2)
            {
                fillPosition = point2;
                if (fillPosition >= length)
                    fillPosition = 0;
            }

            // Wrap source position
            if (sourcePosition >= length)
                sourcePosition = 0;

            child.Genes[fillPosition] = fillParent.Genes[sourcePosition];

            fillPosition++;
            sourcePosition++;
        }

        return child;
    }
}
