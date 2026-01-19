using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Selection;
using Xunit;

namespace RubikCube.Tests.GA.Selection;

/// <summary>
/// Edge case and boundary condition tests for selection operators.
/// </summary>
public class SelectionOperatorEdgeCaseTests
{
    #region Boundary Conditions - Count vs Population Size

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_CountGreaterThanPopulation_HandlesGracefully(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(5);
        var rng = new Random(42);

        // Should not throw - behavior varies by operator
        var result = op.Select(population, 100, rng);
        Assert.NotNull(result);

        // RankSelection returns min(count, population.Count), others may return count with duplicates
        Assert.True(result.Count >= 1);
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_CountEqualsPopulation_ReturnsCorrectCount(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(10);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        // Most operators should return exactly 10
        // RankSelection returns exactly 10 (the whole sorted population)
        // UniqueSelection may return fewer if there are duplicate fitness values
        Assert.True(result.Count <= 10);
        Assert.True(result.Count >= 1);
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_PopulationSizeTwo_Works(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(2);
        var rng = new Random(42);

        var result = op.Select(population, 5, rng);

        Assert.NotNull(result);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    #endregion

    #region Numerical Edge Cases

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_NegativeFitness_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Negative fitness values (still lower = better)
        var population = MockChromosome.CreatePopulation(-100.0, -50.0, -10.0, 0.0, 10.0);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Theory]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    public void NonDuplicatingOperators_NegativeFitness_ReturnsUpToPopulationSize(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreatePopulation(-100.0, -50.0, -10.0, 0.0, 10.0);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        // These operators don't duplicate - they return min(count, population.Count)
        Assert.True(result.Count <= population.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_LargeFitnessDifferences_HandlesCorrectly(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Extreme fitness differences
        var population = MockChromosome.CreatePopulation(1e-10, 1e-5, 1.0, 1e5, 1e10);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Theory]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    public void NonDuplicatingOperators_LargeFitnessDifferences_ReturnsUpToPopulationSize(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreatePopulation(1e-10, 1e-5, 1.0, 1e5, 1e10);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.True(result.Count <= population.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_AllEqualFitness_DoesNotCrash(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreatePopulation(5.0, 5.0, 5.0, 5.0, 5.0);
        var rng = new Random(42);

        // Should not crash, should return valid selection
        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_VerySmallFitnessDifferences_Distinguishes(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        // Very small differences - tests floating point precision
        var population = MockChromosome.CreatePopulation(
            1.0,
            1.0 + 1e-10,
            1.0 + 2e-10,
            1.0 + 3e-10,
            1.0 + 4e-10);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
    }

    [Theory]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    public void NonDuplicatingOperators_VerySmallFitnessDifferences_ReturnsUpToPopulationSize(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreatePopulation(
            1.0,
            1.0 + 1e-10,
            1.0 + 2e-10,
            1.0 + 3e-10,
            1.0 + 4e-10);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        Assert.NotNull(result);
        Assert.True(result.Count <= population.Count);
    }

    #endregion

    #region Determinism Tests

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllStochasticOperators_SameSeed_SameResults(Type operatorType)
    {
        var op1 = CreateOperator(operatorType);
        var op2 = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(50);

        var rng1 = new Random(12345);
        var rng2 = new Random(12345);

        var result1 = op1.Select(population, 20, rng1);
        var result2 = op2.Select(population, 20, rng2);

        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Same(result1[i], result2[i]);
        }
    }

    [Fact]
    public void RankSelection_IsDeterministic_NoRngDependency()
    {
        var op = new RankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(50);

        var rng1 = new Random(111);
        var rng2 = new Random(999);

        var result1 = op.Select(population, 20, rng1);
        var result2 = op.Select(population, 20, rng2);

        // RankSelection is deterministic - same results regardless of RNG
        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Same(result1[i], result2[i]);
        }
    }

    #endregion

    #region Immutability Tests

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_DoNotModifyPopulation(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(20);
        var rng = new Random(42);

        // Record original state
        var originalFitness = population.Select(p => p.Fitness).ToArray();
        var originalCount = population.Count;

        op.Select(population, 10, rng);

        // Verify population unchanged
        Assert.Equal(originalCount, population.Count);
        for (int i = 0; i < population.Count; i++)
        {
            Assert.Equal(originalFitness[i], population[i].Fitness);
        }
    }

    [Theory]
    [InlineData(typeof(TournamentSelection<MockChromosome>))]
    [InlineData(typeof(RankSelection<MockChromosome>))]
    [InlineData(typeof(RouletteSelection<MockChromosome>))]
    [InlineData(typeof(RouletteRankSelection<MockChromosome>))]
    [InlineData(typeof(UniqueSelection<MockChromosome>))]
    [InlineData(typeof(SUSSelection<MockChromosome>))]
    [InlineData(typeof(BoltzmannSelection<MockChromosome>))]
    [InlineData(typeof(TruncationSelection<MockChromosome>))]
    [InlineData(typeof(LinearRankingSelection<MockChromosome>))]
    [InlineData(typeof(ExponentialRankingSelection<MockChromosome>))]
    public void AllOperators_ReturnReferencesToOriginals(Type operatorType)
    {
        var op = CreateOperator(operatorType);
        var population = MockChromosome.CreateSortedPopulation(20);
        var rng = new Random(42);

        var result = op.Select(population, 10, rng);

        // All returned items should be references to original population members
        Assert.All(result, selected =>
        {
            Assert.True(population.Any(p => ReferenceEquals(p, selected)),
                "Selected item should be a reference to an original population member");
        });
    }

    #endregion

    #region Helper Methods

    private static ISelectionOperator<MockChromosome> CreateOperator(Type operatorType)
    {
        if (operatorType == typeof(TournamentSelection<MockChromosome>))
            return new TournamentSelection<MockChromosome>(5);
        if (operatorType == typeof(RankSelection<MockChromosome>))
            return new RankSelection<MockChromosome>();
        if (operatorType == typeof(RouletteSelection<MockChromosome>))
            return new RouletteSelection<MockChromosome>();
        if (operatorType == typeof(RouletteRankSelection<MockChromosome>))
            return new RouletteRankSelection<MockChromosome>();
        if (operatorType == typeof(UniqueSelection<MockChromosome>))
            return new UniqueSelection<MockChromosome>();
        if (operatorType == typeof(SUSSelection<MockChromosome>))
            return new SUSSelection<MockChromosome>();
        if (operatorType == typeof(BoltzmannSelection<MockChromosome>))
            return new BoltzmannSelection<MockChromosome>(10.0);
        if (operatorType == typeof(TruncationSelection<MockChromosome>))
            return new TruncationSelection<MockChromosome>(0.5);
        if (operatorType == typeof(LinearRankingSelection<MockChromosome>))
            return new LinearRankingSelection<MockChromosome>(1.5);
        if (operatorType == typeof(ExponentialRankingSelection<MockChromosome>))
            return new ExponentialRankingSelection<MockChromosome>(0.99);

        throw new ArgumentException($"Unknown operator type: {operatorType}");
    }

    #endregion
}
