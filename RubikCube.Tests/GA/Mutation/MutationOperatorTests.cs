using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Mutation;
using Xunit;

namespace RubikCube.Tests.GA.Mutation;

/// <summary>
/// Basic functional tests for all mutation operators.
/// Tests generic operators that work with any IChromosome.
/// </summary>
public class MutationOperatorTests
{
    #region Basic Functionality Tests

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_DoesNotThrow(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);

        // Should not throw
        var exception = Record.Exception(() => op.Mutate(chromosome, rng));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_PreservesLength(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);
        int originalLength = chromosome.Length;

        op.Mutate(chromosome, rng);

        Assert.Equal(originalLength, chromosome.Length);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    public void PermutationOperators_PreservesGeneSet(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);

        var originalGenes = new HashSet<double>(chromosome.Genes);

        op.Mutate(chromosome, rng);

        var mutatedGenes = new HashSet<double>(chromosome.Genes);
        Assert.Equal(originalGenes, mutatedGenes);
    }

    #endregion

    #region Mutation Effect Tests

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_CanChangeChromosome(Type operatorType)
    {
        var op = CreateOperator(operatorType);

        int changeCount = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            if (!original.SequenceEqual(chromosome.Genes))
                changeCount++;
        }

        // Should change the chromosome at least sometimes
        Assert.True(changeCount > 0,
            $"{operatorType.Name}: Never changed the chromosome in 100 trials");
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_MutatesInPlace(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var originalRef = chromosome;
        var rng = new Random(42);

        op.Mutate(chromosome, rng);

        // Should be the same object reference
        Assert.Same(originalRef, chromosome);
    }

    #endregion

    #region Determinism Tests

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_SameSeed_SameResult(Type operatorType)
    {
        var op1 = CreateOperator(operatorType);
        var op2 = CreateOperator(operatorType);

        var chromosome1 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var chromosome2 = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        op1.Mutate(chromosome1, rng1);
        op2.Mutate(chromosome2, rng2);

        Assert.Equal(chromosome1.Genes, chromosome2.Genes);
    }

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_DifferentSeeds_LikelyDifferentResults(Type operatorType)
    {
        var op = CreateOperator(operatorType);

        var results = new List<double[]>();
        for (int seed = 0; seed < 20; seed++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var rng = new Random(seed);
            op.Mutate(chromosome, rng);
            results.Add(chromosome.Genes.ToArray());
        }

        // Count unique results
        int uniqueCount = results.Select(r => string.Join(",", r)).Distinct().Count();

        Assert.True(uniqueCount > 1,
            $"{operatorType.Name}: All results were identical with different seeds");
    }

    #endregion

    #region Multiple Mutations

    [Theory]
    [InlineData(typeof(SwapMutation<MockChromosome>))]
    [InlineData(typeof(InversionMutation<MockChromosome>))]
    [InlineData(typeof(ScrambleMutation<MockChromosome>))]
    [InlineData(typeof(ShiftMutation<MockChromosome>))]
    [InlineData(typeof(DisplacementMutation<MockChromosome>))]
    [InlineData(typeof(TranslocationMutation<MockChromosome>))]
    [InlineData(typeof(RandomMutation<MockChromosome>))]
    public void AllOperators_MultipleMutations_DoNotCorruptState(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var rng = new Random(42);

        // Perform multiple mutations
        for (int i = 0; i < 100; i++)
        {
            op.Mutate(chromosome, rng);

            // Verify chromosome is still valid
            Assert.Equal(10, chromosome.Length);
            Assert.NotNull(chromosome.Genes);
        }
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
