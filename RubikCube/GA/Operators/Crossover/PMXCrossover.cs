using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Partially Mapped Crossover (PMX) - a crossover operator that maintains
/// absolute position relationships between genes.
///
/// Classic PMX Algorithm (for permutation problems):
/// 1. Select two random crossover points
/// 2. Copy the segment between points from Parent1 to Child1
/// 3. For each position outside the segment in Child1:
///    a. Look at the gene at that position in Parent2
///    b. If it's not in the copied segment, use it directly
///    c. If it IS in the segment, follow the mapping chain:
///       - Find where this gene is in Parent1's segment
///       - Get the corresponding gene from Parent2 at that position
///       - Repeat until finding a gene not in the segment
///
/// For Rubik's Cube (non-permutation problem where genes can repeat):
/// PMX still provides meaningful recombination by:
/// - Preserving a segment of moves from one parent
/// - Using position-based mapping to fill remaining positions
/// - Handling repeated genes gracefully with cycle detection
///
/// This creates children that maintain local structure from one parent
/// while incorporating mapped genetic material from the other.
///
/// Example (permutation case):
/// P1: [1 2 | 3 4 5 | 6 7 8]
/// P2: [4 7 | 2 5 1 | 8 3 6]
///
/// Segment: positions 2-4
/// Mappings from segment: 3↔2, 4↔5, 5↔1
///
/// Child1 segment: [_ _ | 3 4 5 | _ _ _]
/// Position 0: P2[0]=4, 4 is in segment, map 4→5→1, use 1
/// Position 1: P2[1]=7, not in segment, use 7
/// etc.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class PMXCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;
        if (length < 3)
        {
            // Fall back to simple swap for very short chromosomes
            var c1 = new T();
            var c2 = new T();
            Array.Copy(parent1.Genes, c1.Genes, length);
            Array.Copy(parent2.Genes, c2.Genes, length);
            return (c1, c2);
        }

        // Select two crossover points
        int point1 = rng.Next(0, length - 1);
        int point2 = rng.Next(point1 + 1, length);

        var child1 = CreateChild(parent1, parent2, point1, point2, length);
        var child2 = CreateChild(parent2, parent1, point1, point2, length);

        return (child1, child2);
    }

    private static T CreateChild(T segmentParent, T mappingParent, int point1, int point2, int length)
    {
        var child = new T();

        // Step 1: Copy segment from segmentParent to child
        var segmentGenes = new HashSet<double>();
        for (int i = point1; i < point2; i++)
        {
            child.Genes[i] = segmentParent.Genes[i];
            segmentGenes.Add(segmentParent.Genes[i]);
        }

        // Step 2: Build mapping from segment (segmentParent gene → mappingParent gene)
        var mapping = new Dictionary<double, double>();
        for (int i = point1; i < point2; i++)
        {
            double segmentGene = segmentParent.Genes[i];
            double mappingGene = mappingParent.Genes[i];
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (segmentGene != mappingGene)
            {
                mapping[segmentGene] = mappingGene;
            }
        }

        // Step 3: Fill positions outside the segment
        for (int i = 0; i < length; i++)
        {
            // Skip segment positions (already filled)
            if (i >= point1 && i < point2)
                continue;

            double gene = mappingParent.Genes[i];

            // If gene is not in segment, use it directly
            if (!segmentGenes.Contains(gene))
            {
                child.Genes[i] = gene;
            }
            else
            {
                // Follow mapping chain until finding a gene not in segment
                // Use visited set to detect cycles and prevent infinite loops
                var visited = new HashSet<double> { gene };
                double mappedGene = gene;

                while (segmentGenes.Contains(mappedGene) && mapping.TryGetValue(mappedGene, out double nextGene))
                {
                    if (visited.Contains(nextGene))
                    {
                        // Cycle detected - use the current mapped gene
                        break;
                    }
                    visited.Add(nextGene);
                    mappedGene = nextGene;
                }

                child.Genes[i] = mappedGene;
            }
        }

        return child;
    }
}
