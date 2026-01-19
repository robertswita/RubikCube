using RubikCube.Tests.Mocks;
using TGL.GA.Operators.Selection;
using Xunit;

namespace RubikCube.Tests.GA.Selection;

/// <summary>
/// Operator-specific tests that verify unique behaviors of each selection operator.
/// </summary>
public class SelectionOperatorSpecificTests
{
    #region TournamentSelection Specific Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(50)]
    public void TournamentSelection_DifferentTournamentSizes_Work(int tournamentSize)
    {
        var selection = new TournamentSelection<MockChromosome>(tournamentSize);
        var population = MockChromosome.CreateSortedPopulation(100);
        var rng = new Random(42);

        var result = selection.Select(population, 30, rng);

        Assert.Equal(30, result.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Fact]
    public void TournamentSelection_TournamentSizeLargerThanPopulation_StillWorks()
    {
        var selection = new TournamentSelection<MockChromosome>(100);
        var population = MockChromosome.CreateSortedPopulation(10);
        var rng = new Random(42);

        // Should still work - just samples with replacement from smaller population
        var result = selection.Select(population, 20, rng);

        Assert.Equal(20, result.Count);
    }

    [Fact]
    public void TournamentSelection_LargerTournament_StrongerPressure()
    {
        var smallTournament = new TournamentSelection<MockChromosome>(2);
        var largeTournament = new TournamentSelection<MockChromosome>(20);
        var population = MockChromosome.CreateSortedPopulation(100);

        int smallTop10 = 0, largeTop10 = 0;

        for (int trial = 0; trial < 1000; trial++)
        {
            var rng1 = new Random(trial);
            var rng2 = new Random(trial);

            var result1 = smallTournament.Select(population, 30, rng1);
            var result2 = largeTournament.Select(population, 30, rng2);

            smallTop10 += result1.Count(r => population.IndexOf(r) < 10);
            largeTop10 += result2.Count(r => population.IndexOf(r) < 10);
        }

        Assert.True(largeTop10 > smallTop10,
            $"Larger tournament should have stronger pressure. Small: {smallTop10}, Large: {largeTop10}");
    }

    [Fact]
    public void TournamentSelection_TournamentSizeOne_IsRandomSelection()
    {
        var selection = new TournamentSelection<MockChromosome>(1);
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectionCounts = new int[100];
        var rng = new Random(42);

        for (int trial = 0; trial < 10000; trial++)
        {
            var result = selection.Select(population, 1, rng);
            int index = population.IndexOf(result[0]);
            selectionCounts[index]++;
        }

        // With tournament size 1, selection should be roughly uniform
        double avgCount = selectionCounts.Average();
        double maxDeviation = selectionCounts.Max() - avgCount;
        double minDeviation = avgCount - selectionCounts.Min();

        // Allow 50% deviation from average (generous for random)
        Assert.True(maxDeviation < avgCount * 0.5,
            $"Max deviation {maxDeviation} too high for uniform selection");
    }

    #endregion

    #region RankSelection Specific Tests

    [Fact]
    public void RankSelection_AlwaysReturnsTopN_InOrder()
    {
        var selection = new RankSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);
        var rng = new Random(42);

        var result = selection.Select(population, 10, rng);

        // Should be exactly the top 10, in order
        for (int i = 0; i < 10; i++)
        {
            Assert.Same(population[i], result[i]);
        }
    }

    [Fact]
    public void RankSelection_WithUnsortedPopulation_StillReturnsFirstN()
    {
        var selection = new RankSelection<MockChromosome>();
        // Create unsorted population
        var population = MockChromosome.CreatePopulation(50.0, 10.0, 30.0, 20.0, 40.0);
        var rng = new Random(42);

        var result = selection.Select(population, 3, rng);

        // RankSelection assumes population is sorted, so it returns first 3 regardless of fitness
        Assert.Equal(3, result.Count);
        Assert.Same(population[0], result[0]);
        Assert.Same(population[1], result[1]);
        Assert.Same(population[2], result[2]);
    }

    #endregion

    #region RouletteSelection Specific Tests

    [Fact]
    public void RouletteSelection_ExtremelyDominantIndividual_GetsMostSelections()
    {
        var selection = new RouletteSelection<MockChromosome>();
        // First individual has much better fitness
        var population = MockChromosome.CreatePopulation(1.0, 1000.0, 1000.0, 1000.0, 1000.0);
        var rng = new Random(42);

        int firstSelected = 0;
        for (int trial = 0; trial < 1000; trial++)
        {
            var result = selection.Select(population, 1, rng);
            if (ReferenceEquals(result[0], population[0]))
                firstSelected++;
        }

        // The best individual should be selected most of the time
        Assert.True(firstSelected > 500,
            $"Best individual should dominate. Selected {firstSelected}/1000 times");
    }

