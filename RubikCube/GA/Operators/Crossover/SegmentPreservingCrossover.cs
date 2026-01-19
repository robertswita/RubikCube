using System;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Segment-preserving crossover - identifies and preserves "good" segments from parents.
/// For Rubik's cube chromosomes, preserves the segment that achieved the best fitness
/// (up to MovesCount) from the better parent, filling the rest from the other parent.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class SegmentPreservingCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;

        var child1 = new T();
        var child2 = new T();

        // Determine the better parent based on fitness (lower is better)
        T betterParent, worseParent;
        if (parent1.Fitness <= parent2.Fitness)
        {
            betterParent = parent1;
            worseParent = parent2;
        }
        else
        {
            betterParent = parent2;
            worseParent = parent1;
        }

        // For Rubik chromosomes, get the effective length (MovesCount)
        int preserveEnd = length;
        if (betterParent is IRubikChromosome rubikBetter && rubikBetter.MovesCount > 0)
        {
            preserveEnd = Math.Min(rubikBetter.MovesCount, length);
        }

        // Determine preserve start - keep at least half of the effective segment.
        // Guard against preserveEnd <= 1 where rng.Next(0) would throw.
        int preserveStart = preserveEnd > 1 ? rng.Next(preserveEnd / 2) : 0;

        // Child 1: Preserve segment from better parent, rest from worse parent
        // [worseParent: 0..preserveStart) + [betterParent: preserveStart..preserveEnd) + [worseParent: preserveEnd..length)
        for (int i = 0; i < length; i++)
        {
            if (i >= preserveStart && i < preserveEnd)
            {
                child1.Genes[i] = betterParent.Genes[i];
            }
            else
            {
                child1.Genes[i] = worseParent.Genes[i];
            }
        }

        // Child 2: Reverse - preserve from worse parent's effective segment, rest from better
        int worsePreserveEnd = length;
        if (worseParent is IRubikChromosome rubikWorse && rubikWorse.MovesCount > 0)
        {
            worsePreserveEnd = Math.Min(rubikWorse.MovesCount, length);
        }
        // Guard against worsePreserveEnd <= 1 where rng.Next(0) would throw.
        int worsePreserveStart = worsePreserveEnd > 1 ? rng.Next(worsePreserveEnd / 2) : 0;

        for (int i = 0; i < length; i++)
        {
            if (i >= worsePreserveStart && i < worsePreserveEnd)
            {
                child2.Genes[i] = worseParent.Genes[i];
            }
            else
            {
                child2.Genes[i] = betterParent.Genes[i];
            }
        }

        return (child1, child2);
    }
}
