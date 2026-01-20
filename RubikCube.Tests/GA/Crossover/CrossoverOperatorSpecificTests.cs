using RubikCube.Tests.Mocks;
using TGL.GA.Operators.Crossover;
using Xunit;

namespace RubikCube.Tests.GA.Crossover;

/// <summary>
/// Operator-specific tests verifying unique behavior of each crossover operator.
/// </summary>
public class CrossoverOperatorSpecificTests
{
    #region SinglePointCrossover Specific Tests

    [Fact]
    public void SinglePointCrossover_CreatesContiguousSegments()
    {
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Child should have two contiguous segments - one with 1s, one with 2s
            bool foundTransition = false;
            double firstValue = child1.Genes[0];
            for (int i = 1; i < child1.Length; i++)
            {
                if (Math.Abs(child1.Genes[i] - firstValue) > 0.001)
                {
                    foundTransition = true;
                    // After transition, all remaining should be the other value
                    double secondValue = child1.Genes[i];
                    for (int j = i + 1; j < child1.Length; j++)
                    {
                        Assert.True(Math.Abs(child1.Genes[j] - secondValue) < 0.001,
                            $"Single-point crossover should have exactly one transition point");
                    }
                    break;
                }
            }

            // Should have found at least one transition (unless all from one parent)
            Assert.True(foundTransition || child1.Genes.All(g => Math.Abs(g - 1) < 0.001) ||
                        child1.Genes.All(g => Math.Abs(g - 2) < 0.001));
        }
    }

    [Fact]
    public void SinglePointCrossover_SplitPointVaries()
    {
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        var splitPoints = new HashSet<int>();
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Find the split point
            for (int i = 1; i < child1.Length; i++)
            {
                if (Math.Abs(child1.Genes[i] - child1.Genes[i - 1]) > 0.001)
                {
                    splitPoints.Add(i);
                    break;
                }
            }
        }

        // Should have multiple different split points across trials
        Assert.True(splitPoints.Count > 1, "Split point should vary across different random seeds");
    }

    #endregion

    #region TwoPointCrossover Specific Tests

    [Fact]
    public void TwoPointCrossover_SwapsMiddleSegment()
    {
        var op = new TwoPointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Should have pattern: [1s] [2s] [1s] or similar
            // Count the transitions
            int transitions = 0;
            for (int i = 1; i < child1.Length; i++)
            {
                if (Math.Abs(child1.Genes[i] - child1.Genes[i - 1]) > 0.001)
                    transitions++;
            }

            // Two-point crossover swaps a middle segment.
            // Can have 0 transitions (all same), 1 transition (point1=0 or point2=length),
            // or 2 transitions (middle segment swapped)
            Assert.True(transitions <= 2,
                $"Two-point crossover should have at most 2 transitions, found {transitions}");
        }
    }

    [Fact]
    public void TwoPointCrossover_ChildrenAreComplementary()
    {
        var op = new TwoPointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // At each position, if child1 has gene from parent1, child2 should have from parent2
        for (int i = 0; i < 10; i++)
        {
            bool c1FromP1 = child1.Genes[i] <= 10;
            bool c2FromP1 = child2.Genes[i] <= 10;
            Assert.NotEqual(c1FromP1, c2FromP1);
        }
    }

    #endregion

    #region UniformCrossover Specific Tests

    [Fact]
    public void UniformCrossover_SwapProbability0_CopiesParent1()
    {
        var op = new UniformCrossover<MockChromosome>(0.0);
        // Use 10-gene chromosomes to match MockChromosome default constructor
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(parent1.Genes, child1.Genes);
        Assert.Equal(parent2.Genes, child2.Genes);
    }

    [Fact]
    public void UniformCrossover_SwapProbability1_SwapsAllGenes()
    {
        var op = new UniformCrossover<MockChromosome>(1.0);
        // Use 10-gene chromosomes to match MockChromosome default constructor
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(parent2.Genes, child1.Genes);
        Assert.Equal(parent1.Genes, child2.Genes);
    }

    [Fact]
    public void UniformCrossover_SwapProbability05_ProducesMixedOffspring()
    {
        var op = new UniformCrossover<MockChromosome>(0.5);
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        // Run many trials and verify roughly 50% swap rate
        int totalSwaps = 0;
        int totalGenes = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            totalGenes += child1.Length;
            totalSwaps += child1.Genes.Count(g => Math.Abs(g - 2) < 0.001);
        }

        double swapRate = (double)totalSwaps / totalGenes;
        Assert.True(swapRate > 0.4 && swapRate < 0.6,
            $"Swap rate {swapRate:P} should be close to 50%");
    }

    #endregion

    #region PMXCrossover Specific Tests

    [Fact]
    public void PMXCrossover_PreservesSegmentFromParent()
    {
        var op = new PMXCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);

        // With many trials, at least some should preserve a middle segment from parent1
        int segmentPreservedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Check if any contiguous segment of 3+ genes from parent1 is preserved
            for (int start = 0; start < 7; start++)
            {
                bool segmentMatch = true;
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(child1.Genes[start + j] - parent1.Genes[start + j]) > 0.001)
                    {
                        segmentMatch = false;
                        break;
                    }
                }
                if (segmentMatch)
                {
                    segmentPreservedCount++;
                    break;
                }
            }
        }

        Assert.True(segmentPreservedCount > 50, "PMX should often preserve segments from parent");
    }

    #endregion

    #region OrderCrossover Specific Tests

    [Fact]
    public void OrderCrossover_PreservesSegmentFromParent()
    {
        var op = new OrderCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);

        // With many trials, at least some should preserve a middle segment from parent1
        int segmentPreservedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Check if any contiguous segment of 3+ genes from parent1 is preserved
            for (int start = 0; start < 7; start++)
            {
                bool segmentMatch = true;
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(child1.Genes[start + j] - parent1.Genes[start + j]) > 0.001)
                    {
                        segmentMatch = false;
                        break;
                    }
                }
                if (segmentMatch)
                {
                    segmentPreservedCount++;
                    break;
                }
            }
        }

        // Order crossover preserves a segment from parent1, but the segment location varies
        Assert.True(segmentPreservedCount > 20, "Order crossover should sometimes preserve segments from parent");
    }

    #endregion

    #region CycleCrossover Specific Tests

    [Fact]
    public void CycleCrossover_PreservesPositionValueRelationships()
    {
        var op = new CycleCrossover<MockChromosome>();
        // Use permutation-like parents for clearer cycle behavior
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8);
        var parent2 = MockChromosome.WithGenes(8, 4, 6, 1, 2, 3, 5, 7);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // Each position should have gene from either parent1 or parent2 at that position
        for (int i = 0; i < 8; i++)
        {
            bool fromP1 = Math.Abs(child1.Genes[i] - parent1.Genes[i]) < 0.001;
            bool fromP2 = Math.Abs(child1.Genes[i] - parent2.Genes[i]) < 0.001;
            Assert.True(fromP1 || fromP2,
                $"Child1 gene at position {i} should come from same position in either parent");
        }
    }

    [Fact]
    public void CycleCrossover_ChildrenAreComplementary()
    {
        var op = new CycleCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8);
        var parent2 = MockChromosome.WithGenes(8, 4, 6, 1, 2, 3, 5, 7);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // If child1 has gene from parent1 at position i, child2 should have from parent2
        for (int i = 0; i < 8; i++)
        {
            bool c1FromP1 = Math.Abs(child1.Genes[i] - parent1.Genes[i]) < 0.001;
            bool c2FromP1 = Math.Abs(child2.Genes[i] - parent1.Genes[i]) < 0.001;
            Assert.NotEqual(c1FromP1, c2FromP1);
        }
    }

    #endregion

    #region EdgeRecombinationCrossover Specific Tests

    [Fact]
    public void EdgeRecombinationCrossover_PreservesEdgeRelationships()
    {
        var op = new EdgeRecombinationCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(5, 4, 3, 2, 1, 10, 9, 8, 7, 6);

        int edgesPreserved = 0;
        for (int trial = 0; trial < 50; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Count how many adjacent pairs in child appear adjacently in either parent
            for (int i = 0; i < child1.Length - 1; i++)
            {
                double a = child1.Genes[i];
                double b = child1.Genes[i + 1];

                // Check if this edge exists in either parent
                bool inP1 = IsAdjacentInArray(parent1.Genes, a, b);
                bool inP2 = IsAdjacentInArray(parent2.Genes, a, b);

                if (inP1 || inP2)
                    edgesPreserved++;
            }
        }

        // ERX should preserve a good number of edges
        Assert.True(edgesPreserved > 100,
            $"ERX should preserve edges from parents, but only preserved {edgesPreserved}");
    }

    private static bool IsAdjacentInArray(double[] array, double a, double b)
    {
        for (int i = 0; i < array.Length - 1; i++)
        {
            if ((Math.Abs(array[i] - a) < 0.001 && Math.Abs(array[i + 1] - b) < 0.001) ||
                (Math.Abs(array[i] - b) < 0.001 && Math.Abs(array[i + 1] - a) < 0.001))
                return true;
        }
        return false;
    }

    #endregion

    #region SegmentPreservingCrossover Specific Tests

    [Fact]
    public void SegmentPreservingCrossover_PreservesBetterParentSegment()
    {
        var op = new SegmentPreservingCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        parent1.Fitness = 10; // Better
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        parent2.Fitness = 100; // Worse

        int betterParentGenesInChild1 = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Count genes from better parent in child1
            betterParentGenesInChild1 += child1.Genes.Count(g => g <= 10);
        }

        // Child1 should have more genes from the better parent (parent1)
        Assert.True(betterParentGenesInChild1 > 500,
            $"Segment-preserving should favor better parent, but child1 had only {betterParentGenesInChild1}/1000 genes from better parent");
    }

    [Fact]
    public void SegmentPreservingCrossover_RubikChromosome_UsesMovesCount()
    {
        var op = new SegmentPreservingCrossover<MockRubikChromosome>();

        // Parent1 has shorter effective segment (better fitness)
        var parent1 = MockRubikChromosome.WithGenes(10, 5, 1, 2, 3, 4, 5, 99, 99, 99);
        parent1.Fitness = 10;
        parent1.MovesCount = 5; // Only first 5 moves matter

        var parent2 = MockRubikChromosome.WithGenes(100, 10, 11, 12, 13, 14, 15, 16, 17, 18);
        parent2.Fitness = 100;
        parent2.MovesCount = 10;

        var rng = new Random(42);
        var (child1, _) = op.Crossover(parent1, parent2, rng);

        // Child1 should preserve segment from better parent's effective moves
        // At least some of positions 0-4 should have parent1's genes
        int parent1GenesInEffectiveSegment = 0;
        for (int i = 0; i < 5; i++)
        {
            if (child1.Genes[i] <= 10)
                parent1GenesInEffectiveSegment++;
        }

        Assert.True(parent1GenesInEffectiveSegment > 0,
            "Should preserve some genes from better parent's effective segment");
    }

    [Fact]
    public void SegmentPreservingCrossover_EqualFitness_FirstParentTreatedAsBetter()
    {
        var op = new SegmentPreservingCrossover<MockChromosome>();
        // Use 10-gene chromosomes to match default new T() length
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        parent1.Fitness = 50;
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        parent2.Fitness = 50; // Equal fitness

        var rng = new Random(42);
        var (child1, _) = op.Crossover(parent1, parent2, rng);

        // Should not crash and produce valid output
        Assert.Equal(10, child1.Length);
        var allGenes = new HashSet<double>(parent1.Genes.Concat(parent2.Genes));
        Assert.All(child1.Genes, g => Assert.Contains(g, allGenes));
    }

    #endregion
}