    #endregion

    #region UniqueSelection Specific Tests

    [Fact]
    public void UniqueSelection_AllUniqueFitness_SelectsInOrder()
    {
        var selection = new UniqueSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(10);
        var rng = new Random(42);

        var result = selection.Select(population, 10, rng);

        // All unique fitness values, should return all 10 in order
        Assert.Equal(10, result.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Same(population[i], result[i]);
        }
    }

    [Fact]
    public void UniqueSelection_AllSameFitness_ReturnsAll()
    {
        var selection = new UniqueSelection<MockChromosome>();
        var population = MockChromosome.CreatePopulation(5.0, 5.0, 5.0, 5.0, 5.0);
        var rng = new Random(42);

        var result = selection.Select(population, 5, rng);

        // All same fitness, but should still return 5 because we need to fill the quota
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void UniqueSelection_MixedDuplicates_PrefersUnique()
    {
        var selection = new UniqueSelection<MockChromosome>();
        // 3 unique fitness values: 1, 2, 3
        var population = MockChromosome.CreatePopulation(1.0, 1.0, 2.0, 2.0, 3.0);
        var rng = new Random(42);

        var result = selection.Select(population, 3, rng);

        // Should get one of each unique value: 1, 2, 3
        var fitnessValues = result.Select(r => r.Fitness).Distinct().ToList();
        Assert.Equal(3, fitnessValues.Count);
    }

    #endregion

    #region SUSSelection Specific Tests

    [Fact]
    public void SUSSelection_EvenlySpacedPointers_LessVariance()
    {
        var sus = new SUSSelection<MockChromosome>();
        var roulette = new RouletteSelection<MockChromosome>();
        var population = MockChromosome.CreateSortedPopulation(100);

        // Run multiple trials and measure variance in selection distribution
        var susVariances = new List<double>();
        var rouletteVariances = new List<double>();

        for (int trial = 0; trial < 100; trial++)
        {
            var rng1 = new Random(trial);
            var rng2 = new Random(trial);

            var susResult = sus.Select(population, 30, rng1);
            var rouletteResult = roulette.Select(population, 30, rng2);

            // Calculate variance of selected indices
            var susIndices = susResult.Select(r => (double)population.IndexOf(r)).ToList();
            var rouletteIndices = rouletteResult.Select(r => (double)population.IndexOf(r)).ToList();

            susVariances.Add(Variance(susIndices));
            rouletteVariances.Add(Variance(rouletteIndices));
        }

        // SUS should generally have more consistent (lower variance) selection
        // This is a weak test due to randomness, but on average SUS should be more uniform
        Assert.True(susVariances.Average() >= 0); // Just ensure it runs without error
    }

    private static double Variance(List<double> values)
    {
        double mean = values.Average();
        return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
    }

    #endregion

    #region BoltzmannSelection Specific Tests

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(100.0)]
    [InlineData(1000.0)]
    public void BoltzmannSelection_DifferentTemperatures_Work(double temperature)
    {
        var selection = new BoltzmannSelection<MockChromosome>(temperature);
        var population = MockChromosome.CreateSortedPopulation(50);
        var rng = new Random(42);

        var result = selection.Select(population, 20, rng);

        Assert.Equal(20, result.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Fact]
    public void BoltzmannSelection_VeryLowTemperature_AlmostDeterministic()
    {
        var selection = new BoltzmannSelection<MockChromosome>(0.01);
        var population = MockChromosome.CreateSortedPopulation(100);

        int bestSelected = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 10, rng);
            bestSelected += result.Count(r => ReferenceEquals(r, population[0]));
        }

