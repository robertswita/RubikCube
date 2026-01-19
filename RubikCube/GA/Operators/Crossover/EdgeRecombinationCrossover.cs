using System;
using System.Collections.Generic;
using System.Linq;
using TGL.GA.Interfaces;

namespace TGL.GA.Operators.Crossover;

/// <summary>
/// Edge Recombination Crossover (ERX) - preserves edge (adjacency) relationships.
///
/// Classic ERX Algorithm (for TSP/permutation problems):
/// 1. Build an edge table: for each gene, list its neighbors in both parents
/// 2. Start with a random gene or one with fewest edges
/// 3. Repeat until child is complete:
///    a. Remove current gene from all neighbor lists
///    b. If current gene has neighbors, pick the one with fewest remaining edges
///    c. Otherwise, pick a random unvisited gene
///
/// For Rubik's Cube (non-permutation problem):
/// ERX provides meaningful recombination by:
/// - Preserving move sequences that appear adjacently in either parent
/// - Favoring moves that are "well-connected" in the parent solutions
/// - Creating children that maintain local move relationships
///
/// This is especially useful for Rubik's cube because adjacent moves often
/// form meaningful patterns (setup moves, triggers, etc.).
///
/// Example:
/// P1: [A B C D E]
/// P2: [B D A C E]
///
/// Edge table:
/// A: {B, D, C} (B from P1, D and C from P2)
/// B: {A, C, D} (A and C from P1, D from P2)
/// C: {B, D, A, E} (B and D from P1, A and E from P2)
/// D: {C, E, B, A} (C and E from P1, B and A from P2)
/// E: {D, C} (D from P1, C from P2)
///
/// Building child starting from A:
/// - Pick A, neighbors are {B, D, C}
/// - B has 2 edges left, D has 3, C has 3 → pick B
/// - etc.
/// </summary>
/// <typeparam name="T">The chromosome type.</typeparam>
public class EdgeRecombinationCrossover<T> : ICrossoverOperator<T> where T : IChromosome, new()
{
    public (T child1, T child2) Crossover(T parent1, T parent2, Random rng)
    {
        int length = parent1.Length;
        if (length < 3)
        {
            var c1 = new T();
            var c2 = new T();
            Array.Copy(parent1.Genes, c1.Genes, length);
            Array.Copy(parent2.Genes, c2.Genes, length);
            return (c1, c2);
        }

        // Create two children using ERX
        var child1 = CreateChild(parent1, parent2, rng, length);
        var child2 = CreateChild(parent2, parent1, rng, length);

        return (child1, child2);
    }

    private static T CreateChild(T primaryParent, T secondaryParent, Random rng, int length)
    {
        var child = new T();

        // Build edge table: gene -> set of neighboring genes
        var edgeTable = BuildEdgeTable(primaryParent, secondaryParent, length);

        // Get all unique genes from primary parent
        var allGenes = new List<double>();
        var usedGenes = new HashSet<double>();

        for (int i = 0; i < length; i++)
        {
            double gene = primaryParent.Genes[i];
            if (!usedGenes.Contains(gene))
            {
                allGenes.Add(gene);
                usedGenes.Add(gene);
            }
        }

        // Track which genes have been placed
        var placed = new HashSet<double>();
        var remaining = new HashSet<double>(allGenes);

        // Start with the gene that has fewest edges, or random
        double current = SelectStartGene(edgeTable, allGenes, rng);
        child.Genes[0] = current;
        placed.Add(current);
        remaining.Remove(current);
        RemoveFromEdgeTable(edgeTable, current);

        // Fill remaining positions
        for (int pos = 1; pos < length; pos++)
        {
            // If we've used all unique genes, start reusing from parent
            if (remaining.Count == 0)
            {
                // For non-permutation: just copy remaining from primary parent
                child.Genes[pos] = primaryParent.Genes[pos];
                continue;
            }

            // Get neighbors of current gene that haven't been placed
            HashSet<double>? neighbors = null;
            if (edgeTable.TryGetValue(current, out var currentNeighbors))
            {
                neighbors = currentNeighbors;
            }

            double next;
            if (neighbors != null && neighbors.Count > 0)
            {
                // Pick neighbor with fewest remaining edges (greedy heuristic)
                next = SelectBestNeighbor(edgeTable, neighbors, rng);
            }
            else
            {
                // No available neighbors - pick random from remaining
                int idx = rng.Next(remaining.Count);
                next = remaining.ElementAt(idx);
            }

            child.Genes[pos] = next;
            placed.Add(next);
            remaining.Remove(next);
            RemoveFromEdgeTable(edgeTable, next);
            current = next;
        }

        return child;
    }

    private static Dictionary<double, HashSet<double>> BuildEdgeTable(T parent1, T parent2, int length)
    {
        var edgeTable = new Dictionary<double, HashSet<double>>();

        void AddEdge(double from, double to)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (from == to) return; // No self-edges

            if (!edgeTable.TryGetValue(from, out var neighbors))
            {
                neighbors = new HashSet<double>();
                edgeTable[from] = neighbors;
            }
            neighbors.Add(to);
        }

        // Add edges from parent1
        for (int i = 0; i < length; i++)
        {
            double gene = parent1.Genes[i];
            if (i > 0)
                AddEdge(gene, parent1.Genes[i - 1]);
            if (i < length - 1)
                AddEdge(gene, parent1.Genes[i + 1]);
        }

        // Add edges from parent2
        for (int i = 0; i < length; i++)
        {
            double gene = parent2.Genes[i];
            if (i > 0)
                AddEdge(gene, parent2.Genes[i - 1]);
            if (i < length - 1)
                AddEdge(gene, parent2.Genes[i + 1]);
        }

        return edgeTable;
    }

    private static double SelectStartGene(Dictionary<double, HashSet<double>> edgeTable, List<double> allGenes, Random rng)
    {
        // Pick gene with fewest edges (most constrained)
        double best = allGenes[0];
        int bestCount = int.MaxValue;

        foreach (double gene in allGenes)
        {
            int count = edgeTable.TryGetValue(gene, out var neighbors) ? neighbors.Count : 0;
            if (count < bestCount)
            {
                bestCount = count;
                best = gene;
            }
        }

        // If multiple genes have same count, we already pick the first one (deterministic start)
        // Add some randomization: 30% chance to pick random instead
        if (rng.NextDouble() < 0.3)
        {
            return allGenes[rng.Next(allGenes.Count)];
        }

        return best;
    }

    private static double SelectBestNeighbor(Dictionary<double, HashSet<double>> edgeTable, HashSet<double> candidates, Random rng)
    {
        // Find candidates with minimum edge count
        var best = new List<double>();
        int bestCount = int.MaxValue;

        foreach (double candidate in candidates)
        {
            int count = edgeTable.TryGetValue(candidate, out var neighbors) ? neighbors.Count : 0;
            if (count < bestCount)
            {
                bestCount = count;
                best.Clear();
                best.Add(candidate);
            }
            else if (count == bestCount)
            {
                best.Add(candidate);
            }
        }

        // Random tie-breaker
        return best[rng.Next(best.Count)];
    }

    private static void RemoveFromEdgeTable(Dictionary<double, HashSet<double>> edgeTable, double gene)
    {
        // Remove this gene from all neighbor lists
        foreach (var neighbors in edgeTable.Values)
        {
            neighbors.Remove(gene);
        }
    }
}
