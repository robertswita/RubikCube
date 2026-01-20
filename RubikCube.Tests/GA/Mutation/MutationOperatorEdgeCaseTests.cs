using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Mutation;
using Xunit;

namespace RubikCube.Tests.GA.Mutation;

/// <summary>
/// Edge case and boundary condition tests for mutation operators.
/// </summary>
public class MutationOperatorEdgeCaseTests
{
    #region Short Chromosome Tests

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_SingleGene_DoesNotCrash(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = new MockChromosome(1);
        chromosome.Genes[0] = 42;
        var rng = new Random(42);

        // Should not throw
        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(1, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_TwoGenes_DoesNotCrash(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2);
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(2, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_ThreeGenes_DoesNotCrash(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3);
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(3, chromosome.Length);
    }

    [Fact]
    public void SwapMutation_SingleGene_NoChange()
    {
        var op = new SwapMutation<MockChromosome>();
        var chromosome = new MockChromosome(1);
        chromosome.Genes[0] = 42;
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        Assert.Equal(42, chromosome.Genes[0]);
    }

    [Fact]
    public void InversionMutation_SingleGene_NoChange()
    {
        var op = new InversionMutation<MockChromosome>();
        var chromosome = new MockChromosome(1);
        chromosome.Genes[0] = 42;
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        Assert.Equal(42, chromosome.Genes[0]);
    }

    [Fact]
    public void DisplacementMutation_TwoGenes_NoChange()
    {
        var op = new DisplacementMutation<MockChromosome>();
        var chromosome = MockChromosome.WithGenes(1, 2);
        var original = chromosome.Genes.ToArray();
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // With < 3 genes, displacement should return without changing
        Assert.Equal(original, chromosome.Genes);
    }

    [Fact]
    public void TranslocationMutation_ThreeGenes_NoChange()
    {
        var op = new TranslocationMutation<MockChromosome>();
        var chromosome = MockChromosome.WithGenes(1, 2, 3);
        var original = chromosome.Genes.ToArray();
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // With < 4 genes, translocation should return without changing
        Assert.Equal(original, chromosome.Genes);
    }

    #endregion

    #region Numerical Edge Cases

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_NegativeGeneValues_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(-10, -5, 0, 5, 10, -20, -15, 15, 20, 25);
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(10, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_VeryLargeGeneValues_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1e10, 2e10, 3e10, 4e10, 5e10, 6e10, 7e10, 8e10, 9e10, 1e11);
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(10, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_AllZeroGenes_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var rng = new Random(42);

        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
        Assert.Equal(10, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    public void PermutationOperators_AllIdenticalGenes_PreservesValues(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(5, 5, 5, 5, 5, 5, 5, 5, 5, 5);
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // All genes should still be 5 since we're just rearranging
        Assert.All(chromosome.Genes, g => Assert.Equal(5, g));
    }

    #endregion

    #region Minimum Size Requirements

    [Fact]
    public void SwapMutation_RequiresAtLeast2Genes()
    {
        var op = new SwapMutation<MockChromosome>();
        var chromosome = new MockChromosome(1);
        chromosome.Genes[0] = 42;
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // Should not change with only 1 gene
        Assert.Equal(42, chromosome.Genes[0]);
    }

    [Fact]
    public void DisplacementMutation_RequiresAtLeast3Genes()
    {
        var op = new DisplacementMutation<MockChromosome>();

        // Test with 2 genes - should not mutate
        var chromosome2 = MockChromosome.WithGenes(1, 2);
        var original2 = chromosome2.Genes.ToArray();
        op.Mutate(chromosome2, new Random(42));
        Assert.Equal(original2, chromosome2.Genes);

        // Test with 3 genes - should mutate
        var chromosome3 = MockChromosome.WithGenes(1, 2, 3);
        int changedCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var chr = MockChromosome.WithGenes(1, 2, 3);
            var orig = chr.Genes.ToArray();
            op.Mutate(chr, new Random(i));
            if (!orig.SequenceEqual(chr.Genes))
                changedCount++;
        }
        Assert.True(changedCount > 0, "Displacement should work with 3+ genes");
    }

    [Fact]
    public void TranslocationMutation_RequiresAtLeast4Genes()
    {
        var op = new TranslocationMutation<MockChromosome>();

        // Test with 3 genes - should not mutate
        var chromosome3 = MockChromosome.WithGenes(1, 2, 3);
        var original3 = chromosome3.Genes.ToArray();
        op.Mutate(chromosome3, new Random(42));
        Assert.Equal(original3, chromosome3.Genes);

        // Test with 4+ genes - should be able to mutate
        int changedCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var chr = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6);
            var orig = chr.Genes.ToArray();
            op.Mutate(chr, new Random(i));
            if (!orig.SequenceEqual(chr.Genes))
                changedCount++;
        }
        Assert.True(changedCount > 0, "Translocation should work with 4+ genes");
    }

    #endregion

    #region Parameter Configuration Tests

    [Fact]
    public void ShiftMutation_SegmentOnly_ShiftsOnlySegment()
    {
        var op = new ShiftMutation<MockChromosome>(segmentOnly: true);
        var rng = new Random(42);

        int changedCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            op.Mutate(chromosome, new Random(trial));

            if (!original.SequenceEqual(chromosome.Genes))
                changedCount++;
        }

        Assert.True(changedCount > 0, "Segment-only shift should still make changes");
    }

    [Fact]
    public void RandomMutation_MultipleMutations_MutatesMultipleGenes()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 5);
        var chromosome = MockChromosome.WithGenes(1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // Some genes should have been randomized (different from 1)
        // Note: RandomMutation uses chromosome.Randomize() which generates values in [0, 100)
        int changedGenes = chromosome.Genes.Count(g => Math.Abs(g - 1) > 0.001);
        Assert.True(changedGenes > 0, "Random mutation should change some genes");
    }

    #endregion

    #region Helper Methods

    private static IMutationOperator<MockChromosome> CreateOperator(Type operatorType)
    {
        if (operatorType == typeof(SwapMutation<MockChromosome>))
            return new SwapMutation<MockChromosome>();
        if (operatorType == typeof(InversionMutation<MockChromosome>))
            return new InversionMutation<MockChromosome>();
        if (operatorType == typeof(ScrambleMutation<MockChromosome>))
            return new ScrambleMutation<MockChromosome>();
        if (operatorType == typeof(ShiftMutation<MockChromosome>))
            return new ShiftMutation<MockChromosome>();
        if (operatorType == typeof(DisplacementMutation<MockChromosome>))
            return new DisplacementMutation<MockChromosome>();
        if (operatorType == typeof(TranslocationMutation<MockChromosome>))
            return new TranslocationMutation<MockChromosome>();
        if (operatorType == typeof(RandomMutation<MockChromosome>))
            return new RandomMutation<MockChromosome>();

        throw new ArgumentException($"Unknown operator type: {operatorType}");
    }

    #endregion
}
