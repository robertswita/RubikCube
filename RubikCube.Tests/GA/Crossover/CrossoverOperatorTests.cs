using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Crossover;
using Xunit;

namespace RubikCube.Tests.GA.Crossover;

/// <summary>
/// Basic functional tests for all crossover operators.
/// </summary>
public class CrossoverOperatorTests
{
    #region Basic Functionality Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_ReturnsTwoChildren(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_ChildrenHaveCorrectLength(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_ChildrenAreNotSameAsParents(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotSame(parent1, child1);
        Assert.NotSame(parent2, child1);
        Assert.NotSame(parent1, child2);
        Assert.NotSame(parent2, child2);
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_DoNotModifyParents(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);
        var rng = new Random(42);

        // Store original values
        var original1 = parent1.Genes.ToArray();
        var original2 = parent2.Genes.ToArray();

        op.Crossover(parent1, parent2, rng);

        // Verify parents unchanged
        Assert.Equal(original1, parent1.Genes);
        Assert.Equal(original2, parent2.Genes);
    }

    #endregion

    #region Gene Inheritance Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void StandardCrossover_ChildGenesFromParents(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use distinct values to track origin
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // Every gene in children should come from one of the parents
        var allParentGenes = new HashSet<double>(parent1.Genes.Concat(parent2.Genes));
        Assert.All(child1.Genes, g => Assert.Contains(g, allParentGenes));
        Assert.All(child2.Genes, g => Assert.Contains(g, allParentGenes));
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    public void PositionPreservingCrossover_ChildGenesAtCorrectPositions(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // Each position should have gene from either parent1 or parent2 at that position
        for (int i = 0; i < 10; i++)
        {
            bool fromP1 = Math.Abs(child1.Genes[i] - parent1.Genes[i]) < 0.001;
            bool fromP2 = Math.Abs(child1.Genes[i] - parent2.Genes[i]) < 0.001;
            Assert.True(fromP1 || fromP2, $"Child1 gene at position {i} doesn't match either parent");
        }
    }

    #endregion

    #region Determinism Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_SameSeed_SameResults(Type operatorType)
    {
        var op1 = CreateOperator(operatorType);
        var op2 = CreateOperator(operatorType);

        var parent1a = CreateParentWithSequentialGenes(10, 1);
        var parent2a = CreateParentWithSequentialGenes(10, 11);
        var parent1b = CreateParentWithSequentialGenes(10, 1);
        var parent2b = CreateParentWithSequentialGenes(10, 11);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var (child1a, child2a) = op1.Crossover(parent1a, parent2a, rng1);
        var (child1b, child2b) = op2.Crossover(parent1b, parent2b, rng2);

        Assert.Equal(child1a.Genes, child1b.Genes);
        Assert.Equal(child2a.Genes, child2b.Genes);
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    public void AllOperators_DifferentSeeds_LikelyDifferentResults(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use default length (10) to match new T() in crossover operators
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);

        var results = new List<double[]>();
        for (int seed = 0; seed < 10; seed++)
        {
            var rng = new Random(seed);
            var (child1, _) = op.Crossover(parent1, parent2, rng);
            results.Add(child1.Genes.ToArray());
        }

        // At least some results should be different
        int differentCount = 0;
        for (int i = 1; i < results.Count; i++)
        {
            if (!results[0].SequenceEqual(results[i]))
                differentCount++;
        }

        Assert.True(differentCount > 0, "All results were identical with different seeds");
    }

    #endregion

    #region Symmetric Crossover Tests

    [Fact]
    public void SinglePointCrossover_Children_CombineParentSegments()
    {
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        // Run multiple times to verify behavior
        int child1HasMix = 0, child2HasMix = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, child2) = op.Crossover(parent1, parent2, rng);

            bool c1HasBothValues = child1.Genes.Contains(1) && child1.Genes.Contains(2);
            bool c2HasBothValues = child2.Genes.Contains(1) && child2.Genes.Contains(2);

            if (c1HasBothValues) child1HasMix++;
            if (c2HasBothValues) child2HasMix++;
        }

        // Both children should usually have mixed genes
        Assert.True(child1HasMix > 90, $"Child1 had mixed genes only {child1HasMix}/100 times");
        Assert.True(child2HasMix > 90, $"Child2 had mixed genes only {child2HasMix}/100 times");
    }

    [Fact]
    public void TwoPointCrossover_SwapsMiddleSegment()
    {
        var op = new TwoPointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        int mixedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var (child1, _) = op.Crossover(parent1, parent2, rng);

            // Child should have both 1s and 2s
            if (child1.Genes.Contains(1) && child1.Genes.Contains(2))
                mixedCount++;
        }

        Assert.True(mixedCount > 90, $"Two-point crossover produced mixed children only {mixedCount}/100 times");
    }

    [Fact]
    public void UniformCrossover_MixesGenesUniformly()
    {
        var op = new UniformCrossover<MockChromosome>(0.5);
        var parent1 = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var parent2 = MockChromosome.WithGenes(2, 2, 2, 2, 2, 2, 2, 2, 2, 2);

        var rng = new Random(42);
        var (child1, _) = op.Crossover(parent1, parent2, rng);

        // Count genes from each parent
        int fromP1 = child1.Genes.Count(g => Math.Abs(g - 1) < 0.001);
        int fromP2 = child1.Genes.Count(g => Math.Abs(g - 2) < 0.001);

        // Should have mix from both parents
        Assert.True(fromP1 > 0 && fromP2 > 0, "Uniform crossover should mix genes from both parents");
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

    private static MockChromosome CreateParentWithSequentialGenes(int length, int startValue)
    {
        var parent = new MockChromosome(length);
        for (int i = 0; i < length; i++)
            parent.Genes[i] = startValue + i;
        parent.Fitness = startValue; // Lower fitness for lower start value (better)
        return parent;
    }

    #endregion
}
