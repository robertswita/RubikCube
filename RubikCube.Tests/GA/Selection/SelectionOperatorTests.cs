using RubikCube.Tests.Mocks;
using TGL.GA.Interfaces;
using TGL.GA.Operators.Selection;
using Xunit;

namespace RubikCube.Tests.GA.Selection;

/// <summary>
/// Unit tests for all selection operators.
/// </summary>
public class SelectionOperatorTests
{
    private readonly Random _rng = new(42); // Fixed seed for reproducibility

    #region TournamentSelection Tests

    [Fact]
    public void TournamentSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new TournamentSelection<MockChromosome>(5);
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void TournamentSelection_ZeroCount_ReturnsEmpty()
    {
        var selection = new TournamentSelection<MockChromosome>(5);
        var population = MockChromosome.CreateSortedPopulation(10);
        var result = selection.Select(population, 0, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void TournamentSelection_ReturnsRequestedCount()
    {
        var selection = new TournamentSelection<MockChromosome>(5);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void TournamentSelection_FavorsBetterIndividuals()
    {
        var selection = new TournamentSelection<MockChromosome>(5);
        var population = MockChromosome.CreateSortedPopulation(100);

        // Run selection many times and count how often each fitness rank is selected
        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        // Best individuals (lower indices) should be selected more often
        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    [Fact]
    public void TournamentSelection_SingleElement_ReturnsThatElement()
    {
        var selection = new TournamentSelection<MockChromosome>(5);
        var population = MockChromosome.CreatePopulation(42.0);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.Equal(42.0, r.Fitness));
    }

    #endregion

    #region RankSelection Tests

    [Fact]
    public void RankSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new RankSelection<MockChromosome>();
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void RankSelection_ReturnsTopN()
    {
        var selection = new RankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 10, _rng);

        Assert.Equal(10, result.Count);
        // Should be the top 10 individuals
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(i + 1, result[i].Fitness);
        }
    }

    [Fact]
    public void RankSelection_CountExceedsPopulation_ReturnsAll()
    {
        var selection = new RankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(5);
        var result = selection.Select(population, 10, _rng);

        Assert.Equal(5, result.Count);
    }

    #endregion

    #region RouletteSelection Tests

    [Fact]
    public void RouletteSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new RouletteSelection<MockChromosome>();
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void RouletteSelection_ZeroCount_ReturnsEmpty()
    {
        var selection = new RouletteSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(10);
        var result = selection.Select(population, 0, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void RouletteSelection_AllZeroFitness_FallsBackToUniform()
    {
        var selection = new RouletteSelection<MockChromosome>();
        var population = MockChromosome.CreatePopulation(0, 0, 0, 0, 0);
        var result = selection.Select(population, 10, _rng);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public void RouletteSelection_ReturnsRequestedCount()
    {
        var selection = new RouletteSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void RouletteSelection_FavorsBetterIndividuals()
    {
        var selection = new RouletteSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    #endregion

    #region RouletteRankSelection Tests

    [Fact]
    public void RouletteRankSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new RouletteRankSelection<MockChromosome>();
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void RouletteRankSelection_SingleElement_ReturnsIt()
    {
        var selection = new RouletteRankSelection<MockChromosome>();
        var population = MockChromosome.CreatePopulation(42.0);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.Equal(42.0, r.Fitness));
    }

    [Fact]
    public void RouletteRankSelection_ReturnsRequestedCount()
    {
        var selection = new RouletteRankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void RouletteRankSelection_FavorsBetterIndividuals()
    {
        var selection = new RouletteRankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    #endregion

    #region UniqueSelection Tests

    [Fact]
    public void UniqueSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new UniqueSelection<MockChromosome>();
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void UniqueSelection_SelectsUniqueFitnessValues()
    {
        var selection = new UniqueSelection<MockChromosome>();
        // Create population with duplicate fitness values
        var population = MockChromosome.CreatePopulation(1, 1, 1, 2, 2, 3, 4, 5, 5, 5);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
        // First should be fitness 1, then 2, 3, 4, 5 (unique values preferred)
        var fitnessValues = result.Select(r => r.Fitness).ToList();
        Assert.Equal(1, fitnessValues[0]);
    }

    [Fact]
    public void UniqueSelection_FillsRemainingWhenNotEnoughUnique()
    {
        var selection = new UniqueSelection<MockChromosome>();
        // All same fitness - only 1 unique value, but need 5
        var population = MockChromosome.CreatePopulation(1, 1, 1, 1, 1);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void UniqueSelection_ReturnsRequestedCount()
    {
        var selection = new UniqueSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    #endregion

    #region SUSSelection Tests

    [Fact]
    public void SUSSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new SUSSelection<MockChromosome>();
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void SUSSelection_ZeroCount_ReturnsEmpty()
    {
        var selection = new SUSSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(10);
        var result = selection.Select(population, 0, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void SUSSelection_ReturnsRequestedCount()
    {
        var selection = new SUSSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void SUSSelection_AllEqualFitness_FallsBackToUniform()
    {
        var selection = new SUSSelection<MockChromosome>();
        var population = MockChromosome.CreatePopulation(5, 5, 5, 5, 5);
        var result = selection.Select(population, 10, _rng);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public void SUSSelection_FavorsBetterIndividuals()
    {
        var selection = new SUSSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    #endregion

    #region BoltzmannSelection Tests

    [Fact]
    public void BoltzmannSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new BoltzmannSelection<MockChromosome>(10);
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void BoltzmannSelection_ZeroCount_ReturnsEmpty()
    {
        var selection = new BoltzmannSelection<MockChromosome>(10);
        var population = MockChromosome.CreateSortedPopulation(10);
        var result = selection.Select(population, 0, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void BoltzmannSelection_InvalidTemperature_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BoltzmannSelection<MockChromosome>(0));
        Assert.Throws<ArgumentException>(() => new BoltzmannSelection<MockChromosome>(-5));
    }

    [Fact]
    public void BoltzmannSelection_ReturnsRequestedCount()
    {
        var selection = new BoltzmannSelection<MockChromosome>(10);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void BoltzmannSelection_LowTemperature_StronglyFavorsBest()
    {
        var selection = new BoltzmannSelection<MockChromosome>(1.0); // Low temperature = strong pressure
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        // With low temperature, top individuals should dominate
        double top10Selections = selectionCounts.Take(10).Sum();
        double rest90Selections = selectionCounts.Skip(10).Sum();

        Assert.True(top10Selections > rest90Selections,
            $"Top 10 should dominate with low temperature. Top10: {top10Selections}, Rest: {rest90Selections}");
    }

    [Fact]
    public void BoltzmannSelection_HighTemperature_MoreUniform()
    {
        var selection = new BoltzmannSelection<MockChromosome>(100.0); // High temperature = more uniform
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        // With high temperature, selection should be more spread out
        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        // Still favors better, but not as strongly
        double ratio = topHalfSelections / (bottomHalfSelections + 1);
        Assert.True(ratio < 5, $"High temperature should be more uniform. Ratio: {ratio}");
    }

    #endregion

    #region TruncationSelection Tests

    [Fact]
    public void TruncationSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new TruncationSelection<MockChromosome>(0.5);
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void TruncationSelection_ReturnsRequestedCount()
    {
        var selection = new TruncationSelection<MockChromosome>(0.5);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void TruncationSelection_OnlySelectsFromTopPortion()
    {
        var selection = new TruncationSelection<MockChromosome>(0.2); // Only top 20%
        var population = MockChromosome.CreateSortedPopulation(100);

        var rng = new Random(42);
        for (int trial = 0; trial < 100; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                // All selected should be from top 20 (fitness 1-20)
                Assert.True(chromosome.Fitness <= 20,
                    $"Should only select from top 20%, but got fitness {chromosome.Fitness}");
            }
        }
    }

    [Fact]
    public void TruncationSelection_TruncationRateClamped()
    {
        // Rate below 0.01 should be clamped to 0.01
        var selection = new TruncationSelection<MockChromosome>(0.001);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 10, _rng);

        Assert.Equal(10, result.Count);
        // Should select from at least 1 individual (1% clamped)
    }

    #endregion

    #region LinearRankingSelection Tests

    [Fact]
    public void LinearRankingSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new LinearRankingSelection<MockChromosome>(1.5);
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void LinearRankingSelection_SingleElement_ReturnsIt()
    {
        var selection = new LinearRankingSelection<MockChromosome>(1.5);
        var population = MockChromosome.CreatePopulation(42.0);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.Equal(42.0, r.Fitness));
    }

    [Fact]
    public void LinearRankingSelection_ReturnsRequestedCount()
    {
        var selection = new LinearRankingSelection<MockChromosome>(1.5);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void LinearRankingSelection_PressureClamped()
    {
        // Pressure outside [1.0, 2.0] should be clamped
        var lowPressure = new LinearRankingSelection<MockChromosome>(0.5);
        var highPressure = new LinearRankingSelection<MockChromosome>(3.0);

        var population = MockChromosome.CreateSortedPopulation(100);

        // Both should work without throwing
        var result1 = lowPressure.Select(population, 10, _rng);
        var result2 = highPressure.Select(population, 10, _rng);

        Assert.Equal(10, result1.Count);
        Assert.Equal(10, result2.Count);
    }

    [Fact]
    public void LinearRankingSelection_FavorsBetterIndividuals()
    {
        var selection = new LinearRankingSelection<MockChromosome>(1.5);
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    #endregion

    #region ExponentialRankingSelection Tests

    [Fact]
    public void ExponentialRankingSelection_EmptyPopulation_ReturnsEmpty()
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(0.99);
        var result = selection.Select(new List<MockChromosome>(), 10, _rng);
        Assert.Empty(result);
    }

    [Fact]
    public void ExponentialRankingSelection_SingleElement_ReturnsIt()
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(0.99);
        var population = MockChromosome.CreatePopulation(42.0);
        var result = selection.Select(population, 5, _rng);

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.Equal(42.0, r.Fitness));
    }

    [Fact]
    public void ExponentialRankingSelection_ReturnsRequestedCount()
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(0.99);
        var population = MockChromosome.CreateSortedPopulation(100);
        var result = selection.Select(population, 30, _rng);
        Assert.Equal(30, result.Count);
    }

    [Fact]
    public void ExponentialRankingSelection_BaseClamped()
    {
        // Base outside [0.01, 0.9999] should be clamped
        var lowBase = new ExponentialRankingSelection<MockChromosome>(0.0001);
        var highBase = new ExponentialRankingSelection<MockChromosome>(1.5);

        var population = MockChromosome.CreateSortedPopulation(100);

        // Both should work without throwing
        var result1 = lowBase.Select(population, 10, _rng);
        var result2 = highBase.Select(population, 10, _rng);

        Assert.Equal(10, result1.Count);
        Assert.Equal(10, result2.Count);
    }

    [Fact]
    public void ExponentialRankingSelection_FavorsBetterIndividuals()
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(0.99);
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 30, rng);
            foreach (var chromosome in result)
            {
                int index = population.IndexOf(chromosome);
                if (index >= 0) selectionCounts[index]++;
            }
        }

        double topHalfSelections = selectionCounts.Take(50).Sum();
        double bottomHalfSelections = selectionCounts.Skip(50).Sum();

        Assert.True(topHalfSelections > bottomHalfSelections,
            $"Top half should be selected more often. Top: {topHalfSelections}, Bottom: {bottomHalfSelections}");
    }

    [Fact]
    public void ExponentialRankingSelection_LowerBase_StrongerPressure()
    {
        var lowBase = new ExponentialRankingSelection<MockChromosome>(0.5);  // Stronger pressure
        var highBase = new ExponentialRankingSelection<MockChromosome>(0.99); // Weaker pressure

        var population = MockChromosome.CreateSortedPopulation(100);

        // Count selections for top 10 individuals
        int lowBaseTop10 = 0, highBaseTop10 = 0;
        var rng1 = new Random(42);
        var rng2 = new Random(42);

        for (int trial = 0; trial < 1000; trial++)
        {
            var result1 = lowBase.Select(population, 30, rng1);
            var result2 = highBase.Select(population, 30, rng2);

            lowBaseTop10 += result1.Count(r => population.IndexOf(r) < 10);
            highBaseTop10 += result2.Count(r => population.IndexOf(r) < 10);
        }

        Assert.True(lowBaseTop10 > highBaseTop10,
            $"Lower base should have stronger pressure. LowBase top10: {lowBaseTop10}, HighBase top10: {highBaseTop10}");
    }

    #endregion

    #region Cross-Operator Comparison Tests

    [Fact]
    public void AllSelectionOperators_ReturnPopulationMembers()
    {
        var population = MockChromosome.CreateSortedPopulation(50);
        var operators = new ISelectionOperator<MockChromosome>[]
        {
            new TournamentSelection<MockChromosome>(5),
            new RankSelection<MockChromosome>(),
            new RouletteSelection<MockChromosome>(),
            new RouletteRankSelection<MockChromosome>(),
            new UniqueSelection<MockChromosome>(),
            new SUSSelection<MockChromosome>(),
            new BoltzmannSelection<MockChromosome>(10),
            new TruncationSelection<MockChromosome>(0.5),
            new LinearRankingSelection<MockChromosome>(1.5),
            new ExponentialRankingSelection<MockChromosome>(0.99)
        };

        var rng = new Random(42);
        foreach (var op in operators)
        {
            var result = op.Select(population, 20, rng);

            Assert.All(result, selected =>
                Assert.Contains(selected, population));
        }
    }

    [Fact]
    public void AllSelectionOperators_HandleSmallPopulations()
    {
        var population = MockChromosome.CreateSortedPopulation(3);
        var operators = new ISelectionOperator<MockChromosome>[]
        {
            new TournamentSelection<MockChromosome>(5),
            new RankSelection<MockChromosome>(),
            new RouletteSelection<MockChromosome>(),
            new RouletteRankSelection<MockChromosome>(),
            new UniqueSelection<MockChromosome>(),
            new SUSSelection<MockChromosome>(),
            new BoltzmannSelection<MockChromosome>(10),
            new TruncationSelection<MockChromosome>(0.5),
            new LinearRankingSelection<MockChromosome>(1.5),
            new ExponentialRankingSelection<MockChromosome>(0.99)
        };

        var rng = new Random(42);
        foreach (var op in operators)
        {
            // Should not throw
            var result = op.Select(population, 10, rng);
            Assert.NotNull(result);
        }
    }

    #endregion
}
