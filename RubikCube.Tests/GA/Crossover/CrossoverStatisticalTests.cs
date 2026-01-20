using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Crossover;
using Xunit;

namespace RubikCube.Tests.GA.Crossover;

/// <summary>
/// Statistical tests to verify crossover operators produce expected distributions.
/// </summary>
public class CrossoverStatisticalTests
{
    private const int NumTrials = 1000;

    #region Gene Distribution Tests

    [Fact]
    public void SinglePointCrossover_SplitPointDistribution_IsUniform()
    {
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        var splitCounts = new int[10]; // Split points 1-9

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Find split point
            for (int i = 1; i < child1.Length; i++)
            {
                if (Math.Abs(child1.Genes[i] - child1.Genes[i - 1]) > 0.001)
                {
                    splitCounts[i]++;
                    break;
                }
            }
        }

        // Split points should be roughly uniformly distributed (1-9)
        double expectedCount = NumTrials / 9.0;
        double tolerance = expectedCount * 0.4; // 40% tolerance

        for (int i = 1; i < 10; i++)
        {
            Assert.True(Math.Abs(splitCounts[i] - expectedCount) < tolerance,
                $"Split point {i} count {splitCounts[i]} deviates too much from expected {expectedCount:F0}");
        }
    }

    [Fact]
    public void TwoPointCrossover_MiddleSegmentSize_Varies()
    {
        var op = new TwoPointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        var segmentSizes = new HashSet<int>();

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Count the middle segment (genes from parent2 in child1, assuming child1 starts with parent1)
            int segmentStart = -1, segmentEnd = -1;
            for (int i = 0; i < child1.Length; i++)
            {
                bool fromP2 = Math.Abs(child1.Genes[i] - 2) < 0.001;
                if (fromP2 && segmentStart < 0) segmentStart = i;
                if (fromP2) segmentEnd = i;
            }

            if (segmentStart >= 0)
            {
                int size = segmentEnd - segmentStart + 1;
                segmentSizes.Add(size);
            }
        }

        // Should have variety of segment sizes
        Assert.True(segmentSizes.Count > 5,
            $"Two-point crossover should produce varied segment sizes, but only got {segmentSizes.Count} unique sizes");
    }

    [Fact]
    public void UniformCrossover_GeneSwapRate_MatchesProbability()
    {
        double expectedProbability = 0.3;
        var op = new UniformCrossover<MockChromosome>(expectedProbability);
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        int totalSwaps = 0;
        int totalGenes = 0;

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            totalGenes += child1.Length;
            totalSwaps += child1.Genes.Count(g => Math.Abs(g - 2) < 0.001);
        }

        double actualProbability = (double)totalSwaps / totalGenes;
        Assert.True(Math.Abs(actualProbability - expectedProbability) < 0.05,
            $"Actual swap rate {actualProbability:P} differs from expected {expectedProbability:P}");
    }

    #endregion

    #region Genetic Material Inheritance Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    public void PositionPreservingCrossover_GeneticMaterialBalanced(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        int genesFromP1 = 0, genesFromP2 = 0;

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            genesFromP1 += child1.Genes.Count(g => g <= 10);
            genesFromP2 += child1.Genes.Count(g => g > 10);
        }

        int totalGenes = genesFromP1 + genesFromP2;
        double p1Ratio = (double)genesFromP1 / totalGenes;

        // Should be reasonably balanced (between 25% and 75%)
        Assert.True(p1Ratio > 0.25 && p1Ratio < 0.75,
            $"{operatorType.Name}: P1 ratio {p1Ratio:P} is not balanced");
    }

    [Fact]
    public void SegmentPreservingCrossover_BetterParentFavored()
    {
        var op = new SegmentPreservingCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        parent1.Fitness = 10; // Better
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        parent2.Fitness = 100; // Worse

        int genesFromBetter = 0;
        int totalGenes = 0;

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            totalGenes += child1.Length;
            genesFromBetter += child1.Genes.Count(g => g <= 10);
        }

        double betterRatio = (double)genesFromBetter / totalGenes;

        // Better parent's genes should be preserved more often (> 50%)
        Assert.True(betterRatio > 0.5,
            $"Segment-preserving should favor better parent, but ratio is only {betterRatio:P}");
    }

    #endregion

    #region Offspring Diversity Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    public void AllStochasticOperators_ProduceDiverseOffspring(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use default length (10) to match new T() in crossover operators
        var parent1 = CreateSequentialParent(10, 1);
        var parent2 = CreateSequentialParent(10, 11);

        var uniqueOffspring = new HashSet<string>();

        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Create string representation of genes
            string key = string.Join(",", child1.Genes.Select(g => g.ToString("F0")));
            uniqueOffspring.Add(key);
        }

        // Should produce multiple unique offspring (SinglePointCrossover has only 9 split points for 10 genes)
        Assert.True(uniqueOffspring.Count > 5,
            $"{operatorType.Name}: Only produced {uniqueOffspring.Count} unique offspring in 100 trials");
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    public void PositionPreservingCrossover_ChildrenAreDifferent(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use default length (10) to match new T() in crossover operators
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        int differentChildren = 0;

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, child2) = op.Crossover(parent1, parent2, rng);

            if (!child1.Genes.SequenceEqual(child2.Genes))
                differentChildren++;
        }

        // Children should usually be different
        Assert.True(differentChildren > NumTrials * 0.9,
            $"{operatorType.Name}: Children were different only {differentChildren}/{NumTrials} times");
    }

    #endregion

    #region Coverage Tests

    [Fact]
    public void SinglePointCrossover_AllPositionsCanBeFromEitherParent()
    {
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        var fromP1Count = new int[10];
        var fromP2Count = new int[10];

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            for (int i = 0; i < 10; i++)
            {
                if (child1.Genes[i] <= 10)
                    fromP1Count[i]++;
                else
                    fromP2Count[i]++;
            }
        }

        // Except first (always from P1) and last positions, all should have both
        for (int i = 1; i < 9; i++)
        {
            Assert.True(fromP1Count[i] > 0 && fromP2Count[i] > 0,
                $"Position {i} never received genes from both parents");
        }
    }

    [Fact]
    public void UniformCrossover_AllPositionsReceiveFromBothParents()
    {
        var op = new UniformCrossover<MockChromosome>(0.5);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        var fromP1Count = new int[10];
        var fromP2Count = new int[10];

        for (int trial = 0; trial < NumTrials; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            for (int i = 0; i < 10; i++)
            {
                if (child1.Genes[i] <= 10)
                    fromP1Count[i]++;
                else
                    fromP2Count[i]++;
            }
        }

        // All positions should have received genes from both parents
        for (int i = 0; i < 10; i++)
        {
            Assert.True(fromP1Count[i] > 0 && fromP2Count[i] > 0,
                $"Position {i} never received genes from both parents");
        }
    }

    #endregion

    #region Edge Preservation Statistical Tests

    [Fact]
    public void EdgeRecombinationCrossover_PreservesSignificantEdges()
    {
        var op = new EdgeRecombinationCrossover<MockChromosome>();
        var parent1 = CreateSequentialParent(10, 0); // 0,1,2,3,4,5,6,7,8,9
        var parent2 = CreateSequentialParent(10, 0);
        // Reverse parent2 for different edges
        Array.Reverse(parent2.Genes);

        int totalEdges = 0;
        int preservedEdges = 0;

        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Count edges in child
            for (int i = 0; i < child1.Length - 1; i++)
            {
                totalEdges++;

                double a = child1.Genes[i];
                double b = child1.Genes[i + 1];

                // Check if edge exists in either parent
                if (EdgeExistsIn(parent1.Genes, a, b) || EdgeExistsIn(parent2.Genes, a, b))
                    preservedEdges++;
            }
        }

        double preservationRate = (double)preservedEdges / totalEdges;

        // ERX should preserve a significant portion of edges (> 30%)
        Assert.True(preservationRate > 0.3,
            $"ERX preserved only {preservationRate:P} of edges, expected > 30%");
    }

    private static bool EdgeExistsIn(double[] genes, double a, double b)
    {
        for (int i = 0; i < genes.Length - 1; i++)
        {
            if ((Math.Abs(genes[i] - a) < 0.001 && Math.Abs(genes[i + 1] - b) < 0.001) ||
                (Math.Abs(genes[i] - b) < 0.001 && Math.Abs(genes[i + 1] - a) < 0.001))
                return true;
        }
        return false;
    }

    #endregion

    #region Helper Methods

    private static ICrossoverOperator<MockChromosome> CreateOperator(Type operatorType)
    {
        if (operatorType == typeof(SinglePointCrossover<MockChromosome>))
            return new SinglePointCrossover<MockChromosome>();
        if (operatorType == typeof(TwoPointCrossover<MockChromosome>))
            return new TwoPointCrossover<MockChromosome>();
        if (operatorType == typeof(UniformCrossover<MockChromosome>))
            return new UniformCrossover<MockChromosome>(0.5);
        if (operatorType == typeof(PMXCrossover<MockChromosome>))
            return new PMXCrossover<MockChromosome>();
        if (operatorType == typeof(OrderCrossover<MockChromosome>))
            return new OrderCrossover<MockChromosome>();
        if (operatorType == typeof(CycleCrossover<MockChromosome>))
            return new CycleCrossover<MockChromosome>();
        if (operatorType == typeof(EdgeRecombinationCrossover<MockChromosome>))
            return new EdgeRecombinationCrossover<MockChromosome>();
        if (operatorType == typeof(SegmentPreservingCrossover<MockChromosome>))
            return new SegmentPreservingCrossover<MockChromosome>();

        throw new ArgumentException($"Unknown operator type: {operatorType}");
    }

    private static MockChromosome CreateSequentialParent(int length, int startValue)
    {
        var parent = new MockChromosome(length);
        for (int i = 0; i < length; i++)
            parent.Genes[i] = startValue + i;
        return parent;
    }

    #endregion
}
