using RubikCube.Tests.Mocks;
using TGL.GA.Operators.Mutation;
using Xunit;

namespace RubikCube.Tests.GA.Mutation;

/// <summary>
/// Statistical tests for mutation operators.
/// Verifies distributions, randomness, and statistical properties.
/// </summary>
public class MutationStatisticalTests
{
    private const int TrialCount = 1000;

    #region Position Distribution Tests

    [Fact]
    public void SwapMutation_AllPositionsCanBeSelected()
    {
        var op = new SwapMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never selected for swapping");
        }
    }

    [Fact]
    public void InversionMutation_AllPositionsCanBeInSegment()
    {
        var op = new InversionMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never part of an inversion segment");
        }
    }

    [Fact]
    public void ScrambleMutation_AllPositionsCanBeInSegment()
    {
        var op = new ScrambleMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // All positions should be affected at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > 0,
                $"Position {i} was never part of a scramble segment");
        }
    }

    [Fact]
    public void RandomMutation_AllPositionsCanBeMutated()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 1);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // All positions should be mutated at least sometimes
        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > 0,
                $"Position {i} was never selected for random mutation");
        }
    }

    #endregion

    #region Uniformity Tests

    [Fact]
    public void SwapMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new SwapMutation<MockChromosome>();
        var positionChangeCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionChangeCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        // Expected: ~2000 total changes / 10 positions = ~200 per position
        // Allow for statistical variance (50% tolerance)
        double totalChanges = positionChangeCounts.Sum();
        double expectedPerPosition = totalChanges / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionChangeCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} selected too infrequently: {positionChangeCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    [Fact]
    public void RandomMutation_PositionSelectionIsReasonablyUniform()
    {
        var op = new RandomMutation<MockChromosome>(genesToMutate: 1);
        var positionMutationCounts = new int[10];

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    positionMutationCounts[i]++;
            }
        }

        // Each position should be selected with roughly equal probability
        // Expected: ~1000 total / 10 positions = ~100 per position
        double totalMutations = positionMutationCounts.Sum();
        double expectedPerPosition = totalMutations / 10;
        double tolerance = expectedPerPosition * 0.5;

        for (int i = 0; i < 10; i++)
        {
            Assert.True(positionMutationCounts[i] > expectedPerPosition - tolerance,
                $"Position {i} mutated too infrequently: {positionMutationCounts[i]} (expected ~{expectedPerPosition})");
        }
    }

    #endregion


    #region Segment Length Distribution Tests

    [Fact]
    public void InversionMutation_VariousSegmentLengths()
    {
        var op = new InversionMutation<MockChromosome>();
        var segmentLengths = new Dictionary<int, int>();

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find segment length (count of changed positions)
            int changedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            if (changedCount > 0)
            {
                if (!segmentLengths.ContainsKey(changedCount))
                    segmentLengths[changedCount] = 0;
                segmentLengths[changedCount]++;
            }
        }

        // Should produce various segment lengths
        Assert.True(segmentLengths.Count >= 3,
            $"Only {segmentLengths.Count} different segment lengths observed, expected more variety");
    }

    [Fact]
    public void ScrambleMutation_VariousSegmentLengths()
    {
        var op = new ScrambleMutation<MockChromosome>();
        var segmentLengths = new Dictionary<int, int>();

        for (int trial = 0; trial < TrialCount; trial++)
        {
            var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            var original = chromosome.Genes.ToArray();
            var rng = new Random(trial);

            op.Mutate(chromosome, rng);

            // Find segment length (count of changed positions)
            int changedCount = 0;
            for (int i = 0; i < 10; i++)
            {
                if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                    changedCount++;
            }

            if (changedCount > 0)
            {
                if (!segmentLengths.ContainsKey(changedCount))
                    segmentLengths[changedCount] = 0;
                segmentLengths[changedCount]++;
            }
        }

        // Should produce various segment lengths
        Assert.True(segmentLengths.Count >= 3,
            $"Only {segmentLengths.Count} different segment lengths observed, expected more variety");
    }

    #endregion

    #region Mutation Rate Consistency Tests

    [Fact]
    public void RandomMutation_GenesToMutateIsRespected()
    {
        for (int genesToMutate = 1; genesToMutate <= 5; genesToMutate++)
        {
            var op = new RandomMutation<MockChromosome>(genesToMutate: genesToMutate);
            var mutationCounts = new int[11]; // Index = number of mutations

            for (int trial = 0; trial < 500; trial++)
            {
                var chromosome = MockChromosome.WithGenes(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
                var original = chromosome.Genes.ToArray();
                var rng = new Random(trial);

                op.Mutate(chromosome, rng);

                int mutations = 0;
                for (int i = 0; i < 10; i++)
                {
                    if (Math.Abs(original[i] - chromosome.Genes[i]) > 0.001)
                        mutations++;
                }
                mutationCounts[mutations]++;
            }

            // Mutations should be at most genesToMutate
            for (int i = genesToMutate + 1; i < 11; i++)
            {
                Assert.Equal(0, mutationCounts[i]);
            }
        }
    }

    #endregion
}
