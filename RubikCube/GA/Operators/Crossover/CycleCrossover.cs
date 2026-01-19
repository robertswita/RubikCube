using System;
using System.Collections.Generic;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Cycle Crossover (CX) - a permutation-preserving crossover operator.
///
/// Classic CX Algorithm (for permutation problems):
/// 1. Start at position 0 in Parent1
/// 2. Identify the "cycle" - positions where Parent1 and Parent2 genes
///    form a closed loop when following: P1[i] → find P1[i] in P2 → use that position
/// 3. Child1 gets Parent1 genes at cycle positions, Parent2 genes elsewhere
/// 4. Child2 gets Parent2 genes at cycle positions, Parent1 genes elsewhere
///
/// For Rubik's Cube (non-permutation problem where genes can repeat):
/// CX still provides meaningful recombination by:
/// - Identifying positions where parent genes form natural groupings
/// - Preserving position-value relationships within cycles
/// - Creating children that inherit coherent blocks from each parent
///
/// Example (permutation case):
/// Position: [0  1  2  3  4  5  6  7]
/// P1:       [1  2  3  4  5  6  7  8]
/// P2:       [8  4  6  1  2  3  5  7]
///
/// Cycle starting at position 0:
/// - P1[0]=1, find 1 in P2 → position 3
/// - P1[3]=4, find 4 in P2 → position 1
/// - P1[1]=2, find 2 in P2 → position 4
/// - P1[4]=5, find 5 in P2 → position 6
/// - P1[6]=7, find 7 in P2 → position 7
/// - P1[7]=8, find 8 in P2 → position 0 (back to start)
/// Cycle positions: {0, 1, 3, 4, 6, 7}
///
/// Child1: P1 at cycle positions, P2 elsewhere
/// [1  2  6  4  5  3  7  8]
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class CycleCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;
        if (length < 2)
        {
            var c1 = new T();
            var c2 = new T();
            Array.Copy(parent1.Genes, c1.Genes, length);
            Array.Copy(parent2.Genes, c2.Genes, length);
            return (c1, c2);
        }

        var child1 = new T();
        var child2 = new T();

        // Build index lookup for Parent2 (gene value → first position with that value)
        var p2Positions = new Dictionary<double, int>();
        for (int i = 0; i < length; i++)
        {
            double gene = parent2.Genes[i];
            // Only store first occurrence (for non-permutation problems)
            if (!p2Positions.ContainsKey(gene))
            {
                p2Positions[gene] = i;
            }
        }

        // Track which positions belong to current cycle (true = from P1, false = from P2)
        var fromParent1 = new bool[length];
        var visited = new bool[length];

        // Find all cycles, alternating which parent they come from
        bool currentCycleFromP1 = true;

        for (int startPos = 0; startPos < length; startPos++)
        {
            if (visited[startPos])
                continue;

            // Follow the cycle starting at this position
            var cyclePositions = new List<int>();
            int pos = startPos;

            while (!visited[pos])
            {
                visited[pos] = true;
                cyclePositions.Add(pos);

                // Find where P1[pos] is located in P2
                double gene = parent1.Genes[pos];
                if (p2Positions.TryGetValue(gene, out int nextPos) && nextPos != pos && !visited[nextPos])
                {
                    pos = nextPos;
                }
                else
                {
                    // No valid next position - cycle ends here
                    // Try to continue from P2's perspective for non-permutation problems
                    double p2Gene = parent2.Genes[pos];
                    bool found = false;
                    for (int i = 0; i < length && !found; i++)
                    {
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (!visited[i] && parent1.Genes[i] == p2Gene)
                        {
                            pos = i;
                            found = true;
                        }
                    }
                    if (!found) break;
                }
            }

            // Mark all positions in this cycle
            foreach (int cyclePos in cyclePositions)
            {
                fromParent1[cyclePos] = currentCycleFromP1;
            }

            // Alternate for next cycle
            currentCycleFromP1 = !currentCycleFromP1;
        }

        // Build children based on cycle membership
        for (int i = 0; i < length; i++)
        {
            if (fromParent1[i])
            {
                child1.Genes[i] = parent1.Genes[i];
                child2.Genes[i] = parent2.Genes[i];
            }
            else
            {
                child1.Genes[i] = parent2.Genes[i];
                child2.Genes[i] = parent1.Genes[i];
            }
        }

        return (child1, child2);
    }
}
