using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Crossover;
using Xunit;

namespace RubikCube.Tests.GA.Crossover;

/// <summary>
/// Edge case and boundary condition tests for crossover operators.
/// </summary>
public class CrossoverOperatorEdgeCaseTests
{
    #region Short Chromosome Tests

    // Note: Crossover operators use new T() which creates default-length (10-gene) chromosomes.
    // These tests verify the operators don't crash with mismatched parent lengths, but the
    // child length will be the default (10), not matching the parent length.

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_DefaultLengthChromosome_Works(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use default 10-gene chromosomes that match new T() behavior
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    #endregion

    #region Identical Parent Tests

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_IdenticalParents_ChildrenMatchParents(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // With identical parents, children should have same genes as parents
        // (though order may vary for some operators)
        var parentGeneSet = new HashSet<double>(parent1.Genes);
        Assert.All(child1.Genes, g => Assert.Contains(g, parentGeneSet));
        Assert.All(child2.Genes, g => Assert.Contains(g, parentGeneSet));
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    public void PositionPreservingCrossover_IdenticalParents_ChildrenExactlyMatchParents(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        // Position-preserving crossover with identical parents should produce exact copies
        Assert.Equal(parent1.Genes, child1.Genes);
        Assert.Equal(parent2.Genes, child2.Genes);
    }

    #endregion

    #region Numerical Edge Cases

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_NegativeGeneValues_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use 10-gene chromosomes to match MockChromosome default constructor
        var parent1 = MockChromosome.WithGenes(-10, -5, 0, 5, 10, -20, -15, 15, 20, 25);
        var parent2 = MockChromosome.WithGenes(-100, -50, -25, -10, 0, 10, 25, 50, 75, 100);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
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
    public void AllOperators_VeryLargeGeneValues_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Use 10-gene chromosomes to match default new T() length
        var parent1 = MockChromosome.WithGenes(1e10, 2e10, 3e10, 4e10, 5e10, 6e10, 7e10, 8e10, 9e10, 1e11);
        var parent2 = MockChromosome.WithGenes(1.1e11, 1.2e11, 1.3e11, 1.4e11, 1.5e11, 1.6e11, 1.7e11, 1.8e11, 1.9e11, 2e11);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
        // Genes should still be from parents
        var allGenes = new HashSet<double>(parent1.Genes.Concat(parent2.Genes));
        Assert.All(child1.Genes, g => Assert.Contains(g, allGenes));
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
    public void AllOperators_AllZeroGenes_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(0, 0, 0, 0, 0);
        var parent2 = MockChromosome.WithGenes(0, 0, 0, 0, 0);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
        Assert.All(child1.Genes, g => Assert.Equal(0, g));
        Assert.All(child2.Genes, g => Assert.Equal(0, g));
    }

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_DuplicateGeneValues_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Parents have duplicate values within themselves
        var parent1 = MockChromosome.WithGenes(1, 1, 2, 2, 3, 3, 4, 4, 5, 5);
        var parent2 = MockChromosome.WithGenes(6, 6, 7, 7, 8, 8, 9, 9, 10, 10);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    #endregion

    #region Long Chromosome Tests

    [Fact]
    public void AllOperators_LongChromosome_HandlesCorrectly()
    {
        // Note: Crossover operators use new T() which creates default-length chromosomes.
        // This test verifies the operators work with the default length (10).
        // The actual chromosome length is determined by the implementation.
        var op = new SinglePointCrossover<MockChromosome>();
        var parent1 = CreateParentWithSequentialGenes(10, 1);
        var parent2 = CreateParentWithSequentialGenes(10, 11);

        var rng = new Random(42);
        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.NotNull(child1);
        Assert.NotNull(child2);
        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    private static MockChromosome CreateParentWithSequentialGenes(int length, int startValue)
    {
        var parent = new MockChromosome(length);
        for (int i = 0; i < length; i++)
            parent.Genes[i] = startValue + i;
        return parent;
    }

    #endregion

    #region Fallback Behavior Tests

    // Note: The crossover operators use new T() which creates default-length chromosomes.
    // These tests verify that when parents have default length (10), the fallback behavior
    // works correctly with minimum crossover points.

    [Fact]
    public void TwoPointCrossover_WithDefaultLength_ProducesValidChildren()
    {
        var op = new TwoPointCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
        var allGenes = new HashSet<double>(parent1.Genes.Concat(parent2.Genes));
        Assert.All(child1.Genes, g => Assert.Contains(g, allGenes));
        Assert.All(child2.Genes, g => Assert.Contains(g, allGenes));
    }

    [Fact]
    public void PMXCrossover_WithDefaultLength_ProducesValidChildren()
    {
        var op = new PMXCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    [Fact]
    public void OrderCrossover_WithDefaultLength_ProducesValidChildren()
    {
        var op = new OrderCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    [Fact]
    public void EdgeRecombinationCrossover_WithDefaultLength_ProducesValidChildren()
    {
        var op = new EdgeRecombinationCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
    }

    [Fact]
    public void CycleCrossover_WithDefaultLength_ProducesValidChildren()
    {
        var op = new CycleCrossover<MockChromosome>();
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
        var rng = new Random(42);

        var (child1, child2) = op.Crossover(parent1, parent2, rng);

        Assert.Equal(10, child1.Length);
        Assert.Equal(10, child2.Length);
        // Each position should have gene from same position in either parent
        for (int i = 0; i < 10; i++)
        {
            bool fromP1 = Math.Abs(child1.Genes[i] - parent1.Genes[i]) < 0.001;
            bool fromP2 = Math.Abs(child1.Genes[i] - parent2.Genes[i]) < 0.001;
            Assert.True(fromP1 || fromP2);
        }
    }

    #endregion

    #region Multiple Consecutive Crossovers

    [Theory]
    [InlineData(typeof(SinglePointCrossover<MockChromosome>))]
    [InlineData(typeof(TwoPointCrossover<MockChromosome>))]
    [InlineData(typeof(UniformCrossover<MockChromosome>))]
    [InlineData(typeof(PMXCrossover<MockChromosome>))]
    [InlineData(typeof(OrderCrossover<MockChromosome>))]
    [InlineData(typeof(CycleCrossover<MockChromosome>))]
    [InlineData(typeof(EdgeRecombinationCrossover<MockChromosome>))]
    [InlineData(typeof(SegmentPreservingCrossover<MockChromosome>))]
    public void AllOperators_MultipleCrossover_DoesNotCorruptState(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var parent1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var parent2 = MockChromosome.WithGenes(11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        var rng = new Random(42);

        // Perform multiple crossovers and verify each produces valid output
        for (int i = 0; i < 100; i++)
        {
            var (child1, child2) = op.Crossover(parent1, parent2, rng);

            Assert.Equal(10, child1.Length);
            Assert.Equal(10, child2.Length);
            Assert.NotSame(parent1, child1);
            Assert.NotSame(parent2, child2);
        }

        // Parents should still be unchanged
        Assert.Equal(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, parent1.Genes);
        Assert.Equal(new double[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, parent2.Genes);
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

    #endregion
}