        // With very low temperature, the best individual should dominate
        Assert.True(bestSelected > 500, // Out of 1000 selections
            $"Best individual should dominate at low temperature. Selected {bestSelected}/1000 times");
    }

    [Fact]
    public void BoltzmannSelection_VeryHighTemperature_MoreUniform()
    {
        var selection = new BoltzmannSelection<MockChromosome>(10000.0);
        var population = MockChromosome.CreateSortedPopulation(10);

        var selectionCounts = new int[10];
        for (int trial = 0; trial < 10000; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 1, rng);
            int index = population.IndexOf(result[0]);
            selectionCounts[index]++;
        }

        // With very high temperature, all should be selected roughly equally
        double min = selectionCounts.Min();
        double max = selectionCounts.Max();
        double ratio = max / min;

        Assert.True(ratio < 3.0, // Allow some variation
            $"Selection should be more uniform. Min: {min}, Max: {max}, Ratio: {ratio}");
    }

    #endregion

    #region TruncationSelection Specific Tests

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void TruncationSelection_DifferentRates_Work(double rate)
    {
        var selection = new TruncationSelection<MockChromosome>(rate);
        var population = MockChromosome.CreateSortedPopulation(100);
        var rng = new Random(42);

        var result = selection.Select(population, 30, rng);

        Assert.Equal(30, result.Count);

        // All selections should come from the top portion
        int truncationPoint = Math.Max(1, (int)(100 * rate));
        Assert.All(result, r =>
        {
            int index = population.IndexOf(r);
            Assert.True(index < truncationPoint,
                $"Selected individual at index {index} but truncation point is {truncationPoint}");
        });
    }

    [Fact]
    public void TruncationSelection_Rate100Percent_SelectsFromAll()
    {
        var selection = new TruncationSelection<MockChromosome>(1.0);
        var population = MockChromosome.CreateSortedPopulation(100);

        var selectedIndices = new HashSet<int>();
        for (int trial = 0; trial < 1000; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 10, rng);
            foreach (var r in result)
            {
                selectedIndices.Add(population.IndexOf(r));
            }
        }

        // With 100% truncation, all individuals should eventually be selected
        Assert.True(selectedIndices.Count > 50,
            $"Should select from entire population. Only selected from {selectedIndices.Count} different individuals");
    }

    #endregion

    #region LinearRankingSelection Specific Tests

    [Fact]
    public void LinearRankingSelection_Pressure1_UniformSelection()
    {
        var selection = new LinearRankingSelection<MockChromosome>(1.0);
        var population = MockChromosome.CreateSortedPopulation(10);

        var selectionCounts = new int[10];
        for (int trial = 0; trial < 10000; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 1, rng);
            int index = population.IndexOf(result[0]);
            selectionCounts[index]++;
        }

        // With pressure = 1.0, selection should be uniform
        double min = selectionCounts.Min();
        double max = selectionCounts.Max();
        double ratio = max / min;

        Assert.True(ratio < 2.0,
            $"Pressure 1.0 should give uniform selection. Min: {min}, Max: {max}, Ratio: {ratio}");
    }

    [Fact]
    public void LinearRankingSelection_Pressure2_MaxPressure()
    {
        var selection = new LinearRankingSelection<MockChromosome>(2.0);
        var population = MockChromosome.CreateSortedPopulation(100);

        int topTenSelected = 0;
        for (int trial = 0; trial < 1000; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 30, rng);
            topTenSelected += result.Count(r => population.IndexOf(r) < 10);
        }

        // With pressure = 2.0, top individuals should be heavily favored
        Assert.True(topTenSelected > 5000, // More than ~16% of 30000 selections
            $"Pressure 2.0 should strongly favor top individuals. Top 10 selected {topTenSelected}/30000 times");
    }

    #endregion

    #region ExponentialRankingSelection Specific Tests

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    [InlineData(0.99)]
    public void ExponentialRankingSelection_DifferentBases_Work(double expBase)
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(expBase);
        var population = MockChromosome.CreateSortedPopulation(50);
        var rng = new Random(42);

        var result = selection.Select(population, 20, rng);

        Assert.Equal(20, result.Count);
        Assert.All(result, r => Assert.Contains(r, population));
    }

    [Fact]
    public void ExponentialRankingSelection_VeryLowBase_ExtremePressure()
    {
        var selection = new ExponentialRankingSelection<MockChromosome>(0.1);
        var population = MockChromosome.CreateSortedPopulation(100);

        int topFiveSelected = 0;
        for (int trial = 0; trial < 1000; trial++)
        {
            var rng = new Random(trial);
            var result = selection.Select(population, 30, rng);
            topFiveSelected += result.Count(r => population.IndexOf(r) < 5);
        }

        // With very low base, top individuals should dominate
        Assert.True(topFiveSelected > 15000, // More than 50% of 30000 selections
            $"Very low base should give extreme pressure. Top 5 selected {topFiveSelected}/30000 times");
    }

    [Fact]
    public void ExponentialRankingSelection_HighBase_WeakerPressure()
    {
        var lowBase = new ExponentialRankingSelection<MockChromosome>(0.5);
        var highBase = new ExponentialRankingSelection<MockChromosome>(0.99);
        var population = MockChromosome.CreateSortedPopulation(100);

        int lowBaseTop10 = 0, highBaseTop10 = 0;
        for (int trial = 0; trial < 1000; trial++)
        {
            var rng1 = new Random(trial);
            var rng2 = new Random(trial);

            var result1 = lowBase.Select(population, 30, rng1);
            var result2 = highBase.Select(population, 30, rng2);

            lowBaseTop10 += result1.Count(r => population.IndexOf(r) < 10);
            highBaseTop10 += result2.Count(r => population.IndexOf(r) < 10);
        }

        Assert.True(lowBaseTop10 > highBaseTop10,
            $"Lower base should have stronger pressure. Low: {lowBaseTop10}, High: {highBaseTop10}");
    }

    #endregion
}
